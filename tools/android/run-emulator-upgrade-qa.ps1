param(
    [string]$ApkPath = "Builds\Android\CatGuardTowerDefense-emulator.apk",
    [string]$PackageName = "com.catguard.towerdefense.qa",
    [string]$DeviceSerial = "",
    [string]$OutputDir = "Builds\Android\qa-device\e6-upgrade-strategies",
    [int]$TimeoutSeconds = 210,
    [switch]$SkipInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$runner = Join-Path $PSScriptRoot "run-emulator-level-qa.ps1"
$towers = @("cat_dart", "yarn_cannon", "bell_sniper", "laser_pointer", "blanket_boom")
$strategies = @(
    [pscustomobject]@{
        Id = "e6-branch-a-high-load"
        Branches = @("dart_rapid", "yarn_impact", "bell_marksman", "laser_chain", "blanket_blast")
        Priority = "Strong"
        Sell = $false
    },
    [pscustomobject]@{
        Id = "e6-branch-b-sell"
        Branches = @("dart_precision", "yarn_snare", "bell_resonance", "laser_focus", "blanket_burn")
        Priority = "Last"
        Sell = $true
    }
)

$first = $true
foreach ($strategy in $strategies) {
    $arguments = @{
        LevelId = "level_10"
        TowerIds = $towers
        ScenarioId = $strategy.Id
        StartingBattleFish = 3200
        UpgradeBranchIds = $strategy.Branches
        UpgradeTargetTier = 3
        TargetPriority = $strategy.Priority
        ApkPath = $ApkPath
        PackageName = $PackageName
        DeviceSerial = $DeviceSerial
        OutputDir = $OutputDir
        TimeoutSeconds = $TimeoutSeconds
        MinimumAverageFps = 28.1
        MaximumP95FrameTimeMs = 54
        RequireVictory = $true
        RequirePerformance = $true
        RequireBattleUpgrades = $true
        SkipInstall = [bool]($SkipInstall -or -not $first)
    }

    if ($strategy.Sell) {
        $arguments.SellAfterUpgrade = $true
    }

    & $runner @arguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $first = $false
}

Write-Host "E6 upgrade QA passed: both branch strategies won level_10; sell, target priority, save isolation, analytics and performance checks completed."
exit 0
