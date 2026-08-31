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
    [string]$JavaTempRoot = $env:CATGUARD_JAVA_TEMP_ROOT,
    [switch]$NonInteractive,
    [switch]$AllowDirtyWorkingTree
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "e15-build-log-redaction.ps1")
. (Join-Path $PSScriptRoot "e15-java-temp.ps1")
. (Join-Path $PSScriptRoot "e15-signing-credential-rotation.ps1")

$environmentNames = @(
    "CATGUARD_ANDROID_KEYSTORE_PATH",
    "CATGUARD_ANDROID_KEYSTORE_PASSWORD",
    "CATGUARD_ANDROID_KEY_ALIAS",
    "CATGUARD_ANDROID_KEY_PASSWORD",
    "TEMP",
    "TMP"
)

function ConvertTo-PlainText {
    param([System.Security.SecureString]$Value)

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Resolve-UnityExecutable {
    param(
        [string]$RequestedPath,
        [string]$ProjectRoot
    )

    if ($RequestedPath) {
        $resolved = (Resolve-Path -LiteralPath $RequestedPath).Path
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            throw "Unity executable was not found: $resolved"
        }

        return $resolved
    }

    $versionFile = Join-Path $ProjectRoot "ProjectSettings\ProjectVersion.txt"
    $versionLine = Get-Content -LiteralPath $versionFile -Encoding UTF8 | Select-Object -First 1
    if ($versionLine -notmatch "^m_EditorVersion:\s+(.+)$") {
        throw "Could not read the Unity editor version from $versionFile"
    }

    $version = $Matches[1].Trim()
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Unity\Hub\Editor\$version\Editor\Unity.exe"),
        (Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return $candidate
        }
    }

    throw "Unity $version was not found in the supported Unity Hub locations. Pass -UnityPath explicitly."
}

function Test-PathInsideDirectory {
    param(
        [string]$CandidatePath,
        [string]$DirectoryPath
    )

    $directory = [IO.Path]::GetFullPath($DirectoryPath).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $candidate = [IO.Path]::GetFullPath($CandidatePath)
    return $candidate.StartsWith($directory, [StringComparison]::OrdinalIgnoreCase)
}

function Get-RepositoryRelativePath {
    param(
        [string]$Path,
        [string]$RepositoryRoot
    )

    $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path is outside the Git repository: $fullPath"
    }

    return $fullPath.Substring($root.Length).Replace('\', '/')
}

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$projectSettingsPath = Join-Path $repoRoot "ProjectSettings\ProjectSettings.asset"
$projectSettingsSnapshot = [IO.File]::ReadAllBytes($projectSettingsPath)
$resolvedKeystorePath = $null
$keystorePasswordPlain = ""
$keyPasswordPlain = ""
$originalEnvironment = @{}
$buildStartedAtUtc = $null
$buildCompletedAtUtc = $null
$outputPath = $null
$provenancePath = $null
$logPath = $null
$executeMethod = $null
$unityVersion = $null
$gitHeadBefore = $null
$gitBranch = $null
$cleanWorkingTreeBefore = $false
$projectSettingsSha256Before = (Get-FileHash -LiteralPath $projectSettingsPath -Algorithm SHA256).Hash
$effectiveJavaTempRoot = if ($JavaTempRoot) { $JavaTempRoot } else { "C:\cgjtmp" }
$javaTempDirectory = $null

foreach ($name in $environmentNames) {
    $originalEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
}

try {
    if (-not $KeystorePath) {
        throw "Pass -KeystorePath or set CATGUARD_ANDROID_KEYSTORE_PATH."
    }

    $resolvedKeystorePath = (Resolve-Path -LiteralPath $KeystorePath).Path
    if (-not (Test-Path -LiteralPath $resolvedKeystorePath -PathType Leaf)) {
        throw "Keystore file was not found: $resolvedKeystorePath"
    }

    if (Test-PathInsideDirectory -CandidatePath $resolvedKeystorePath -DirectoryPath $repoRoot) {
        throw "Keystore must be stored outside the Git repository."
    }

    if (-not $KeyAlias) {
        throw "Pass -KeyAlias or set CATGUARD_ANDROID_KEY_ALIAS."
    }

    $signingRotation = Test-E15SigningCredentialRotationRecord `
        -RecordPath $SigningCredentialRotationRecordPath `
        -KeystorePath $resolvedKeystorePath `
        -CredentialPath $SigningCredentialPath `
        -KeyAlias $KeyAlias `
        -RepositoryRoot $repoRoot
    if (-not [bool]$signingRotation.passed) {
        throw "E15 signing credential rotation is incomplete: $($signingRotation.reasons -join ' ')"
    }
    if ($env:CATGUARD_ANDROID_KEYSTORE_PASSWORD `
        -or $env:CATGUARD_ANDROID_KEY_PASSWORD `
        -or $null -ne $KeystorePassword `
        -or $null -ne $KeyPassword) {
        throw "E15 signed builds accept passwords only from the hash-bound DPAPI credential bundle."
    }

    $resolvedUnityPath = Resolve-UnityExecutable -RequestedPath $UnityPath -ProjectRoot $repoRoot
    $keytoolPath = Join-Path (Split-Path -Parent $resolvedUnityPath) "Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe"
    $signingCredentialAccess = Test-E15SigningCredentialAccess `
        -KeystorePath $resolvedKeystorePath `
        -CredentialPath $SigningCredentialPath `
        -KeyAlias $KeyAlias `
        -KeytoolPath $keytoolPath `
        -ExpectedCertificateSha256 (Get-E15SigningCredentialRotationPolicy).certificateSha256 `
        -ExpectedKeytoolSha256 $signingRotation.keytoolSha256
    if (-not [bool]$signingCredentialAccess.passed) {
        throw "E15 signing DPAPI credential or keytool binding is invalid: $($signingCredentialAccess.reasons -join ' ')"
    }
    $credentialBundle = Get-E15SigningCredentialBundle `
        -CredentialPath $SigningCredentialPath `
        -ExpectedKeyAlias $KeyAlias
    if (-not [bool]$credentialBundle.passed) {
        throw "E15 signing DPAPI credential bundle is invalid: $($credentialBundle.reasons -join ' ')"
    }
    $keystorePasswordPlain = ConvertTo-PlainText -Value $credentialBundle.keystorePassword
    $keyPasswordPlain = ConvertTo-PlainText -Value $credentialBundle.keyPassword

    $gitStatusBefore = @(& git -C $repoRoot status --porcelain=v1)
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect the Git working tree."
    }
    $cleanWorkingTreeBefore = $gitStatusBefore.Count -eq 0
    if (-not $AllowDirtyWorkingTree -and -not $cleanWorkingTreeBefore) {
        throw "The Git working tree is not clean. Commit or stash tracked and untracked source files before building a store artifact."
    }

    $gitHeadBefore = (& git -C $repoRoot rev-parse HEAD).Trim()
    $gitBranch = (& git -C $repoRoot branch --show-current).Trim()

    $unityVersion = ((Get-Content -LiteralPath (Join-Path $repoRoot "ProjectSettings\ProjectVersion.txt") -Encoding UTF8 | Select-Object -First 1) -replace '^m_EditorVersion:\s*', '').Trim()
    $logDirectory = Join-Path $repoRoot "Builds\Android\logs"
    New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $artifactLabel = $Artifact.ToLowerInvariant()
    $executeMethod = switch ($Artifact) {
        "Aab" { "Phase11ProjectSetup.BuildSignedAab" }
        "Apk" { "Phase11ProjectSetup.BuildSignedApk" }
        "CaptureApk" { "Phase11ProjectSetup.BuildSignedStoreCaptureApk" }
    }
    $outputFileName = switch ($Artifact) {
        "Aab" { "CatGuardTowerDefense-store.aab" }
        "Apk" { "CatGuardTowerDefense-store.apk" }
        "CaptureApk" { "CatGuardTowerDefense-store-capture-x86_64.apk" }
    }
    $logPath = Join-Path $logDirectory "signed-store-$artifactLabel-$timestamp.log"
    $outputPath = Join-Path $repoRoot "Builds\Android\$outputFileName"
    $provenancePath = "$outputPath.provenance.json"

    if (Test-Path -LiteralPath $outputPath) {
        Remove-Item -LiteralPath $outputPath -Force
    }
    if (Test-Path -LiteralPath $provenancePath) {
        Remove-Item -LiteralPath $provenancePath -Force
    }

    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEYSTORE_PATH", $resolvedKeystorePath, "Process")
    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEYSTORE_PASSWORD", $keystorePasswordPlain, "Process")
    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEY_ALIAS", $KeyAlias.Trim(), "Process")
    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEY_PASSWORD", $keyPasswordPlain, "Process")
    $javaTempDirectory = New-E15JavaTempDirectory -Root $effectiveJavaTempRoot
    [Environment]::SetEnvironmentVariable("TEMP", $javaTempDirectory.path, "Process")
    [Environment]::SetEnvironmentVariable("TMP", $javaTempDirectory.path, "Process")

    Write-Host "Building signed store $($Artifact.ToUpperInvariant()) with Unity..."
    Write-Host "Output: $outputPath"
    Write-Host "Log: $logPath"
    $buildStartedAtUtc = (Get-Date).ToUniversalTime()

    $unityArguments = @(
        "-batchmode",
        "-nographics",
        "-quit",
        "-projectPath",
        ('"' + $repoRoot + '"'),
        "-executeMethod",
        $executeMethod,
        "-logFile",
        ('"' + $logPath + '"')
    )
    $unityProcess = Start-Process `
        -FilePath $resolvedUnityPath `
        -ArgumentList $unityArguments `
        -PassThru `
        -WindowStyle Hidden
    $unityProcess.WaitForExit()
    $unityExitCode = $unityProcess.ExitCode

    if ($unityExitCode -ne 0) {
        throw "Unity signed store $Artifact build failed with exit code $unityExitCode. See: $logPath"
    }

    if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
        throw "Unity reported success but the store $Artifact was not created: $outputPath"
    }

    $buildCompletedAtUtc = (Get-Date).ToUniversalTime()
    $artifactFile = Get-Item -LiteralPath $outputPath
    Write-Host "Signed store $Artifact created: $($artifactFile.FullName) ($($artifactFile.Length) bytes)"
}
finally {
    $cleanupFailure = ""
    if ($logPath -and (Test-Path -LiteralPath $logPath -PathType Leaf)) {
        try {
            $redaction = Protect-E15BuildLogFile `
                -Path $logPath `
                -SensitiveValues @($keystorePasswordPlain, $keyPasswordPlain)
            if (-not [bool]$redaction.passed) {
                throw $redaction.reason
            }
        }
        catch {
            [IO.File]::Delete($logPath)
            $cleanupFailure = "The Unity build log could not be redacted and was deleted: $($_.Exception.Message)"
        }
    }
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $originalEnvironment[$name], "Process")
    }

    if ($null -ne $projectSettingsSnapshot) {
        [IO.File]::WriteAllBytes($projectSettingsPath, $projectSettingsSnapshot)
    }

    $keystorePasswordPlain = $null
    $keyPasswordPlain = $null
    if ($null -ne $javaTempDirectory) {
        try {
            Remove-E15JavaTempDirectory -Path $javaTempDirectory.path -Root $effectiveJavaTempRoot
        }
        catch {
            if (-not $cleanupFailure) {
                $cleanupFailure = $_.Exception.Message
            }
        }
    }
    if ($cleanupFailure) {
        throw $cleanupFailure
    }
}

$projectSettingsSha256After = (Get-FileHash -LiteralPath $projectSettingsPath -Algorithm SHA256).Hash
$gitHeadAfter = (& git -C $repoRoot rev-parse HEAD).Trim()
$gitStatusAfter = @(& git -C $repoRoot status --porcelain=v1)
if ($LASTEXITCODE -ne 0) {
    throw "Could not inspect the Git working tree after the signed build."
}
$cleanWorkingTreeAfter = $gitStatusAfter.Count -eq 0
$projectSettingsRestored = $projectSettingsSha256Before -eq $projectSettingsSha256After
if ($gitHeadAfter -ne $gitHeadBefore) {
    throw "Git HEAD changed during the signed artifact build. Provenance was not written."
}
if (-not $projectSettingsRestored) {
    throw "ProjectSettings.asset was not restored after the signed artifact build. Provenance was not written."
}
if (-not $AllowDirtyWorkingTree -and -not $cleanWorkingTreeAfter) {
    throw "The signed artifact build left source changes in the Git working tree. Provenance was not written."
}

$artifactFile = Get-Item -LiteralPath $outputPath
$provenance = [pscustomobject]@{
    schemaVersion = 1
    passed = $true
    artifact = $Artifact
    artifactRelativePath = Get-RepositoryRelativePath -Path $outputPath -RepositoryRoot $repoRoot
    artifactSha256 = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash
    artifactBytes = $artifactFile.Length
    buildMethod = $executeMethod
    buildStartedAtUtc = $buildStartedAtUtc.ToString("o")
    buildCompletedAtUtc = $buildCompletedAtUtc.ToString("o")
    gitHeadBefore = $gitHeadBefore
    gitHeadAfter = $gitHeadAfter
    gitBranch = $gitBranch
    cleanWorkingTreeBefore = $cleanWorkingTreeBefore
    cleanWorkingTreeAfter = $cleanWorkingTreeAfter
    allowDirtyWorkingTree = [bool]$AllowDirtyWorkingTree
    projectSettingsRestored = $projectSettingsRestored
    projectSettingsSha256Before = $projectSettingsSha256Before
    projectSettingsSha256After = $projectSettingsSha256After
    unityVersion = $unityVersion
}
$provenance | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $provenancePath -Encoding UTF8
Write-Host "Build provenance: $provenancePath"
