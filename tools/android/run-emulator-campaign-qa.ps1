param(
    [string]$ApkPath = "Builds\Android\CatGuardTowerDefense-emulator.apk",
    [string]$PackageName = "com.catguard.towerdefense.qa",
    [string]$DeviceSerial = "",
    [string]$OutputDir = "Builds\Android\qa-device\e12-campaign",
    [int]$TimeoutSeconds = 240,
    [double]$MinimumAverageFps = 15,
    [double]$MaximumP95FrameTimeMs = 80,
    [switch]$ExpansionLoadout
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$levelScript = Join-Path $PSScriptRoot "run-emulator-level-qa.ps1"
$resolvedApk = if ([System.IO.Path]::IsPathRooted($ApkPath)) { $ApkPath } else { Join-Path $repoRoot $ApkPath }
$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputDir)) { $OutputDir } else { Join-Path $repoRoot $OutputDir }
$adbPath = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"

if (-not (Test-Path -LiteralPath $resolvedApk)) {
    throw "E12 APK не найден: $resolvedApk"
}

if (-not (Test-Path -LiteralPath $adbPath)) {
    throw "ADB не найден: $adbPath"
}

if (-not $DeviceSerial) {
    $DeviceSerial = [string](@(& $adbPath devices | Where-Object { $_ -match '^emulator-\d+\s+device' } | ForEach-Object { ($_ -split '\s+')[0] } | Select-Object -First 1))
}

if (-not $DeviceSerial) {
    throw "Запущенный Android-эмулятор не найден."
}

New-Item -ItemType Directory -Force -Path $resolvedOutput | Out-Null
& $adbPath -s $DeviceSerial install -r -t $resolvedApk | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Не удалось установить E12 Development APK."
}

& $adbPath -s $DeviceSerial shell pm clear $PackageName | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Не удалось очистить изолированное QA-приложение $PackageName."
}

$towerIds = @(
    "cat_dart", "yarn_cannon", "bell_sniper", "laser_pointer", "blanket_boom",
    "cat_dart", "yarn_cannon", "bell_sniper", "laser_pointer", "blanket_boom",
    "cat_dart", "yarn_cannon"
)
$expansionBranches = @(
    "dart_precision", "yarn_snare", "bell_marksman", "laser_focus", "blanket_burn",
    "dart_precision", "yarn_snare", "bell_marksman", "laser_focus", "blanket_burn",
    "dart_precision", "yarn_snare"
)
$expansionUltimates = @("yarn_meteor_shower", "catnip_moon")
$runs = New-Object System.Collections.Generic.List[object]
for ($index = 1; $index -le 12; $index++) {
    $levelId = "level_{0:d2}" -f $index
    $challengeId = "challenge_level_{0:d2}" -f $index
    foreach ($mode in @("normal", "challenge")) {
        $isHeaviest = $index -eq 12 -and $mode -eq "challenge"
        $isFinale = $index -eq 12
        $isEasiest = $index -eq 1 -and $mode -eq "normal"
        $scenarioId = "e12-$levelId-$mode"
        $combatSampleDelay = if ($ExpansionLoadout) { 3 } else { 10 }
        $arguments = @{
            LevelId = $levelId
            ScenarioId = $scenarioId
            TowerIds = $towerIds
            StartingLives = 50
            StartingBattleFish = 3000
            ApkPath = $resolvedApk
            PackageName = $PackageName
            DeviceSerial = $DeviceSerial
            OutputDir = $resolvedOutput
            TimeoutSeconds = $TimeoutSeconds
            CombatSampleDelaySeconds = $combatSampleDelay
            MinimumAverageFps = $MinimumAverageFps
            MaximumP95FrameTimeMs = $MaximumP95FrameTimeMs
            SkipInstall = $true
            RequireVictory = $true
            RequirePerformance = [bool]($isEasiest -or $isHeaviest)
        }
        if ($ExpansionLoadout) {
            $arguments.StartingLives = 999
            $arguments.StartingBattleFish = 9999
            $arguments.UpgradeBranchIds = $expansionBranches
            $arguments.UpgradeTargetTier = 2
            $arguments.TargetPriority = "Strong"
            $arguments.UltimateIds = $expansionUltimates
            $arguments.ExerciseUltimateTargeting = $true
            $arguments.PreferredBattleSpeed = 2
            $arguments.RequireBattleUpgrades = $isFinale
            $arguments.RequireUltimates = $isFinale
        }
        if ($mode -eq "challenge") {
            $arguments.ChallengeId = $challengeId
        }

        & $levelScript @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "E12 campaign QA failed: $scenarioId"
        }

        $runs.Add([pscustomobject]@{
            scenarioId = $scenarioId
            levelId = $levelId
            challengeId = if ($mode -eq "challenge") { $challengeId } else { "" }
            performanceRequired = [bool]($isEasiest -or $isHeaviest)
        })
    }
}

$manifest = [pscustomobject]@{
    timestampUtc = (Get-Date).ToUniversalTime().ToString("o")
    packageName = $PackageName
    deviceSerial = $DeviceSerial
    runCount = $runs.Count
    normalCount = @($runs | Where-Object { -not $_.challengeId }).Count
    challengeCount = @($runs | Where-Object { $_.challengeId }).Count
    biomes = @("backyard_dawn", "moonlit_rooftops", "pantry_underpass")
    runs = $runs
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $resolvedOutput "campaign-qa-manifest.json") -Encoding UTF8
Write-Host "E12 campaign QA passed: 12 normal + 12 challenge traversals; easiest and level_12 challenge performance gates passed."
