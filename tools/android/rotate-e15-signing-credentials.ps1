[CmdletBinding()]
param(
    [string]$KeystorePath = $env:CATGUARD_ANDROID_KEYSTORE_PATH,
    [string]$LegacyCredentialPath = $env:CATGUARD_SIGNING_CREDENTIAL_PATH,
    [string]$NewCredentialPath = $env:CATGUARD_ROTATED_SIGNING_CREDENTIAL_PATH,
    [string]$RotationRecordPath = $env:CATGUARD_SIGNING_ROTATION_RECORD_PATH,
    [string]$KeyAlias = $env:CATGUARD_ANDROID_KEY_ALIAS,
    [string]$KeytoolPath = $env:CATGUARD_KEYTOOL_PATH,
    [string]$BackupPath = "",
    [switch]$PreflightOnly,
    [switch]$ConfirmRotation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
. (Join-Path $PSScriptRoot "e15-signing-credential-rotation.ps1")

function Test-SecureStringEqual {
    param(
        [System.Security.SecureString]$Left,
        [System.Security.SecureString]$Right
    )

    if ($null -eq $Left -or $null -eq $Right -or $Left.Length -ne $Right.Length) {
        return $false
    }
    $leftPointer = [IntPtr]::Zero
    $rightPointer = [IntPtr]::Zero
    try {
        $leftPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Left)
        $rightPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Right)
        $different = 0
        for ($index = 0; $index -lt $Left.Length; $index++) {
            $different = $different -bor (
                [Runtime.InteropServices.Marshal]::ReadInt16($leftPointer, $index * 2) -bxor
                [Runtime.InteropServices.Marshal]::ReadInt16($rightPointer, $index * 2))
        }
        return $different -eq 0
    }
    finally {
        if ($leftPointer -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($leftPointer)
        }
        if ($rightPointer -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($rightPointer)
        }
    }
}

function Read-ConfirmedPassword {
    param([string]$Label)

    for ($attempt = 1; $attempt -le 3; $attempt++) {
        $first = Read-Host $Label -AsSecureString
        $second = Read-Host "$Label (repeat)" -AsSecureString
        if ($first.Length -ge 16 -and (Test-SecureStringEqual -Left $first -Right $second)) {
            $second = $null
            return $first
        }
        $first = $null
        $second = $null
        Write-Warning "Passwords did not match or were shorter than 16 characters. Try again."
    }
    throw "E15 signing password confirmation failed after three attempts."
}

function Invoke-KeytoolWithSecureEnvironment {
    param(
        [string]$Arguments,
        [hashtable]$SecureEnvironment
    )

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $script:ResolvedKeytool
    $startInfo.Arguments = $Arguments
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($inheritedPasswordVariable in @($startInfo.EnvironmentVariables.Keys) | Where-Object {
        $_ -in @("CATGUARD_ANDROID_KEYSTORE_PASSWORD", "CATGUARD_ANDROID_KEY_PASSWORD") `
            -or $_ -like "CGE15_*"
    }) {
        $startInfo.EnvironmentVariables.Remove($inheritedPasswordVariable)
    }

    $pointers = @{}
    $plainValues = @{}
    $process = $null
    try {
        foreach ($name in $SecureEnvironment.Keys) {
            $value = $SecureEnvironment[$name]
            if ($value -isnot [System.Security.SecureString] -or $value.Length -eq 0) {
                throw "Secure keytool input '$name' is missing."
            }
            $pointers[$name] = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($value)
            $plainValues[$name] = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointers[$name])
            $startInfo.EnvironmentVariables[$name] = $plainValues[$name]
        }

        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo
        if (-not $process.Start()) {
            throw "keytool did not start."
        }
        foreach ($name in $SecureEnvironment.Keys) {
            $startInfo.EnvironmentVariables.Remove($name)
            $plainValues[$name] = $null
        }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $output = $stdoutTask.Result + [Environment]::NewLine + $stderrTask.Result
        if ($process.ExitCode -ne 0) {
            throw "keytool rejected the protected signing operation without exposing its output."
        }
        return $output
    }
    finally {
        foreach ($name in $SecureEnvironment.Keys) {
            $startInfo.EnvironmentVariables.Remove($name)
            $plainValues[$name] = $null
            if ($pointers.ContainsKey($name) -and $pointers[$name] -ne [IntPtr]::Zero) {
                [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointers[$name])
            }
        }
        if ($null -ne $process) {
            $process.Dispose()
        }
    }
}

function New-RandomProbePassword {
    $bytes = New-Object byte[] 32
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
        $plain = [Convert]::ToBase64String($bytes)
        return ConvertTo-SecureString -String $plain -AsPlainText -Force
    }
    finally {
        $plain = $null
        [Array]::Clear($bytes, 0, $bytes.Length)
        $generator.Dispose()
    }
}

$required = @(
    [pscustomobject]@{ name = "keystore"; value = $KeystorePath },
    [pscustomobject]@{ name = "legacy credential"; value = $LegacyCredentialPath },
    [pscustomobject]@{ name = "new credential"; value = $NewCredentialPath },
    [pscustomobject]@{ name = "rotation record"; value = $RotationRecordPath },
    [pscustomobject]@{ name = "key alias"; value = $KeyAlias })
foreach ($item in $required) {
    if ([string]::IsNullOrWhiteSpace($item.value)) {
        throw "E15 signing rotation $($item.name) input is missing."
    }
}
if ($KeyAlias -notmatch '^[A-Za-z0-9._-]+$') {
    throw "E15 signing rotation key alias contains unsupported characters."
}

if (-not $KeytoolPath) {
    $versionLine = Get-Content -LiteralPath (Join-Path $repoRoot "ProjectSettings\ProjectVersion.txt") -Encoding UTF8 |
        Select-Object -First 1
    $version = ($versionLine -replace '^m_EditorVersion:\s*', '').Trim()
    foreach ($candidate in @(
        (Join-Path $env:LOCALAPPDATA "Unity\Hub\Editor\$version\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe"),
        (Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe"))) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $KeytoolPath = $candidate
            break
        }
    }
}
$script:ResolvedKeytool = if ($KeytoolPath) { [IO.Path]::GetFullPath($KeytoolPath) } else { "" }
if (-not $script:ResolvedKeytool -or -not (Test-Path -LiteralPath $script:ResolvedKeytool -PathType Leaf)) {
    throw "Unity Android keytool was not found."
}

$resolvedKeystore = [IO.Path]::GetFullPath($KeystorePath)
$resolvedLegacyCredential = [IO.Path]::GetFullPath($LegacyCredentialPath)
$resolvedNewCredential = [IO.Path]::GetFullPath($NewCredentialPath)
$resolvedRecord = [IO.Path]::GetFullPath($RotationRecordPath)
$resolvedBackup = if ($BackupPath) {
    [IO.Path]::GetFullPath($BackupPath)
}
else {
    Join-Path ([IO.Path]::GetDirectoryName($resolvedKeystore)) (
        "catguard-upload.pre-rotation-{0}.jks" -f (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ"))
}

foreach ($inputPath in @($resolvedKeystore, $resolvedLegacyCredential)) {
    if (-not (Test-Path -LiteralPath $inputPath -PathType Leaf)) {
        throw "E15 signing rotation input is missing: $inputPath"
    }
}
foreach ($externalPath in @(
    $resolvedKeystore,
    $resolvedLegacyCredential,
    $resolvedNewCredential,
    $resolvedRecord,
    $resolvedBackup)) {
    if (Test-E15SigningPathInsideDirectory -Path $externalPath -Directory $repoRoot) {
        throw "E15 signing rotation material must remain outside the repository: $externalPath"
    }
}
foreach ($newPath in @($resolvedNewCredential, $resolvedRecord, $resolvedBackup)) {
    if (Test-Path -LiteralPath $newPath) {
        throw "Refusing to overwrite E15 signing rotation output: $newPath"
    }
}

$policy = Get-E15SigningCredentialRotationPolicy
$keystoreSha256 = (Get-FileHash -LiteralPath $resolvedKeystore -Algorithm SHA256).Hash
$legacyCredentialSha256 = (Get-FileHash -LiteralPath $resolvedLegacyCredential -Algorithm SHA256).Hash
if ($keystoreSha256 -ine $policy.retiredKeystoreSha256 `
    -or $legacyCredentialSha256 -ine $policy.retiredCredentialFileSha256) {
    throw "E15 signing rotation accepts only the exact machine-blocked pre-rotation JKS and credential pair."
}

$legacyCredential = Import-Clixml -LiteralPath $resolvedLegacyCredential
if ($legacyCredential -isnot [System.Management.Automation.PSCredential] `
    -or $legacyCredential.UserName -cne $KeyAlias `
    -or $legacyCredential.Password.Length -eq 0) {
    throw "The retired E15 credential is not the expected alias-bound PSCredential."
}
$oldPassword = $legacyCredential.Password
$legacyCredential = $null

$inspectVariable = "CGE15_INSPECT_" + [guid]::NewGuid().ToString("N").ToUpperInvariant()
$inspectOutput = Invoke-KeytoolWithSecureEnvironment `
    -Arguments "-list -v -keystore `"$resolvedKeystore`" -alias `"$KeyAlias`" -storepass:env $inspectVariable" `
    -SecureEnvironment @{ $inspectVariable = $oldPassword }
$certificateMatch = [regex]::Match($inspectOutput, '(?im)^\s*SHA256:\s*([0-9A-F:]{95})\s*$')
if (-not $certificateMatch.Success `
    -or $certificateMatch.Groups[1].Value.Replace(":", "") -ine $policy.certificateSha256) {
    throw "The retired E15 JKS does not contain the approved upload certificate."
}
$inspectOutput = $null

Write-Host "E15 signing rotation preflight passed."
Write-Host "Keystore: $resolvedKeystore"
Write-Host "Backup: $resolvedBackup"
Write-Host "New DPAPI bundle: $resolvedNewCredential"
Write-Host "Rotation record: $resolvedRecord"
if ($PreflightOnly) {
    exit 0
}
if (-not $ConfirmRotation) {
    throw "Pass -ConfirmRotation to authorize JKS password changes after reviewing the preflight paths."
}

$newStorePassword = Read-ConfirmedPassword -Label "New Android keystore password"
$newKeyPassword = Read-ConfirmedPassword -Label "New Android private-key password"
if (Test-SecureStringEqual -Left $newStorePassword -Right $newKeyPassword) {
    throw "The new keystore and private-key passwords must be different."
}
if ((Test-SecureStringEqual -Left $newStorePassword -Right $oldPassword) `
    -or (Test-SecureStringEqual -Left $newKeyPassword -Right $oldPassword)) {
    throw "Both new signing passwords must differ from the retired password."
}

$probePassword = New-RandomProbePassword
$keystoreMutated = $false
$bundleCreated = $false
$recordCreated = $false
Copy-Item -LiteralPath $resolvedKeystore -Destination $resolvedBackup
try {
    $oldStoreVariable = "CGE15_OLD_STORE_" + [guid]::NewGuid().ToString("N").ToUpperInvariant()
    $oldKeyVariable = "CGE15_OLD_KEY_" + [guid]::NewGuid().ToString("N").ToUpperInvariant()
    $newKeyVariable = "CGE15_NEW_KEY_" + [guid]::NewGuid().ToString("N").ToUpperInvariant()
    Invoke-KeytoolWithSecureEnvironment `
        -Arguments "-keypasswd -alias `"$KeyAlias`" -keystore `"$resolvedKeystore`" -storepass:env $oldStoreVariable -keypass:env $oldKeyVariable -new:env $newKeyVariable" `
        -SecureEnvironment @{
            $oldStoreVariable = $oldPassword
            $oldKeyVariable = $oldPassword
            $newKeyVariable = $newKeyPassword
        } | Out-Null
    $keystoreMutated = $true

    $oldStoreVariable = "CGE15_OLD_STORE_" + [guid]::NewGuid().ToString("N").ToUpperInvariant()
    $newStoreVariable = "CGE15_NEW_STORE_" + [guid]::NewGuid().ToString("N").ToUpperInvariant()
    Invoke-KeytoolWithSecureEnvironment `
        -Arguments "-storepasswd -keystore `"$resolvedKeystore`" -storepass:env $oldStoreVariable -new:env $newStoreVariable" `
        -SecureEnvironment @{
            $oldStoreVariable = $oldPassword
            $newStoreVariable = $newStorePassword
        } | Out-Null

    $storeVariable = "CGE15_STORE_" + [guid]::NewGuid().ToString("N").ToUpperInvariant()
    $keyVariable = "CGE15_KEY_" + [guid]::NewGuid().ToString("N").ToUpperInvariant()
    $probeVariable = "CGE15_PROBE_" + [guid]::NewGuid().ToString("N").ToUpperInvariant()
    Invoke-KeytoolWithSecureEnvironment `
        -Arguments "-keypasswd -alias `"$KeyAlias`" -keystore `"$resolvedKeystore`" -storepass:env $storeVariable -keypass:env $keyVariable -new:env $probeVariable" `
        -SecureEnvironment @{
            $storeVariable = $newStorePassword
            $keyVariable = $newKeyPassword
            $probeVariable = $probePassword
        } | Out-Null
    Invoke-KeytoolWithSecureEnvironment `
        -Arguments "-keypasswd -alias `"$KeyAlias`" -keystore `"$resolvedKeystore`" -storepass:env $storeVariable -keypass:env $probeVariable -new:env $keyVariable" `
        -SecureEnvironment @{
            $storeVariable = $newStorePassword
            $keyVariable = $newKeyPassword
            $probeVariable = $probePassword
        } | Out-Null

    $bundle = New-E15SigningCredentialBundle `
        -OutputPath $resolvedNewCredential `
        -KeyAlias $KeyAlias `
        -KeystorePassword $newStorePassword `
        -KeyPassword $newKeyPassword `
        -RepositoryRoot $repoRoot
    $bundleCreated = $true

    $record = New-E15SigningCredentialRotationRecord `
        -OutputPath $resolvedRecord `
        -KeystorePath $resolvedKeystore `
        -CredentialPath $resolvedNewCredential `
        -KeyAlias $KeyAlias `
        -RepositoryRoot $repoRoot `
        -KeytoolPath $script:ResolvedKeytool
    $recordCreated = $true

    Write-Host "E15 signing credentials rotated and verified."
    Write-Host "Keystore SHA256: $($record.keystoreSha256)"
    Write-Host "DPAPI bundle SHA256: $($record.credentialFileSha256)"
    Write-Host "Rotation record SHA256: $($record.recordSha256)"
}
catch {
    if ($keystoreMutated) {
        Copy-Item -LiteralPath $resolvedBackup -Destination $resolvedKeystore -Force
    }
    if ($recordCreated -and (Test-Path -LiteralPath $resolvedRecord -PathType Leaf)) {
        Remove-Item -LiteralPath $resolvedRecord -Force
    }
    if ($bundleCreated -and (Test-Path -LiteralPath $resolvedNewCredential -PathType Leaf)) {
        Remove-Item -LiteralPath $resolvedNewCredential -Force
    }
    throw "E15 signing rotation failed; the original JKS was restored from the verified backup. $($_.Exception.Message)"
}
finally {
    $oldPassword = $null
    $newStorePassword = $null
    $newKeyPassword = $null
    $probePassword = $null
}
