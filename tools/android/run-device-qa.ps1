param(
    [string]$ApkPath = "Builds\Android\CatGuardTowerDefense-qa.apk",
    [string]$PackageName = "com.catguard.towerdefense.qa",
    [string]$DeviceSerial = "",
    [int]$LaunchWaitSeconds = 25,
    [string]$OutputDir = "Builds\Android\qa-device",
    [double]$MinimumAverageFps = 30,
    [double]$MaximumP95FrameTimeMs = 50,
    [int]$MinimumFrameSamples = 30,
    [switch]$SkipInstall,
    [switch]$Offline,
    [switch]$RequirePerformance,
    [switch]$RequirePhysicalDevice,
    [switch]$UseRunningApp,
    [ValidateSet("launch-surface", "level_12-heavy-wave")]
    [string]$PerformanceContext = "launch-surface",
    [switch]$RequireReleasePerformanceEvidence
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
. (Join-Path $PSScriptRoot "android-qa-provenance.ps1")

function Resolve-ProjectPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return (Join-Path $script:RepoRoot $Path)
}

function Find-Adb {
    $candidatePaths = New-Object System.Collections.Generic.List[string]

    if ($env:ANDROID_HOME) {
        $candidatePaths.Add((Join-Path $env:ANDROID_HOME "platform-tools\adb.exe"))
    }

    if ($env:ANDROID_SDK_ROOT) {
        $candidatePaths.Add((Join-Path $env:ANDROID_SDK_ROOT "platform-tools\adb.exe"))
    }

    $candidatePaths.Add((Join-Path $env:ProgramFiles "Unity\Hub\Editor\6000.4.12f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"))
    $candidatePaths.Add((Join-Path $env:LOCALAPPDATA "Unity\Hub\Editor\6000.4.12f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"))
    $candidatePaths.Add((Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"))

    foreach ($candidatePath in $candidatePaths) {
        if ($candidatePath -and (Test-Path -LiteralPath $candidatePath)) {
            return (Resolve-Path -LiteralPath $candidatePath).Path
        }
    }

    $adbCommand = Get-Command "adb.exe" -ErrorAction SilentlyContinue
    if ($adbCommand) {
        return $adbCommand.Source
    }

    $adbCommand = Get-Command "adb" -ErrorAction SilentlyContinue
    if ($adbCommand) {
        return $adbCommand.Source
    }

    throw "adb was not found. Install Android SDK platform-tools or run this script on a machine with Unity Android Build Support."
}

function Invoke-Adb {
    param(
        [string[]]$Arguments,
        [switch]$AllowFailure
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = @(& $script:AdbPath @Arguments 2>&1 | ForEach-Object { $_.ToString() })
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw "adb $($Arguments -join ' ') failed with exit code $exitCode.`n$($output -join [Environment]::NewLine)"
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = [string[]]$output
    }
}

function Invoke-TargetAdb {
    param(
        [string[]]$Arguments,
        [switch]$AllowFailure
    )

    return Invoke-Adb -Arguments ($script:TargetArgs + $Arguments) -AllowFailure:$AllowFailure
}

function Get-RuntimePerformanceCheckpoint {
    param(
        [string]$PackageName,
        [string]$OutputPath
    )

    $externalFilesPath = "/sdcard/Android/data/$PackageName/files"
    $remoteRequest = "$externalFilesPath/catguard-performance-checkpoint.request"
    $remoteResponse = "$externalFilesPath/catguard-performance-checkpoint.json"
    Invoke-TargetAdb -Arguments @("shell", "rm", "-f", $remoteRequest, $remoteResponse) -AllowFailure | Out-Null

    try {
        $requestResult = Invoke-TargetAdb -Arguments @("shell", "touch", $remoteRequest) -AllowFailure
        if ($requestResult.ExitCode -ne 0) {
            return $null
        }

        $deadline = (Get-Date).AddSeconds(10)
        do {
            Start-Sleep -Milliseconds 250
            $responseResult = Invoke-TargetAdb -Arguments @("shell", "cat", $remoteResponse) -AllowFailure
        } while ($responseResult.ExitCode -ne 0 -and (Get-Date) -lt $deadline)

        $responseText = ($responseResult.Output -join [Environment]::NewLine).Trim()
        if ($responseResult.ExitCode -ne 0 -or -not $responseText) {
            return $null
        }

        $responseText | Set-Content -LiteralPath $OutputPath -Encoding UTF8
        return $responseText | ConvertFrom-Json
    }
    catch {
        Write-Warning "Runtime performance checkpoint could not be captured: $($_.Exception.Message)"
        return $null
    }
    finally {
        Invoke-TargetAdb -Arguments @("shell", "rm", "-f", $remoteRequest, $remoteResponse) -AllowFailure | Out-Null
    }
}

function Write-Lines {
    param(
        [string]$Path,
        [string[]]$Lines
    )

    $Lines | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Get-Percentile {
    param(
        [double[]]$Values,
        [double]$Percentile
    )

    if ($Values.Count -eq 0) {
        return 0
    }

    $sortedValues = @($Values | Sort-Object)
    $rank = ([Math]::Max(0, [Math]::Min(100, $Percentile)) / 100) * ($sortedValues.Count - 1)
    $lowerIndex = [Math]::Floor($rank)
    $upperIndex = [Math]::Ceiling($rank)
    if ($lowerIndex -eq $upperIndex) {
        return [double]$sortedValues[$lowerIndex]
    }

    $weight = $rank - $lowerIndex
    return ([double]$sortedValues[$lowerIndex] * (1 - $weight)) + ([double]$sortedValues[$upperIndex] * $weight)
}

function Get-SurfaceFrameMetrics {
    param(
        [string[]]$Lines,
        [double]$MinimumFps,
        [double]$MaximumP95Ms,
        [int]$MinimumSamples
    )

    $refreshPeriodNanoseconds = 0L
    $presentTimestamps = New-Object System.Collections.Generic.List[long]

    foreach ($line in $Lines) {
        $trimmed = $line.Trim()
        if ($refreshPeriodNanoseconds -eq 0 -and $trimmed -match '^\d+$') {
            $refreshPeriodNanoseconds = [long]$trimmed
            continue
        }

        if ($trimmed -match '^(\d+)\s+(\d+)\s+(\d+)$') {
            $actualPresent = [long]$Matches[2]
            if ($actualPresent -gt 0) {
                $presentTimestamps.Add($actualPresent)
            }
        }
    }

    $orderedTimestamps = @($presentTimestamps | Sort-Object -Unique)
    $frameTimesMs = New-Object System.Collections.Generic.List[double]
    for ($index = 1; $index -lt $orderedTimestamps.Count; $index++) {
        $deltaMs = ([long]$orderedTimestamps[$index] - [long]$orderedTimestamps[$index - 1]) / 1000000.0
        if ($deltaMs -gt 0 -and $deltaMs -le 1000) {
            $frameTimesMs.Add($deltaMs)
        }
    }

    $sampleCount = $frameTimesMs.Count
    $durationSeconds = if ($orderedTimestamps.Count -gt 1) {
        ([long]$orderedTimestamps[-1] - [long]$orderedTimestamps[0]) / 1000000000.0
    }
    else {
        0
    }
    $averageFps = if ($durationSeconds -gt 0) { $sampleCount / $durationSeconds } else { 0 }
    $refreshPeriodMs = if ($refreshPeriodNanoseconds -gt 0) { $refreshPeriodNanoseconds / 1000000.0 } else { 0 }
    $jankThresholdMs = if ($refreshPeriodMs -gt 0) { $refreshPeriodMs * 1.5 } else { 25 }
    $jankyFrameCount = @($frameTimesMs | Where-Object { $_ -gt $jankThresholdMs }).Count
    $jankPercent = if ($sampleCount -gt 0) { ($jankyFrameCount * 100.0) / $sampleCount } else { 0 }
    $medianFrameTimeMs = Get-Percentile -Values $frameTimesMs.ToArray() -Percentile 50
    $p95FrameTimeMs = Get-Percentile -Values $frameTimesMs.ToArray() -Percentile 95
    $sampleSufficient = $sampleCount -ge $MinimumSamples
    $passed = $sampleSufficient -and $averageFps -ge $MinimumFps -and $p95FrameTimeMs -le $MaximumP95Ms

    return [pscustomobject]@{
        sampleCount = $sampleCount
        durationSeconds = [Math]::Round($durationSeconds, 2)
        refreshPeriodMs = [Math]::Round($refreshPeriodMs, 2)
        averageFps = [Math]::Round($averageFps, 2)
        medianFrameTimeMs = [Math]::Round($medianFrameTimeMs, 2)
        p95FrameTimeMs = [Math]::Round($p95FrameTimeMs, 2)
        jankyFrameCount = $jankyFrameCount
        jankPercent = [Math]::Round($jankPercent, 2)
        sampleSufficient = $sampleSufficient
        passed = $passed
    }
}

$resolvedApkPath = Resolve-ProjectPath $ApkPath
if (-not (Test-Path -LiteralPath $resolvedApkPath)) {
    Write-Host "APK not found: $resolvedApkPath"
    Write-Host "Build it first with Phase10ProjectSetup.BuildAll or pass -ApkPath to an existing APK."
    exit 2
}
$expectedApkSha256 = (Get-FileHash -LiteralPath $resolvedApkPath -Algorithm SHA256).Hash
if ($RequireReleasePerformanceEvidence -and (
    -not $RequirePerformance `
    -or -not $RequirePhysicalDevice `
    -or -not $SkipInstall `
    -or -not $UseRunningApp `
    -or $LaunchWaitSeconds -lt 20 `
    -or $PerformanceContext -ne "level_12-heavy-wave")) {
    Write-Host "Release performance evidence requires -RequirePerformance, -RequirePhysicalDevice, -SkipInstall, -UseRunningApp, -LaunchWaitSeconds 20 or more, and -PerformanceContext level_12-heavy-wave."
    exit 2
}

$script:AdbPath = Find-Adb
$devices = Invoke-Adb -Arguments @("devices", "-l")
$deviceLines = @($devices.Output | Where-Object { $_ -match "^\S+\s+device\b" })
$physicalDeviceLines = @($deviceLines | Where-Object {
    $_ -notmatch '^emulator-\d+\s+' -and $_ -notmatch '\bmodel:sdk_'
})

if ($deviceLines.Count -eq 0) {
    Write-Host "No connected Android device is ready for QA."
    Write-Host "Enable USB debugging, confirm the device trust dialog, then verify it appears in: adb devices -l"
    exit 2
}

if ($DeviceSerial) {
    $serialPattern = "^" + [regex]::Escape($DeviceSerial) + "\s+device\b"
    $selectedLine = @($deviceLines | Where-Object { $_ -match $serialPattern } | Select-Object -First 1)

    if ($selectedLine.Count -eq 0) {
        Write-Host "Requested device serial was not found or is not ready: $DeviceSerial"
        Write-Host "Ready devices:"
        $deviceLines | ForEach-Object { Write-Host "  $_" }
        exit 2
    }

    $targetSerial = $DeviceSerial
} else {
    $selectedLine = if ($physicalDeviceLines.Count -gt 0) {
        $physicalDeviceLines[0]
    }
    else {
        $deviceLines[0]
    }
    $targetSerial = ($selectedLine -split "\s+")[0]

    if ($deviceLines.Count -gt 1) {
        Write-Host "Multiple Android devices are ready. Preferring target: $targetSerial"
        Write-Host "Pass -DeviceSerial to target another device."
    }
}

$script:TargetArgs = @("-s", $targetSerial)
$emulatorResult = Invoke-TargetAdb -Arguments @("shell", "getprop", "ro.kernel.qemu") -AllowFailure
$isEmulator = $targetSerial -match '^emulator-\d+$' -or (($emulatorResult.Output -join "").Trim() -eq "1")
if ($RequirePhysicalDevice -and $isEmulator) {
    Write-Host "Physical Android device is required, but selected target is an emulator: $targetSerial"
    Write-Host "Connect the phone with USB debugging enabled or pass its serial through -DeviceSerial."
    exit 2
}

$deviceModel = ((Invoke-TargetAdb -Arguments @("shell", "getprop", "ro.product.model") -AllowFailure).Output -join "").Trim()
$deviceManufacturer = ((Invoke-TargetAdb -Arguments @("shell", "getprop", "ro.product.manufacturer") -AllowFailure).Output -join "").Trim()
$deviceAbi = ((Invoke-TargetAdb -Arguments @("shell", "getprop", "ro.product.cpu.abi") -AllowFailure).Output -join "").Trim()
$androidVersion = ((Invoke-TargetAdb -Arguments @("shell", "getprop", "ro.build.version.release") -AllowFailure).Output -join "").Trim()
$androidApiLevel = ((Invoke-TargetAdb -Arguments @("shell", "getprop", "ro.build.version.sdk") -AllowFailure).Output -join "").Trim()

$resolvedOutputDir = Resolve-ProjectPath $OutputDir
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runDir = Join-Path $resolvedOutputDir $timestamp
New-Item -ItemType Directory -Force -Path $runDir | Out-Null

$devicesPath = Join-Path $runDir "devices.txt"
$logcatPath = Join-Path $runDir "logcat.txt"
$windowPath = Join-Path $runDir "dumpsys-window.txt"
$displayPath = Join-Path $runDir "dumpsys-display.txt"
$gfxInfoPath = Join-Path $runDir "dumpsys-gfxinfo.txt"
$surfaceLatencyPath = Join-Path $runDir "surfaceflinger-latency.txt"
$graphicsPath = Join-Path $runDir "graphics-provenance.txt"
$screenshotPath = Join-Path $runDir "screen.png"
$summaryPath = Join-Path $runDir "qa-summary.json"
$savePath = Join-Path $runDir "catguard-save.json"
$installedApkPath = Join-Path $runDir "installed-base.apk"
$runtimeCheckpointBeforePath = Join-Path $runDir "runtime-performance-checkpoint-before.json"
$runtimeCheckpointAfterPath = Join-Path $runDir "runtime-performance-checkpoint-after.json"

Write-Lines -Path $devicesPath -Lines $devices.Output

Write-Host "Target device: $targetSerial"
Write-Host "APK: $resolvedApkPath"
Write-Host "Output: $runDir"

Invoke-TargetAdb -Arguments @("logcat", "-c") -AllowFailure | Out-Null
Invoke-TargetAdb -Arguments @("shell", "dumpsys", "SurfaceFlinger", "--latency-clear") -AllowFailure | Out-Null

if (-not $SkipInstall) {
    Write-Host "Installing APK..."
    Invoke-TargetAdb -Arguments @("install", "-r", $resolvedApkPath) | Out-Null
}

$packagePathsResult = Invoke-TargetAdb -Arguments @("shell", "pm", "path", $PackageName) -AllowFailure
$installedBaseRemotePath = [string](@($packagePathsResult.Output | Where-Object {
    $_ -match '^package:.+/base\.apk$'
} | ForEach-Object {
    $_.Substring("package:".Length).Trim()
} | Select-Object -First 1))
$installedApkSha256 = ""
if ($installedBaseRemotePath) {
    $installedPull = Invoke-TargetAdb -Arguments @("pull", $installedBaseRemotePath, $installedApkPath) -AllowFailure
    if ($installedPull.ExitCode -eq 0 -and (Test-Path -LiteralPath $installedApkPath -PathType Leaf)) {
        $installedApkSha256 = (Get-FileHash -LiteralPath $installedApkPath -Algorithm SHA256).Hash
    }
}
$apkIdentityMatched = $installedApkSha256 `
    -and $installedApkSha256 -ieq $expectedApkSha256

$offlineRequested = $false
if ($Offline) {
    $offlineRequested = $true
    Write-Host "Disabling Wi-Fi and mobile data for offline QA..."
    Invoke-TargetAdb -Arguments @("shell", "svc", "wifi", "disable") -AllowFailure | Out-Null
    Invoke-TargetAdb -Arguments @("shell", "svc", "data", "disable") -AllowFailure | Out-Null
}

if ($UseRunningApp) {
    Write-Host "Sampling the already running app without relaunching it..."
    $launchResult = [pscustomobject]@{ ExitCode = 0; Output = @("UseRunningApp") }
}
else {
    Write-Host "Launching app..."
    Invoke-TargetAdb -Arguments @("shell", "input", "keyevent", "KEYCODE_WAKEUP") -AllowFailure | Out-Null
    Invoke-TargetAdb -Arguments @("shell", "wm", "dismiss-keyguard") -AllowFailure | Out-Null
    Invoke-TargetAdb -Arguments @("shell", "cmd", "statusbar", "collapse") -AllowFailure | Out-Null
    $launchResult = Invoke-TargetAdb -Arguments @("shell", "monkey", "-p", $PackageName, "-c", "android.intent.category.LAUNCHER", "1") -AllowFailure
}
$runtimeCheckpointBefore = $null
$runtimeCheckpointAfter = $null
if ($PerformanceContext -eq "level_12-heavy-wave") {
    $runtimeCheckpointBefore = Get-RuntimePerformanceCheckpoint `
        -PackageName $PackageName `
        -OutputPath $runtimeCheckpointBeforePath
    Invoke-TargetAdb -Arguments @("shell", "dumpsys", "SurfaceFlinger", "--latency-clear") -AllowFailure | Out-Null
}
Start-Sleep -Seconds $LaunchWaitSeconds
if ($PerformanceContext -eq "level_12-heavy-wave") {
    $runtimeCheckpointAfter = Get-RuntimePerformanceCheckpoint `
        -PackageName $PackageName `
        -OutputPath $runtimeCheckpointAfterPath
}

$pidResult = Invoke-TargetAdb -Arguments @("shell", "pidof", $PackageName) -AllowFailure
$appPid = ($pidResult.Output -join "").Trim()

$logcatResult = Invoke-TargetAdb -Arguments @("logcat", "-d", "-v", "time") -AllowFailure
Write-Lines -Path $logcatPath -Lines $logcatResult.Output

$windowResult = Invoke-TargetAdb -Arguments @("shell", "dumpsys", "window") -AllowFailure
Write-Lines -Path $windowPath -Lines $windowResult.Output

$displayResult = Invoke-TargetAdb -Arguments @("shell", "dumpsys", "display") -AllowFailure
Write-Lines -Path $displayPath -Lines $displayResult.Output

$gfxInfoResult = Invoke-TargetAdb -Arguments @("shell", "dumpsys", "gfxinfo", $PackageName, "framestats") -AllowFailure
Write-Lines -Path $gfxInfoPath -Lines $gfxInfoResult.Output

$surfaceListResult = Invoke-TargetAdb -Arguments @("shell", "dumpsys", "SurfaceFlinger", "--list") -AllowFailure
$surfaceFlingerResult = Invoke-TargetAdb -Arguments @("shell", "dumpsys", "SurfaceFlinger") -AllowFailure
$hardwareEgl = ((Invoke-TargetAdb -Arguments @("shell", "getprop", "ro.hardware.egl") -AllowFailure).Output -join "").Trim()
$hardwareVulkan = ((Invoke-TargetAdb -Arguments @("shell", "getprop", "ro.hardware.vulkan") -AllowFailure).Output -join "").Trim()
$qemuGles = ((Invoke-TargetAdb -Arguments @("shell", "getprop", "ro.kernel.qemu.gles") -AllowFailure).Output -join "").Trim()
$graphicsProvenance = Get-AndroidGraphicsProvenance `
    -SurfaceFlingerLines $surfaceFlingerResult.Output `
    -HardwareEgl $hardwareEgl `
    -HardwareVulkan $hardwareVulkan `
    -QemuGles $qemuGles `
    -IsEmulator $isEmulator
Write-Lines -Path $graphicsPath -Lines $surfaceFlingerResult.Output
$surfaceLayer = [string](@($surfaceListResult.Output | Where-Object {
    $_ -like "*$PackageName*" -and $_ -like "*SurfaceView*" -and $_ -like "*(BLAST)*"
} | Select-Object -First 1))
if (-not $surfaceLayer) {
    $surfaceLayer = [string](@($surfaceListResult.Output | Where-Object {
        $_ -like "*$PackageName*" -and $_ -like "*SurfaceView*"
    } | Select-Object -First 1))
}

$surfaceLatencyLines = @()
if ($surfaceLayer) {
    $surfaceLatencyResult = Invoke-TargetAdb -Arguments @(
        "shell",
        "dumpsys",
        "SurfaceFlinger",
        "--latency",
        "'$surfaceLayer'") -AllowFailure
    $surfaceLatencyLines = @($surfaceLatencyResult.Output)
}
Write-Lines -Path $surfaceLatencyPath -Lines (@("surfaceLayer=$surfaceLayer") + $surfaceLatencyLines)
$performance = Get-SurfaceFrameMetrics `
    -Lines $surfaceLatencyLines `
    -MinimumFps $MinimumAverageFps `
    -MaximumP95Ms $MaximumP95FrameTimeMs `
    -MinimumSamples $MinimumFrameSamples

$screenshotCaptured = $false
$screenshotWidth = 0
$screenshotHeight = 0
$remoteScreenshotPath = "/sdcard/catguard-qa-screen-$timestamp.png"
$screencapResult = Invoke-TargetAdb -Arguments @("shell", "screencap", "-p", $remoteScreenshotPath) -AllowFailure
if ($screencapResult.ExitCode -eq 0) {
    $pullResult = Invoke-TargetAdb -Arguments @("pull", $remoteScreenshotPath, $screenshotPath) -AllowFailure
    $screenshotCaptured = $pullResult.ExitCode -eq 0 -and (Test-Path -LiteralPath $screenshotPath)
    Invoke-TargetAdb -Arguments @("shell", "rm", $remoteScreenshotPath) -AllowFailure | Out-Null

    if ($screenshotCaptured) {
        Add-Type -AssemblyName System.Drawing
        $screenshotImage = [System.Drawing.Image]::FromFile($screenshotPath)
        try {
            $screenshotWidth = $screenshotImage.Width
            $screenshotHeight = $screenshotImage.Height
        }
        finally {
            $screenshotImage.Dispose()
        }
    }
}

$landscapeConfirmed = $screenshotCaptured -and $screenshotWidth -gt $screenshotHeight

$saveReadable = $false
$externalSavePath = "/sdcard/Android/data/$PackageName/files/catguard-save.json"
$saveResult = Invoke-TargetAdb -Arguments @("shell", "cat", $externalSavePath) -AllowFailure
if ($saveResult.ExitCode -eq 0 -and (($saveResult.Output -join "").Trim().Length -gt 0)) {
    Write-Lines -Path $savePath -Lines $saveResult.Output
    $saveReadable = $true
} else {
    $runAsSaveResult = Invoke-TargetAdb -Arguments @("shell", "run-as", $PackageName, "cat", "files/catguard-save.json") -AllowFailure
    if ($runAsSaveResult.ExitCode -eq 0 -and (($runAsSaveResult.Output -join "").Trim().Length -gt 0)) {
        Write-Lines -Path $savePath -Lines $runAsSaveResult.Output
        $saveReadable = $true
    }
}

$fatalPattern = "FATAL EXCEPTION|Fatal signal|Abort message|NullReferenceException|MissingMethodException|DllNotFoundException| E/AndroidRuntime"
$fatalHits = @($logcatResult.Output | Where-Object { $_ -match $fatalPattern })
$focusLines = @($windowResult.Output | Where-Object { $_ -match "mCurrentFocus|mFocusedApp|mFocusedWindow|topResumedActivity|mTopFocusedDisplay" })
$packageFocused = @($focusLines | Where-Object { $_ -match [regex]::Escape($PackageName) }).Count -gt 0
$frameRateLines = @($displayResult.Output | Where-Object { $_ -match "(?i)fps|refresh|frameRate|DisplayDeviceInfo|mode" } | Select-Object -First 80)
$releasePerformanceEvidenceEligible = $PerformanceContext -eq "level_12-heavy-wave" `
    -and [bool]$RequirePerformance `
    -and [bool]$RequirePhysicalDevice `
    -and [bool]$SkipInstall `
    -and [bool]$UseRunningApp `
    -and $LaunchWaitSeconds -ge 20 `
    -and $packageFocused `
    -and $apkIdentityMatched `
    -and $null -ne $runtimeCheckpointBefore `
    -and [bool]$runtimeCheckpointBefore.heavyWaveEligible `
    -and $null -ne $runtimeCheckpointAfter `
    -and [bool]$runtimeCheckpointAfter.heavyWaveEligible `
    -and [bool]$graphicsProvenance.releaseAcceptanceEligible `
    -and [bool]$performance.passed `
    -and $appPid.Length -gt 0 `
    -and $fatalHits.Count -eq 0 `
    -and $landscapeConfirmed

$summary = [pscustomobject]@{
    timestampUtc = (Get-Date).ToUniversalTime().ToString("o")
    deviceSerial = $targetSerial
    deviceKind = if ($isEmulator) { "emulator" } else { "physical" }
    deviceManufacturer = $deviceManufacturer
    deviceModel = $deviceModel
    deviceAbi = $deviceAbi
    androidVersion = $androidVersion
    androidApiLevel = $androidApiLevel
    packageName = $PackageName
    apkPath = $resolvedApkPath
    apkSha256 = $expectedApkSha256
    installedBaseRemotePath = $installedBaseRemotePath
    installedApkPath = if ($installedApkSha256) { $installedApkPath } else { "" }
    installedApkSha256 = $installedApkSha256
    apkIdentityMatched = [bool]$apkIdentityMatched
    skipInstall = [bool]$SkipInstall
    offlineRequested = $offlineRequested
    launchExitCode = $launchResult.ExitCode
    appPid = $appPid
    launched = $appPid.Length -gt 0
    fatalPatternCount = $fatalHits.Count
    currentFocusFound = $focusLines.Count -gt 0
    packageFocused = $packageFocused
    saveReadable = $saveReadable
    screenshotCaptured = $screenshotCaptured
    screenshotWidth = $screenshotWidth
    screenshotHeight = $screenshotHeight
    landscapeConfirmed = $landscapeConfirmed
    runDir = $runDir
    logcatPath = $logcatPath
    windowPath = $windowPath
    displayPath = $displayPath
    gfxInfoPath = $gfxInfoPath
    surfaceLatencyPath = $surfaceLatencyPath
    graphicsPath = $graphicsPath
    screenshotPath = if ($screenshotCaptured) { $screenshotPath } else { "" }
    savePath = if ($saveReadable) { $savePath } else { "" }
    focusLines = $focusLines
    frameRateLines = $frameRateLines
    performanceThresholds = [pscustomobject]@{
        minimumAverageFps = $MinimumAverageFps
        maximumP95FrameTimeMs = $MaximumP95FrameTimeMs
        minimumFrameSamples = $MinimumFrameSamples
        required = [bool]$RequirePerformance
    }
    performanceContext = $PerformanceContext
    samplingWindowSeconds = $LaunchWaitSeconds
    releasePerformanceEvidenceRequired = [bool]$RequireReleasePerformanceEvidence
    physicalDeviceRequired = [bool]$RequirePhysicalDevice
    useRunningApp = [bool]$UseRunningApp
    graphicsProvenance = $graphicsProvenance
    runtimeCheckpointBeforePath = if ($null -ne $runtimeCheckpointBefore) { $runtimeCheckpointBeforePath } else { "" }
    runtimeCheckpointBefore = $runtimeCheckpointBefore
    runtimeCheckpointAfterPath = if ($null -ne $runtimeCheckpointAfter) { $runtimeCheckpointAfterPath } else { "" }
    runtimeCheckpointAfter = $runtimeCheckpointAfter
    releasePerformanceEvidenceEligible = $releasePerformanceEvidenceEligible
    performance = $performance
    fatalLines = $fatalHits
}

$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $summaryPath -Encoding UTF8

Write-Host "QA summary: $summaryPath"
Write-Host "App PID: $appPid"
Write-Host "Fatal crash pattern count: $($fatalHits.Count)"
Write-Host "Screenshot captured: $screenshotCaptured"
Write-Host "Landscape confirmed: $landscapeConfirmed ($screenshotWidth x $screenshotHeight)"
Write-Host "Save readable: $saveReadable"
Write-Host "Surface frame samples: $($performance.sampleCount)"
Write-Host "Average FPS: $($performance.averageFps)"
Write-Host "P95 frame time: $($performance.p95FrameTimeMs) ms"
Write-Host "Performance threshold passed: $($performance.passed)"
Write-Host "Graphics: $($graphicsProvenance.classification), renderer '$($graphicsProvenance.glesRenderer)'."
Write-Host "Installed APK identity matched: $([bool]$apkIdentityMatched)"
Write-Host "Runtime heavy-wave checkpoint before sample eligible: $([bool]($null -ne $runtimeCheckpointBefore -and $runtimeCheckpointBefore.heavyWaveEligible))"
Write-Host "Runtime heavy-wave checkpoint after sample eligible: $([bool]($null -ne $runtimeCheckpointAfter -and $runtimeCheckpointAfter.heavyWaveEligible))"
Write-Host "Release performance evidence eligible: $releasePerformanceEvidenceEligible"

if ($appPid.Length -eq 0 `
    -or $fatalHits.Count -gt 0 `
    -or -not $landscapeConfirmed `
    -or ($RequirePerformance -and -not $performance.passed) `
    -or ($RequireReleasePerformanceEvidence -and -not $releasePerformanceEvidenceEligible)) {
    exit 1
}

exit 0
