param(
    [string]$ApkPath = "Builds\Android\CatGuardTowerDefense-qa.apk",
    [string]$PackageName = "com.catguard.towerdefense.qa",
    [string]$DeviceSerial = "",
    [int]$LaunchWaitSeconds = 25,
    [string]$OutputDir = "Builds\Android\qa-device",
    [switch]$SkipInstall,
    [switch]$Offline
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path

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

function Write-Lines {
    param(
        [string]$Path,
        [string[]]$Lines
    )

    $Lines | Set-Content -LiteralPath $Path -Encoding UTF8
}

$resolvedApkPath = Resolve-ProjectPath $ApkPath
if (-not (Test-Path -LiteralPath $resolvedApkPath)) {
    Write-Host "APK not found: $resolvedApkPath"
    Write-Host "Build it first with Phase10ProjectSetup.BuildAll or pass -ApkPath to an existing APK."
    exit 2
}

$script:AdbPath = Find-Adb
$devices = Invoke-Adb -Arguments @("devices", "-l")
$deviceLines = @($devices.Output | Where-Object { $_ -match "^\S+\s+device\b" })

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
    $selectedLine = $deviceLines[0]
    $targetSerial = ($selectedLine -split "\s+")[0]

    if ($deviceLines.Count -gt 1) {
        Write-Host "Multiple Android devices are ready. Using first device: $targetSerial"
        Write-Host "Pass -DeviceSerial to target another device."
    }
}

$script:TargetArgs = @("-s", $targetSerial)

$resolvedOutputDir = Resolve-ProjectPath $OutputDir
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runDir = Join-Path $resolvedOutputDir $timestamp
New-Item -ItemType Directory -Force -Path $runDir | Out-Null

$devicesPath = Join-Path $runDir "devices.txt"
$logcatPath = Join-Path $runDir "logcat.txt"
$windowPath = Join-Path $runDir "dumpsys-window.txt"
$displayPath = Join-Path $runDir "dumpsys-display.txt"
$gfxInfoPath = Join-Path $runDir "dumpsys-gfxinfo.txt"
$screenshotPath = Join-Path $runDir "screen.png"
$summaryPath = Join-Path $runDir "qa-summary.json"
$savePath = Join-Path $runDir "catguard-save.json"

Write-Lines -Path $devicesPath -Lines $devices.Output

Write-Host "Target device: $targetSerial"
Write-Host "APK: $resolvedApkPath"
Write-Host "Output: $runDir"

Invoke-TargetAdb -Arguments @("logcat", "-c") -AllowFailure | Out-Null

if (-not $SkipInstall) {
    Write-Host "Installing APK..."
    Invoke-TargetAdb -Arguments @("install", "-r", $resolvedApkPath) | Out-Null
}

$offlineRequested = $false
if ($Offline) {
    $offlineRequested = $true
    Write-Host "Disabling Wi-Fi and mobile data for offline QA..."
    Invoke-TargetAdb -Arguments @("shell", "svc", "wifi", "disable") -AllowFailure | Out-Null
    Invoke-TargetAdb -Arguments @("shell", "svc", "data", "disable") -AllowFailure | Out-Null
}

Write-Host "Launching app..."
Invoke-TargetAdb -Arguments @("shell", "input", "keyevent", "KEYCODE_WAKEUP") -AllowFailure | Out-Null
Invoke-TargetAdb -Arguments @("shell", "wm", "dismiss-keyguard") -AllowFailure | Out-Null
Invoke-TargetAdb -Arguments @("shell", "cmd", "statusbar", "collapse") -AllowFailure | Out-Null
$launchResult = Invoke-TargetAdb -Arguments @("shell", "monkey", "-p", $PackageName, "-c", "android.intent.category.LAUNCHER", "1") -AllowFailure
Start-Sleep -Seconds $LaunchWaitSeconds

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

$screenshotCaptured = $false
$remoteScreenshotPath = "/sdcard/catguard-qa-screen-$timestamp.png"
$screencapResult = Invoke-TargetAdb -Arguments @("shell", "screencap", "-p", $remoteScreenshotPath) -AllowFailure
if ($screencapResult.ExitCode -eq 0) {
    $pullResult = Invoke-TargetAdb -Arguments @("pull", $remoteScreenshotPath, $screenshotPath) -AllowFailure
    $screenshotCaptured = $pullResult.ExitCode -eq 0 -and (Test-Path -LiteralPath $screenshotPath)
    Invoke-TargetAdb -Arguments @("shell", "rm", $remoteScreenshotPath) -AllowFailure | Out-Null
}

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
$frameRateLines = @($displayResult.Output | Where-Object { $_ -match "(?i)fps|refresh|frameRate|DisplayDeviceInfo|mode" } | Select-Object -First 80)

$summary = [pscustomobject]@{
    timestampUtc = (Get-Date).ToUniversalTime().ToString("o")
    deviceSerial = $targetSerial
    packageName = $PackageName
    apkPath = $resolvedApkPath
    skipInstall = [bool]$SkipInstall
    offlineRequested = $offlineRequested
    launchExitCode = $launchResult.ExitCode
    appPid = $appPid
    launched = $appPid.Length -gt 0
    fatalPatternCount = $fatalHits.Count
    currentFocusFound = $focusLines.Count -gt 0
    saveReadable = $saveReadable
    screenshotCaptured = $screenshotCaptured
    runDir = $runDir
    logcatPath = $logcatPath
    windowPath = $windowPath
    displayPath = $displayPath
    gfxInfoPath = $gfxInfoPath
    screenshotPath = if ($screenshotCaptured) { $screenshotPath } else { "" }
    savePath = if ($saveReadable) { $savePath } else { "" }
    focusLines = $focusLines
    frameRateLines = $frameRateLines
    fatalLines = $fatalHits
}

$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $summaryPath -Encoding UTF8

Write-Host "QA summary: $summaryPath"
Write-Host "App PID: $appPid"
Write-Host "Fatal crash pattern count: $($fatalHits.Count)"
Write-Host "Screenshot captured: $screenshotCaptured"
Write-Host "Save readable: $saveReadable"

if ($appPid.Length -eq 0 -or $fatalHits.Count -gt 0) {
    exit 1
}

exit 0
