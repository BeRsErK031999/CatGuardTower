[CmdletBinding()]
param(
    [string]$BaselineCommit = "28f7e88",
    [string]$OutputPath = "Builds\Android\baseline\CatGuardTowerDefense-0.1.0-universal.apk",
    [string]$ProvenancePath = "",
    [string]$KeystorePath = $env:CATGUARD_ANDROID_KEYSTORE_PATH,
    [string]$KeyAlias = $env:CATGUARD_ANDROID_KEY_ALIAS,
    [string]$SigningCredentialPath = $env:CATGUARD_SIGNING_CREDENTIAL_PATH,
    [string]$SigningCredentialRotationRecordPath = $env:CATGUARD_SIGNING_ROTATION_RECORD_PATH,
    [System.Security.SecureString]$KeystorePassword,
    [System.Security.SecureString]$KeyPassword,
    [string]$UnityPath,
    [string]$JavaTempRoot = $env:CATGUARD_JAVA_TEMP_ROOT,
    [switch]$NonInteractive
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "e15-baseline-build-diagnostics.ps1")
. (Join-Path $PSScriptRoot "e15-java-temp.ps1")
. (Join-Path $PSScriptRoot "e15-signing-credential-rotation.ps1")

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

function Test-PathInsideDirectory {
    param(
        [string]$CandidatePath,
        [string]$DirectoryPath
    )

    $directory = [IO.Path]::GetFullPath($DirectoryPath).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $candidate = [IO.Path]::GetFullPath($CandidatePath)
    return $candidate.StartsWith($directory, [StringComparison]::OrdinalIgnoreCase)
}

$script:RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$resolvedOutput = if ([IO.Path]::IsPathRooted($OutputPath)) {
    [IO.Path]::GetFullPath($OutputPath)
} else {
    [IO.Path]::GetFullPath((Join-Path $script:RepoRoot $OutputPath))
}
$resolvedProvenance = if ($ProvenancePath) {
    if ([IO.Path]::IsPathRooted($ProvenancePath)) {
        [IO.Path]::GetFullPath($ProvenancePath)
    }
    else {
        [IO.Path]::GetFullPath((Join-Path $script:RepoRoot $ProvenancePath))
    }
}
else {
    "$resolvedOutput.provenance.json"
}
$resolvedKeystore = if ($KeystorePath) { (Resolve-Path -LiteralPath $KeystorePath).Path } else { "" }
if (-not $resolvedKeystore -or -not (Test-Path -LiteralPath $resolvedKeystore -PathType Leaf)) {
    throw "Pass -KeystorePath or set CATGUARD_ANDROID_KEYSTORE_PATH."
}
if (-not $KeyAlias) {
    throw "Pass -KeyAlias or set CATGUARD_ANDROID_KEY_ALIAS."
}
if (Test-PathInsideDirectory -CandidatePath $resolvedKeystore -DirectoryPath $script:RepoRoot) {
    throw "Keystore must be stored outside the Git repository."
}
$signingRotation = Test-E15SigningCredentialRotationRecord `
    -RecordPath $SigningCredentialRotationRecordPath `
    -KeystorePath $resolvedKeystore `
    -CredentialPath $SigningCredentialPath `
    -KeyAlias $KeyAlias `
    -RepositoryRoot $script:RepoRoot
if (-not [bool]$signingRotation.passed) {
    throw "E15 signing credential rotation is incomplete: $($signingRotation.reasons -join ' ')"
}
if ($env:CATGUARD_ANDROID_KEYSTORE_PASSWORD `
    -or $env:CATGUARD_ANDROID_KEY_PASSWORD `
    -or $null -ne $KeystorePassword `
    -or $null -ne $KeyPassword) {
    throw "E15 signed builds accept passwords only from the hash-bound DPAPI credential bundle."
}
$unity = Resolve-UnityExecutable $UnityPath
$editorRoot = Split-Path -Parent $unity
$androidPlayer = Join-Path $editorRoot "Data\PlaybackEngines\AndroidPlayer"
$keytoolPath = Join-Path $androidPlayer "OpenJDK\bin\keytool.exe"
$signingCredentialAccess = Test-E15SigningCredentialAccess `
    -KeystorePath $resolvedKeystore `
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

$currentStatusBefore = @(& git -C $script:RepoRoot status --porcelain=v1)
if ($LASTEXITCODE -ne 0) {
    throw "Could not inspect the current Git working tree."
}
if ($currentStatusBefore.Count -gt 0) {
    throw "The current Git working tree must be clean before reproducing the release baseline."
}
$orchestratorGitHead = (& git -C $script:RepoRoot rev-parse HEAD).Trim()
$orchestratorGitBranch = (& git -C $script:RepoRoot branch --show-current).Trim()
$baselineCommitResolved = (& git -C $script:RepoRoot rev-parse "$BaselineCommit^{commit}").Trim()
if ($LASTEXITCODE -ne 0 -or $baselineCommitResolved -notmatch '^[0-9a-f]{40}$') {
    throw "Could not resolve baseline commit: $BaselineCommit"
}

if (Test-Path -LiteralPath $resolvedProvenance -PathType Leaf) {
    Remove-Item -LiteralPath $resolvedProvenance -Force
}

$keystorePasswordPlain = ConvertTo-PlainText $credentialBundle.keystorePassword
$keyPasswordPlain = ConvertTo-PlainText $credentialBundle.keyPassword

$phase11Source = @(& git -C $script:RepoRoot show "$baselineCommitResolved`:Assets/Editor/ProjectSetup/Phase11ProjectSetup.cs") -join [Environment]::NewLine
if ($LASTEXITCODE -ne 0 `
    -or $phase11Source -notmatch 'StoreVersionName\s*=\s*"0\.1\.0"' `
    -or $phase11Source -notmatch 'StoreVersionCode\s*=\s*1') {
    throw "Baseline commit $BaselineCommit is not the expected 0.1.0 (1) store source."
}

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
    TEMP = [Environment]::GetEnvironmentVariable("TEMP", "Process")
    TMP = [Environment]::GetEnvironmentVariable("TMP", "Process")
}
$worktreeAdded = $false
$buildStartedAtUtc = (Get-Date).ToUniversalTime()
$buildCompletedAtUtc = $null
$sourceWorktreeCleanBefore = $false
$sourceWorktreeCleanAfter = $false
$sourceProjectSettingsSha256Before = ""
$sourceProjectSettingsSha256After = ""
$baselineAabSha256 = ""
$baselineAabBytes = 0L
$apksSha256 = ""
$apksBytes = 0L
$artifactSha256 = ""
$artifactBytes = 0L
$bundletoolSha256 = (Get-FileHash -LiteralPath $bundletool -Algorithm SHA256).Hash
$unityVersion = ((Get-Content -LiteralPath (Join-Path $script:RepoRoot "ProjectSettings\ProjectVersion.txt") -Encoding UTF8 | Select-Object -First 1) -replace '^m_EditorVersion:\s*', '').Trim()
$diagnosticRoot = Join-Path $script:RepoRoot "Builds\Android\logs\e15-baseline"
$effectiveJavaTempRoot = if ($JavaTempRoot) { $JavaTempRoot } else { "C:\cgjtmp" }
$javaTempDirectory = $null

try {
    & git -C $script:RepoRoot worktree add --detach $worktreePath $baselineCommitResolved
    if ($LASTEXITCODE -ne 0) { throw "Could not create the temporary baseline worktree." }
    $worktreeAdded = $true

    $worktreeHead = (& git -C $worktreePath rev-parse HEAD).Trim()
    $worktreeStatusBefore = @(& git -C $worktreePath status --porcelain=v1)
    $sourceWorktreeCleanBefore = $LASTEXITCODE -eq 0 `
        -and $worktreeHead -eq $baselineCommitResolved `
        -and $worktreeStatusBefore.Count -eq 0
    if (-not $sourceWorktreeCleanBefore) {
        throw "The detached baseline source worktree is not clean or is on the wrong commit."
    }
    $sourceProjectSettingsPath = Join-Path $worktreePath "ProjectSettings\ProjectSettings.asset"
    $sourceProjectSettingsSha256Before = (Get-FileHash -LiteralPath $sourceProjectSettingsPath -Algorithm SHA256).Hash

    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEYSTORE_PATH", $resolvedKeystore, "Process")
    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEYSTORE_PASSWORD", $keystorePasswordPlain, "Process")
    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEY_ALIAS", $KeyAlias, "Process")
    [Environment]::SetEnvironmentVariable("CATGUARD_ANDROID_KEY_PASSWORD", $keyPasswordPlain, "Process")
    $javaTempDirectory = New-E15JavaTempDirectory -Root $effectiveJavaTempRoot
    [Environment]::SetEnvironmentVariable("TEMP", $javaTempDirectory.path, "Process")
    [Environment]::SetEnvironmentVariable("TMP", $javaTempDirectory.path, "Process")

    $baselineBuilder = Join-Path $worktreePath "tools\android\build-signed-store-aab.ps1"
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $baselineBuilder -NonInteractive -UnityPath $unity
    if ($LASTEXITCODE -ne 0) {
        $diagnostics = Copy-E15BaselineBuildDiagnostics `
            -SourceDirectory (Join-Path $worktreePath "Builds\Android\logs") `
            -DestinationRoot $diagnosticRoot `
            -SensitiveValues @($keystorePasswordPlain, $keyPasswordPlain)
        if ([bool]$diagnostics.passed) {
            throw "The 0.1.0 baseline AAB build failed. Preserved diagnostics: $($diagnostics.runRoot)"
        }
        throw "The 0.1.0 baseline AAB build failed. $($diagnostics.reason)"
    }

    $baselineAab = Join-Path $worktreePath "Builds\Android\CatGuardTowerDefense-store.aab"
    if (-not (Test-Path -LiteralPath $baselineAab -PathType Leaf)) {
        throw "The baseline AAB was not created."
    }
    $baselineAabFile = Get-Item -LiteralPath $baselineAab
    $baselineAabSha256 = (Get-FileHash -LiteralPath $baselineAab -Algorithm SHA256).Hash
    $baselineAabBytes = $baselineAabFile.Length

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
    $apksFile = Get-Item -LiteralPath $apksPath
    $apksSha256 = (Get-FileHash -LiteralPath $apksPath -Algorithm SHA256).Hash
    $apksBytes = $apksFile.Length

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
    $artifactSha256 = $hash.Hash
    $artifactBytes = $artifact.Length
    $sourceProjectSettingsSha256After = (Get-FileHash -LiteralPath $sourceProjectSettingsPath -Algorithm SHA256).Hash
    $worktreeStatusAfter = @(& git -C $worktreePath status --porcelain=v1)
    $sourceWorktreeCleanAfter = $LASTEXITCODE -eq 0 `
        -and $worktreeStatusAfter.Count -eq 0 `
        -and $sourceProjectSettingsSha256After -eq $sourceProjectSettingsSha256Before
    if (-not $sourceWorktreeCleanAfter) {
        throw "The baseline build did not leave the detached source worktree clean and restored."
    }
    $buildCompletedAtUtc = (Get-Date).ToUniversalTime()
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
    if ($null -ne $javaTempDirectory) {
        Remove-E15JavaTempDirectory -Path $javaTempDirectory.path -Root $effectiveJavaTempRoot
    }
}

$currentStatusAfter = @(& git -C $script:RepoRoot status --porcelain=v1)
if ($LASTEXITCODE -ne 0 -or $currentStatusAfter.Count -gt 0) {
    throw "The baseline build changed the current Git working tree. Provenance was not written."
}
$orchestratorGitHeadAfter = (& git -C $script:RepoRoot rev-parse HEAD).Trim()
if ($orchestratorGitHeadAfter -ne $orchestratorGitHead) {
    throw "The current Git HEAD changed during the baseline build. Provenance was not written."
}
if (-not $buildCompletedAtUtc `
    -or -not $sourceWorktreeCleanBefore `
    -or -not $sourceWorktreeCleanAfter `
    -or -not (Test-Path -LiteralPath $resolvedOutput -PathType Leaf)) {
    throw "The baseline build did not complete its provenance contract."
}

$provenanceFolder = Split-Path -Parent $resolvedProvenance
New-Item -ItemType Directory -Force -Path $provenanceFolder | Out-Null
$provenance = [pscustomobject]@{
    schemaVersion = 1
    passed = $true
    artifact = "BaselineUniversalApk"
    artifactFileName = [IO.Path]::GetFileName($resolvedOutput)
    artifactSha256 = $artifactSha256
    artifactBytes = $artifactBytes
    packageName = "com.berserk031999.catguardtower"
    versionName = "0.1.0"
    versionCode = 1
    baselineCommitRequested = $BaselineCommit
    baselineCommitResolved = $baselineCommitResolved
    orchestratorGitHeadBefore = $orchestratorGitHead
    orchestratorGitHeadAfter = $orchestratorGitHeadAfter
    orchestratorGitBranch = $orchestratorGitBranch
    currentWorkingTreeCleanBefore = $currentStatusBefore.Count -eq 0
    currentWorkingTreeCleanAfter = $currentStatusAfter.Count -eq 0
    sourceWorktreeCleanBefore = $sourceWorktreeCleanBefore
    sourceWorktreeCleanAfter = $sourceWorktreeCleanAfter
    sourceProjectSettingsRestored = $sourceProjectSettingsSha256Before -eq $sourceProjectSettingsSha256After
    sourceProjectSettingsSha256Before = $sourceProjectSettingsSha256Before
    sourceProjectSettingsSha256After = $sourceProjectSettingsSha256After
    baselineAabSha256 = $baselineAabSha256
    baselineAabBytes = $baselineAabBytes
    apksSha256 = $apksSha256
    apksBytes = $apksBytes
    transformation = "bundletool build-apks --mode universal"
    bundletoolSha256 = $bundletoolSha256
    unityVersion = $unityVersion
    buildStartedAtUtc = $buildStartedAtUtc.ToString("o")
    buildCompletedAtUtc = $buildCompletedAtUtc.ToString("o")
}
$provenance | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $resolvedProvenance -Encoding UTF8
Write-Host "Baseline provenance: $resolvedProvenance"
