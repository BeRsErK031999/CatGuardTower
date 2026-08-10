[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BaselineApkPath,
    [string]$CandidateApkPath = "Builds\Android\CatGuardTowerDefense-store.apk",
    [string]$CandidateAabPath = "Builds\Android\CatGuardTowerDefense-store.aab",
    [string]$BaselineSaveFixturePath = "tools\android\fixtures\e15-schema-v4-save.json",
    [string]$PackageName = "com.berserk031999.catguardtower",
    [string]$DeviceSerial = "",
    [string]$OutputDir = "Builds\Android\qa-device\e15-release",
    [int]$LaunchWaitSeconds = 25,
    [switch]$ArtifactOnly,
    [switch]$RequirePhysicalDevice,
    [switch]$RequirePerformance,
    [switch]$ConfirmPackageReset
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$script:DeviceQaScript = Join-Path $PSScriptRoot "run-device-qa.ps1"
$script:TargetArgs = @()

function Resolve-ProjectPath {
    param([string]$Path)

    if ([IO.Path]::IsPathRooted($Path)) {
        return [IO.Path]::GetFullPath($Path)
    }

    return [IO.Path]::GetFullPath((Join-Path $script:RepoRoot $Path))
}

function Find-FirstTool {
    param([string[]]$Candidates)

    foreach ($candidate in $Candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    return $null
}

function Find-AndroidTools {
    $editorVersion = ((Get-Content -LiteralPath (Join-Path $script:RepoRoot "ProjectSettings\ProjectVersion.txt") -Encoding UTF8 | Select-Object -First 1) -replace '^m_EditorVersion:\s*', '').Trim()
    $unityRoot = Join-Path $env:LOCALAPPDATA "Unity\Hub\Editor\$editorVersion\Editor"
    $sdkRoots = @(
        $env:ANDROID_SDK_ROOT,
        $env:ANDROID_HOME,
        (Join-Path $env:LOCALAPPDATA "Android\Sdk"),
        (Join-Path $unityRoot "Data\PlaybackEngines\AndroidPlayer\SDK")
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Container) }

    $adb = Find-FirstTool ($sdkRoots | ForEach-Object { Join-Path $_ "platform-tools\adb.exe" })
    $buildToolDirs = @($sdkRoots | ForEach-Object {
        Get-ChildItem -LiteralPath (Join-Path $_ "build-tools") -Directory -ErrorAction SilentlyContinue
    } | Sort-Object { [version]$_.Name } -Descending)
    $aapt = Find-FirstTool ($buildToolDirs | ForEach-Object { Join-Path $_.FullName "aapt.exe" })
    $apksigner = Find-FirstTool ($buildToolDirs | ForEach-Object { Join-Path $_.FullName "apksigner.bat" })
    $java = Find-FirstTool @(
        (Join-Path $unityRoot "Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\java.exe"),
        (Join-Path $unityRoot "Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\java")
    )
    $jarSigner = Find-FirstTool @(
        (Join-Path $unityRoot "Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\jarsigner.exe"),
        (Join-Path $unityRoot "Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\jarsigner")
    )
    $keytool = Find-FirstTool @(
        (Join-Path $unityRoot "Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe"),
        (Join-Path $unityRoot "Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool")
    )
    $bundletool = Get-ChildItem -LiteralPath (Join-Path $unityRoot "Data\PlaybackEngines\AndroidPlayer") `
        -Recurse -File -Filter "bundletool*.jar" -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName

    if (-not $adb -or -not $aapt -or -not $apksigner -or -not $java -or -not $jarSigner -or -not $keytool -or -not $bundletool) {
        throw "E15 requires adb, aapt, apksigner, Java, jarsigner, keytool, and bundletool from Unity Android Build Support."
    }

    return [pscustomobject]@{
        Adb = $adb
        Aapt = $aapt
        ApkSigner = $apksigner
        Java = $java
        JarSigner = $jarSigner
        Keytool = $keytool
        Bundletool = $bundletool
    }
}

function Invoke-External {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [switch]$AllowFailure
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = @(& $FilePath @Arguments 2>&1 | ForEach-Object { $_.ToString() })
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw "$FilePath $($Arguments -join ' ') failed with exit code $exitCode.`n$($output -join [Environment]::NewLine)"
    }

    return [pscustomobject]@{ ExitCode = $exitCode; Output = [string[]]$output }
}

function Invoke-Adb {
    param(
        [string[]]$Arguments,
        [switch]$AllowFailure
    )

    return Invoke-External -FilePath $script:Tools.Adb -Arguments ($script:TargetArgs + $Arguments) -AllowFailure:$AllowFailure
}

function Get-ApkMetadata {
    param([string]$Path)

    $badging = Invoke-External -FilePath $script:Tools.Aapt -Arguments @("dump", "badging", $Path)
    $packageLine = $badging.Output | Where-Object { $_ -match "^package: name='" } | Select-Object -First 1
    if (-not $packageLine -or $packageLine -notmatch "name='([^']+)'\s+versionCode='([^']+)'\s+versionName='([^']+)'" ) {
        throw "Could not parse APK identity: $Path"
    }
    $packageName = $Matches[1]
    $versionCode = [long]$Matches[2]
    $versionName = $Matches[3]

    $sdkLine = $badging.Output | Where-Object { $_ -match "^sdkVersion:'" } | Select-Object -First 1
    $targetLine = $badging.Output | Where-Object { $_ -match "^targetSdkVersion:'" } | Select-Object -First 1
    $nativeLine = $badging.Output | Where-Object { $_ -match "^native-code:" } | Select-Object -First 1
    $signature = Invoke-External -FilePath $script:Tools.ApkSigner -Arguments @("verify", "--print-certs", $Path)
    $digestLine = $signature.Output | Where-Object { $_ -match "certificate SHA-256 digest:" } | Select-Object -First 1
    if (-not $digestLine -or $digestLine -notmatch "digest:\s*(\S+)") {
        throw "Could not read APK signing certificate: $Path"
    }

    return [pscustomobject]@{
        path = $Path
        packageName = $packageName
        versionCode = $versionCode
        versionName = $versionName
        minimumSdk = [int]([regex]::Match($sdkLine, "'([0-9]+)'").Groups[1].Value)
        targetSdk = [int]([regex]::Match($targetLine, "'([0-9]+)'").Groups[1].Value)
        nativeCode = if ($nativeLine) { $nativeLine.Trim() } else { "" }
        certificateSha256 = [regex]::Match($digestLine, "digest:\s*(\S+)").Groups[1].Value
        sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
        bytes = (Get-Item -LiteralPath $Path).Length
    }
}

function Get-AabMetadata {
    param([string]$Path)

    $manifestResult = Invoke-External -FilePath $script:Tools.Java -Arguments @(
        "-jar", $script:Tools.Bundletool, "dump", "manifest", "--bundle=$Path", "--module=base"
    )
    $manifest = $manifestResult.Output -join [Environment]::NewLine
    if ($manifest -notmatch 'package="([^"]+)"') {
        throw "Could not parse AAB package identity: $Path"
    }
    $package = $Matches[1]
    $versionCodeMatch = [regex]::Match($manifest, 'android:versionCode="([0-9]+)"')
    $versionNameMatch = [regex]::Match($manifest, 'android:versionName="([^"]+)"')
    $targetSdkMatch = [regex]::Match($manifest, 'android:targetSdkVersion="([0-9]+)"')
    $permissionNames = @([regex]::Matches($manifest, '<uses-permission[^>]+android:name="([^"]+)"') | ForEach-Object {
        $_.Groups[1].Value
    })

    $jarSignature = Invoke-External -FilePath $script:Tools.Java -Arguments @(
        "-jar", $script:Tools.Bundletool, "validate", "--bundle=$Path"
    )
    $jarVerification = Invoke-External -FilePath $script:Tools.JarSigner -Arguments @("-verify", "-certs", $Path)
    $certificate = Invoke-External -FilePath $script:Tools.Keytool -Arguments @("-printcert", "-jarfile", $Path)
    $shaLine = $certificate.Output | Where-Object { $_ -match '^\s*SHA256:' } | Select-Object -First 1
    if (-not $shaLine -or $shaLine -notmatch 'SHA256:\s*([0-9A-Fa-f:]+)') {
        throw "Could not read AAB signing certificate: $Path"
    }
    $certificateSha256 = $Matches[1].Replace(":", "").ToUpperInvariant()

    return [pscustomobject]@{
        path = $Path
        packageName = $package
        versionCode = if ($versionCodeMatch.Success) { [long]$versionCodeMatch.Groups[1].Value } else { 0 }
        versionName = if ($versionNameMatch.Success) { $versionNameMatch.Groups[1].Value } else { "" }
        targetSdk = if ($targetSdkMatch.Success) { [int]$targetSdkMatch.Groups[1].Value } else { 0 }
        permissions = $permissionNames
        bundletoolValidationExitCode = $jarSignature.ExitCode
        jarSignatureVerificationExitCode = $jarVerification.ExitCode
        certificateSha256 = $certificateSha256
        sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
        bytes = (Get-Item -LiteralPath $Path).Length
        manifestPath = ""
        manifestText = $manifest
    }
}

function Assert-ArtifactContract {
    param(
        [pscustomobject]$Baseline,
        [pscustomobject]$Candidate,
        [pscustomobject]$Bundle
    )

    foreach ($artifact in @($Baseline, $Candidate, $Bundle)) {
        if ($artifact.packageName -ne $PackageName) {
            throw "Unexpected package '$($artifact.packageName)' in $($artifact.path); expected $PackageName."
        }
    }
    if ($Candidate.versionCode -le $Baseline.versionCode) {
        throw "Candidate APK versionCode must be greater than baseline versionCode."
    }
    if ($Candidate.versionCode -ne $Bundle.versionCode -or $Candidate.versionName -ne $Bundle.versionName) {
        throw "Candidate APK and AAB version identities do not match."
    }
    if ($Candidate.targetSdk -lt 36 -or $Bundle.targetSdk -lt 36) {
        throw "E15 candidate artifacts must target Android API 36 or newer."
    }
    if ($Baseline.certificateSha256 -ne $Candidate.certificateSha256) {
        throw "Baseline and candidate APKs are not signed by the same certificate; Android upgrade is impossible."
    }
    if ($Candidate.certificateSha256.Replace(":", "").ToUpperInvariant() -ne $Bundle.certificateSha256) {
        throw "Candidate APK and AAB are not signed by the same upload certificate."
    }

    $sensitivePermissions = @($Bundle.permissions | Where-Object {
        $_ -match 'INTERNET|AD_ID|BILLING|CAMERA|RECORD_AUDIO|READ_CONTACTS|ACCESS_(COARSE|FINE)_LOCATION|READ_EXTERNAL_STORAGE|WRITE_EXTERNAL_STORAGE|POST_NOTIFICATIONS'
    })
    if ($sensitivePermissions.Count -gt 0) {
        throw "AAB permission audit conflicts with the no-live-SDK Data Safety draft: $($sensitivePermissions -join ', ')"
    }
}

function Select-Device {
    $devices = Invoke-External -FilePath $script:Tools.Adb -Arguments @("devices", "-l")
    $ready = @($devices.Output | Where-Object { $_ -match '^\S+\s+device\b' })
    if ($ready.Count -eq 0) {
        throw "No ready Android target was found."
    }

    if ($DeviceSerial) {
        $line = $ready | Where-Object { $_ -match ('^' + [regex]::Escape($DeviceSerial) + '\s+device\b') } | Select-Object -First 1
        if (-not $line) {
            throw "Requested Android target is not ready: $DeviceSerial"
        }
        $serial = $DeviceSerial
    } else {
        $physical = $ready | Where-Object { $_ -notmatch '^emulator-' -and $_ -notmatch '\bmodel:sdk_' } | Select-Object -First 1
        $line = if ($physical) { $physical } else { $ready | Select-Object -First 1 }
        $serial = ($line -split '\s+')[0]
    }

    $script:TargetArgs = @("-s", $serial)
    $qemu = (Invoke-Adb -Arguments @("shell", "getprop", "ro.kernel.qemu") -AllowFailure).Output -join ""
    $isEmulator = $serial -match '^emulator-' -or $qemu.Trim() -eq "1"
    if ($RequirePhysicalDevice -and $isEmulator) {
        throw "E15 physical-device QA is required, but selected target is an emulator: $serial"
    }

    return [pscustomobject]@{ serial = $serial; isEmulator = $isEmulator; adbLine = $line }
}

function Invoke-DeviceQa {
    param(
        [string]$ApkPath,
        [string]$RunOutputDir,
        [switch]$Offline,
        [switch]$SkipInstall,
        [switch]$Performance
    )

    $arguments = @(
        "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $script:DeviceQaScript,
        "-ApkPath", $ApkPath,
        "-PackageName", $PackageName,
        "-DeviceSerial", $script:Device.serial,
        "-LaunchWaitSeconds", $LaunchWaitSeconds,
        "-OutputDir", $RunOutputDir,
        "-MinimumAverageFps", 24,
        "-MaximumP95FrameTimeMs", 70
    )
    if ($Offline) { $arguments += "-Offline" }
    if ($SkipInstall) { $arguments += "-SkipInstall" }
    if ($Performance) { $arguments += "-RequirePerformance" }
    if ($RequirePhysicalDevice) { $arguments += "-RequirePhysicalDevice" }

    $result = Invoke-External -FilePath "powershell.exe" -Arguments $arguments
    $summaryPath = Get-ChildItem -LiteralPath $RunOutputDir -Recurse -File -Filter "qa-summary.json" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
    if (-not $summaryPath) {
        throw "Device QA did not create qa-summary.json under $RunOutputDir"
    }

    return Get-Content -LiteralPath $summaryPath -Encoding UTF8 -Raw | ConvertFrom-Json
}

function Compare-SaveContinuity {
    param(
        [string]$BeforePath,
        [string]$AfterPath
    )

    if (-not $BeforePath -or -not $AfterPath `
        -or -not (Test-Path -LiteralPath $BeforePath) `
        -or -not (Test-Path -LiteralPath $AfterPath)) {
        return [pscustomobject]@{ passed = $false; reason = "Save file was not readable before and after upgrade." }
    }

    $before = Get-Content -LiteralPath $BeforePath -Encoding UTF8 -Raw | ConvertFrom-Json
    $after = Get-Content -LiteralPath $AfterPath -Encoding UTF8 -Raw | ConvertFrom-Json
    $preservedCompleted = @($before.completedLevelIds | Where-Object { $_ -notin @($after.completedLevelIds) }).Count -eq 0
    $preservedUnlocked = @($before.unlockedLevelIds | Where-Object { $_ -notin @($after.unlockedLevelIds) }).Count -eq 0
    $passed = [int]$after.schemaVersion -ge [int]$before.schemaVersion `
        -and [int]$after.fishCoins -ge [int]$before.fishCoins `
        -and $preservedCompleted `
        -and $preservedUnlocked

    return [pscustomobject]@{
        passed = $passed
        beforeSchemaVersion = [int]$before.schemaVersion
        afterSchemaVersion = [int]$after.schemaVersion
        beforeFishCoins = [int]$before.fishCoins
        afterFishCoins = [int]$after.fishCoins
        completedLevelsPreserved = $preservedCompleted
        unlockedLevelsPreserved = $preservedUnlocked
        beforeSha256 = (Get-FileHash -LiteralPath $BeforePath -Algorithm SHA256).Hash
        afterSha256 = (Get-FileHash -LiteralPath $AfterPath -Algorithm SHA256).Hash
    }
}

$resolvedBaselineApk = Resolve-ProjectPath $BaselineApkPath
$resolvedCandidateApk = Resolve-ProjectPath $CandidateApkPath
$resolvedCandidateAab = Resolve-ProjectPath $CandidateAabPath
$resolvedBaselineSaveFixture = Resolve-ProjectPath $BaselineSaveFixturePath
$resolvedOutputDir = Resolve-ProjectPath $OutputDir
foreach ($path in @($resolvedBaselineApk, $resolvedCandidateApk, $resolvedCandidateAab)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "E15 artifact is missing: $path"
    }
}
if (-not $ArtifactOnly -and -not $ConfirmPackageReset) {
    throw "E15 clean-install QA deletes app data for exactly '$PackageName'. Re-run with -ConfirmPackageReset after confirming the target."
}

$script:Tools = Find-AndroidTools
$javaBinDirectory = Split-Path -Parent $script:Tools.Java
$env:JAVA_HOME = Split-Path -Parent $javaBinDirectory
$baseline = Get-ApkMetadata $resolvedBaselineApk
$candidate = Get-ApkMetadata $resolvedCandidateApk
$bundle = Get-AabMetadata $resolvedCandidateAab
Assert-ArtifactContract -Baseline $baseline -Candidate $candidate -Bundle $bundle

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path $resolvedOutputDir $timestamp
$manifestName = if ($ArtifactOnly) { "e15-artifact-preflight.json" } else { "e15-release-gate.json" }
$manifestPath = Join-Path $runRoot $manifestName
$aabManifestPath = Join-Path $runRoot "candidate-aab-manifest.xml"
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null
$bundle.manifestText | Set-Content -LiteralPath $aabManifestPath -Encoding UTF8
$bundle.manifestPath = $aabManifestPath
$bundle.PSObject.Properties.Remove("manifestText")

$gitHead = (& git -C $script:RepoRoot rev-parse HEAD).Trim()
$gitBranch = (& git -C $script:RepoRoot branch --show-current).Trim()
if ($ArtifactOnly) {
    $preflight = [pscustomobject]@{
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        passed = $true
        kind = "artifact-only-preflight"
        fullReleaseGate = $false
        gitBranch = $gitBranch
        gitHead = $gitHead
        packageName = $PackageName
        baselineApk = $baseline
        candidateApk = $candidate
        candidateAab = $bundle
    }
    $preflight | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

    Write-Host "E15 artifact preflight manifest: $manifestPath"
    Write-Host "Candidate: $($candidate.versionName) ($($candidate.versionCode))"
    Write-Host "Artifact preflight passed. Full release gate was not run."
    exit 0
}

$script:Device = Select-Device

Write-Host "E15 target: $($script:Device.serial)"
Write-Host "Package reset scope: $PackageName"
Write-Host "Candidate: $($candidate.versionName) ($($candidate.versionCode))"

Invoke-Adb -Arguments @("uninstall", $PackageName) -AllowFailure | Out-Null
$cleanSummary = Invoke-DeviceQa `
    -ApkPath $resolvedCandidateApk `
    -RunOutputDir (Join-Path $runRoot "clean-install-offline") `
    -Offline `
    -Performance:$RequirePerformance

Invoke-Adb -Arguments @("uninstall", $PackageName) -AllowFailure | Out-Null
$baselineSummary = Invoke-DeviceQa `
    -ApkPath $resolvedBaselineApk `
    -RunOutputDir (Join-Path $runRoot "baseline")
$beforeSavePath = [string]$baselineSummary.savePath
if (Test-Path -LiteralPath $resolvedBaselineSaveFixture -PathType Leaf) {
    $remoteSavePath = "/sdcard/Android/data/$PackageName/files/catguard-save.json"
    Invoke-Adb -Arguments @("push", $resolvedBaselineSaveFixture, $remoteSavePath) | Out-Null
    $beforeSavePath = $resolvedBaselineSaveFixture
}
$upgradeSummary = Invoke-DeviceQa `
    -ApkPath $resolvedCandidateApk `
    -RunOutputDir (Join-Path $runRoot "upgrade-offline") `
    -Offline `
    -Performance:$RequirePerformance

$saveContinuity = Compare-SaveContinuity `
    -BeforePath $beforeSavePath `
    -AfterPath ([string]$upgradeSummary.savePath)

$passed = [bool]$cleanSummary.launched `
    -and [bool]$cleanSummary.landscapeConfirmed `
    -and [int]$cleanSummary.fatalPatternCount -eq 0 `
    -and [bool]$upgradeSummary.launched `
    -and [bool]$upgradeSummary.landscapeConfirmed `
    -and [int]$upgradeSummary.fatalPatternCount -eq 0 `
    -and [bool]$saveContinuity.passed

$gate = [pscustomobject]@{
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    passed = $passed
    gitBranch = $gitBranch
    gitHead = $gitHead
    packageName = $PackageName
    device = $script:Device
    baselineApk = $baseline
    candidateApk = $candidate
    candidateAab = $bundle
    cleanInstallSummary = $cleanSummary
    baselineSummary = $baselineSummary
    upgradeSummary = $upgradeSummary
    saveContinuity = $saveContinuity
}
$gate | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Host "E15 release gate manifest: $manifestPath"
Write-Host "Clean install passed: $([bool]$cleanSummary.launched -and [bool]$cleanSummary.landscapeConfirmed)"
Write-Host "Upgrade save continuity passed: $($saveContinuity.passed)"
Write-Host "E15 release gate passed: $passed"
exit $(if ($passed) { 0 } else { 1 })
