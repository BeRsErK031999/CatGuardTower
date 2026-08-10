[CmdletBinding()]
param(
    [ValidateSet("Aab", "Apk", "CaptureApk")]
    [string]$Artifact = "Aab",
    [string]$KeystorePath = $env:CATGUARD_ANDROID_KEYSTORE_PATH,
    [string]$KeyAlias = $env:CATGUARD_ANDROID_KEY_ALIAS,
    [System.Security.SecureString]$KeystorePassword,
    [System.Security.SecureString]$KeyPassword,
    [string]$UnityPath,
    [switch]$NonInteractive,
    [switch]$AllowDirtyWorkingTree
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$environmentNames = @(
    "CATGUARD_ANDROID_KEYSTORE_PATH",
    "CATGUARD_ANDROID_KEYSTORE_PASSWORD",
    "CATGUARD_ANDROID_KEY_ALIAS",
    "CATGUARD_ANDROID_KEY_PASSWORD"
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

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$projectSettingsPath = Join-Path $repoRoot "ProjectSettings\ProjectSettings.asset"
$projectSettingsSnapshot = [IO.File]::ReadAllBytes($projectSettingsPath)
$resolvedKeystorePath = $null
$keystorePasswordPlain = $env:CATGUARD_ANDROID_KEYSTORE_PASSWORD
$keyPasswordPlain = $env:CATGUARD_ANDROID_KEY_PASSWORD
$originalEnvironment = @{}

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

    if (-not $keystorePasswordPlain) {
        if (-not $KeystorePassword) {
            if ($NonInteractive) {
                throw "Set CATGUARD_ANDROID_KEYSTORE_PASSWORD for a non-interactive build."
            }

            $KeystorePassword = Read-Host "Android keystore password" -AsSecureString
        }

        $keystorePasswordPlain = ConvertTo-PlainText -Value $KeystorePassword
    }

    if (-not $keyPasswordPlain) {
        if (-not $KeyPassword) {
            if ($NonInteractive) {
                throw "Set CATGUARD_ANDROID_KEY_PASSWORD for a non-interactive build."
            }

            $KeyPassword = Read-Host "Android key password" -AsSecureString
        }

        $keyPasswordPlain = ConvertTo-PlainText -Value $KeyPassword
    }

    if (-not $AllowDirtyWorkingTree) {
        Push-Location $repoRoot
        try {
            $trackedChanges = @(& git status --porcelain --untracked-files=no)
            if ($LASTEXITCODE -ne 0) {
                throw "Could not inspect the Git working tree."
            }

            if ($trackedChanges.Count -gt 0) {
                throw "Tracked files are modified. Commit or stash them before building a store AAB."
            }
        }
        finally {
            Pop-Location
        }
    }

    $resolvedUnityPath = Resolve-UnityExecutable -RequestedPath $UnityPath -ProjectRoot $repoRoot
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

    if (Test-Path -LiteralPath $outputPath) {
        Remove-Item -LiteralPath $outputPath -Force
    }

    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEYSTORE_PATH", $resolvedKeystorePath, "Process")
    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEYSTORE_PASSWORD", $keystorePasswordPlain, "Process")
    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEY_ALIAS", $KeyAlias.Trim(), "Process")
    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEY_PASSWORD", $keyPasswordPlain, "Process")

    Write-Host "Building signed store $($Artifact.ToUpperInvariant()) with Unity..."
    Write-Host "Output: $outputPath"
    Write-Host "Log: $logPath"

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

    $artifactFile = Get-Item -LiteralPath $outputPath
    Write-Host "Signed store $Artifact created: $($artifactFile.FullName) ($($artifactFile.Length) bytes)"
}
finally {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $originalEnvironment[$name], "Process")
    }

    if ($null -ne $projectSettingsSnapshot) {
        [IO.File]::WriteAllBytes($projectSettingsPath, $projectSettingsSnapshot)
    }

    $keystorePasswordPlain = $null
    $keyPasswordPlain = $null
}
