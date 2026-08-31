Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "e15-artifact-set-manifest.ps1")

$gitHead = "1" * 40
$baselineCommit = "2" * 40
$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$repoRoot = [IO.Path]::GetFullPath((Join-Path $tempBase ("catguard-e15-artifact-repo-" + [guid]::NewGuid().ToString("N"))))
$runRoot = Join-Path $repoRoot "Builds\Android\qa-device\e15-artifact-set\fixture"
if (-not $repoRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase) `
    -or (Split-Path -Leaf $repoRoot) -notmatch '^catguard-e15-artifact-repo-[0-9a-f]{32}$') {
    throw "Unsafe temporary test path: $repoRoot"
}

function Copy-Evidence {
    param([object]$Evidence)

    return $Evidence | ConvertTo-Json -Depth 12 | ConvertFrom-Json
}

function Write-FixtureManifest {
    param(
        [string]$Name,
        [object]$Evidence
    )

    $path = Join-Path $runRoot "$Name.json"
    $Evidence | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding UTF8
    return $path
}

function Invoke-Fixture {
    param(
        [string]$Name,
        [object]$Evidence,
        [bool]$ExpectedPassed,
        [string]$ExpectedReason = "",
        [System.Collections.IDictionary]$ExpectedArtifactPaths = $script:FixtureExpectedArtifactPaths
    )

    $path = Write-FixtureManifest -Name $Name -Evidence $Evidence
    $result = Test-E15ArtifactSetManifest `
        -ManifestPath $path `
        -ExpectedGitHead $gitHead `
        -ExpectedUpstream $gitHead `
        -ExpectedBaselineCommit $baselineCommit `
        -ExpectedRepositoryRoot $repoRoot `
        -ExpectedArtifactPaths $ExpectedArtifactPaths `
        -RequireEvidenceFiles
    if ([bool]$result.passed -ne $ExpectedPassed) {
        throw "$Name expected passed=$ExpectedPassed but got $($result.passed): $($result.reasons -join ' ')"
    }
    if ($ExpectedReason -and -not (($result.reasons -join " ") -match $ExpectedReason)) {
        throw "$Name did not emit expected reason '$ExpectedReason': $($result.reasons -join ' ')"
    }
    Write-Host "$Name`: passed=$($result.passed)."
}

New-Item -ItemType Directory -Force -Path $runRoot | Out-Null
try {
    $artifactRoot = Join-Path $repoRoot "Builds\Android"
    New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
    $artifactNames = @(
        "baselineApk",
        "baselineApkProvenance",
        "candidateApk",
        "candidateAab",
        "candidateApkProvenance",
        "candidateAabProvenance"
    )
    $artifacts = [ordered]@{}
    $hashes = [ordered]@{}
    foreach ($name in $artifactNames) {
        $path = Join-Path $artifactRoot "$name.bin"
        "fixture bytes for $name" | Set-Content -LiteralPath $path -Encoding UTF8
        $artifacts[$name] = $path
        $hashes[$name] = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    }
    $script:FixtureExpectedArtifactPaths = $artifacts

    $preflight = [pscustomobject]@{
        passed = $true
        kind = "artifact-only-preflight"
        fullReleaseGate = $false
        gitHead = $gitHead
        baselineApk = [pscustomobject]@{ sha256 = $hashes.baselineApk }
        baselineBuildProvenance = [pscustomobject]@{ passed = $true; sha256 = $hashes.baselineApkProvenance }
        candidateApk = [pscustomobject]@{ sha256 = $hashes.candidateApk }
        candidateAab = [pscustomobject]@{ sha256 = $hashes.candidateAab }
        candidateApkBuildProvenance = [pscustomobject]@{ passed = $true; sha256 = $hashes.candidateApkProvenance }
        candidateAabBuildProvenance = [pscustomobject]@{ passed = $true; sha256 = $hashes.candidateAabProvenance }
    }
    $preflightPath = Join-Path $runRoot "e15-artifact-preflight.json"
    $preflight | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $preflightPath -Encoding UTF8
    $preflightLogPath = Join-Path $runRoot "artifact-only-release-gate.log"
    "Artifact preflight passed." | Set-Content -LiteralPath $preflightLogPath -Encoding UTF8

    $valid = [pscustomobject]@{
        schemaVersion = 1
        state = "artifact_set_prepared"
        passed = $true
        generatedAtUtc = "2026-08-31T08:05:00.0000000Z"
        buildStartedAtUtc = "2026-08-31T08:00:00.0000000Z"
        gitBranch = "codex/e15-release-gate"
        gitHead = $gitHead
        upstream = $gitHead
        baselineCommit = $baselineCommit
        runRoot = $runRoot
        artifacts = $artifacts
        hashes = $hashes
        artifactPreflightManifestPath = $preflightPath
        artifactPreflightManifestSha256 = (Get-FileHash -LiteralPath $preflightPath -Algorithm SHA256).Hash
        artifactPreflightLogPath = $preflightLogPath
        artifactPreflightLogSha256 = (Get-FileHash -LiteralPath $preflightLogPath -Algorithm SHA256).Hash
    }

    Invoke-Fixture -Name "valid" -Evidence (Copy-Evidence $valid) -ExpectedPassed $true

    $mismatchedPaths = [ordered]@{}
    foreach ($name in $artifactNames) {
        $mismatchedPaths[$name] = $artifacts[$name]
    }
    $mismatchedPaths["candidateAab"] = $artifacts["candidateApk"]
    Invoke-Fixture `
        -Name "mismatched-input" `
        -Evidence (Copy-Evidence $valid) `
        -ExpectedPassed $false `
        -ExpectedReason "candidateAab.*does not match" `
        -ExpectedArtifactPaths $mismatchedPaths

    $wrongHead = Copy-Evidence $valid
    $wrongHead.gitHead = "3" * 40
    $wrongHead.upstream = $wrongHead.gitHead
    Invoke-Fixture -Name "wrong-head" -Evidence $wrongHead -ExpectedPassed $false -ExpectedReason "pushed Git HEAD"

    $missingArtifact = Copy-Evidence $valid
    $missingPath = $missingArtifact.artifacts.candidateAab
    $missingBytes = [IO.File]::ReadAllBytes($missingPath)
    Remove-Item -LiteralPath $missingPath -Force
    Invoke-Fixture -Name "missing-artifact" -Evidence $missingArtifact -ExpectedPassed $false -ExpectedReason "candidateAab.*missing"
    [IO.File]::WriteAllBytes($missingPath, $missingBytes)

    $wrongHash = Copy-Evidence $valid
    $wrongHash.hashes.candidateApk = "4" * 64
    Invoke-Fixture -Name "wrong-hash" -Evidence $wrongHash -ExpectedPassed $false -ExpectedReason "candidateApk.*hash"

    $wrongPreflight = Copy-Evidence $valid
    $modifiedPreflight = Copy-Evidence $preflight
    $modifiedPreflight.gitHead = "5" * 40
    $modifiedPreflight | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $preflightPath -Encoding UTF8
    $wrongPreflight.artifactPreflightManifestSha256 = (Get-FileHash -LiteralPath $preflightPath -Algorithm SHA256).Hash
    Invoke-Fixture -Name "wrong-preflight" -Evidence $wrongPreflight -ExpectedPassed $false -ExpectedReason "preflight.*Git HEAD"
    $preflight | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $preflightPath -Encoding UTF8

    $tamperedLog = Copy-Evidence $valid
    "tampered" | Add-Content -LiteralPath $preflightLogPath -Encoding UTF8
    Invoke-Fixture -Name "tampered-log" -Evidence $tamperedLog -ExpectedPassed $false -ExpectedReason "preflight log.*hash"
}
finally {
    if (Test-Path -LiteralPath $repoRoot -PathType Container) {
        Remove-Item -LiteralPath $repoRoot -Recurse -Force
    }
}

Write-Host "E15 artifact-set manifest contract tests passed: 7/7."
