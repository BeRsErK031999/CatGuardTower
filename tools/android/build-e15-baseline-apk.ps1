[CmdletBinding()]
param(
    [string]$BaselineCommit = "28f7e88",
    [string]$OutputPath = "Builds\Android\baseline\CatGuardTowerDefense-0.1.0-universal.apk",
    [string]$KeystorePath = $env:CATGUARD_ANDROID_KEYSTORE_PATH,
    [string]$KeyAlias = $env:CATGUARD_ANDROID_KEY_ALIAS,
    [System.Security.SecureString]$KeystorePassword,
    [System.Security.SecureString]$KeyPassword,
    [string]$UnityPath,
    [switch]$NonInteractive
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function ConvertTo-PlainText {
    param([System.Security.SecureString]$Value)

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Resolve-UnityExecutable {
    param([string]$RequestedPath)

    if ($RequestedPath) {
        return (Resolve-Path -LiteralPath $RequestedPath).Path
    }

    $versionLine = Get-Content -LiteralPath (Join-Path $script:RepoRoot "ProjectSettings\ProjectVersion.txt") -Encoding UTF8 | Select-Object -First 1
    $version = ($versionLine -replace '^m_EditorVersion:\s*', '').Trim()
    $candidate = Join-Path $env:LOCALAPPDATA "Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "Unity $version was not found. Pass -UnityPath explicitly."
    }
    return (Resolve-Path -LiteralPath $candidate).Path
}

function Assert-SafeTemporaryWorktree {
    param([string]$Path)

    $resolvedTemp = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $resolvedPath = [IO.Path]::GetFullPath($Path)
    if (-not $resolvedPath.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase) `
        -or -not ([IO.Path]::GetFileName($resolvedPath)).StartsWith("CGE15-", [StringComparison]::Ordinal)) {
        throw "Refusing to clean an unexpected baseline worktree path: $resolvedPath"
    }
}

$script:RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$resolvedOutput = if ([IO.Path]::IsPathRooted($OutputPath)) {
    [IO.Path]::GetFullPath($OutputPath)
} else {
    [IO.Path]::GetFullPath((Join-Path $script:RepoRoot $OutputPath))
}
$resolvedKeystore = if ($KeystorePath) { (Resolve-Path -LiteralPath $KeystorePath).Path } else { "" }
if (-not $resolvedKeystore -or -not (Test-Path -LiteralPath $resolvedKeystore -PathType Leaf)) {
    throw "Pass -KeystorePath or set CATGUARD_ANDROID_KEYSTORE_PATH."
}
if (-not $KeyAlias) {
    throw "Pass -KeyAlias or set CATGUARD_ANDROID_KEY_ALIAS."
}

$keystorePasswordPlain = $env:CATGUARD_ANDROID_KEYSTORE_PASSWORD
$keyPasswordPlain = $env:CATGUARD_ANDROID_KEY_PASSWORD
if (-not $keystorePasswordPlain) {
    if (-not $KeystorePassword) {
        if ($NonInteractive) { throw "Set CATGUARD_ANDROID_KEYSTORE_PASSWORD." }
        $KeystorePassword = Read-Host "Android keystore password" -AsSecureString
    }
    $keystorePasswordPlain = ConvertTo-PlainText $KeystorePassword
}
if (-not $keyPasswordPlain) {
    if (-not $KeyPassword) {
        if ($NonInteractive) { throw "Set CATGUARD_ANDROID_KEY_PASSWORD." }
        $KeyPassword = Read-Host "Android key password" -AsSecureString
    }
    $keyPasswordPlain = ConvertTo-PlainText $KeyPassword
}

$phase11Source = @(& git -C $script:RepoRoot show "$BaselineCommit`:Assets/Editor/ProjectSetup/Phase11ProjectSetup.cs") -join [Environment]::NewLine
if ($LASTEXITCODE -ne 0 `
    -or $phase11Source -notmatch 'StoreVersionName\s*=\s*"0\.1\.0"' `
    -or $phase11Source -notmatch 'StoreVersionCode\s*=\s*1') {
    throw "Baseline commit $BaselineCommit is not the expected 0.1.0 (1) store source."
}

$unity = Resolve-UnityExecutable $UnityPath
$editorRoot = Split-Path -Parent $unity
$androidPlayer = Join-Path $editorRoot "Data\PlaybackEngines\AndroidPlayer"
$java = Join-Path $androidPlayer "OpenJDK\bin\java.exe"
$bundletool = Get-ChildItem `
    -LiteralPath $androidPlayer `
    -Recurse `
    -File `
    -Filter "bundletool*.jar" `
    -ErrorAction SilentlyContinue |
    Select-Object -First 1 -ExpandProperty FullName
if (-not (Test-Path -LiteralPath $java -PathType Leaf) -or -not $bundletool) {
    throw "Unity Android Java/bundletool was not found."
}

$worktreePath = Join-Path $env:TEMP ("CGE15-" + [Guid]::NewGuid().ToString("N").Substring(0, 8))
Assert-SafeTemporaryWorktree $worktreePath
$keystorePassFile = Join-Path $env:TEMP ("CatGuardTower-E15-KsPass-" + [Guid]::NewGuid().ToString("N") + ".txt")
$keyPassFile = Join-Path $env:TEMP ("CatGuardTower-E15-KeyPass-" + [Guid]::NewGuid().ToString("N") + ".txt")
$originalEnvironment = @{
    CATGUARD_ANDROID_KEYSTORE_PATH = [Environment]::GetEnvironmentVariable("CATGUARD_ANDROID_KEYSTORE_PATH", "Process")
    CATGUARD_ANDROID_KEYSTORE_PASSWORD = [Environment]::GetEnvironmentVariable("CATGUARD_ANDROID_KEYSTORE_PASSWORD", "Process")
    CATGUARD_ANDROID_KEY_ALIAS = [Environment]::GetEnvironmentVariable("CATGUARD_ANDROID_KEY_ALIAS", "Process")
    CATGUARD_ANDROID_KEY_PASSWORD = [Environment]::GetEnvironmentVariable("CATGUARD_ANDROID_KEY_PASSWORD", "Process")
}
$worktreeAdded = $false

try {
    & git -C $script:RepoRoot worktree add --detach $worktreePath $BaselineCommit
    if ($LASTEXITCODE -ne 0) { throw "Could not create the temporary baseline worktree." }
    $worktreeAdded = $true

    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEYSTORE_PATH", $resolvedKeystore, "Process")
    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEYSTORE_PASSWORD", $keystorePasswordPlain, "Process")
    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEY_ALIAS", $KeyAlias, "Process")
    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEY_PASSWORD", $keyPasswordPlain, "Process")

    $baselineBuilder = Join-Path $worktreePath "tools\android\build-signed-store-aab.ps1"
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $baselineBuilder -NonInteractive -UnityPath $unity
    if ($LASTEXITCODE -ne 0) { throw "The 0.1.0 baseline AAB build failed." }

    $baselineAab = Join-Path $worktreePath "Builds\Android\CatGuardTowerDefense-store.aab"
    if (-not (Test-Path -LiteralPath $baselineAab -PathType Leaf)) {
        throw "The baseline AAB was not created."
    }

    [IO.File]::WriteAllText($keystorePassFile, $keystorePasswordPlain, [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($keyPassFile, $keyPasswordPlain, [Text.UTF8Encoding]::new($false))
    $apksPath = Join-Path $worktreePath "Builds\Android\CatGuardTowerDefense-0.1.0.apks"
    & $java -jar $bundletool build-apks `
        "--bundle=$baselineAab" `
        "--output=$apksPath" `
        "--mode=universal" `
        "--ks=$resolvedKeystore" `
        "--ks-key-alias=$KeyAlias" `
        "--ks-pass=file:$keystorePassFile" `
        "--key-pass=file:$keyPassFile"
    if ($LASTEXITCODE -ne 0) { throw "bundletool could not create the universal baseline APK set." }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $extractPath = Join-Path $worktreePath "Builds\Android\baseline-extracted"
    [IO.Compression.ZipFile]::ExtractToDirectory($apksPath, $extractPath)
    $universalApk = Join-Path $extractPath "universal.apk"
    if (-not (Test-Path -LiteralPath $universalApk -PathType Leaf)) {
        throw "bundletool output does not contain universal.apk."
    }

    $outputFolder = Split-Path -Parent $resolvedOutput
    New-Item -ItemType Directory -Force -Path $outputFolder | Out-Null
    Copy-Item -LiteralPath $universalApk -Destination $resolvedOutput -Force
    $artifact = Get-Item -LiteralPath $resolvedOutput
    $hash = Get-FileHash -LiteralPath $resolvedOutput -Algorithm SHA256
    Write-Host "E15 baseline APK: $($artifact.FullName) ($($artifact.Length) bytes)"
    Write-Host "SHA256: $($hash.Hash)"
} finally {
    foreach ($name in $originalEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $originalEnvironment[$name], "Process")
    }
    $keystorePasswordPlain = $null
    $keyPasswordPlain = $null

    foreach ($passwordFile in @($keystorePassFile, $keyPassFile)) {
        if (Test-Path -LiteralPath $passwordFile -PathType Leaf) {
            Remove-Item -LiteralPath $passwordFile -Force
        }
    }

    if ($worktreeAdded) {
        Assert-SafeTemporaryWorktree $worktreePath
        & git -c core.longpaths=true -C $script:RepoRoot worktree remove --force $worktreePath | Out-Null
        if ($LASTEXITCODE -ne 0 -and [IO.Directory]::Exists($worktreePath)) {
            [IO.Directory]::Delete("\\?\" + [IO.Path]::GetFullPath($worktreePath), $true)
        }

        & git -C $script:RepoRoot worktree prune | Out-Null
        if ($LASTEXITCODE -ne 0 -or [IO.Directory]::Exists($worktreePath)) {
            throw "Could not clean the verified baseline worktree: $worktreePath"
        }
    }
}
