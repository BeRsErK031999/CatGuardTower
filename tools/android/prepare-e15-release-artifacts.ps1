[CmdletBinding()]
param(
    [string]$BaselineCommit = "28f7e88",
    [string]$KeystorePath = $env:CATGUARD_ANDROID_KEYSTORE_PATH,
    [string]$KeyAlias = $env:CATGUARD_ANDROID_KEY_ALIAS,
    [System.Security.SecureString]$KeystorePassword,
    [System.Security.SecureString]$KeyPassword,
    [string]$UnityPath,
    [string]$OutputDir = "Builds\Android\qa-device\e15-artifact-set",
    [switch]$NonInteractive,
    [switch]$PreflightOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
. (Join-Path $PSScriptRoot "e15-artifact-set-manifest.ps1")
$script:Preconditions = New-Object System.Collections.Generic.List[object]

function Add-Precondition {
    param(
        [string]$Id,
        [bool]$Passed,
        [string]$Expected,
        [string]$Actual
    )

    $script:Preconditions.Add([pscustomobject]@{
        id = $Id
        passed = $Passed
        expected = $Expected
        actual = $Actual
    })
}

function Test-PathInsideDirectory {
    param(
        [string]$CandidatePath,
        [string]$DirectoryPath
    )

    if (-not $CandidatePath -or -not $DirectoryPath) {
        return $false
    }
    $directory = [IO.Path]::GetFullPath($DirectoryPath).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $candidate = [IO.Path]::GetFullPath($CandidatePath)
    return $candidate.StartsWith($directory, [StringComparison]::OrdinalIgnoreCase)
}

function Resolve-UnityExecutable {
    param([string]$RequestedPath)

    if ($RequestedPath) {
        $resolved = (Resolve-Path -LiteralPath $RequestedPath).Path
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            throw "Unity executable was not found: $resolved"
        }
        return $resolved
    }

    $versionLine = Get-Content -LiteralPath (Join-Path $script:RepoRoot "ProjectSettings\ProjectVersion.txt") -Encoding UTF8 |
        Select-Object -First 1
    $version = ($versionLine -replace '^m_EditorVersion:\s*', '').Trim()
    foreach ($candidate in @(
        (Join-Path $env:LOCALAPPDATA "Unity\Hub\Editor\$version\Editor\Unity.exe"),
        (Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe")
    )) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }
    throw "Unity $version executable was not found."
}

function Assert-SourceUnchanged {
    param(
        [string]$ExpectedHead,
        [string]$Context
    )

    $actualHead = (& git -C $script:RepoRoot rev-parse HEAD).Trim()
    $status = @(& git -C $script:RepoRoot status --porcelain=v1)
    if ($LASTEXITCODE -ne 0 -or $actualHead -ne $ExpectedHead -or $status.Count -gt 0) {
        throw "E15 artifact preparation changed source state after $Context. Expected clean HEAD $ExpectedHead."
    }
}

$gitStatus = @(& git -C $script:RepoRoot status --porcelain=v1)
$gitStatusReadable = $LASTEXITCODE -eq 0
$head = (& git -C $script:RepoRoot rev-parse HEAD).Trim()
$branch = (& git -C $script:RepoRoot branch --show-current).Trim()
$upstreamResult = @(& git -C $script:RepoRoot rev-parse '@{upstream}' 2>$null)
$upstream = if ($LASTEXITCODE -eq 0) { ($upstreamResult -join "").Trim() } else { "missing" }
$baselineCommitResolved = (& git -C $script:RepoRoot rev-parse "$BaselineCommit^{commit}" 2>$null)
$baselineCommitResolved = if ($LASTEXITCODE -eq 0) { ($baselineCommitResolved -join "").Trim() } else { "missing" }

Add-Precondition `
    -Id "clean-worktree" `
    -Passed:($gitStatusReadable -and $gitStatus.Count -eq 0) `
    -Expected "clean tracked and untracked source" `
    -Actual $(if ($gitStatusReadable -and $gitStatus.Count -eq 0) { "clean" } else { "$($gitStatus.Count) change(s)" })
Add-Precondition `
    -Id "upstream-sync" `
    -Passed:($upstream -ne "missing" -and $head -eq $upstream) `
    -Expected "HEAD equals configured upstream" `
    -Actual "HEAD=$head upstream=$upstream"
Add-Precondition `
    -Id "baseline-commit" `
    -Passed:($baselineCommitResolved -match '^[0-9a-f]{40}$') `
    -Expected "resolvable historical baseline commit" `
    -Actual $baselineCommitResolved
$baselinePhase11Source = if ($baselineCommitResolved -match '^[0-9a-f]{40}$') {
    @(& git -C $script:RepoRoot show "$baselineCommitResolved`:Assets/Editor/ProjectSetup/Phase11ProjectSetup.cs" 2>$null) -join [Environment]::NewLine
}
else {
    ""
}
$baselineIdentityMatches = $LASTEXITCODE -eq 0 `
    -and $baselinePhase11Source -match 'StoreVersionName\s*=\s*"0\.1\.0"' `
    -and $baselinePhase11Source -match 'StoreVersionCode\s*=\s*1'
Add-Precondition `
    -Id "baseline-identity" `
    -Passed:$baselineIdentityMatches `
    -Expected "historical 0.1.0 (1) release source" `
    -Actual $(if ($baselineIdentityMatches) { "0.1.0 (1)" } else { "mismatch" })

$resolvedKeystore = ""
if ($KeystorePath) {
    try {
        $resolvedKeystore = (Resolve-Path -LiteralPath $KeystorePath).Path
    }
    catch {
        $resolvedKeystore = [IO.Path]::GetFullPath($KeystorePath)
    }
}
$keystoreReady = $resolvedKeystore `
    -and (Test-Path -LiteralPath $resolvedKeystore -PathType Leaf) `
    -and -not (Test-PathInsideDirectory -CandidatePath $resolvedKeystore -DirectoryPath $script:RepoRoot)
Add-Precondition `
    -Id "external-keystore" `
    -Passed:$keystoreReady `
    -Expected "existing keystore outside repository" `
    -Actual $(if ($resolvedKeystore) { $resolvedKeystore } else { "missing" })
Add-Precondition `
    -Id "key-alias" `
    -Passed:(-not [string]::IsNullOrWhiteSpace($KeyAlias)) `
    -Expected "non-empty signing key alias" `
    -Actual $(if ($KeyAlias) { "configured" } else { "missing" })

$unity = ""
try {
    $unity = Resolve-UnityExecutable -RequestedPath $UnityPath
}
catch {
    $unity = "missing"
}
Add-Precondition `
    -Id "unity" `
    -Passed:($unity -ne "missing" -and (Test-Path -LiteralPath $unity -PathType Leaf)) `
    -Expected "configured Unity executable" `
    -Actual $unity

$passwordsConfigured = ([bool]$env:CATGUARD_ANDROID_KEYSTORE_PASSWORD -or $null -ne $KeystorePassword) `
    -and ([bool]$env:CATGUARD_ANDROID_KEY_PASSWORD -or $null -ne $KeyPassword)
Add-Precondition `
    -Id "signing-password-input" `
    -Passed:($passwordsConfigured -or -not $NonInteractive) `
    -Expected $(if ($NonInteractive) { "both signing passwords injected" } else { "injected passwords or secure interactive prompts" }) `
    -Actual $(if ($passwordsConfigured) { "configured" } elseif ($NonInteractive) { "missing" } else { "secure prompts required" })

foreach ($scriptName in @(
    "build-e15-baseline-apk.ps1",
    "e15-baseline-build-diagnostics.ps1",
    "build-signed-store-aab.ps1",
    "run-e15-release-gate.ps1")) {
    $scriptPath = Join-Path $PSScriptRoot $scriptName
    Add-Precondition `
        -Id "script:$scriptName" `
        -Passed:(Test-Path -LiteralPath $scriptPath -PathType Leaf) `
        -Expected "existing artifact workflow script" `
        -Actual $scriptPath
}

$preflightPassed = @($script:Preconditions | Where-Object { -not $_.passed }).Count -eq 0
foreach ($precondition in $script:Preconditions) {
    Write-Host "$($precondition.id): passed=$($precondition.passed); actual=$($precondition.actual)"
}
Write-Host "E15 artifact preparation preflight passed: $preflightPassed"
if ($PreflightOnly) {
    exit $(if ($preflightPassed) { 0 } else { 1 })
}
if (-not $preflightPassed) {
    throw "E15 artifact preparation prerequisites are incomplete."
}

if (-not $env:CATGUARD_ANDROID_KEYSTORE_PASSWORD -and $null -eq $KeystorePassword) {
    $KeystorePassword = Read-Host "Android keystore password" -AsSecureString
}
if (-not $env:CATGUARD_ANDROID_KEY_PASSWORD -and $null -eq $KeyPassword) {
    $KeyPassword = Read-Host "Android key password" -AsSecureString
}

$buildArguments = @{
    KeystorePath = $resolvedKeystore
    KeyAlias = $KeyAlias
    UnityPath = $unity
    NonInteractive = $true
}
if ($null -ne $KeystorePassword) {
    $buildArguments.KeystorePassword = $KeystorePassword
}
if ($null -ne $KeyPassword) {
    $buildArguments.KeyPassword = $KeyPassword
}

$startedAtUtc = (Get-Date).ToUniversalTime()
$baselineScript = Join-Path $PSScriptRoot "build-e15-baseline-apk.ps1"
& $baselineScript -BaselineCommit $baselineCommitResolved @buildArguments
Assert-SourceUnchanged -ExpectedHead $head -Context "baseline APK build"

$candidateScript = Join-Path $PSScriptRoot "build-signed-store-aab.ps1"
& $candidateScript -Artifact Apk @buildArguments
Assert-SourceUnchanged -ExpectedHead $head -Context "candidate APK build"
& $candidateScript -Artifact Aab @buildArguments
Assert-SourceUnchanged -ExpectedHead $head -Context "candidate AAB build"

$resolvedOutputDir = if ([IO.Path]::IsPathRooted($OutputDir)) {
    [IO.Path]::GetFullPath($OutputDir)
}
else {
    [IO.Path]::GetFullPath((Join-Path $script:RepoRoot $OutputDir))
}
$runRoot = Join-Path $resolvedOutputDir (Get-Date -Format "yyyyMMdd-HHmmss")
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null
$releaseGateLog = Join-Path $runRoot "artifact-only-release-gate.log"
$releaseGateOutput = Join-Path $runRoot "preflight"
$baselineApk = Join-Path $script:RepoRoot "Builds\Android\baseline\CatGuardTowerDefense-0.1.0-universal.apk"
$baselineProvenance = "$baselineApk.provenance.json"
$candidateApk = Join-Path $script:RepoRoot "Builds\Android\CatGuardTowerDefense-store.apk"
$candidateAab = Join-Path $script:RepoRoot "Builds\Android\CatGuardTowerDefense-store.aab"
$candidateApkProvenance = "$candidateApk.provenance.json"
$candidateAabProvenance = "$candidateAab.provenance.json"

$gateOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass `
    -File (Join-Path $PSScriptRoot "run-e15-release-gate.ps1") `
    -BaselineApkPath $baselineApk `
    -BaselineApkProvenancePath $baselineProvenance `
    -BaselineCommit $baselineCommitResolved `
    -CandidateApkPath $candidateApk `
    -CandidateAabPath $candidateAab `
    -CandidateApkProvenancePath $candidateApkProvenance `
    -CandidateAabProvenancePath $candidateAabProvenance `
    -OutputDir $releaseGateOutput `
    -ArtifactOnly 2>&1 | ForEach-Object { $_.ToString() })
$gateExitCode = $LASTEXITCODE
Set-Content -LiteralPath $releaseGateLog -Encoding UTF8 -Value ($gateOutput -join [Environment]::NewLine)
if ($gateExitCode -ne 0) {
    throw "E15 artifact-only release gate failed with exit code $gateExitCode. See: $releaseGateLog"
}

$preflightManifests = @(Get-ChildItem -LiteralPath $releaseGateOutput -Recurse -File -Filter "e15-artifact-preflight.json")
if ($preflightManifests.Count -ne 1) {
    throw "Expected exactly one artifact-only preflight manifest under $releaseGateOutput; found $($preflightManifests.Count)."
}
$preflightManifest = $preflightManifests[0]
$preflightEvidence = Get-Content -LiteralPath $preflightManifest.FullName -Encoding UTF8 -Raw | ConvertFrom-Json
if (-not [bool]$preflightEvidence.passed `
    -or $preflightEvidence.kind -ne "artifact-only-preflight" `
    -or [bool]$preflightEvidence.fullReleaseGate `
    -or $preflightEvidence.gitHead -ne $head `
    -or -not [bool]$preflightEvidence.baselineBuildProvenance.passed `
    -or -not [bool]$preflightEvidence.candidateApkBuildProvenance.passed `
    -or -not [bool]$preflightEvidence.candidateAabBuildProvenance.passed) {
    throw "E15 artifact-only preflight manifest is not bound to the prepared source and provenance set."
}
Assert-SourceUnchanged -ExpectedHead $head -Context "artifact-only release gate"

$artifacts = [ordered]@{
    baselineApk = $baselineApk
    baselineApkProvenance = $baselineProvenance
    candidateApk = $candidateApk
    candidateAab = $candidateAab
    candidateApkProvenance = $candidateApkProvenance
    candidateAabProvenance = $candidateAabProvenance
}
$hashes = [ordered]@{}
foreach ($name in $artifacts.Keys) {
    $path = $artifacts[$name]
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Prepared E15 artifact is missing: $path"
    }
    $hashes[$name] = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
}

$manifestPath = Join-Path $runRoot "e15-artifact-set.json"
$manifest = [pscustomobject]@{
    schemaVersion = 1
    state = "artifact_set_prepared"
    passed = $true
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    buildStartedAtUtc = $startedAtUtc.ToString("o")
    gitBranch = $branch
    gitHead = $head
    upstream = $upstream
    baselineCommit = $baselineCommitResolved
    runRoot = $runRoot
    artifacts = $artifacts
    hashes = $hashes
    artifactPreflightManifestPath = $preflightManifest.FullName
    artifactPreflightManifestSha256 = (Get-FileHash -LiteralPath $preflightManifest.FullName -Algorithm SHA256).Hash
    artifactPreflightLogPath = $releaseGateLog
    artifactPreflightLogSha256 = (Get-FileHash -LiteralPath $releaseGateLog -Algorithm SHA256).Hash
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
$manifestContract = Test-E15ArtifactSetManifest `
    -ManifestPath $manifestPath `
    -ExpectedGitHead $head `
    -ExpectedUpstream $upstream `
    -ExpectedBaselineCommit $baselineCommitResolved `
    -ExpectedRepositoryRoot $script:RepoRoot `
    -RequireEvidenceFiles
if (-not [bool]$manifestContract.passed) {
    $manifest.state = "artifact_set_contract_failed"
    $manifest.passed = $false
    $manifest | Add-Member -NotePropertyName contractReasons -NotePropertyValue $manifestContract.reasons
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
    throw "E15 artifact-set manifest contract failed: $($manifestContract.reasons -join ' ')"
}
Write-Host "E15 release artifact set prepared from clean pushed HEAD $head."
Write-Host "Manifest: $manifestPath"
