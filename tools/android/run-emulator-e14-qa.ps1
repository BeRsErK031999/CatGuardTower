param(
    [string]$ApkPath = "Builds\Android\CatGuardTowerDefense-emulator.apk",
    [string]$PackageName = "com.catguard.towerdefense.qa",
    [string]$DeviceSerial = "",
    [string]$OutputDir = "Builds\Android\qa-device\e14-quality",
    [int]$TimeoutSeconds = 300,
    [switch]$SkipCampaignRegression
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$levelRunner = Join-Path $PSScriptRoot "run-emulator-level-qa.ps1"
$campaignRunner = Join-Path $PSScriptRoot "run-emulator-campaign-qa.ps1"
$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputDir)) { $OutputDir } else { Join-Path $repoRoot $OutputDir }
$adbPath = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
New-Item -ItemType Directory -Force -Path $resolvedOutput | Out-Null

if (-not (Test-Path -LiteralPath $adbPath)) {
    throw "ADB not found: $adbPath"
}

if (-not $DeviceSerial) {
    $DeviceSerial = [string](@(& $adbPath devices | Where-Object {
        $_ -match '^emulator-\d+\s+device'
    } | ForEach-Object {
        ($_ -split '\s+')[0]
    } | Select-Object -First 1))
}

if (-not $DeviceSerial) {
    throw "A running Android emulator is required for E14 QA."
}

$installed = $false
if (-not $SkipCampaignRegression) {
    & $campaignRunner `
        -ApkPath $ApkPath `
        -PackageName $PackageName `
        -DeviceSerial $DeviceSerial `
        -OutputDir (Join-Path $resolvedOutput "campaign") `
        -TimeoutSeconds $TimeoutSeconds `
        -MinimumAverageFps 15 `
        -MaximumP95FrameTimeMs 80 `
        -ExpansionLoadout
    if ($LASTEXITCODE -ne 0) {
        throw "E14 full landscape campaign regression failed."
    }
    $installed = $true
}

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
    [pscustomobject]@{ Id = "worst_case"; Level = "level_12"; Language = "ru"; Kind = "worst_case" },
    [pscustomobject]@{ Id = "settings_persistence"; Level = "level_04"; Language = "en"; Kind = "settings_persistence" },
    [pscustomobject]@{ Id = "soak_1"; Level = "level_12"; Language = "ru"; Kind = "soak" },
    [pscustomobject]@{ Id = "soak_2"; Level = "level_12"; Language = "en"; Kind = "soak" },
    [pscustomobject]@{ Id = "soak_3"; Level = "level_12"; Language = "ru"; Kind = "soak" },
    [pscustomobject]@{ Id = "offline"; Level = "level_01"; Language = "en"; Kind = "offline" }
)

$entries = New-Object System.Collections.Generic.List[object]
foreach ($scenario in $scenarios) {
    $parameters = @{
        LevelId = $scenario.Level
        ScenarioId = "e14-$($scenario.Id)"
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
        RequireVictory = $true
        RequireBattleUpgrades = $true
        RequireUltimates = $true
        RequirePooling = $true
    }

    if ($installed) {
        $parameters.SkipInstall = $true
    }

    if ($scenario.Kind -eq "worst_case") {
        $parameters.RequireBoss = $true
        $parameters.ExpectedBossPhases = 3
        $parameters.RequireMapRules = $true
        $parameters.RequirePerformance = $true
        $parameters.MinimumAverageFps = 24
        $parameters.MaximumP95FrameTimeMs = 70
        $parameters.CameraShakeLevel = 0
        $parameters.ReducedFlashMode = 1
        $parameters.TextScalePercent = 120
        $parameters.PreferredBattleSpeed = 2
        $parameters.ExerciseTimeControls = $true
        $parameters.RequireSettings = $true
        $parameters.RequireTimeControls = $true
        $parameters.ExpectedCameraShakeLevel = 0
        $parameters.ExpectedReducedFlashMode = 1
        $parameters.ExpectedTextScalePercent = 120
        $parameters.ExpectedPreferredBattleSpeed = 2
    }
    elseif ($scenario.Kind -eq "settings_persistence") {
        $parameters.RequireBoss = $true
        $parameters.ExpectedBossPhases = 2
        $parameters.RequireMapRules = $true
        $parameters.RequireSettings = $true
        $parameters.ExpectedCameraShakeLevel = 0
        $parameters.ExpectedReducedFlashMode = 1
        $parameters.ExpectedTextScalePercent = 120
        $parameters.ExpectedPreferredBattleSpeed = 2
    }

    $offline = $scenario.Kind -eq "offline"
    try {
        if ($offline) {
            & $adbPath -s $DeviceSerial shell svc wifi disable | Out-Null
            & $adbPath -s $DeviceSerial shell svc data disable | Out-Null
        }

        Write-Host "E14 scenario: $($scenario.Id)"
        & $levelRunner @parameters
        if ($LASTEXITCODE -ne 0) {
            throw "E14 scenario '$($scenario.Id)' failed with exit code $LASTEXITCODE."
        }
        $installed = $true
    }
    finally {
        if ($offline) {
            & $adbPath -s $DeviceSerial shell svc wifi enable | Out-Null
            & $adbPath -s $DeviceSerial shell svc data enable | Out-Null
        }
    }

    $run = Get-ChildItem -LiteralPath $resolvedOutput -Directory |
        Where-Object { $_.Name -like "*-e14-$($scenario.Id)" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($null -eq $run) {
        throw "E14 scenario output is missing for '$($scenario.Id)'."
    }

    $summary = Get-Content -LiteralPath (Join-Path $run.FullName "qa-summary.json") -Raw -Encoding UTF8 |
        ConvertFrom-Json
    $entries.Add([pscustomobject]@{
        scenarioId = $scenario.Id
        kind = $scenario.Kind
        levelId = $scenario.Level
        languageCode = $scenario.Language
        state = $summary.result.state
        landscapeConfirmed = $summary.landscapeConfirmed
        fatalPatternCount = $summary.fatalPatternCount
        performance = $summary.performance
        cameraShakeLevel = $summary.result.cameraShakeLevel
        reducedFlash = $summary.result.reducedFlash
        textScalePercent = $summary.result.textScalePercent
        preferredBattleSpeed = $summary.result.preferredBattleSpeed
        timeControlsVerified = $summary.result.timeControlsVerified
        enemyPoolCreated = $summary.result.enemyPoolCreated
        enemyPoolReused = $summary.result.enemyPoolReused
        enemyPoolPeakActive = $summary.result.enemyPoolPeakActive
        simpleVfxCreated = $summary.result.simpleVfxCreated
        simpleVfxReused = $summary.result.simpleVfxReused
        simpleVfxDropped = $summary.result.simpleVfxDropped
        cachedAudioClips = $summary.result.cachedAudioClips
        cleanupComplete = $summary.result.battleRuntimeCleanupComplete
        selectedUpgradeBranches = $summary.result.selectedUpgradeBranches
        runDir = $run.FullName
    })
}

$pickRates = @($branches | Group-Object | ForEach-Object {
    [pscustomobject]@{
        branchId = $_.Name
        selections = $_.Count * $scenarios.Count
        controlledShare = [Math]::Round($_.Count / [double]$branches.Count, 4)
    }
})
$manifest = [pscustomobject]@{
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    campaignRegressionIncluded = -not [bool]$SkipCampaignRegression
    worstCaseLevelId = "level_12"
    lowEndFloorFps = 24
    maximumP95FrameTimeMs = 70
    scenarioCount = $entries.Count
    allPassed = @($entries | Where-Object {
        $_.state -ne "won" `
            -or -not $_.landscapeConfirmed `
            -or $_.fatalPatternCount -gt 0 `
            -or $_.simpleVfxDropped -gt 0 `
            -or -not $_.cleanupComplete
    }).Count -eq 0
    controlledUpgradeBranchPickRates = $pickRates
    scenarios = $entries.ToArray()
}
$manifestPath = Join-Path $resolvedOutput "e14-qa-manifest.json"
$manifest | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Host "E14 quality QA complete: $($entries.Count)/$($scenarios.Count) focused scenarios passed."
Write-Host "Manifest: $manifestPath"
if (-not $manifest.allPassed) {
    exit 1
}

exit 0
