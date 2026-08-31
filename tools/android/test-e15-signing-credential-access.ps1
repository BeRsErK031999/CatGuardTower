Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "e15-signing-credential-rotation.ps1")

$versionLine = Get-Content -LiteralPath (Join-Path $PSScriptRoot "..\..\ProjectSettings\ProjectVersion.txt") -Encoding UTF8 |
    Select-Object -First 1
$version = ($versionLine -replace '^m_EditorVersion:\s*', '').Trim()
$keytool = Join-Path $env:LOCALAPPDATA "Unity\Hub\Editor\$version\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe"
if (-not (Test-Path -LiteralPath $keytool -PathType Leaf)) {
    throw "Unity Android keytool fixture dependency is missing: $keytool"
}

$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixtureRoot = Join-Path $tempBase ("catguard-e15-credential-access-" + [guid]::NewGuid().ToString("N"))
if (-not $fixtureRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase) `
    -or (Split-Path -Leaf $fixtureRoot) -notmatch '^catguard-e15-credential-access-[0-9a-f]{32}$') {
    throw "Unsafe temporary test path: $fixtureRoot"
}

$keystorePath = Join-Path $fixtureRoot "fixture.jks"
$credentialPath = Join-Path $fixtureRoot "fixture.dpapi.xml"
$wrongPasswordPath = Join-Path $fixtureRoot "wrong-password.dpapi.xml"
$wrongAliasPath = Join-Path $fixtureRoot "wrong-alias.dpapi.xml"
$legacyCredentialPath = Join-Path $fixtureRoot "legacy-pscredential.dpapi.xml"
$missingKeyPasswordPath = Join-Path $fixtureRoot "missing-key-password.dpapi.xml"
$staleBundlePath = Join-Path $fixtureRoot "stale-bundle.dpapi.xml"
$alias = "fixture-upload"
$fixturePassword = "Fixture-" + [guid]::NewGuid().ToString("N") + "-9a!"

function Write-FixtureBundle {
    param(
        [string]$Path,
        [string]$BundleAlias,
        [string]$StorePassword,
        [string]$PrivateKeyPassword,
        [string]$CreatedAtUtc = "2026-08-31T07:00:00Z",
        [bool]$IncludeKeyPassword = $true
    )

    $bundle = [ordered]@{
        schemaVersion = 1
        state = "signing_credential_ready"
        createdAtUtc = $CreatedAtUtc
        keyAlias = $BundleAlias
        keystorePassword = ConvertTo-SecureString -String $StorePassword -AsPlainText -Force
    }
    if ($IncludeKeyPassword) {
        $bundle["keyPassword"] = ConvertTo-SecureString -String $PrivateKeyPassword -AsPlainText -Force
    }
    [pscustomobject]$bundle | Export-Clixml -LiteralPath $Path
}

function Invoke-Fixture {
    param(
        [string]$Name,
        [string]$Credential,
        [string]$SelectedKeytool,
        [string]$ExpectedCertificate,
        [bool]$ExpectedPassed,
        [string]$ExpectedReason = "",
        [string]$ExpectedKeytool = ""
    )

    $result = Test-E15SigningCredentialAccess `
        -KeystorePath $keystorePath `
        -CredentialPath $Credential `
        -KeyAlias $alias `
        -KeytoolPath $SelectedKeytool `
        -ExpectedCertificateSha256 $ExpectedCertificate `
        -ExpectedKeytoolSha256 $ExpectedKeytool
    if ([bool]$result.passed -ne $ExpectedPassed) {
        throw "$Name expected passed=$ExpectedPassed but got $($result.passed): $($result.reasons -join ' ')"
    }
    if ($ExpectedReason -and -not (($result.reasons -join " ") -match $ExpectedReason)) {
        throw "$Name did not emit expected reason '$ExpectedReason': $($result.reasons -join ' ')"
    }
    Write-Host "$Name`: passed=$($result.passed)."
}

New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
try {
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & $keytool `
            -genkeypair `
            -alias $alias `
            -keystore $keystorePath `
            -storetype JKS `
            -storepass $fixturePassword `
            -keypass $fixturePassword `
            -keyalg RSA `
            -keysize 2048 `
            -validity 1 `
            -dname "CN=CatGuard E15 Credential Fixture" 2>&1 | Out-Null
        $generationExitCode = $LASTEXITCODE
        $certificateOutput = @(& $keytool -list -v -keystore $keystorePath -alias $alias -storepass $fixturePassword 2>&1) -join [Environment]::NewLine
        $listExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
    if ($generationExitCode -ne 0) {
        throw "Could not generate the isolated E15 credential-access fixture keystore."
    }
    if ($listExitCode -ne 0) {
        throw "Could not inspect the isolated E15 credential-access fixture certificate."
    }
    $fingerprintMatch = [regex]::Match($certificateOutput, '(?im)^\s*SHA256:\s*([0-9A-F:]{95})\s*$')
    if (-not $fingerprintMatch.Success) {
        throw "Fixture keytool output did not contain SHA-256."
    }
    $certificateSha256 = $fingerprintMatch.Groups[1].Value.Replace(":", "").ToUpperInvariant()

    Write-FixtureBundle -Path $credentialPath -BundleAlias $alias -StorePassword $fixturePassword -PrivateKeyPassword $fixturePassword
    Write-FixtureBundle -Path $wrongPasswordPath -BundleAlias $alias -StorePassword ($fixturePassword + "wrong") -PrivateKeyPassword $fixturePassword
    Write-FixtureBundle -Path $wrongAliasPath -BundleAlias "other-upload" -StorePassword $fixturePassword -PrivateKeyPassword $fixturePassword
    Write-FixtureBundle -Path $missingKeyPasswordPath -BundleAlias $alias -StorePassword $fixturePassword -PrivateKeyPassword $fixturePassword -IncludeKeyPassword $false
    Write-FixtureBundle -Path $staleBundlePath -BundleAlias $alias -StorePassword $fixturePassword -PrivateKeyPassword $fixturePassword -CreatedAtUtc "2026-08-31T05:07:26Z"
    $legacySecurePassword = ConvertTo-SecureString -String $fixturePassword -AsPlainText -Force
    (New-Object System.Management.Automation.PSCredential($alias, $legacySecurePassword)) |
        Export-Clixml -LiteralPath $legacyCredentialPath

    Invoke-Fixture -Name "valid" -Credential $credentialPath -SelectedKeytool $keytool -ExpectedCertificate $certificateSha256 -ExpectedPassed $true
    Invoke-Fixture -Name "wrong-password" -Credential $wrongPasswordPath -SelectedKeytool $keytool -ExpectedCertificate $certificateSha256 -ExpectedPassed $false -ExpectedReason "could not open"
    Invoke-Fixture -Name "wrong-alias" -Credential $wrongAliasPath -SelectedKeytool $keytool -ExpectedCertificate $certificateSha256 -ExpectedPassed $false -ExpectedReason "bundle alias"
    Invoke-Fixture -Name "wrong-certificate" -Credential $credentialPath -SelectedKeytool $keytool -ExpectedCertificate ("A" * 64) -ExpectedPassed $false -ExpectedReason "certificate fingerprint"
    Invoke-Fixture -Name "legacy-pscredential" -Credential $legacyCredentialPath -SelectedKeytool $keytool -ExpectedCertificate $certificateSha256 -ExpectedPassed $false -ExpectedReason "schema is incomplete"
    Invoke-Fixture -Name "missing-key-password" -Credential $missingKeyPasswordPath -SelectedKeytool $keytool -ExpectedCertificate $certificateSha256 -ExpectedPassed $false -ExpectedReason "schema is incomplete"
    Invoke-Fixture -Name "stale-bundle" -Credential $staleBundlePath -SelectedKeytool $keytool -ExpectedCertificate $certificateSha256 -ExpectedPassed $false -ExpectedReason "timestamp"
    Invoke-Fixture -Name "missing-keytool" -Credential $credentialPath -SelectedKeytool (Join-Path $fixtureRoot "missing-keytool.exe") -ExpectedCertificate $certificateSha256 -ExpectedPassed $false -ExpectedReason "keytool file is missing"
    Invoke-Fixture -Name "wrong-keytool-binding" -Credential $credentialPath -SelectedKeytool $keytool -ExpectedCertificate $certificateSha256 -ExpectedPassed $false -ExpectedReason "does not match the rotation record" -ExpectedKeytool ("B" * 64)
}
finally {
    $fixturePassword = $null
    if (Test-Path -LiteralPath $fixtureRoot -PathType Container) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}

Write-Host "E15 signing credential access tests passed: 9/9."
