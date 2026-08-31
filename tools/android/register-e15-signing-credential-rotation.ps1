[CmdletBinding()]
param(
    [string]$KeystorePath = $env:CATGUARD_ANDROID_KEYSTORE_PATH,
    [string]$CredentialPath = $env:CATGUARD_SIGNING_CREDENTIAL_PATH,
    [string]$KeyAlias = $env:CATGUARD_ANDROID_KEY_ALIAS,
    [string]$OutputPath = $env:CATGUARD_SIGNING_ROTATION_RECORD_PATH,
    [string]$KeytoolPath = $env:CATGUARD_KEYTOOL_PATH
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
. (Join-Path $PSScriptRoot "e15-signing-credential-rotation.ps1")

if ($env:CATGUARD_ANDROID_KEYSTORE_PASSWORD -or $env:CATGUARD_ANDROID_KEY_PASSWORD) {
    throw "E15 signing rotation registration accepts passwords only from the DPAPI credential bundle."
}

if (-not $KeytoolPath) {
    $versionLine = Get-Content -LiteralPath (Join-Path $repoRoot "ProjectSettings\ProjectVersion.txt") -Encoding UTF8 |
        Select-Object -First 1
    $version = ($versionLine -replace '^m_EditorVersion:\s*', '').Trim()
    foreach ($candidate in @(
        (Join-Path $env:LOCALAPPDATA "Unity\Hub\Editor\$version\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe"),
        (Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe"))) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $KeytoolPath = (Resolve-Path -LiteralPath $candidate).Path
            break
        }
    }
}
if (-not $KeytoolPath -or -not (Test-Path -LiteralPath $KeytoolPath -PathType Leaf)) {
    throw "Unity Android keytool was not found. Pass -KeytoolPath or set CATGUARD_KEYTOOL_PATH."
}

$result = New-E15SigningCredentialRotationRecord `
    -OutputPath $OutputPath `
    -KeystorePath $KeystorePath `
    -CredentialPath $CredentialPath `
    -KeyAlias $KeyAlias `
    -RepositoryRoot $repoRoot `
    -KeytoolPath $KeytoolPath

Write-Host "E15 signing credential rotation record registered."
Write-Host "Record: $($result.recordPath)"
Write-Host "Record SHA256: $($result.recordSha256)"
