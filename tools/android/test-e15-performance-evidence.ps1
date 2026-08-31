Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$releaseGatePath = Join-Path $PSScriptRoot "run-e15-release-gate.ps1"
$tokens = $null
$parseErrors = $null
$releaseGateAst = [Management.Automation.Language.Parser]::ParseFile(
    $releaseGatePath,
    [ref]$tokens,
    [ref]$parseErrors)
if ($parseErrors.Count -gt 0) {
    throw "Release-gate parser errors prevent evidence contract tests."
}

$functionAst = $releaseGateAst.Find({
    param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] `
        -and $node.Name -eq "Test-HeavyWavePerformanceEvidence"
}, $true)
if ($null -eq $functionAst) {
    throw "Test-HeavyWavePerformanceEvidence was not found in run-e15-release-gate.ps1."
}

. ([scriptblock]::Create($functionAst.Extent.Text))

$PackageName = "com.berserk031999.catguardtower"
$candidate = [pscustomobject]@{
    sha256 = "AABBCCDD"
    versionName = "0.2.0"
}
$device = [pscustomobject]@{
    serial = "physical-123"
    isEmulator = $false
}
$checkpointBefore = [pscustomobject]@{
    capturedAtUtc = "2026-08-12T08:00:00.0000000Z"
    heavyWaveEligible = $true
    applicationIdentifier = $PackageName
    applicationVersion = "0.2.0"
    levelId = "level_12"
    state = "running"
    paused = $false
    towerCount = 4
    activeEnemyCount = 8
    bossActive = $false
    graphicsDeviceName = "Adreno (TM) 740"
    graphicsDeviceVendor = "Qualcomm"
    graphicsDeviceType = "Vulkan"
}
$checkpointAfter = $checkpointBefore | ConvertTo-Json -Depth 6 | ConvertFrom-Json
$checkpointAfter.capturedAtUtc = "2026-08-12T08:00:25.0000000Z"
$validEvidence = [pscustomobject]@{
    deviceKind = "physical"
    deviceSerial = $device.serial
    packageName = $PackageName
    apkSha256 = $candidate.sha256
    installedApkSha256 = $candidate.sha256
    apkIdentityMatched = $true
    skipInstall = $true
    performanceContext = "level_12-heavy-wave"
    samplingWindowSeconds = 25
    useRunningApp = $true
    packageFocused = $true
    runtimeCheckpointBefore = $checkpointBefore
    runtimeCheckpointAfter = $checkpointAfter
    releasePerformanceEvidenceEligible = $true
    graphicsProvenance = [pscustomobject]@{
        classification = "physical-hardware"
        releaseAcceptanceEligible = $true
        softwareRenderer = $false
    }
    fatalPatternCount = 0
    landscapeConfirmed = $true
    performance = [pscustomobject]@{
        passed = $true
        sampleCount = 120
        averageFps = 55.5
        p95FrameTimeMs = 30.2
    }
}

function Copy-Evidence {
    param([object]$Evidence)

    return $Evidence | ConvertTo-Json -Depth 10 | ConvertFrom-Json
}

function Invoke-EvidenceFixture {
    param(
        [string]$Name,
        [object]$Evidence,
        [bool]$ExpectedPassed,
        [string]$ExpectedReasonPattern = ""
    )

    $fixturePath = Join-Path $script:TempRoot "$Name.json"
    $Evidence | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $fixturePath -Encoding UTF8
    $result = Test-HeavyWavePerformanceEvidence `
        -Path $fixturePath `
        -Candidate $candidate `
        -Device $device
    if ([bool]$result.passed -ne $ExpectedPassed) {
        throw "$Name expected passed=$ExpectedPassed but got $($result.passed): $($result.reasons -join ' ')"
    }
    if ($ExpectedReasonPattern `
        -and -not (($result.reasons -join " ") -match $ExpectedReasonPattern)) {
        throw "$Name did not emit expected reason '$ExpectedReasonPattern': $($result.reasons -join ' ')"
    }

    Write-Host "$Name`: passed=$($result.passed)."
}

$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$script:TempRoot = [IO.Path]::GetFullPath((Join-Path $tempBase ("catguard-e15-performance-" + [guid]::NewGuid().ToString("N"))))
if (-not $script:TempRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase) `
    -or (Split-Path -Leaf $script:TempRoot) -notmatch '^catguard-e15-performance-[0-9a-f]{32}$') {
    throw "Unsafe temporary test path: $script:TempRoot"
}
New-Item -ItemType Directory -Force -Path $script:TempRoot | Out-Null
try {
    Invoke-EvidenceFixture -Name "valid" -Evidence (Copy-Evidence $validEvidence) -ExpectedPassed $true

    $softwareRenderer = Copy-Evidence $validEvidence
    $softwareRenderer.graphicsProvenance.classification = "physical-software"
    $softwareRenderer.graphicsProvenance.releaseAcceptanceEligible = $false
    $softwareRenderer.graphicsProvenance.softwareRenderer = $true
    $softwareRenderer.releasePerformanceEvidenceEligible = $false
    Invoke-EvidenceFixture `
        -Name "software-renderer" `
        -Evidence $softwareRenderer `
        -ExpectedPassed $false `
        -ExpectedReasonPattern "renderer"

    $shortWindow = Copy-Evidence $validEvidence
    $shortWindow.samplingWindowSeconds = 10
    $shortWindow.runtimeCheckpointAfter.capturedAtUtc = "2026-08-12T08:00:10.0000000Z"
    Invoke-EvidenceFixture `
        -Name "short-window" `
        -Evidence $shortWindow `
        -ExpectedPassed $false `
        -ExpectedReasonPattern "20-second"

    $loadExited = Copy-Evidence $validEvidence
    $loadExited.runtimeCheckpointAfter.heavyWaveEligible = $false
    $loadExited.runtimeCheckpointAfter.activeEnemyCount = 2
    Invoke-EvidenceFixture `
        -Name "load-exited" `
        -Evidence $loadExited `
        -ExpectedPassed $false `
        -ExpectedReasonPattern "before and after"

    $wrongApk = Copy-Evidence $validEvidence
    $wrongApk.installedApkSha256 = "DEADBEEF"
    $wrongApk.apkIdentityMatched = $false
    Invoke-EvidenceFixture `
        -Name "wrong-apk" `
        -Evidence $wrongApk `
        -ExpectedPassed $false `
        -ExpectedReasonPattern "exact candidate APK"
}
finally {
    if (Test-Path -LiteralPath $script:TempRoot -PathType Container) {
        Remove-Item -LiteralPath $script:TempRoot -Recurse -Force
    }
}

Write-Host "E15 performance evidence contract tests passed: 5/5."
