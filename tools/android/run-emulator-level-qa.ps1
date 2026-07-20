param(
    [string]$LevelId = "level_10",
    [string[]]$TowerIds = @(
        "cat_dart",
        "yarn_cannon",
        "bell_sniper",
        "laser_pointer",
        "blanket_boom",
        "blanket_boom",
        "yarn_cannon",
        "bell_sniper",
        "laser_pointer",
        "cat_dart",
        "blanket_boom",
        "bell_sniper"
    ),
    [string]$ScenarioId = "",
    [string]$ApkPath = "Builds\Android\CatGuardTowerDefense-emulator.apk",
    [string]$PackageName = "com.catguard.towerdefense.qa",
    [string]$DeviceSerial = "",
    [string]$OutputDir = "Builds\Android\qa-device\level-scenarios",
    [int]$TimeoutSeconds = 180,
    [int]$CombatSampleDelaySeconds = 12,
    [double]$MinimumAverageFps = 30,
    [double]$MaximumP95FrameTimeMs = 50,
    [switch]$SkipInstall,
    [switch]$RequireVictory,
    [switch]$RequirePerformance
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
    throw "ADB не найден: $adbPath"
}

if (-not $SkipInstall -and -not (Test-Path -LiteralPath $resolvedApkPath)) {
    throw "APK не найден: $resolvedApkPath"
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
        throw "ADB завершился с кодом ${exitCode}: adb $($Arguments -join ' ')`n$($output -join [Environment]::NewLine)"
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

function Get-Percentile {
    param(
        [double[]]$Values,
        [double]$Percentile
    )

    if ($Values.Count -eq 0) {
        return 0
    }

    $sorted = @($Values | Sort-Object)
    $rank = ([Math]::Max(0, [Math]::Min(100, $Percentile)) / 100) * ($sorted.Count - 1)
    $lowerIndex = [Math]::Floor($rank)
    $upperIndex = [Math]::Ceiling($rank)
    if ($lowerIndex -eq $upperIndex) {
        return [double]$sorted[$lowerIndex]
    }

    $weight = $rank - $lowerIndex
    return ([double]$sorted[$lowerIndex] * (1 - $weight)) + ([double]$sorted[$upperIndex] * $weight)
}

function Get-FrameMetrics {
    param([string[]]$Lines)

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

    $durationSeconds = if ($orderedTimestamps.Count -gt 1) {
        ([long]$orderedTimestamps[-1] - [long]$orderedTimestamps[0]) / 1000000000.0
    }
    else {
        0
    }
    $averageFps = if ($durationSeconds -gt 0) { $frameTimesMs.Count / $durationSeconds } else { 0 }
    $refreshPeriodMs = if ($refreshPeriodNanoseconds -gt 0) { $refreshPeriodNanoseconds / 1000000.0 } else { 0 }
    $jankThresholdMs = if ($refreshPeriodMs -gt 0) { $refreshPeriodMs * 1.5 } else { 25 }
    $jankyFrames = @($frameTimesMs | Where-Object { $_ -gt $jankThresholdMs }).Count

    return [pscustomobject]@{
        sampleCount = $frameTimesMs.Count
        durationSeconds = [Math]::Round($durationSeconds, 2)
        averageFps = [Math]::Round($averageFps, 2)
        medianFrameTimeMs = [Math]::Round((Get-Percentile -Values $frameTimesMs.ToArray() -Percentile 50), 2)
        p95FrameTimeMs = [Math]::Round((Get-Percentile -Values $frameTimesMs.ToArray() -Percentile 95), 2)
        jankPercent = if ($frameTimesMs.Count -gt 0) {
            [Math]::Round(($jankyFrames * 100.0) / $frameTimesMs.Count, 2)
        }
        else {
            0
        }
    }
}

function Capture-Screenshot {
    param(
        [string]$RemotePath,
        [string]$LocalPath
    )

    Invoke-TargetAdb -Arguments @("shell", "screencap", "-p", $RemotePath) | Out-Null
    Invoke-TargetAdb -Arguments @("pull", $RemotePath, $LocalPath) | Out-Null
    Invoke-TargetAdb -Arguments @("shell", "rm", "-f", $RemotePath) -AllowFailure | Out-Null
}

Invoke-Adb -Arguments @("start-server") | Out-Null
$deviceLines = @((Invoke-Adb -Arguments @("devices", "-l")).Output | Where-Object { $_ -match '^\S+\s+device\b' })
if (-not @($deviceLines | Where-Object { $_ -match '^emulator-\d+\s+' -or $_ -match '\bmodel:sdk_' })) {
    Invoke-Adb -Arguments @("connect", "127.0.0.1:5555") -AllowFailure | Out-Null
    $deviceLines = @((Invoke-Adb -Arguments @("devices", "-l")).Output | Where-Object { $_ -match '^\S+\s+device\b' })
}

if ($DeviceSerial) {
    $script:TargetSerial = $DeviceSerial
}
else {
    $script:TargetSerial = [string](@($deviceLines | Where-Object {
        $_ -match '^emulator-\d+\s+' -or $_ -match '\bmodel:sdk_'
    } | ForEach-Object { ($_ -split '\s+')[0] } | Select-Object -First 1))
}

if (-not $script:TargetSerial) {
    throw "Запущенный Android-эмулятор не найден. Сначала выполните tools/android/start-emulator-qa.ps1."
}

$isEmulator = ((Invoke-TargetAdb -Arguments @("shell", "getprop", "ro.kernel.qemu")).Output -join "").Trim() -eq "1"
if (-not $isEmulator) {
    throw "Цель $script:TargetSerial не является эмулятором. Этот скрипт не запускает сценарии на физическом телефоне."
}

if (-not $SkipInstall) {
    Write-Host "Устанавливаю Development APK на $script:TargetSerial..."
    Invoke-TargetAdb -Arguments @("install", "-r", "-t", $resolvedApkPath) | Out-Null
}

$effectiveScenarioId = if ($ScenarioId) { $ScenarioId } else { "$LevelId-mixed" }
$safeScenarioId = $effectiveScenarioId -replace '[^a-zA-Z0-9._-]', '_'
$runDir = Join-Path $resolvedOutputDir ("{0}-{1}" -f (Get-Date -Format "yyyyMMdd-HHmmss"), $safeScenarioId)
New-Item -ItemType Directory -Force -Path $runDir | Out-Null

$commandPath = Join-Path $runDir "catguard-qa-command.json"
$resultPath = Join-Path $runDir "catguard-qa-result.json"
$latencyPath = Join-Path $runDir "combat-surface-latency.txt"
$combatScreenshotPath = Join-Path $runDir "combat-screen.png"
$resultScreenshotPath = Join-Path $runDir "result-screen.png"
$logcatPath = Join-Path $runDir "logcat.txt"
$summaryPath = Join-Path $runDir "qa-summary.json"
$remoteCommandPath = "/data/local/tmp/catguard-qa-command.json"
$externalFilesPath = "/sdcard/Android/data/$PackageName/files"

[pscustomobject]@{
    scenarioId = $effectiveScenarioId
    levelId = $LevelId
    towerIds = $TowerIds
} | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $commandPath -Encoding UTF8

Invoke-TargetAdb -Arguments @("shell", "am", "force-stop", $PackageName) -AllowFailure | Out-Null
Invoke-TargetAdb -Arguments @("shell", "rm", "-f", "$externalFilesPath/catguard-qa-command.json", "$externalFilesPath/catguard-qa-result.json") -AllowFailure | Out-Null
Invoke-TargetAdb -Arguments @("shell", "run-as", $PackageName, "mkdir", "-p", "files") | Out-Null
Invoke-TargetAdb -Arguments @("shell", "run-as", $PackageName, "rm", "-f", "files/catguard-qa-command.json", "files/catguard-qa-result.json") | Out-Null
Invoke-TargetAdb -Arguments @("push", $commandPath, $remoteCommandPath) | Out-Null
Invoke-TargetAdb -Arguments @("shell", "run-as", $PackageName, "cp", $remoteCommandPath, "files/catguard-qa-command.json") | Out-Null
Invoke-TargetAdb -Arguments @("logcat", "-c") -AllowFailure | Out-Null
Invoke-TargetAdb -Arguments @("shell", "dumpsys", "SurfaceFlinger", "--latency-clear") -AllowFailure | Out-Null

Write-Host "Запускаю сценарий '$effectiveScenarioId' на уровне '$LevelId'..."
Invoke-TargetAdb -Arguments @(
    "shell",
    "am",
    "start",
    "-n",
    "$PackageName/com.unity3d.player.UnityPlayerGameActivity") | Out-Null

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
Start-Sleep -Seconds ([Math]::Max(1, $CombatSampleDelaySeconds))

$surfaceList = (Invoke-TargetAdb -Arguments @("shell", "dumpsys", "SurfaceFlinger", "--list") -AllowFailure).Output
$surfaceLayer = [string](@($surfaceList | Where-Object {
    $_ -like "*$PackageName*" -and $_ -like "*SurfaceView*" -and $_ -like "*(BLAST)*"
} | Select-Object -First 1))
$latencyLines = @()
if ($surfaceLayer) {
    $latencyLines = @((Invoke-TargetAdb -Arguments @(
        "shell",
        "dumpsys",
        "SurfaceFlinger",
        "--latency",
        "'$surfaceLayer'") -AllowFailure).Output)
}
$latencyLines | Set-Content -LiteralPath $latencyPath -Encoding UTF8
Capture-Screenshot -RemotePath "/sdcard/catguard-combat.png" -LocalPath $combatScreenshotPath

$resultReady = $false
do {
    $probe = Invoke-TargetAdb -Arguments @(
        "shell",
        "run-as",
        $PackageName,
        "test",
        "-f",
        "files/catguard-qa-result.json") -AllowFailure
    $resultReady = $probe.ExitCode -eq 0
    if (-not $resultReady) {
        Start-Sleep -Seconds 2
    }
} while (-not $resultReady -and (Get-Date) -lt $deadline)

if (-not $resultReady) {
    throw "Сценарий не завершился за $TimeoutSeconds секунд. Материалы запуска: $runDir"
}

$resultLines = (Invoke-TargetAdb -Arguments @(
    "shell",
    "run-as",
    $PackageName,
    "cat",
    "files/catguard-qa-result.json")).Output
$resultLines | Set-Content -LiteralPath $resultPath -Encoding UTF8
$scenarioResult = ($resultLines -join [Environment]::NewLine) | ConvertFrom-Json
Capture-Screenshot -RemotePath "/sdcard/catguard-result.png" -LocalPath $resultScreenshotPath

$logcatLines = (Invoke-TargetAdb -Arguments @("logcat", "-d", "-v", "time") -AllowFailure).Output
$logcatLines | Set-Content -LiteralPath $logcatPath -Encoding UTF8
$fatalPattern = "FATAL EXCEPTION|Fatal signal|Abort message|NullReferenceException|MissingMethodException|DllNotFoundException"
$fatalLines = @($logcatLines | Where-Object { $_ -match $fatalPattern })
$performance = Get-FrameMetrics -Lines $latencyLines
$performancePassed = $performance.sampleCount -ge 30 `
    -and $performance.averageFps -ge $MinimumAverageFps `
    -and $performance.p95FrameTimeMs -le $MaximumP95FrameTimeMs

$summary = [pscustomobject]@{
    timestampUtc = (Get-Date).ToUniversalTime().ToString("o")
    deviceSerial = $script:TargetSerial
    scenarioId = $effectiveScenarioId
    levelId = $LevelId
    result = $scenarioResult
    fatalPatternCount = $fatalLines.Count
    performance = $performance
    performancePassed = $performancePassed
    runDir = $runDir
}
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $summaryPath -Encoding UTF8

Write-Host "Результат: $($scenarioResult.state), жизни: $($scenarioResult.lives), побеждено: $($scenarioResult.defeatedEnemies), пропущено: $($scenarioResult.escapedEnemies)."
Write-Host "Боевая производительность: $($performance.averageFps) FPS, P95 $($performance.p95FrameTimeMs) мс, кадров $($performance.sampleCount)."
Write-Host "Фатальные ошибки: $($fatalLines.Count)."
Write-Host "Материалы QA: $runDir"

$failed = $scenarioResult.state -eq "error" `
    -or $fatalLines.Count -gt 0 `
    -or ($RequireVictory -and $scenarioResult.state -ne "won") `
    -or ($RequirePerformance -and -not $performancePassed)
if ($failed) {
    exit 1
}

exit 0
