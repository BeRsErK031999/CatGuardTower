[CmdletBinding()]
param(
    [ValidateSet("Aab", "Apk", "CaptureApk")]
    [string]$Artifact = "Aab",
    [string]$KeystorePath = $env:CATGUARD_ANDROID_KEYSTORE_PATH,
    [string]$KeyAlias = $env:CATGUARD_ANDROID_KEY_ALIAS,
    [string]$SigningCredentialPath = $env:CATGUARD_SIGNING_CREDENTIAL_PATH,
    [string]$SigningCredentialRotationRecordPath = $env:CATGUARD_SIGNING_ROTATION_RECORD_PATH,
    [System.Security.SecureString]$KeystorePassword,
    [System.Security.SecureString]$KeyPassword,
    [string]$UnityPath,
    [switch]$NonInteractive,
    [switch]$AllowDirtyWorkingTree
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$implementation = Join-Path $PSScriptRoot "build-signed-store-aab.ps1"
& $implementation @PSBoundParameters
exit 0
