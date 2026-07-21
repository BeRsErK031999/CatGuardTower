param(
    [string]$ApkPath = "Builds\Android\CatGuardTowerDefense-emulator.apk",
    [string]$PackageName = "com.catguard.towerdefense.qa",
    [string]$DeviceSerial = "",
    [string]$OutputDir = "Builds\Android\qa-device\e1-landscape",
    [int]$LaunchWaitSeconds = 8,
    [switch]$SkipInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$adbPath = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
$resolvedApkPath = if ([System.IO.Path]::IsPathRooted($ApkPath)) {
    $ApkPath
}
else {
    Join-Path $repoRoot $ApkPath
}
$resolvedOutputDir = if ([System.IO.Path]::IsPathRooted($OutputDir)) {
    $OutputDir
}
else {
    Join-Path $repoRoot $OutputDir
}

if (-not (Test-Path -LiteralPath $adbPath)) {
    throw "ADB was not found: $adbPath"
}

if (-not $SkipInstall -and -not (Test-Path -LiteralPath $resolvedApkPath)) {
    throw "APK was not found: $resolvedApkPath"
}

function Invoke-Adb {
    param(
        [string[]]$Arguments,
        [switch]$AllowFailure
    )

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = @(& $adbPath @Arguments 2>&1 | ForEach-Object { $_.ToString() })
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw "ADB failed with exit code ${exitCode}: adb $($Arguments -join ' ')`n$($output -join [Environment]::NewLine)"
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

    return Invoke-Adb -Arguments (@("-s", $script:TargetSerial) + $Arguments) -AllowFailure:$AllowFailure
}

function Get-RunningEmulator {
    $lines = (Invoke-Adb -Arguments @("devices", "-l")).Output
    return [string](@($lines | Where-Object {
        $_ -match '^emulator-\d+\s+device\b' -or $_ -match '^\S+\s+device\b.*\bmodel:sdk_'
    } | ForEach-Object { ($_ -split '\s+')[0] } | Select-Object -First 1))
}

function Get-SurfaceOrientation {
    $windowLines = (Invoke-TargetAdb -Arguments @("shell", "dumpsys", "window", "displays") -AllowFailure).Output
    $orientationLine = [string](@($windowLines | Where-Object {
        $_ -match '^\s*mRotation=\d'
    } | Select-Object -First 1))

    if ($orientationLine -match 'mRotation=(\d+)') {
        return [int]$Matches[1]
    }

    return -1
}

function Capture-Screenshot {
    param(
        [string]$Name,
        [int]$ExpectedSurfaceOrientation = -1
    )

    $localPath = Join-Path $script:RunDir "$Name.png"
    $remotePath = "/sdcard/catguard-$Name.png"
    Invoke-TargetAdb -Arguments @("shell", "screencap", "-p", $remotePath) | Out-Null
    Invoke-TargetAdb -Arguments @("pull", $remotePath, $localPath) | Out-Null
    Invoke-TargetAdb -Arguments @("shell", "rm", "-f", $remotePath) -AllowFailure | Out-Null

    Add-Type -AssemblyName System.Drawing
    $image = [System.Drawing.Image]::FromFile($localPath)
    try {
        $width = $image.Width
        $height = $image.Height
    }
    finally {
        $image.Dispose()
    }

    $surfaceOrientation = Get-SurfaceOrientation
    $landscape = $width -gt $height
    $directionConfirmed = $ExpectedSurfaceOrientation -lt 0 `
        -or $surfaceOrientation -eq $ExpectedSurfaceOrientation
    $record = [pscustomobject]@{
        name = $Name
        path = $localPath
        width = $width
        height = $height
        landscape = $landscape
        surfaceOrientation = $surfaceOrientation
        expectedSurfaceOrientation = $ExpectedSurfaceOrientation
        directionConfirmed = $directionConfirmed
        passed = $landscape -and $directionConfirmed
    }
    $script:Screenshots.Add($record)
    return $record
}

function Set-Viewport {
    param(
        [int]$PortraitWidth,
        [int]$PortraitHeight
    )

    Invoke-TargetAdb -Arguments @("shell", "wm", "size", "${PortraitWidth}x${PortraitHeight}") | Out-Null
    Start-Sleep -Seconds 2
}

function Set-Rotation {
    param([int]$Rotation)

    if ($Rotation -eq 0) {
        Invoke-TargetAdb -Arguments @("shell", "cmd", "window", "user-rotation", "lock", "0") | Out-Null
        Start-Sleep -Seconds 3
        return
    }

    Invoke-TargetAdb -Arguments @("shell", "cmd", "window", "user-rotation", "free") | Out-Null
    foreach ($attempt in 0..3) {
        if ((Get-SurfaceOrientation) -eq $Rotation) {
            return
        }

        Invoke-TargetAdb -Arguments @("emu", "rotate") | Out-Null
        Start-Sleep -Seconds 3
    }

    throw "Emulator did not reach display rotation $Rotation."
}

function Start-AppFromPortrait {
    Invoke-TargetAdb -Arguments @("shell", "am", "force-stop", $PackageName) -AllowFailure | Out-Null
    Set-Rotation -Rotation 0
    Invoke-TargetAdb -Arguments @("logcat", "-c") -AllowFailure | Out-Null
    Invoke-TargetAdb -Arguments @(
        "shell",
        "am",
        "start",
        "-n",
        "$PackageName/com.unity3d.player.UnityPlayerGameActivity") | Out-Null
    Start-Sleep -Seconds $LaunchWaitSeconds
}

function Tap-Normalized {
    param(
        [double]$X,
        [double]$Y
    )

    $current = $script:Screenshots[$script:Screenshots.Count - 1]
    $tapX = [Math]::Round($current.width * $X)
    $tapY = [Math]::Round($current.height * $Y)
    Invoke-TargetAdb -Arguments @("shell", "input", "tap", "$tapX", "$tapY") | Out-Null
    Start-Sleep -Seconds 2
}

Invoke-Adb -Arguments @("start-server") | Out-Null
$script:TargetSerial = if ($DeviceSerial) { $DeviceSerial } else { Get-RunningEmulator }
if (-not $script:TargetSerial) {
    throw "A running Android emulator was not found. Start tools/android/start-emulator-qa.ps1 first."
}

$isEmulator = ((Invoke-TargetAdb -Arguments @("shell", "getprop", "ro.kernel.qemu")).Output -join "").Trim() -eq "1"
if (-not $isEmulator) {
    throw "Target $script:TargetSerial is not an Android emulator."
}

if (-not $SkipInstall) {
    Invoke-TargetAdb -Arguments @("install", "-r", "-t", $resolvedApkPath) | Out-Null
}

$script:RunDir = Join-Path $resolvedOutputDir (Get-Date -Format "yyyyMMdd-HHmmss")
New-Item -ItemType Directory -Force -Path $script:RunDir | Out-Null
$script:Screenshots = New-Object System.Collections.Generic.List[object]
$originalSizeLines = (Invoke-TargetAdb -Arguments @("shell", "wm", "size")).Output
$originalAccelerometerRotation = ((Invoke-TargetAdb -Arguments @(
    "shell", "settings", "get", "system", "accelerometer_rotation") -AllowFailure).Output -join "").Trim()
$originalUserRotation = ((Invoke-TargetAdb -Arguments @(
    "shell", "settings", "get", "system", "user_rotation") -AllowFailure).Output -join "").Trim()

try {
    Set-Viewport -PortraitWidth 1080 -PortraitHeight 1920
    Start-AppFromPortrait
    $startup = Capture-Screenshot -Name "01-16x9-started-from-portrait"

    Tap-Normalized -X 0.94 -Y 0.065
    $privacy = Capture-Screenshot -Name "02-privacy"
    Tap-Normalized -X 0.5 -Y 0.84

    Tap-Normalized -X 0.085 -Y 0.285
    $upgrades = Capture-Screenshot -Name "03-upgrades"
    Tap-Normalized -X 0.085 -Y 0.36
    $daily = Capture-Screenshot -Name "04-daily"

    Tap-Normalized -X 0.515 -Y 0.95
    $language = Capture-Screenshot -Name "05-language-toggled"
    Tap-Normalized -X 0.61 -Y 0.95
    $sound = Capture-Screenshot -Name "06-sound-toggled"
    Tap-Normalized -X 0.7 -Y 0.95
    $resetConfirmation = Capture-Screenshot -Name "07-reset-confirmation"

    Set-Rotation -Rotation 3
    $landscapeRight = Capture-Screenshot -Name "08-landscape-right" -ExpectedSurfaceOrientation 3
    Set-Rotation -Rotation 1
    $landscapeLeft = Capture-Screenshot -Name "09-landscape-left" -ExpectedSurfaceOrientation 1

    Tap-Normalized -X 0.085 -Y 0.215
    Tap-Normalized -X 0.37 -Y 0.82
    Start-Sleep -Seconds 5
    $levelPreparing = Capture-Screenshot -Name "10-level-preparing"
    Tap-Normalized -X 0.12 -Y 0.94
    Tap-Normalized -X 0.36 -Y 0.56
    Tap-Normalized -X 0.44 -Y 0.62
    $towerPlacement = Capture-Screenshot -Name "11-tower-placement"
    Tap-Normalized -X 0.88 -Y 0.94
    Start-Sleep -Seconds 5
    $activeWave = Capture-Screenshot -Name "12-active-wave"

    Set-Viewport -PortraitWidth 1080 -PortraitHeight 2400
    Start-AppFromPortrait
    $wideStartup = Capture-Screenshot -Name "13-wide-started-from-portrait"
    Set-Rotation -Rotation 3
    $wideRight = Capture-Screenshot -Name "14-wide-landscape-right" -ExpectedSurfaceOrientation 3
    Set-Rotation -Rotation 1
    $wideLeft = Capture-Screenshot -Name "15-wide-landscape-left" -ExpectedSurfaceOrientation 1

    $logcatLines = (Invoke-TargetAdb -Arguments @("logcat", "-d", "-v", "time") -AllowFailure).Output
    $logcatPath = Join-Path $script:RunDir "logcat.txt"
    $logcatLines | Set-Content -LiteralPath $logcatPath -Encoding UTF8
    $fatalPattern = "FATAL EXCEPTION|Fatal signal|Abort message|NullReferenceException|MissingMethodException|DllNotFoundException| E/AndroidRuntime"
    $fatalLines = @($logcatLines | Where-Object { $_ -match $fatalPattern })
    $failedScreenshots = @($script:Screenshots | Where-Object { -not $_.passed })

    $summary = [pscustomobject]@{
        timestampUtc = (Get-Date).ToUniversalTime().ToString("o")
        deviceSerial = $script:TargetSerial
        apkPath = $resolvedApkPath
        packageName = $PackageName
        originalWmSize = $originalSizeLines
        startedFromPortrait = $startup.landscape
        sixteenByNineConfirmed = $startup.width -eq 1920 -and $startup.height -eq 1080
        wideConfirmed = $wideStartup.width -eq 2400 -and $wideStartup.height -eq 1080
        landscapeLeftConfirmed = $landscapeLeft.directionConfirmed -and $wideLeft.directionConfirmed
        landscapeRightConfirmed = $landscapeRight.directionConfirmed -and $wideRight.directionConfirmed
        uiScenarios = @(
            "levels",
            "upgrades",
            "daily",
            "language",
            "sound",
            "privacy",
            "reset-confirmation",
            "level-preparing",
            "tower-placement",
            "active-wave"
        )
        screenshots = $script:Screenshots
        fatalPatternCount = $fatalLines.Count
        fatalLines = $fatalLines
        passed = $failedScreenshots.Count -eq 0 `
            -and $startup.width -eq 1920 `
            -and $startup.height -eq 1080 `
            -and $wideStartup.width -eq 2400 `
            -and $wideStartup.height -eq 1080 `
            -and $fatalLines.Count -eq 0
        runDir = $script:RunDir
    }
    $summaryPath = Join-Path $script:RunDir "qa-summary.json"
    $summary | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $summaryPath -Encoding UTF8

    Write-Host "E1 landscape QA summary: $summaryPath"
    Write-Host "Portrait start -> landscape: $($summary.startedFromPortrait)"
    Write-Host "16:9: $($summary.sixteenByNineConfirmed); wide: $($summary.wideConfirmed)"
    Write-Host "Landscape Left: $($summary.landscapeLeftConfirmed); Landscape Right: $($summary.landscapeRightConfirmed)"
    Write-Host "Fatal errors: $($fatalLines.Count)"

    if (-not $summary.passed) {
        exit 1
    }
}
finally {
    Invoke-TargetAdb -Arguments @("shell", "wm", "size", "reset") -AllowFailure | Out-Null
    if ($originalAccelerometerRotation -match '^\d+$') {
        Invoke-TargetAdb -Arguments @(
            "shell", "settings", "put", "system", "accelerometer_rotation", $originalAccelerometerRotation) -AllowFailure | Out-Null
    }
    if ($originalUserRotation -match '^\d+$') {
        Invoke-TargetAdb -Arguments @(
            "shell", "settings", "put", "system", "user_rotation", $originalUserRotation) -AllowFailure | Out-Null
    }
    if ($originalAccelerometerRotation -eq '1') {
        Invoke-TargetAdb -Arguments @("shell", "cmd", "window", "user-rotation", "free") -AllowFailure | Out-Null
    }
}

exit 0
