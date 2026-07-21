param(
    [string]$ApkPath = "Builds\Android\CatGuardTowerDefense-emulator.apk",
    [string]$PackageName = "com.catguard.towerdefense.qa",
    [string]$DeviceSerial = "",
    [string]$OutputDir = "Builds\Android\qa-device\e7-ultimates",
    [int]$TimeoutSeconds = 240,
    [switch]$SkipInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$runner = Join-Path $PSScriptRoot "run-emulator-level-qa.ps1"
$ultimates = @("yarn_meteor_shower", "catnip_moon", "nine_lives_ward")
$towers = @("cat_dart", "yarn_cannon", "bell_sniper", "laser_pointer", "blanket_boom")
$branches = @("dart_rapid", "yarn_impact", "bell_marksman", "laser_chain", "blanket_blast")
$scenarios = @(
    [pscustomobject]@{
        Id = "e7-fixed-multi-route"
        LevelId = "level_10"
        Towers = $towers
        Lives = 0
        Victory = $true
        WardBlock = $false
        Performance = $true
        Ultimates = $ultimates
        Branches = $branches
    },
    [pscustomobject]@{
        Id = "e7-scroll-multi-route"
        LevelId = "level_08"
        Towers = $towers
        Lives = 0
        Victory = $true
        WardBlock = $false
        Performance = $false
        Ultimates = $ultimates
        Branches = $branches
    },
    [pscustomobject]@{
        Id = "e7-ward-last-life-edge"
        LevelId = "level_10"
        Towers = @()
        Lives = 1
        Victory = $false
        WardBlock = $true
        Performance = $false
        Ultimates = @("nine_lives_ward")
        Branches = @()
    }
)

$first = $true
foreach ($scenario in $scenarios) {
    $arguments = @{
        LevelId = $scenario.LevelId
        TowerIds = $scenario.Towers
        ScenarioId = $scenario.Id
        StartingLives = $scenario.Lives
        StartingBattleFish = 3200
        UpgradeBranchIds = $scenario.Branches
        UpgradeTargetTier = if ($scenario.Branches.Count -gt 0) { 3 } else { 0 }
        UltimateIds = $scenario.Ultimates
        ExerciseUltimateTargeting = [bool]($scenario.Ultimates -contains "yarn_meteor_shower")
        ApkPath = $ApkPath
        PackageName = $PackageName
        DeviceSerial = $DeviceSerial
        OutputDir = $OutputDir
        TimeoutSeconds = $TimeoutSeconds
        MinimumAverageFps = 27.5
        MaximumP95FrameTimeMs = 56
        RequireUltimates = $true
        SkipInstall = [bool]($SkipInstall -or -not $first)
    }

    if ($scenario.Victory) {
        $arguments.RequireVictory = $true
    }

    if ($scenario.WardBlock) {
        $arguments.RequireWardBlock = $true
    }

    if ($scenario.Performance) {
        $arguments.RequirePerformance = $true
    }

    & $runner @arguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $first = $false
}

Write-Host "E7 ultimate QA passed: all three abilities ran on fixed and scrollable multi-route maps; target cancel/invalid, analytics, pooled VFX, performance, save isolation, and last-life ward protection were verified."
exit 0
