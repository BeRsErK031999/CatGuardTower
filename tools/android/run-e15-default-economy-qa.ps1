param(
    [string]$ApkPath = "Builds\Android\CatGuardTowerDefense-emulator.apk",
    [string]$PackageName = "com.catguard.towerdefense.qa",
    [string]$DeviceSerial = "",
    [string]$OutputDir = "Builds\Android\qa-device\e15-default-economy",
    [int]$TimeoutSeconds = 240
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$expectedQaPackage = "com.catguard.towerdefense.qa"
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$levelRunner = Join-Path $PSScriptRoot "run-emulator-level-qa.ps1"
$adbPath = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
$resolvedApk = if ([IO.Path]::IsPathRooted($ApkPath)) {
    [IO.Path]::GetFullPath($ApkPath)
}
else {
    [IO.Path]::GetFullPath((Join-Path $repoRoot $ApkPath))
}
$resolvedOutput = if ([IO.Path]::IsPathRooted($OutputDir)) {
    [IO.Path]::GetFullPath($OutputDir)
}
else {
    [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDir))
}

if ($PackageName -ne $expectedQaPackage) {
    throw "E15 default-economy QA may reset only the isolated QA package '$expectedQaPackage'."
}
if (-not (Test-Path -LiteralPath $resolvedApk -PathType Leaf)) {
    throw "E15 Development APK not found: $resolvedApk"
}
if (-not (Test-Path -LiteralPath $levelRunner -PathType Leaf)) {
    throw "Level QA runner not found: $levelRunner"
}
if (-not (Test-Path -LiteralPath $adbPath -PathType Leaf)) {
    throw "ADB not found: $adbPath"
}

& $adbPath start-server | Out-Null
if (-not $DeviceSerial) {
    $DeviceSerial = [string](@(& $adbPath devices | Where-Object {
        $_ -match '^emulator-\d+\s+device'
    } | ForEach-Object {
        ($_ -split '\s+')[0]
    } | Select-Object -First 1))
}
if (-not $DeviceSerial) {
    throw "A running Android emulator is required for the E15 default-economy companion gate."
}

$isEmulator = ((& $adbPath -s $DeviceSerial shell getprop ro.kernel.qemu) -join "").Trim() -eq "1"
if (-not $isEmulator) {
    throw "E15 default-economy companion QA is development-only and must not reset a physical device: $DeviceSerial"
}

New-Item -ItemType Directory -Force -Path $resolvedOutput | Out-Null
& $adbPath -s $DeviceSerial install -r -t $resolvedApk | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Failed to install the E15 Development APK."
}

# Remove progression bonuses and prior QA state without touching the store package or a physical device.
& $adbPath -s $DeviceSerial shell pm clear $expectedQaPackage | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Failed to clear the isolated QA package '$expectedQaPackage'."
}

$scenarioId = "e15-level12-default-economy"
$towerIds = @(
    "bell_sniper", "bell_sniper", "bell_sniper", "bell_sniper",
    "bell_sniper", "bell_sniper", "bell_sniper", "bell_sniper",
    "bell_sniper", "bell_sniper", "bell_sniper", "bell_sniper"
)
$parameters = @{
    LevelId = "level_12"
    ScenarioId = $scenarioId
    TowerIds = $towerIds
    StartingLives = 0
    StartingBattleFish = 0
    TargetPriority = "Strong"
    UltimateIds = @("yarn_meteor_shower", "catnip_moon", "nine_lives_ward")
    WriteLiveSnapshots = $true
    ApkPath = $resolvedApk
    PackageName = $expectedQaPackage
    DeviceSerial = $DeviceSerial
    OutputDir = $resolvedOutput
    TimeoutSeconds = $TimeoutSeconds
    CombatSampleDelaySeconds = 10
    SkipInstall = $true
    RequireVictory = $true
    RequireBoss = $true
    ExpectedBossPhases = 3
    RequireMapRules = $true
    ReturnInsteadOfExit = $true
}

Write-Host "E15 default-economy scenario: level_12 with configured lives/Fish and earned reinvestment only."
& $levelRunner @parameters

$run = Get-ChildItem -LiteralPath $resolvedOutput -Directory |
    Where-Object { $_.Name -like "*-$scenarioId" } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if ($null -eq $run) {
    throw "E15 default-economy scenario output is missing."
}

$summaryPath = Join-Path $run.FullName "qa-summary.json"
if (-not (Test-Path -LiteralPath $summaryPath -PathType Leaf)) {
    throw "E15 default-economy summary is missing: $summaryPath"
}

$summary = Get-Content -LiteralPath $summaryPath -Encoding UTF8 -Raw | ConvertFrom-Json
$gitHead = (& git -C $repoRoot rev-parse HEAD).Trim()
$gitBranch = (& git -C $repoRoot branch --show-current).Trim()
$bossPassed = $summary.result.bossId `
    -and $summary.result.defeatedBossIds -contains $summary.result.bossId `
    -and $summary.result.enteredBossPhaseIds.Count -ge 3 `
    -and $summary.result.bossAbilityExecutions -ge 3 `
    -and $summary.result.bossResistanceFeedback -ge 1 `
    -and [bool]$summary.result.battleRuntimeCleanupComplete
$mapRulesPassed = $summary.result.mapRuleActivations -ge 1 `
    -and $summary.result.mapRuleDeactivations -ge 1 `
    -and [bool]$summary.result.battleRuntimeCleanupComplete
$passed = $summary.result.state -eq "won" `
    -and [int]$summary.fatalPatternCount -eq 0 `
    -and [bool]$summary.landscapeConfirmed `
    -and $bossPassed `
    -and $mapRulesPassed
$manifest = [pscustomobject]@{
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    passed = $passed
    developmentCompanionOnly = $true
    humanFairnessEvidence = $false
    gitBranch = $gitBranch
    gitHead = $gitHead
    apkPath = $resolvedApk
    apkSha256 = (Get-FileHash -LiteralPath $resolvedApk -Algorithm SHA256).Hash
    packageName = $expectedQaPackage
    deviceSerial = $DeviceSerial
    levelId = "level_12"
    startingLivesOverride = 0
    startingBattleFishOverride = 0
    strategy = "bell_sniper_reinvestment_with_guardian_ultimates"
    bossContractPassed = [bool]$bossPassed
    mapRulesContractPassed = [bool]$mapRulesPassed
    runDir = $run.FullName
    result = $summary.result
}
$manifestPath = Join-Path $resolvedOutput "e15-default-economy-manifest.json"
$manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Host "E15 default-economy victory: $passed"
Write-Host "Manifest: $manifestPath"
exit $(if ($passed) { 0 } else { 1 })
