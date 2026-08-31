[CmdletBinding()]
param(
    [string]$BaselineApkPath = "Builds\Android\baseline\CatGuardTowerDefense-0.1.0-universal.apk",
    [string]$BaselineApkProvenancePath = "Builds\Android\baseline\CatGuardTowerDefense-0.1.0-universal.apk.provenance.json",
    [string]$BaselineCommit = "28f7e88",
    [string]$CandidateApkPath = "Builds\Android\CatGuardTowerDefense-store.apk",
    [string]$CandidateAabPath = "Builds\Android\CatGuardTowerDefense-store.aab",
    [string]$CandidateApkProvenancePath = "Builds\Android\CatGuardTowerDefense-store.apk.provenance.json",
    [string]$CandidateAabProvenancePath = "Builds\Android\CatGuardTowerDefense-store.aab.provenance.json",
    [string]$PhysicalDeviceSerial = "",
    [string]$EmulatorSerial = "",
    [string]$HeavyWavePerformanceEvidencePath = "",
    [string]$OutputDir = "Builds\Android\qa-device\e15-block-gate",
    [int]$TimeoutSeconds = 300,
    [switch]$PreflightOnly,
    [switch]$ConfirmStorePackageReset
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
. (Join-Path $PSScriptRoot "e15-block-manifest.ps1")
$script:Steps = New-Object System.Collections.Generic.List[object]
$script:Preconditions = New-Object System.Collections.Generic.List[object]
$script:CandidateBase = ""
$resolvedOutputDir = if ([IO.Path]::IsPathRooted($OutputDir)) {
    [IO.Path]::GetFullPath($OutputDir)
}
else {
    [IO.Path]::GetFullPath((Join-Path $script:RepoRoot $OutputDir))
}
$script:RunRoot = Join-Path $resolvedOutputDir (Get-Date -Format "yyyyMMdd-HHmmss")
$script:ManifestPath = Join-Path $script:RunRoot "e15-block-gate-manifest.json"
New-Item -ItemType Directory -Force -Path $script:RunRoot | Out-Null

function Resolve-ProjectPath {
    param([string]$Path)

    if ([IO.Path]::IsPathRooted($Path)) {
        return [IO.Path]::GetFullPath($Path)
    }

    return [IO.Path]::GetFullPath((Join-Path $script:RepoRoot $Path))
}

function Get-StatusValue {
    param([string]$Content)

    $match = [regex]::Match($Content, '(?im)^\s*Status:\s*`?([^`\r\n]+)`?\s*$')
    if ($match.Success) {
        return $match.Groups[1].Value.Trim()
    }

    return "missing"
}

function Get-BacklogSection {
    param(
        [string]$Content,
        [string]$Id
    )

    $marker = "ID: ``$Id``"
    $start = $Content.IndexOf($marker, [StringComparison]::Ordinal)
    if ($start -lt 0) {
        return ""
    }

    $finish = $Content.IndexOf("`n## ", $start + $marker.Length, [StringComparison]::Ordinal)
    if ($finish -lt 0) {
        return $Content.Substring($start)
    }

    return $Content.Substring($start, $finish - $start)
}

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

function Write-Manifest {
    param(
        [bool]$Passed,
        [string]$State
    )

    $head = (& git -C $script:RepoRoot rev-parse HEAD).Trim()
    $branch = (& git -C $script:RepoRoot branch --show-current).Trim()
    $licenseAuditPath = Join-Path $script:RepoRoot "docs\release\SOURCE_ASSET_LICENSE_AUDIT.md"
    $candidateFiles = if ($script:CandidateBase) {
        @(& git -C $script:RepoRoot diff --name-only "$($script:CandidateBase)..HEAD")
    }
    else {
        @()
    }
    $manifest = [pscustomobject]@{
        schemaVersion = 1
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        state = $State
        passed = $Passed
        preflightOnly = [bool]$PreflightOnly
        gitBranch = $branch
        gitHead = $head
        candidateBase = $script:CandidateBase
        candidateFiles = $candidateFiles
        sourceAssetLicenseAuditSha256 = if (Test-Path -LiteralPath $licenseAuditPath -PathType Leaf) {
            (Get-FileHash -LiteralPath $licenseAuditPath -Algorithm SHA256).Hash
        }
        else {
            "missing"
        }
        runRoot = $script:RunRoot
        preconditions = $script:Preconditions.ToArray()
        steps = $script:Steps.ToArray()
    }
    $manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $script:ManifestPath -Encoding UTF8
}

function Find-UnityExecutable {
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

function Get-AndroidTarget {
    param(
        [string]$AdbPath,
        [string]$Serial
    )

    $devices = @(& $AdbPath devices -l 2>$null)
    $line = $devices | Where-Object {
        $_ -match ('^' + [regex]::Escape($Serial) + '\s+device\b')
    } | Select-Object -First 1
    if (-not $line) {
        return [pscustomobject]@{
            ready = $false
            isEmulator = $false
            description = "not ready"
        }
    }

    $qemu = ((& $AdbPath -s $Serial shell getprop ro.kernel.qemu 2>$null) -join "").Trim()
    return [pscustomobject]@{
        ready = $LASTEXITCODE -eq 0
        isEmulator = $Serial -match '^emulator-' -or $qemu -eq "1"
        description = $line.Trim()
    }
}

function Invoke-GateStep {
    param(
        [string]$Id,
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$LogPath,
        [string]$EvidenceLogPath = "",
        [string]$RequiredPattern = ""
    )

    $started = Get-Date
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = @(& $FilePath @Arguments 2>&1 | ForEach-Object { $_.ToString() })
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    Set-Content -LiteralPath $LogPath -Encoding UTF8 -Value ($output -join [Environment]::NewLine)
    $evidencePassed = $true
    if ($RequiredPattern) {
        $evidencePassed = $EvidenceLogPath `
            -and (Test-Path -LiteralPath $EvidenceLogPath -PathType Leaf) `
            -and [bool](Select-String -LiteralPath $EvidenceLogPath -Pattern $RequiredPattern -Quiet)
    }
    $passed = $exitCode -eq 0 -and $evidencePassed
    $logSha256 = (Get-FileHash -LiteralPath $LogPath -Algorithm SHA256).Hash
    $evidenceLogSha256 = if ($EvidenceLogPath -and (Test-Path -LiteralPath $EvidenceLogPath -PathType Leaf)) {
        (Get-FileHash -LiteralPath $EvidenceLogPath -Algorithm SHA256).Hash
    }
    else {
        ""
    }
    $script:Steps.Add([pscustomobject]@{
        id = $Id
        passed = $passed
        exitCode = $exitCode
        requiredPattern = $RequiredPattern
        evidencePassed = $evidencePassed
        evidenceLogPath = $EvidenceLogPath
        evidenceLogSha256 = $evidenceLogSha256
        durationSeconds = [Math]::Round(((Get-Date) - $started).TotalSeconds, 2)
        logPath = $LogPath
        logSha256 = $logSha256
    })
    Write-Manifest -Passed:$false -State $(if ($passed) { "running" } else { "failed" })
    if (-not $passed) {
        throw "E15 block-gate step failed: $Id. Log: $LogPath"
    }
}

$backlogPath = Join-Path $script:RepoRoot "docs\planning\EXTERNAL_PRODUCTION_BACKLOG.md"
$decisionPath = Join-Path $script:RepoRoot "docs\release\E15_RELEASE_DECISION.md"
$readinessPath = Join-Path $script:RepoRoot "docs\planning\E15_RELEASE_READINESS.md"
$backlog = Get-Content -LiteralPath $backlogPath -Encoding UTF8 -Raw
$decision = Get-Content -LiteralPath $decisionPath -Encoding UTF8 -Raw
$readiness = Get-Content -LiteralPath $readinessPath -Encoding UTF8 -Raw

foreach ($id in @("PLAYTEST-001", "DEVICE-QA-001", "STORE-ACCOUNT-001")) {
    $actual = Get-StatusValue (Get-BacklogSection -Content $backlog -Id $id)
    Add-Precondition -Id $id -Passed:($actual -ieq "Completed") -Expected "Completed" -Actual $actual
}

$ownerMatch = [regex]::Match($decision, '(?im)^\s*Owner:\s*(.+?)\s*$')
$owner = if ($ownerMatch.Success) { $ownerMatch.Groups[1].Value.Trim() } else { "missing" }
Add-Precondition `
    -Id "release-owner" `
    -Passed:($owner -and $owner -ine "pending") `
    -Expected "named owner" `
    -Actual $owner

$decisionStatus = Get-StatusValue $decision
Add-Precondition `
    -Id "release-decision-state" `
    -Passed:($decisionStatus -iin @("blocked", "approved")) `
    -Expected "blocked until this technical gate passes, or approved during finalization" `
    -Actual $decisionStatus

$readinessStatus = Get-StatusValue $readiness
Add-Precondition `
    -Id "readiness-state" `
    -Passed:($readinessStatus -iin @("in progress", "completed")) `
    -Expected "in progress until this gate passes, or completed during finalization" `
    -Actual $readinessStatus

$gitStatus = @(& git -C $script:RepoRoot status --porcelain=v1)
Add-Precondition `
    -Id "clean-worktree" `
    -Passed:($gitStatus.Count -eq 0) `
    -Expected "no tracked or untracked source changes" `
    -Actual $(if ($gitStatus.Count -eq 0) { "clean" } else { "$($gitStatus.Count) change(s)" })

$head = (& git -C $script:RepoRoot rev-parse HEAD).Trim()
$upstreamResult = @(& git -C $script:RepoRoot rev-parse '@{upstream}' 2>$null)
$upstream = if ($LASTEXITCODE -eq 0) { ($upstreamResult -join "").Trim() } else { "missing" }
Add-Precondition `
    -Id "upstream-sync" `
    -Passed:($upstream -ne "missing" -and $head -eq $upstream) `
    -Expected "HEAD equals configured upstream" `
    -Actual "HEAD=$head upstream=$upstream"

$developResult = @(& git -C $script:RepoRoot rev-parse refs/remotes/origin/develop 2>$null)
$develop = if ($LASTEXITCODE -eq 0) { ($developResult -join "").Trim() } else { "missing" }
if ($develop -ne "missing") {
    $mergeBaseResult = @(& git -C $script:RepoRoot merge-base HEAD $develop 2>$null)
    if ($LASTEXITCODE -eq 0) {
        $script:CandidateBase = ($mergeBaseResult -join "").Trim()
    }
}
Add-Precondition `
    -Id "candidate-base" `
    -Passed:(-not [string]::IsNullOrWhiteSpace($script:CandidateBase)) `
    -Expected "merge-base between HEAD and origin/develop" `
    -Actual $(if ($script:CandidateBase) { $script:CandidateBase } else { "missing" })

if (-not $PreflightOnly) {
    Add-Precondition `
        -Id "physical-device-input" `
        -Passed:(-not [string]::IsNullOrWhiteSpace($PhysicalDeviceSerial)) `
        -Expected "explicit physical-device serial" `
        -Actual $(if ($PhysicalDeviceSerial) { $PhysicalDeviceSerial } else { "missing" })
    Add-Precondition `
        -Id "emulator-input" `
        -Passed:(-not [string]::IsNullOrWhiteSpace($EmulatorSerial)) `
        -Expected "explicit emulator serial" `
        -Actual $(if ($EmulatorSerial) { $EmulatorSerial } else { "missing" })
    Add-Precondition `
        -Id "store-reset-confirmation" `
        -Passed:([bool]$ConfirmStorePackageReset) `
        -Expected "-ConfirmStorePackageReset" `
        -Actual ([bool]$ConfirmStorePackageReset).ToString()

    $adbPath = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
    $adbExists = Test-Path -LiteralPath $adbPath -PathType Leaf
    Add-Precondition `
        -Id "adb" `
        -Passed:$adbExists `
        -Expected "Android SDK platform-tools adb.exe" `
        -Actual $adbPath
    if ($adbExists -and $EmulatorSerial) {
        $emulatorTarget = Get-AndroidTarget -AdbPath $adbPath -Serial $EmulatorSerial
        Add-Precondition `
            -Id "emulator-target" `
            -Passed:($emulatorTarget.ready -and $emulatorTarget.isEmulator) `
            -Expected "ready emulator" `
            -Actual $emulatorTarget.description
    }
    if ($adbExists -and $PhysicalDeviceSerial) {
        $physicalTarget = Get-AndroidTarget -AdbPath $adbPath -Serial $PhysicalDeviceSerial
        Add-Precondition `
            -Id "physical-device-target" `
            -Passed:($physicalTarget.ready -and -not $physicalTarget.isEmulator) `
            -Expected "ready non-emulator Android device" `
            -Actual $physicalTarget.description
    }

    foreach ($artifact in @(
        $BaselineApkPath,
        $BaselineApkProvenancePath,
        $CandidateApkPath,
        $CandidateAabPath,
        $CandidateApkProvenancePath,
        $CandidateAabProvenancePath)) {
        $resolved = Resolve-ProjectPath $artifact
        Add-Precondition `
            -Id "artifact:$artifact" `
            -Passed:(Test-Path -LiteralPath $resolved -PathType Leaf) `
            -Expected "existing file" `
            -Actual $resolved
    }
    $resolvedPerformanceEvidence = if ($HeavyWavePerformanceEvidencePath) {
        Resolve-ProjectPath $HeavyWavePerformanceEvidencePath
    }
    else {
        "missing"
    }
    Add-Precondition `
        -Id "heavy-wave-performance-evidence" `
        -Passed:($resolvedPerformanceEvidence -ne "missing" -and (Test-Path -LiteralPath $resolvedPerformanceEvidence -PathType Leaf)) `
        -Expected "physical level_12-heavy-wave qa-summary.json" `
        -Actual $resolvedPerformanceEvidence
}

$preconditionsPassed = @($script:Preconditions | Where-Object { -not $_.passed }).Count -eq 0
Write-Manifest -Passed:$preconditionsPassed -State $(if ($preconditionsPassed) { "preflight_passed" } else { "preflight_blocked" })
Write-Host "E15 block-gate preflight passed: $preconditionsPassed"
Write-Host "Manifest: $script:ManifestPath"

if ($PreflightOnly) {
    exit $(if ($preconditionsPassed) { 0 } else { 1 })
}
if (-not $preconditionsPassed) {
    exit 1
}

$unity = Find-UnityExecutable
$developmentApk = Join-Path $script:RepoRoot "Builds\Android\CatGuardTowerDefense-emulator.apk"
$desktopRegressionScripts = @(
    "test-android-qa-provenance.ps1",
    "test-e15-artifact-provenance.ps1",
    "test-e15-baseline-provenance.ps1",
    "test-e15-block-manifest.ps1",
    "test-e15-performance-evidence.ps1"
)
foreach ($scriptName in $desktopRegressionScripts) {
    $safeName = [IO.Path]::GetFileNameWithoutExtension($scriptName)
    Invoke-GateStep `
        -Id "desktop:$safeName" `
        -FilePath "powershell.exe" `
        -Arguments @(
            "-NoProfile", "-ExecutionPolicy", "Bypass",
            "-File", (Join-Path $PSScriptRoot $scriptName)
        ) `
        -LogPath (Join-Path $script:RunRoot "$safeName.log")
}

$validators = @(
    1..11 | ForEach-Object { "Phase${_}ProjectSetup.Validate" }
) + @(
    1..14 | ForEach-Object { "E${_}ProjectSetup.Validate" }
) + @("E15ProjectSetup.ValidateReadiness")

foreach ($method in $validators) {
    $safeName = $method.Replace('.', '-').ToLowerInvariant()
    $unityLog = Join-Path $script:RunRoot "$safeName-unity.log"
    $launcherLog = Join-Path $script:RunRoot "$safeName-launcher.log"
    Invoke-GateStep `
        -Id "unity:$method" `
        -FilePath $unity `
        -Arguments @(
            "-batchmode", "-nographics", "-quit",
            "-projectPath", $script:RepoRoot,
            "-executeMethod", $method,
            "-logFile", $unityLog
        ) `
        -LogPath $launcherLog `
        -EvidenceLogPath $unityLog `
        -RequiredPattern "validation passed"
}

$developmentBuildUnityLog = Join-Path $script:RunRoot "development-apk-unity.log"
Invoke-GateStep `
    -Id "build:development-apk" `
    -FilePath $unity `
    -Arguments @(
        "-batchmode", "-nographics", "-quit",
        "-projectPath", $script:RepoRoot,
        "-executeMethod", "Phase10ProjectSetup.BuildEmulatorApk",
        "-logFile", $developmentBuildUnityLog
    ) `
    -LogPath (Join-Path $script:RunRoot "development-apk-launcher.log")

if (-not (Test-Path -LiteralPath $developmentApk -PathType Leaf)) {
    throw "Fresh Development APK is missing after the Unity build: $developmentApk"
}

$e14Output = Join-Path $script:RunRoot "e14"
Invoke-GateStep `
    -Id "android:e14-functional" `
    -FilePath "powershell.exe" `
    -Arguments @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", (Join-Path $PSScriptRoot "run-emulator-e14-qa.ps1"),
        "-ApkPath", $developmentApk,
        "-DeviceSerial", $EmulatorSerial,
        "-OutputDir", $e14Output,
        "-TimeoutSeconds", $TimeoutSeconds
    ) `
    -LogPath (Join-Path $script:RunRoot "e14-functional.log")

$economyOutput = Join-Path $script:RunRoot "default-economy"
Invoke-GateStep `
    -Id "android:level12-default-economy" `
    -FilePath "powershell.exe" `
    -Arguments @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", (Join-Path $PSScriptRoot "run-e15-default-economy-qa.ps1"),
        "-ApkPath", $developmentApk,
        "-DeviceSerial", $EmulatorSerial,
        "-OutputDir", $economyOutput,
        "-TimeoutSeconds", $TimeoutSeconds
    ) `
    -LogPath (Join-Path $script:RunRoot "default-economy.log")

$releaseOutput = Join-Path $script:RunRoot "signed-release"
Invoke-GateStep `
    -Id "android:signed-physical-release" `
    -FilePath "powershell.exe" `
    -Arguments @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", (Join-Path $PSScriptRoot "run-e15-release-gate.ps1"),
        "-BaselineApkPath", (Resolve-ProjectPath $BaselineApkPath),
        "-BaselineApkProvenancePath", (Resolve-ProjectPath $BaselineApkProvenancePath),
        "-BaselineCommit", $BaselineCommit,
        "-CandidateApkPath", (Resolve-ProjectPath $CandidateApkPath),
        "-CandidateAabPath", (Resolve-ProjectPath $CandidateAabPath),
        "-CandidateApkProvenancePath", (Resolve-ProjectPath $CandidateApkProvenancePath),
        "-CandidateAabProvenancePath", (Resolve-ProjectPath $CandidateAabProvenancePath),
        "-HeavyWavePerformanceEvidencePath", (Resolve-ProjectPath $HeavyWavePerformanceEvidencePath),
        "-DeviceSerial", $PhysicalDeviceSerial,
        "-OutputDir", $releaseOutput,
        "-RequirePhysicalDevice",
        "-RequirePerformance",
        "-ConfirmPackageReset"
    ) `
    -LogPath (Join-Path $script:RunRoot "signed-physical-release.log")

Invoke-GateStep `
    -Id "store:assets" `
    -FilePath "powershell.exe" `
    -Arguments @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", (Join-Path $script:RepoRoot "tools\store\validate-store-assets.ps1")
    ) `
    -LogPath (Join-Path $script:RunRoot "store-assets.log")

Invoke-GateStep `
    -Id "git:candidate-diff-check" `
    -FilePath "git.exe" `
    -Arguments @("-C", $script:RepoRoot, "diff", "--check", "$($script:CandidateBase)..HEAD") `
    -LogPath (Join-Path $script:RunRoot "git-candidate-diff-check.log")

$postGateStatus = @(& git -C $script:RepoRoot status --porcelain=v1)
$postGateClean = $postGateStatus.Count -eq 0
$postGateStatusLog = Join-Path $script:RunRoot "git-post-gate-status.log"
Set-Content -LiteralPath $postGateStatusLog -Encoding UTF8 -Value ($postGateStatus -join [Environment]::NewLine)
$script:Steps.Add([pscustomobject]@{
    id = "git:post-gate-clean-worktree"
    passed = $postGateClean
    exitCode = if ($postGateClean) { 0 } else { 1 }
    requiredPattern = ""
    evidencePassed = $true
    evidenceLogPath = ""
    evidenceLogSha256 = ""
    durationSeconds = 0
    logPath = $postGateStatusLog
    logSha256 = (Get-FileHash -LiteralPath $postGateStatusLog -Algorithm SHA256).Hash
})
if (-not $postGateClean) {
    Write-Manifest -Passed:$false -State "failed"
    throw "E15 block gate changed the source worktree. Inspect: $postGateStatusLog"
}

$artifactHashes = [ordered]@{
    developmentApk = (Get-FileHash -LiteralPath $developmentApk -Algorithm SHA256).Hash
    baselineApk = (Get-FileHash -LiteralPath (Resolve-ProjectPath $BaselineApkPath) -Algorithm SHA256).Hash
    baselineApkProvenance = (Get-FileHash -LiteralPath (Resolve-ProjectPath $BaselineApkProvenancePath) -Algorithm SHA256).Hash
    candidateApk = (Get-FileHash -LiteralPath (Resolve-ProjectPath $CandidateApkPath) -Algorithm SHA256).Hash
    candidateAab = (Get-FileHash -LiteralPath (Resolve-ProjectPath $CandidateAabPath) -Algorithm SHA256).Hash
    candidateApkProvenance = (Get-FileHash -LiteralPath (Resolve-ProjectPath $CandidateApkProvenancePath) -Algorithm SHA256).Hash
    candidateAabProvenance = (Get-FileHash -LiteralPath (Resolve-ProjectPath $CandidateAabProvenancePath) -Algorithm SHA256).Hash
}
$script:Steps.Add([pscustomobject]@{
    id = "artifact:hashes"
    passed = $true
    exitCode = 0
    requiredPattern = ""
    evidencePassed = $true
    evidenceLogPath = ""
    evidenceLogSha256 = ""
    durationSeconds = 0
    logPath = ""
    logSha256 = ""
    hashes = $artifactHashes
})
Write-Manifest -Passed:$true -State "technical_gate_passed"

$licenseAuditPath = Join-Path $script:RepoRoot "docs\release\SOURCE_ASSET_LICENSE_AUDIT.md"
$manifestContract = Test-E15TechnicalGateManifest `
    -ManifestPath $script:ManifestPath `
    -ExpectedGitHead $head `
    -ExpectedCandidateBase $script:CandidateBase `
    -ExpectedArtifactHashes $artifactHashes `
    -ExpectedSourceAssetLicenseAuditSha256 (Get-FileHash -LiteralPath $licenseAuditPath -Algorithm SHA256).Hash `
    -RequireEvidenceFiles
if (-not [bool]$manifestContract.passed) {
    Write-Manifest -Passed:$false -State "failed_manifest_contract"
    throw "E15 technical manifest contract failed: $($manifestContract.reasons -join ' ')"
}

Write-Host "E15 technical block gate passed."
Write-Host "Complete the release report/readiness/decision records, then run E15ProjectSetup.Validate in a cold Unity process."
Write-Host "Manifest: $script:ManifestPath"
exit 0
