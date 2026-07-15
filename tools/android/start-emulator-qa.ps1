param(
    [string]$AvdName = "CatGuard_API34",
    [string]$ApkPath = "Builds\Android\CatGuardTowerDefense-emulator.apk",
    [string]$PackageName = "com.catguard.towerdefense.qa",
    [int]$BootTimeoutSeconds = 180,
    [int]$LaunchWaitSeconds = 25
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$androidSdk = Join-Path $env:LOCALAPPDATA "Android\Sdk"
$emulatorPath = Join-Path $androidSdk "emulator\emulator.exe"
$adbPath = Join-Path $androidSdk "platform-tools\adb.exe"
$resolvedApkPath = if ([System.IO.Path]::IsPathRooted($ApkPath)) {
    $ApkPath
}
else {
    Join-Path $repoRoot $ApkPath
}

foreach ($requiredPath in @($emulatorPath, $adbPath, $resolvedApkPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required file was not found: $requiredPath"
    }
}

$availableAvds = @(& $emulatorPath -list-avds)
if ($availableAvds -notcontains $AvdName) {
    throw "Android virtual device '$AvdName' was not found."
}

& $adbPath start-server | Out-Null
$serial = [string](@(& $adbPath devices | Select-String '^emulator-\d+\s+device$' | ForEach-Object {
    ($_ -split '\s+')[0]
} | Select-Object -First 1))

if (-not $serial) {
    $logFolder = Join-Path $repoRoot "Builds\Android\emulator"
    New-Item -ItemType Directory -Force -Path $logFolder | Out-Null
    Start-Process -FilePath $emulatorPath -ArgumentList @(
        '-avd', $AvdName,
        '-no-boot-anim',
        '-no-snapshot',
        '-no-audio',
        '-gpu', 'swiftshader_indirect',
        '-netdelay', 'none',
        '-netspeed', 'full'
    ) -RedirectStandardOutput (Join-Path $logFolder "emulator.stdout.log") `
        -RedirectStandardError (Join-Path $logFolder "emulator.stderr.log")
}

$deadline = (Get-Date).AddSeconds($BootTimeoutSeconds)
$bootCompleted = ""
do {
    $serial = [string](@(& $adbPath devices | Select-String '^emulator-\d+\s+device$' | ForEach-Object {
        ($_ -split '\s+')[0]
    } | Select-Object -First 1))

    if ($serial) {
        $bootCompleted = (& $adbPath -s $serial shell getprop sys.boot_completed 2>$null).Trim()
        if ($bootCompleted -eq '1') {
            break
        }
    }

    Start-Sleep -Seconds 3
} while ((Get-Date) -lt $deadline)

if (-not $serial -or $bootCompleted -ne '1') {
    throw "Emulator '$AvdName' did not finish booting within $BootTimeoutSeconds seconds."
}

& $adbPath -s $serial shell settings put system screen_off_timeout 2147483647
& $adbPath -s $serial shell svc power stayon true
& $adbPath -s $serial shell settings put secure immersive_mode_confirmations confirmed
& $adbPath -s $serial shell settings put global hide_error_dialogs 1
& $adbPath -s $serial shell wm dismiss-keyguard

Write-Host "Installing emulator APK on $serial..."
& $adbPath -s $serial install -r -t $resolvedApkPath
if ($LASTEXITCODE -ne 0) {
    throw "APK installation failed with exit code $LASTEXITCODE."
}

$env:ANDROID_HOME = $androidSdk
$env:ANDROID_SDK_ROOT = $androidSdk
$qaScript = Join-Path $PSScriptRoot "run-device-qa.ps1"

Write-Host "Launching Cat Guard and collecting QA evidence..."
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $qaScript `
    -ApkPath $resolvedApkPath `
    -PackageName $PackageName `
    -DeviceSerial $serial `
    -LaunchWaitSeconds $LaunchWaitSeconds `
    -SkipInstall

if ($LASTEXITCODE -ne 0) {
    throw "Device QA failed with exit code $LASTEXITCODE."
}

Write-Host "Emulator is ready and remains open: $serial"
