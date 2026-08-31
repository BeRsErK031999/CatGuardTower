Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "e15-signing-credential-rotation.ps1")

$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixtureRoot = Join-Path $tempBase ("catguard-e15-signing-fixture-" + [guid]::NewGuid().ToString("N"))
$repoRoot = Join-Path $fixtureRoot "repo"
$externalRoot = Join-Path $fixtureRoot "external"
if (-not $fixtureRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase) `
    -or (Split-Path -Leaf $fixtureRoot) -notmatch '^catguard-e15-signing-fixture-[0-9a-f]{32}$') {
    throw "Unsafe temporary test path: $fixtureRoot"
}

$policy = Get-E15SigningCredentialRotationPolicy
$currentUtc = [DateTimeOffset]::Parse("2026-08-31T08:00:00Z")
$keystorePath = Join-Path $externalRoot "candidate.jks"
$credentialPath = Join-Path $externalRoot "candidate.dpapi.xml"
$recordPath = Join-Path $externalRoot "rotation.json"
$alias = "catguard-upload"

function Copy-FixtureRecord {
    param([object]$Record)

    return $Record | ConvertTo-Json -Depth 4 | ConvertFrom-Json
}

function Write-FixtureRecord {
    param(
        [string]$Path,
        [object]$Record
    )

    $Record | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Invoke-Fixture {
    param(
        [string]$Name,
        [string]$Path,
        [object]$Record,
        [bool]$ExpectedPassed,
        [string]$ExpectedReason = ""
    )

    Write-FixtureRecord -Path $Path -Record $Record
    $result = Test-E15SigningCredentialRotationRecord `
        -RecordPath $Path `
        -KeystorePath $keystorePath `
        -CredentialPath $credentialPath `
        -KeyAlias $alias `
        -RepositoryRoot $repoRoot `
        -CurrentUtc $currentUtc
    if ([bool]$result.passed -ne $ExpectedPassed) {
        throw "$Name expected passed=$ExpectedPassed but got $($result.passed): $($result.reasons -join ' ')"
    }
    if ($ExpectedReason -and -not (($result.reasons -join " ") -match $ExpectedReason)) {
        throw "$Name did not emit expected reason '$ExpectedReason': $($result.reasons -join ' ')"
    }
    Write-Host "$Name`: passed=$($result.passed)."
}

New-Item -ItemType Directory -Force -Path $repoRoot, $externalRoot | Out-Null
try {
    "rotated keystore fixture" | Set-Content -LiteralPath $keystorePath -Encoding UTF8
    "refreshed DPAPI fixture" | Set-Content -LiteralPath $credentialPath -Encoding UTF8
    $valid = [pscustomobject]@{
        schemaVersion = 2
        state = "credential_rotated"
        passed = $true
        rotatedAtUtc = "2026-08-31T07:00:00Z"
        keyAlias = $alias
        certificateSha256 = $policy.certificateSha256
        keystoreSha256 = (Get-FileHash -LiteralPath $keystorePath -Algorithm SHA256).Hash
        credentialFileSha256 = (Get-FileHash -LiteralPath $credentialPath -Algorithm SHA256).Hash
        credentialVerification = $policy.credentialVerification
        keytoolSha256 = "9" * 64
    }

    Invoke-Fixture -Name "valid" -Path $recordPath -Record (Copy-FixtureRecord $valid) -ExpectedPassed $true

    $stale = Copy-FixtureRecord $valid
    $stale.rotatedAtUtc = "2026-08-31T05:07:26Z"
    Invoke-Fixture -Name "stale-timestamp" -Path $recordPath -Record $stale -ExpectedPassed $false -ExpectedReason "incident cutoff"

    $wrongKeystore = Copy-FixtureRecord $valid
    $wrongKeystore.keystoreSha256 = "1" * 64
    Invoke-Fixture -Name "wrong-keystore-hash" -Path $recordPath -Record $wrongKeystore -ExpectedPassed $false -ExpectedReason "keystore hash"

    $wrongCredential = Copy-FixtureRecord $valid
    $wrongCredential.credentialFileSha256 = "2" * 64
    Invoke-Fixture -Name "wrong-credential-hash" -Path $recordPath -Record $wrongCredential -ExpectedPassed $false -ExpectedReason "credential hash"

    $insideRepoPath = Join-Path $repoRoot "rotation.json"
    Invoke-Fixture -Name "record-inside-repo" -Path $insideRepoPath -Record (Copy-FixtureRecord $valid) -ExpectedPassed $false -ExpectedReason "outside the repository"

    $wrongState = Copy-FixtureRecord $valid
    $wrongState.state = "pending"
    Invoke-Fixture -Name "malformed-state" -Path $recordPath -Record $wrongState -ExpectedPassed $false -ExpectedReason "completed credential rotation"

    $retiredKeystore = Copy-FixtureRecord $valid
    $retiredKeystore.keystoreSha256 = $policy.retiredKeystoreSha256
    Invoke-Fixture -Name "retired-keystore" -Path $recordPath -Record $retiredKeystore -ExpectedPassed $false -ExpectedReason "retired keystore fingerprint"

    $retiredCredential = Copy-FixtureRecord $valid
    $retiredCredential.credentialFileSha256 = $policy.retiredCredentialFileSha256
    Invoke-Fixture -Name "retired-credential" -Path $recordPath -Record $retiredCredential -ExpectedPassed $false -ExpectedReason "retired DPAPI credential fingerprint"
}
finally {
    if (Test-Path -LiteralPath $fixtureRoot -PathType Container) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}

Write-Host "E15 signing credential rotation contract tests passed: 8/8."
