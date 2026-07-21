param(
    [string]$ApkPath = "Builds\Android\CatGuardTowerDefense-emulator.apk",
    [string]$PackageName = "com.catguard.towerdefense.qa",
    [string]$DeviceSerial = "",
    [string]$OutputDir = "Builds\Android\qa-device\e3-routes",
    [switch]$SkipInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$levelRunner = Join-Path $PSScriptRoot "run-emulator-level-qa.ps1"
$adbPath = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
$resolvedApkPath = if ([System.IO.Path]::IsPathRooted($ApkPath)) { $ApkPath } else { Join-Path $repoRoot $ApkPath }
$resolvedOutputDir = if ([System.IO.Path]::IsPathRooted($OutputDir)) { $OutputDir } else { Join-Path $repoRoot $OutputDir }

if (-not (Test-Path -LiteralPath $adbPath)) {
    throw "ADB not found: $adbPath"
}

if (-not (Test-Path -LiteralPath $levelRunner)) {
    throw "Level QA runner not found: $levelRunner"
}

if (-not $SkipInstall -and -not (Test-Path -LiteralPath $resolvedApkPath)) {
    throw "APK not found: $resolvedApkPath"
}

function Invoke-Adb {
    param([string[]]$Arguments, [switch]$AllowFailure)

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = @(& $adbPath @Arguments 2>&1 | ForEach-Object { $_.ToString() })
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw "ADB failed with exit code ${exitCode}: adb $($Arguments -join ' ')`n$($output -join [Environment]::NewLine)"
    }

    return [pscustomobject]@{ ExitCode = $exitCode; Output = [string[]]$output }
}

function Get-RunningEmulator {
    $lines = (Invoke-Adb -Arguments @("devices", "-l")).Output
    return [string](@($lines | Where-Object {
        $_ -match '^emulator-\d+\s+device\b' -or $_ -match '^\S+\s+device\b.*\bmodel:sdk_'
    } | ForEach-Object { ($_ -split '\s+')[0] } | Select-Object -First 1))
}

$script:TargetSerial = if ($DeviceSerial) { $DeviceSerial } else { Get-RunningEmulator }
if (-not $script:TargetSerial) {
    throw "No running Android emulator found."
}

if (-not $SkipInstall) {
    Invoke-Adb -Arguments @("-s", $script:TargetSerial, "install", "-r", "-t", $resolvedApkPath) | Out-Null
}

$script:RunDir = Join-Path $resolvedOutputDir (Get-Date -Format "yyyyMMdd-HHmmss")
New-Item -ItemType Directory -Force -Path $script:RunDir | Out-Null
$script:ScenarioSummaries = New-Object System.Collections.Generic.List[object]
$script:Assertions = [ordered]@{}

function Invoke-RouteScenario {
    param(
        [string]$ScenarioId,
        [string]$LevelId,
        [string]$ExpectedState,
        [string[]]$ExpectedRoutes,
        [string]$RouteFilter = "",
        [int]$StartingLives = 0,
        [switch]$NoTowers
    )

    $before = @(Get-ChildItem -LiteralPath $script:RunDir -Directory -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)
    $arguments = @{
        LevelId = $LevelId
        ScenarioId = $ScenarioId
        ApkPath = $resolvedApkPath
        PackageName = $PackageName
        DeviceSerial = $script:TargetSerial
        OutputDir = $script:RunDir
        SkipInstall = $true
        TimeoutSeconds = 240
        RouteIdFilter = $RouteFilter
        StartingLives = $StartingLives
        CombatSampleDelaySeconds = 2
    }
    if ($NoTowers) {
        $arguments.TowerIds = [string[]]@()
    }
    if ($ExpectedState -eq "won") {
        $arguments.RequireVictory = $true
    }

    & $levelRunner @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Route QA scenario failed: $ScenarioId"
    }

    $after = @(Get-ChildItem -LiteralPath $script:RunDir -Directory | Select-Object -ExpandProperty FullName)
    $scenarioDir = [string]($after | Where-Object { $_ -notin $before } | Select-Object -Last 1)
    if (-not $scenarioDir) {
        throw "Scenario output directory was not created: $ScenarioId"
    }

    $summaryPath = Join-Path $scenarioDir "qa-summary.json"
    $summary = Get-Content -Raw -Encoding UTF8 -LiteralPath $summaryPath | ConvertFrom-Json
    $script:ScenarioSummaries.Add($summary)

    $statePassed = $summary.result.state -eq $ExpectedState
    $script:Assertions["$ScenarioId-state"] = $statePassed
    $routeResults = @($summary.result.routes)
    foreach ($routeId in $ExpectedRoutes) {
        $route = @($routeResults | Where-Object { $_.routeId -eq $routeId } | Select-Object -First 1)
        $covered = $route.Count -eq 1 -and [int]$route[0].spawned -gt 0
        if ($ExpectedState -eq "won") {
            $covered = $covered -and ([int]$route[0].defeated + [int]$route[0].escaped) -eq [int]$route[0].spawned
        }
        else {
            $covered = $covered -and [int]$route[0].escaped -gt 0
        }

        $script:Assertions["$ScenarioId-route-$routeId"] = $covered
    }

    return $summary
}

Invoke-RouteScenario -ScenarioId "e3-control-victory" -LevelId "level_01" -ExpectedState "won" -ExpectedRoutes @("main") | Out-Null
Invoke-RouteScenario -ScenarioId "e3-two-lane-victory" -LevelId "level_08" -ExpectedState "won" -ExpectedRoutes @("north_lane", "south_lane", "well_boss") | Out-Null
Invoke-RouteScenario -ScenarioId "e3-fork-victory" -LevelId "level_07" -ExpectedState "won" -ExpectedRoutes @("west_branch", "moon_branch", "chimney_boss") | Out-Null

$defeatScenarios = @(
    @{ Id = "e3-main-defeat"; Level = "level_01"; Route = "main" },
    @{ Id = "e3-north-defeat"; Level = "level_08"; Route = "north_lane" },
    @{ Id = "e3-south-defeat"; Level = "level_08"; Route = "south_lane" },
    @{ Id = "e3-well-boss-defeat"; Level = "level_08"; Route = "well_boss" },
    @{ Id = "e3-west-branch-defeat"; Level = "level_07"; Route = "west_branch" },
    @{ Id = "e3-moon-branch-defeat"; Level = "level_07"; Route = "moon_branch" },
    @{ Id = "e3-chimney-boss-defeat"; Level = "level_07"; Route = "chimney_boss" }
)

foreach ($scenario in $defeatScenarios) {
    Invoke-RouteScenario `
        -ScenarioId $scenario.Id `
        -LevelId $scenario.Level `
        -ExpectedState "lost" `
        -ExpectedRoutes @($scenario.Route) `
        -RouteFilter $scenario.Route `
        -StartingLives 1 `
        -NoTowers | Out-Null
}

$persistentSavePath = "/sdcard/Android/data/$PackageName/files/catguard-save.json"
$saveLines = (Invoke-Adb -Arguments @(
    "-s", $script:TargetSerial,
    "shell", "cat", $persistentSavePath)).Output
$savePath = Join-Path $script:RunDir "save-after-restarts.json"
$saveLines | Set-Content -LiteralPath $savePath -Encoding UTF8
$save = ($saveLines -join [Environment]::NewLine) | ConvertFrom-Json
$script:Assertions["save-persists-between-level-restarts"] = @($save.unlockedLevelIds) -contains "level_01" `
    -and @($save.unlockedLevelIds) -contains "level_07" `
    -and @($save.unlockedLevelIds) -contains "level_08" `
    -and @($save.completedLevelIds) -contains "level_01" `
    -and @($save.completedLevelIds) -contains "level_07" `
    -and @($save.completedLevelIds) -contains "level_08"

$twoLane = @($script:ScenarioSummaries | Where-Object { $_.scenarioId -eq "e3-two-lane-victory" } | Select-Object -First 1)
$twoLaneRoutes = @($twoLane[0].result.routes | Where-Object { $_.spawned -gt 0 })
$laneStarts = @($twoLaneRoutes | Where-Object { $_.routeId -in @("north_lane", "south_lane") } | ForEach-Object { [double]$_.firstSpawnTime })
$script:Assertions["two-lane-simultaneous-route-data"] = $twoLaneRoutes.Count -ge 3 `
    -and $laneStarts.Count -eq 2 `
    -and [Math]::Abs($laneStarts[0] - $laneStarts[1]) -le 1.5

$fatalTotal = 0
foreach ($scenario in $script:ScenarioSummaries) {
    $fatalTotal += [int]$scenario.fatalPatternCount
}
$failedAssertions = @($script:Assertions.GetEnumerator() | Where-Object { -not $_.Value } | ForEach-Object { $_.Key })
$summary = [ordered]@{
    timestampUtc = (Get-Date).ToUniversalTime().ToString("o")
    deviceSerial = $script:TargetSerial
    assertions = $script:Assertions
    failedAssertions = $failedAssertions
    scenarioCount = $script:ScenarioSummaries.Count
    scenarios = $script:ScenarioSummaries
    savePath = $savePath
    fatalPatternCount = $fatalTotal
    passed = $failedAssertions.Count -eq 0 -and $fatalTotal -eq 0
    runDir = $script:RunDir
}

$summaryPath = Join-Path $script:RunDir "qa-summary.json"
$summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $summaryPath -Encoding UTF8
Write-Host "E3 route QA summary: $summaryPath"
Write-Host "Failed assertions: $($failedAssertions -join ', ')"
Write-Host "Fatal errors: $fatalTotal"

if (-not $summary.passed) {
    exit 1
}

exit 0
