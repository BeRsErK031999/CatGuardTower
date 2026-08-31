Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "e15-signing-credential-rotation.ps1")

$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixtureRoot = Join-Path $tempBase ("catguard-e15-bundle-fixture-" + [guid]::NewGuid().ToString("N"))
$repoRoot = Join-Path $fixtureRoot "repo"
$externalRoot = Join-Path $fixtureRoot "external"
if (-not $fixtureRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase) `
    -or (Split-Path -Leaf $fixtureRoot) -notmatch '^catguard-e15-bundle-fixture-[0-9a-f]{32}$') {
    throw "Unsafe temporary test path: $fixtureRoot"
}

$alias = "fixture-upload"
$storePassword = ConvertTo-SecureString -String ("Store-" + [guid]::NewGuid().ToString("N")) -AsPlainText -Force
$keyPassword = ConvertTo-SecureString -String ("Key-" + [guid]::NewGuid().ToString("N")) -AsPlainText -Force
$bundlePath = Join-Path $externalRoot "fixture.dpapi.xml"

New-Item -ItemType Directory -Force -Path $repoRoot, $externalRoot | Out-Null
try {
    $valid = New-E15SigningCredentialBundle `
        -OutputPath $bundlePath `
        -KeyAlias $alias `
        -KeystorePassword $storePassword `
        -KeyPassword $keyPassword `
        -RepositoryRoot $repoRoot `
        -CreatedAtUtc ([DateTimeOffset]::Parse("2026-08-31T07:00:00Z"))
    if (-not [bool]$valid.passed `
        -or $valid.sha256 -notmatch '^[0-9A-Fa-f]{64}$' `
        -or $valid.keystorePassword -isnot [System.Security.SecureString] `
        -or $valid.keyPassword -isnot [System.Security.SecureString]) {
        throw "valid expected a hash-bound dual SecureString bundle."
    }
    Write-Host "valid: passed=True."

    $wrongAlias = Get-E15SigningCredentialBundle -CredentialPath $bundlePath -ExpectedKeyAlias "other-upload"
    if ([bool]$wrongAlias.passed -or ($wrongAlias.reasons -join " ") -notmatch "alias") {
        throw "wrong-alias expected a bundle alias mismatch."
    }
    Write-Host "wrong-alias: passed=False."

    $overwriteRejected = $false
    try {
        New-E15SigningCredentialBundle -OutputPath $bundlePath -KeyAlias $alias -KeystorePassword $storePassword -KeyPassword $keyPassword -RepositoryRoot $repoRoot | Out-Null
    }
    catch {
        $overwriteRejected = $_.Exception.Message -match "overwrite"
    }
    if (-not $overwriteRejected) {
        throw "existing-output expected overwrite rejection."
    }
    Write-Host "existing-output: passed=False."

    $insideRepoRejected = $false
    try {
        New-E15SigningCredentialBundle -OutputPath (Join-Path $repoRoot "credential.xml") -KeyAlias $alias -KeystorePassword $storePassword -KeyPassword $keyPassword -RepositoryRoot $repoRoot | Out-Null
    }
    catch {
        $insideRepoRejected = $_.Exception.Message -match "outside the repository"
    }
    if (-not $insideRepoRejected) {
        throw "inside-repo expected repository boundary rejection."
    }
    Write-Host "inside-repo: passed=False."

    $emptyPasswordRejected = $false
    try {
        New-E15SigningCredentialBundle -OutputPath (Join-Path $externalRoot "empty.xml") -KeyAlias $alias -KeystorePassword $storePassword -KeyPassword (New-Object System.Security.SecureString) -RepositoryRoot $repoRoot | Out-Null
    }
    catch {
        $emptyPasswordRejected = $_.Exception.Message -match "non-empty"
    }
    if (-not $emptyPasswordRejected) {
        throw "empty-password expected SecureString rejection."
    }
    Write-Host "empty-password: passed=False."

    $legacyPath = Join-Path $externalRoot "legacy.xml"
    (New-Object System.Management.Automation.PSCredential($alias, $storePassword)) | Export-Clixml -LiteralPath $legacyPath
    $legacy = Get-E15SigningCredentialBundle -CredentialPath $legacyPath -ExpectedKeyAlias $alias
    if ([bool]$legacy.passed -or ($legacy.reasons -join " ") -notmatch "schema is incomplete") {
        throw "legacy-pscredential expected the old one-password format to be rejected."
    }
    Write-Host "legacy-pscredential: passed=False."

    $malformedSchemaPath = Join-Path $externalRoot "malformed-schema.xml"
    [pscustomobject]@{
        schemaVersion = "not-an-integer"
        state = "signing_credential_ready"
        createdAtUtc = "2026-08-31T07:00:00.0000000Z"
        keyAlias = $alias
        keystorePassword = $storePassword
        keyPassword = $keyPassword
    } | Export-Clixml -LiteralPath $malformedSchemaPath
    $malformedSchema = Get-E15SigningCredentialBundle `
        -CredentialPath $malformedSchemaPath `
        -ExpectedKeyAlias $alias
    if ([bool]$malformedSchema.passed `
        -or ($malformedSchema.reasons -join " ") -notmatch "state or schema") {
        throw "malformed-schema expected a typed schema rejection without a parser exception."
    }
    Write-Host "malformed-schema: passed=False."
}
finally {
    $storePassword = $null
    $keyPassword = $null
    if (Test-Path -LiteralPath $fixtureRoot -PathType Container) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}

Write-Host "E15 signing credential bundle tests passed: 7/7."
