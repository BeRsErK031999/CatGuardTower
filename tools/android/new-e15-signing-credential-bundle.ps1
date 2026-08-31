[CmdletBinding()]
param(
    [string]$OutputPath = $env:CATGUARD_SIGNING_CREDENTIAL_PATH,
    [string]$KeyAlias = $env:CATGUARD_ANDROID_KEY_ALIAS
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
. (Join-Path $PSScriptRoot "e15-signing-credential-rotation.ps1")

$keystorePassword = Read-Host "Rotated Android keystore password" -AsSecureString
$keyPassword = Read-Host "Rotated Android key password" -AsSecureString
try {
    $result = New-E15SigningCredentialBundle `
        -OutputPath $OutputPath `
        -KeyAlias $KeyAlias `
        -KeystorePassword $keystorePassword `
        -KeyPassword $keyPassword `
        -RepositoryRoot $repoRoot
    Write-Host "E15 signing DPAPI credential bundle created for the current Windows user."
    Write-Host "Bundle: $($result.path)"
    Write-Host "Bundle SHA256: $($result.sha256)"
}
finally {
    $keystorePassword = $null
    $keyPassword = $null
}
