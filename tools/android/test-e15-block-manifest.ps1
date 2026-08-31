Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "e15-block-manifest.ps1")

$gitHead = "1" * 40
$candidateBase = "2" * 40
$licenseHash = "3" * 64
$artifactHashes = [ordered]@{
    developmentApk = "4" * 64
    baselineApk = "5" * 64
    baselineApkProvenance = "6" * 64
    candidateApk = "7" * 64
    candidateAab = "8" * 64
    candidateApkProvenance = "9" * 64
    candidateAabProvenance = "A" * 64
    artifactSetManifest = "B" * 64
}
$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$tempRoot = [IO.Path]::GetFullPath((Join-Path $tempBase ("catguard-e15-manifest-" + [guid]::NewGuid().ToString("N"))))
if (-not $tempRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase) `
    -or (Split-Path -Leaf $tempRoot) -notmatch '^catguard-e15-manifest-[0-9a-f]{32}$') {
    throw "Unsafe temporary test path: $tempRoot"
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

    $path = Join-Path $tempRoot "$Name.json"
    $Evidence | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding UTF8
    return $path
}

function Invoke-Fixture {
    param(
        [string]$Name,
        [object]$Evidence,
        [bool]$ExpectedPassed,
        [string]$ExpectedReason = "",
        [switch]$RequireEvidenceFiles
    )

    $path = Write-FixtureManifest -Name $Name -Evidence $Evidence
    $result = Test-E15TechnicalGateManifest `
        -ManifestPath $path `
        -ExpectedGitHead $gitHead `
        -ExpectedCandidateBase $candidateBase `
        -ExpectedArtifactHashes $artifactHashes `
        -ExpectedSourceAssetLicenseAuditSha256 $licenseHash `
        -RequireEvidenceFiles:$RequireEvidenceFiles
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
    $preconditions = New-Object System.Collections.Generic.List[object]
    foreach ($id in Get-E15RequiredTechnicalGatePreconditionIds) {
        $preconditions.Add([pscustomobject]@{ id = $id; passed = $true; expected = "fixture"; actual = "fixture" })
    }
    foreach ($name in @("baseline-apk", "baseline-provenance", "candidate-apk", "candidate-aab", "candidate-apk-provenance", "candidate-aab-provenance")) {
        $preconditions.Add([pscustomobject]@{ id = "artifact:$name"; passed = $true; expected = "fixture"; actual = "fixture" })
    }

    $steps = New-Object System.Collections.Generic.List[object]
    foreach ($id in Get-E15RequiredTechnicalGateStepIds) {
        if ($id -eq "artifact:hashes") {
            $steps.Add([pscustomobject]@{
                id = $id
                passed = $true
                exitCode = 0
                durationSeconds = 0
                logPath = ""
                logSha256 = ""
                hashes = $artifactHashes
            })
            continue
        }

        $safeName = $id -replace '[^A-Za-z0-9.-]', '-'
        $logPath = Join-Path $tempRoot "$safeName.log"
        "fixture log for $id" | Set-Content -LiteralPath $logPath -Encoding UTF8
        $isUnity = $id -like 'unity:*'
        $evidenceLogPath = ""
        $evidenceLogSha256 = ""
        if ($isUnity) {
            $evidenceLogPath = Join-Path $tempRoot "$safeName-unity.log"
            "fixture validation passed for $id" | Set-Content -LiteralPath $evidenceLogPath -Encoding UTF8
            $evidenceLogSha256 = (Get-FileHash -LiteralPath $evidenceLogPath -Algorithm SHA256).Hash
        }
        $steps.Add([pscustomobject]@{
            id = $id
            passed = $true
            exitCode = 0
            requiredPattern = if ($isUnity) { "validation passed" } else { "" }
            evidencePassed = $true
            evidenceLogPath = $evidenceLogPath
            evidenceLogSha256 = $evidenceLogSha256
            durationSeconds = 1
            logPath = $logPath
            logSha256 = (Get-FileHash -LiteralPath $logPath -Algorithm SHA256).Hash
        })
    }

    $valid = [pscustomobject]@{
        schemaVersion = 1
        generatedAtUtc = "2026-08-31T08:00:00.0000000Z"
        state = "technical_gate_passed"
        passed = $true
        preflightOnly = $false
        gitBranch = "codex/e15-release-gate"
        gitHead = $gitHead
        candidateBase = $candidateBase
        candidateFiles = @("fixture.txt")
        sourceAssetLicenseAuditSha256 = $licenseHash
        runRoot = $tempRoot
        preconditions = $preconditions.ToArray()
        steps = $steps.ToArray()
    }

    Invoke-Fixture -Name "valid" -Evidence (Copy-Evidence $valid) -ExpectedPassed $true -RequireEvidenceFiles

    $wrongHead = Copy-Evidence $valid
    $wrongHead.gitHead = "B" * 40
    Invoke-Fixture -Name "wrong-head" -Evidence $wrongHead -ExpectedPassed $false -ExpectedReason "Git HEAD"

    $missingStep = Copy-Evidence $valid
    $missingStep.steps = @($missingStep.steps | Where-Object { $_.id -ne "android:signed-physical-release" })
    Invoke-Fixture -Name "missing-step" -Evidence $missingStep -ExpectedPassed $false -ExpectedReason "missing required steps"

    $duplicateStep = Copy-Evidence $valid
    $duplicateStep.steps = @($duplicateStep.steps) + @(Copy-Evidence $duplicateStep.steps[0])
    Invoke-Fixture -Name "duplicate-step" -Evidence $duplicateStep -ExpectedPassed $false -ExpectedReason "duplicate step IDs"

    $failedPrecondition = Copy-Evidence $valid
    ($failedPrecondition.preconditions | Where-Object { $_.id -eq "DEVICE-QA-001" }).passed = $false
    Invoke-Fixture -Name "failed-precondition" -Evidence $failedPrecondition -ExpectedPassed $false -ExpectedReason "failed preconditions"

    $wrongArtifactHash = Copy-Evidence $valid
    ($wrongArtifactHash.steps | Where-Object { $_.id -eq "artifact:hashes" }).hashes.candidateAab = "C" * 64
    Invoke-Fixture -Name "wrong-artifact-hash" -Evidence $wrongArtifactHash -ExpectedPassed $false -ExpectedReason "candidateAab"

    $tamperedLog = Copy-Evidence $valid
    $tamperedStep = $tamperedLog.steps | Where-Object { $_.id -eq "store:assets" }
    "tampered after manifest creation" | Add-Content -LiteralPath $tamperedStep.logPath -Encoding UTF8
    Invoke-Fixture -Name "tampered-log" -Evidence $tamperedLog -ExpectedPassed $false -ExpectedReason "log hash" -RequireEvidenceFiles
}
finally {
    if (Test-Path -LiteralPath $tempRoot -PathType Container) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

Write-Host "E15 technical manifest contract tests passed: 7/7."
