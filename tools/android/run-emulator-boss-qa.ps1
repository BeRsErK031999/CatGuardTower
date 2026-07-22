param(
    [string]$ApkPath = "Builds\Android\CatGuardTowerDefense-emulator.apk",
    [string]$PackageName = "com.catguard.towerdefense.qa",
    [string]$DeviceSerial = "",
    [string]$OutputDir = "Builds\Android\qa-device\e13-bosses",
    [int]$TimeoutSeconds = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$levelRunner = Join-Path $PSScriptRoot "run-emulator-level-qa.ps1"
$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputDir)) { $OutputDir } else { Join-Path $repoRoot $OutputDir }
New-Item -ItemType Directory -Force -Path $resolvedOutput | Out-Null

$towerIds = @(
    "cat_dart", "yarn_cannon", "bell_sniper", "laser_pointer", "blanket_boom", "bell_sniper",
    "laser_pointer", "blanket_boom", "yarn_cannon", "bell_sniper", "laser_pointer", "blanket_boom"
)
$branches = @(
    "dart_precision", "yarn_snare", "bell_marksman", "laser_focus", "blanket_burn", "bell_marksman",
    "laser_focus", "blanket_burn", "yarn_snare", "bell_marksman", "laser_focus", "blanket_burn"
)
$ultimates = @("yarn_meteor_shower", "catnip_moon", "nine_lives_ward")
$scenarios = @(
    [pscustomobject]@{ Id = "e13-captain-all-ultimates"; Level = "level_04"; Language = "ru"; Phases = 2; TargetFrameRate = 0; Interrupt = ""; ExpectedState = "won"; RequireBoss = $true },
    [pscustomobject]@{ Id = "e13-owl-all-ultimates"; Level = "level_08"; Language = "en"; Phases = 2; TargetFrameRate = 0; Interrupt = ""; ExpectedState = "won"; RequireBoss = $true },
    [pscustomobject]@{ Id = "e13-king-low-fps"; Level = "level_12"; Language = "ru"; Phases = 3; TargetFrameRate = 12; Interrupt = ""; ExpectedState = "won"; RequireBoss = $true },
    [pscustomobject]@{ Id = "e13-king-transition-restart"; Level = "level_12"; Language = "en"; Phases = 1; TargetFrameRate = 0; Interrupt = "restart"; ExpectedState = "interrupted_restart"; RequireBoss = $false },
    [pscustomobject]@{ Id = "e13-owl-transition-quit"; Level = "level_08"; Language = "ru"; Phases = 1; TargetFrameRate = 0; Interrupt = "quit"; ExpectedState = "interrupted_quit"; RequireBoss = $false },
    [pscustomobject]@{ Id = "e13-king-repeat-cleanup"; Level = "level_12"; Language = "en"; Phases = 3; TargetFrameRate = 0; Interrupt = ""; ExpectedState = "won"; RequireBoss = $true }
)

$manifestEntries = New-Object System.Collections.Generic.List[object]
$installed = $false
foreach ($scenario in $scenarios) {
    $parameters = @{
        LevelId = $scenario.Level
        ScenarioId = $scenario.Id
        LanguageCode = $scenario.Language
        TowerIds = $towerIds
        StartingLives = 999
        StartingBattleFish = 9999
        UpgradeBranchIds = $branches
        UpgradeTargetTier = 2
        TargetPriority = "Strong"
        UltimateIds = $ultimates
        ExerciseUltimateTargeting = $true
        WriteLiveSnapshots = $true
        ApkPath = $ApkPath
        PackageName = $PackageName
        DeviceSerial = $DeviceSerial
        OutputDir = $resolvedOutput
        TimeoutSeconds = $TimeoutSeconds
        CombatSampleDelaySeconds = 10
        ExpectedState = $scenario.ExpectedState
        ExpectedBossPhases = $scenario.Phases
    }
    if ($installed) {
        $parameters.SkipInstall = $true
    }
    if ($scenario.TargetFrameRate -gt 0) {
        $parameters.TargetFrameRate = $scenario.TargetFrameRate
    }
    if ($scenario.Interrupt) {
        $parameters.InterruptAtBossPhase = 1
        $parameters.InterruptAction = $scenario.Interrupt
        $parameters.RequireBossCleanup = $true
    }
    else {
        $parameters.RequireVictory = $true
        $parameters.RequireBoss = $true
        $parameters.RequireMapRules = $true
        $parameters.RequireUltimates = $true
        $parameters.RequireBattleUpgrades = $true
    }

    Write-Host "E13 scenario: $($scenario.Id)"
    & $levelRunner @parameters
    if ($LASTEXITCODE -ne 0) {
        throw "E13 scenario '$($scenario.Id)' failed with exit code $LASTEXITCODE."
    }
    $installed = $true

    $run = Get-ChildItem -LiteralPath $resolvedOutput -Directory |
        Where-Object { $_.Name -like "*-$($scenario.Id)" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($null -eq $run) {
        throw "E13 scenario output is missing for '$($scenario.Id)'."
    }
    $summaryPath = Join-Path $run.FullName "qa-summary.json"
    $summary = Get-Content -LiteralPath $summaryPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $manifestEntries.Add([pscustomobject]@{
        scenarioId = $scenario.Id
        levelId = $scenario.Level
        languageCode = $scenario.Language
        expectedState = $scenario.ExpectedState
        expectedBossPhases = $scenario.Phases
        targetFrameRate = $scenario.TargetFrameRate
        state = $summary.result.state
        bossId = $summary.result.bossId
        enteredBossPhaseIds = $summary.result.enteredBossPhaseIds
        bossAbilityExecutions = $summary.result.bossAbilityExecutions
        bossResistanceFeedback = $summary.result.bossResistanceFeedback
        mapRuleActivations = $summary.result.mapRuleActivations
        mapRuleDeactivations = $summary.result.mapRuleDeactivations
        cleanupComplete = $summary.result.battleRuntimeCleanupComplete
        ultimateUses = $summary.result.ultimateUses
        fatalPatternCount = $summary.fatalPatternCount
        landscapeConfirmed = $summary.landscapeConfirmed
        performance = $summary.performance
        runDir = $run.FullName
    })
}

$manifest = [pscustomobject]@{
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    scenarioCount = $manifestEntries.Count
    allPassed = @($manifestEntries | Where-Object {
        $_.state -ne $_.expectedState `
            -or -not $_.cleanupComplete `
            -or $_.fatalPatternCount -gt 0 `
            -or -not $_.landscapeConfirmed
    }).Count -eq 0
    scenarios = $manifestEntries.ToArray()
}
$manifestPath = Join-Path $resolvedOutput "boss-qa-manifest.json"
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Host "E13 boss QA complete: $($manifestEntries.Count)/$($scenarios.Count) scenarios passed."
Write-Host "Manifest: $manifestPath"
if (-not $manifest.allPassed) {
    exit 1
}

exit 0
