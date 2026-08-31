Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "e15-artifact-provenance.ps1")

$baselineCommit = "1111111111111111111111111111111111111111"
$orchestratorHead = "2222222222222222222222222222222222222222"
$unityVersion = "6000.4.12f1"
$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$tempRoot = [IO.Path]::GetFullPath((Join-Path $tempBase ("catguard-e15-baseline-" + [guid]::NewGuid().ToString("N"))))
if (-not $tempRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase) `
    -or (Split-Path -Leaf $tempRoot) -notmatch '^catguard-e15-baseline-[0-9a-f]{32}$') {
    throw "Unsafe temporary test path: $tempRoot"
}

function Copy-Evidence {
    param([object]$Evidence)

    return $Evidence | ConvertTo-Json -Depth 8 | ConvertFrom-Json
}

function Invoke-Fixture {
    param(
        [string]$Name,
        [object]$Evidence,
        [bool]$ExpectedPassed,
        [string]$ExpectedReason = ""
    )

    $path = Join-Path $tempRoot "$Name.json"
    $Evidence | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $path -Encoding UTF8
    $result = Test-E15BaselineBuildProvenance `
        -ProvenancePath $path `
        -ArtifactPath $script:ArtifactPath `
        -ExpectedBaselineCommit $baselineCommit `
        -ExpectedOrchestratorGitHead $orchestratorHead `
        -ExpectedUnityVersion $unityVersion
    if ([bool]$result.passed -ne $ExpectedPassed) {
        throw "$Name expected passed=$ExpectedPassed but got $($result.passed): $($result.reasons -join ' ')"
    }
    if ($ExpectedReason -and -not (($result.reasons -join " ") -match $ExpectedReason)) {
        throw "$Name did not emit expected reason '$ExpectedReason': $($result.reasons -join ' ')"
    }

    Write-Host "$Name`: passed=$($result.passed)."
}

New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
try {
    $script:ArtifactPath = Join-Path $tempRoot "CatGuardTowerDefense-0.1.0-universal.apk"
    [IO.File]::WriteAllBytes($script:ArtifactPath, [byte[]](6, 7, 8, 9))
    $artifact = Get-Item -LiteralPath $script:ArtifactPath
    $sha = (Get-FileHash -LiteralPath $script:ArtifactPath -Algorithm SHA256).Hash
    $intermediateSha = "A" * 64
    $valid = [pscustomobject]@{
        schemaVersion = 1
        passed = $true
        artifact = "BaselineUniversalApk"
        artifactFileName = $artifact.Name
        artifactSha256 = $sha
        artifactBytes = $artifact.Length
        packageName = "com.berserk031999.catguardtower"
        versionName = "0.1.0"
        versionCode = 1
        baselineCommitRequested = "28f7e88"
        baselineCommitResolved = $baselineCommit
        orchestratorGitHeadBefore = $orchestratorHead
        orchestratorGitHeadAfter = $orchestratorHead
        orchestratorGitBranch = "codex/e15-release-gate"
        currentWorkingTreeCleanBefore = $true
        currentWorkingTreeCleanAfter = $true
        sourceWorktreeCleanBefore = $true
        sourceWorktreeCleanAfter = $true
        sourceProjectSettingsRestored = $true
        sourceProjectSettingsSha256Before = $intermediateSha
        sourceProjectSettingsSha256After = $intermediateSha
        baselineAabSha256 = $intermediateSha
        baselineAabBytes = 100
        apksSha256 = $intermediateSha
        apksBytes = 200
        transformation = "bundletool build-apks --mode universal"
        bundletoolSha256 = $intermediateSha
        unityVersion = $unityVersion
        buildStartedAtUtc = "2026-08-13T08:00:00.0000000Z"
        buildCompletedAtUtc = "2026-08-13T08:03:00.0000000Z"
    }

    Invoke-Fixture -Name "valid" -Evidence (Copy-Evidence $valid) -ExpectedPassed $true

    $wrongSource = Copy-Evidence $valid
    $wrongSource.baselineCommitResolved = "3333333333333333333333333333333333333333"
    Invoke-Fixture -Name "wrong-source" -Evidence $wrongSource -ExpectedPassed $false -ExpectedReason "expected source commit"

    $wrongOrchestrator = Copy-Evidence $valid
    $wrongOrchestrator.orchestratorGitHeadAfter = "4444444444444444444444444444444444444444"
    Invoke-Fixture -Name "wrong-orchestrator" -Evidence $wrongOrchestrator -ExpectedPassed $false -ExpectedReason "current orchestrator"

    $dirtySource = Copy-Evidence $valid
    $dirtySource.sourceWorktreeCleanAfter = $false
    Invoke-Fixture -Name "dirty-source" -Evidence $dirtySource -ExpectedPassed $false -ExpectedReason "clean current and historical"

    $wrongHash = Copy-Evidence $valid
    $wrongHash.artifactSha256 = "DEADBEEF"
    Invoke-Fixture -Name "wrong-hash" -Evidence $wrongHash -ExpectedPassed $false -ExpectedReason "SHA-256"
}
finally {
    if (Test-Path -LiteralPath $tempRoot -PathType Container) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

Write-Host "E15 baseline provenance contract tests passed: 5/5."
