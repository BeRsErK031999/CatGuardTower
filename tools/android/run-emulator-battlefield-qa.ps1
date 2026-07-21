param(
    [string]$ApkPath = "Builds\Android\CatGuardTowerDefense-emulator.apk",
    [string]$PackageName = "com.catguard.towerdefense.qa",
    [string]$DeviceSerial = "",
    [string]$OutputDir = "Builds\Android\qa-device\e2-battlefield",
    [int]$LaunchTimeoutSeconds = 30,
    [switch]$SkipInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$adbPath = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
$resolvedApkPath = if ([System.IO.Path]::IsPathRooted($ApkPath)) { $ApkPath } else { Join-Path $repoRoot $ApkPath }
$resolvedOutputDir = if ([System.IO.Path]::IsPathRooted($OutputDir)) { $OutputDir } else { Join-Path $repoRoot $OutputDir }

if (-not (Test-Path -LiteralPath $adbPath)) {
    throw "ADB was not found: $adbPath"
}

if (-not $SkipInstall -and -not (Test-Path -LiteralPath $resolvedApkPath)) {
    throw "APK was not found: $resolvedApkPath"
}

function Invoke-Adb {
    param([string[]]$Arguments, [switch]$AllowFailure)

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

    return [pscustomobject]@{ ExitCode = $exitCode; Output = [string[]]$output }
}

function Invoke-TargetAdb {
    param([string[]]$Arguments, [switch]$AllowFailure)
    return Invoke-Adb -Arguments (@("-s", $script:TargetSerial) + $Arguments) -AllowFailure:$AllowFailure
}

function Get-RunningEmulator {
    $lines = (Invoke-Adb -Arguments @("devices", "-l")).Output
    return [string](@($lines | Where-Object {
        $_ -match '^emulator-\d+\s+device\b' -or $_ -match '^\S+\s+device\b.*\bmodel:sdk_'
    } | ForEach-Object { ($_ -split '\s+')[0] } | Select-Object -First 1))
}

function Capture-Screenshot {
    param([string]$Name)

    $localPath = Join-Path $script:RunDir "$Name.png"
    $remotePath = "/sdcard/catguard-$Name.png"
    Invoke-TargetAdb -Arguments @("shell", "screencap", "-p", $remotePath) | Out-Null
    Invoke-TargetAdb -Arguments @("pull", $remotePath, $localPath) | Out-Null
    Invoke-TargetAdb -Arguments @("shell", "rm", "-f", $remotePath) -AllowFailure | Out-Null

    Add-Type -AssemblyName System.Drawing
    $image = [System.Drawing.Image]::FromFile($localPath)
    try {
        $record = [pscustomobject]@{
            name = $Name
            path = $localPath
            width = $image.Width
            height = $image.Height
            landscape = $image.Width -gt $image.Height
        }
    }
    finally {
        $image.Dispose()
    }

    $script:Screenshots.Add($record)
    return $record
}

function Start-ManualScenario {
    param([string]$LevelId, [string]$ScenarioId)

    $commandPath = Join-Path $script:RunDir "$ScenarioId-command.json"
    [pscustomobject]@{
        scenarioId = $ScenarioId
        levelId = $LevelId
        towerIds = @()
        manualInput = $true
    } | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $commandPath -Encoding UTF8

    $remoteCommandPath = "/data/local/tmp/catguard-e2-qa-command.json"
    Invoke-TargetAdb -Arguments @("shell", "am", "force-stop", $PackageName) -AllowFailure | Out-Null
    Invoke-TargetAdb -Arguments @("shell", "run-as", $PackageName, "mkdir", "-p", "files") | Out-Null
    Invoke-TargetAdb -Arguments @(
        "shell", "run-as", $PackageName, "rm", "-f",
        "files/catguard-qa-command.json",
        "files/catguard-qa-result.json",
        "files/catguard-qa-snapshot.json") | Out-Null
    Invoke-TargetAdb -Arguments @("push", $commandPath, $remoteCommandPath) | Out-Null
    Invoke-TargetAdb -Arguments @(
        "shell", "run-as", $PackageName, "cp", $remoteCommandPath, "files/catguard-qa-command.json") | Out-Null
    Invoke-TargetAdb -Arguments @(
        "shell", "am", "start", "-n", "$PackageName/com.unity3d.player.UnityPlayerGameActivity") | Out-Null

    $snapshot = Wait-Snapshot -ScenarioId $ScenarioId
    Start-Sleep -Milliseconds 750
    return $snapshot
}

function Wait-Snapshot {
    param(
        [string]$ScenarioId,
        [scriptblock]$Predicate = { param($value) $true }
    )

    $deadline = (Get-Date).AddSeconds($LaunchTimeoutSeconds)
    do {
        $probe = Invoke-TargetAdb -Arguments @(
            "shell", "run-as", $PackageName, "cat", "files/catguard-qa-snapshot.json") -AllowFailure
        if ($probe.ExitCode -eq 0 -and $probe.Output.Count -gt 0) {
            try {
                $snapshot = ($probe.Output -join [Environment]::NewLine) | ConvertFrom-Json
                if ($snapshot.scenarioId -eq $ScenarioId -and (& $Predicate $snapshot)) {
                    return $snapshot
                }
            }
            catch {
                # The game can replace the snapshot while run-as is reading it; retry the next poll.
            }
        }

        Start-Sleep -Milliseconds 350
    } while ((Get-Date) -lt $deadline)

    throw "QA snapshot '$ScenarioId' did not reach the requested state within $LaunchTimeoutSeconds seconds."
}

function Tap-Cell {
    param(
        [object]$Snapshot,
        [object]$Cell
    )

    $beforeCount = [int]$Snapshot.towerCount
    Invoke-TargetAdb -Arguments @("shell", "input", "tap", "$($Cell.screenX)", "$($Cell.screenY)") | Out-Null
    return Wait-Snapshot -ScenarioId $Snapshot.scenarioId -Predicate {
        param($value)
        [int]$value.towerCount -gt $beforeCount
    }
}

function Get-VisibleCell {
    param(
        [object]$Snapshot,
        [ValidateSet("MinX", "MaxX", "MinY", "MaxY", "Center")]
        [string]$Order
    )

    $cells = @($Snapshot.cells | Where-Object { $_.visible -and -not $_.occupied })
    if ($cells.Count -eq 0) {
        throw "No visible unoccupied cells exist for scenario '$($Snapshot.scenarioId)'."
    }

    switch ($Order) {
        "MinX" { return $cells | Sort-Object worldX, worldY | Select-Object -First 1 }
        "MaxX" { return $cells | Sort-Object worldX, worldY -Descending | Select-Object -First 1 }
        "MinY" { return $cells | Sort-Object worldY, worldX | Select-Object -First 1 }
        "MaxY" { return $cells | Sort-Object worldY, worldX -Descending | Select-Object -First 1 }
        default {
            return $cells | Sort-Object @{ Expression = {
                [Math]::Abs([double]$_.screenX - ($script:ScreenWidth * 0.5)) `
                    + [Math]::Abs([double]$_.screenY - ($script:ScreenHeight * 0.5))
            } } | Select-Object -First 1
        }
    }
}

function Swipe-Horizontally {
    param([ValidateSet("Left", "Right")][string]$Direction, [int]$Count = 1)

    $startX = if ($Direction -eq "Left") { [Math]::Round($script:ScreenWidth * 0.72) } else { [Math]::Round($script:ScreenWidth * 0.28) }
    $endX = if ($Direction -eq "Left") { [Math]::Round($script:ScreenWidth * 0.24) } else { [Math]::Round($script:ScreenWidth * 0.76) }
    $y = [Math]::Round($script:ScreenHeight * 0.5)
    for ($index = 0; $index -lt $Count; $index++) {
        Invoke-TargetAdb -Arguments @("shell", "input", "swipe", "$startX", "$y", "$endX", "$y", "220") | Out-Null
        Start-Sleep -Milliseconds 300
    }
}

Invoke-Adb -Arguments @("start-server") | Out-Null
$script:TargetSerial = if ($DeviceSerial) { $DeviceSerial } else { Get-RunningEmulator }
if (-not $script:TargetSerial) {
    throw "A running Android emulator was not found."
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
$originalSize = (Invoke-TargetAdb -Arguments @("shell", "wm", "size")).Output
$assertions = [ordered]@{
    fixedOverview = $false
    legacyCompatibility = $false
    rooftopLayout = $false
    scrollableLarge = $false
    dragSuppressedPlacement = $false
    cameraReachedMinimum = $false
    cameraReachedMaximum = $false
    northEdgePlacement = $false
    southEdgePlacement = $false
    westEdgePlacement = $false
    eastEdgePlacement = $false
}

try {
    Invoke-TargetAdb -Arguments @("logcat", "-c") -AllowFailure | Out-Null

    Invoke-TargetAdb -Arguments @("shell", "wm", "size", "1080x1920") | Out-Null
    Start-Sleep -Seconds 2
    $fixed = Start-ManualScenario -LevelId "level_01" -ScenarioId "e2-fixed-16x9"
    $fixedShot = Capture-Screenshot -Name "01-fixed-garden-16x9"
    $script:ScreenWidth = $fixedShot.width
    $script:ScreenHeight = $fixedShot.height
    $assertions.fixedOverview = $fixed.battlefieldId -eq "garden_gate_wide" `
        -and $fixed.cameraMode -eq "FixedOverview" `
        -and -not $fixed.legacyBattlefield
    $fixed = Tap-Cell -Snapshot $fixed -Cell (Get-VisibleCell -Snapshot $fixed -Order "MinY")
    $assertions.southEdgePlacement = [int]$fixed.towerCount -eq 1
    $fixed = Tap-Cell -Snapshot $fixed -Cell (Get-VisibleCell -Snapshot $fixed -Order "MaxY")
    $assertions.northEdgePlacement = [int]$fixed.towerCount -eq 2
    Capture-Screenshot -Name "02-fixed-edge-towers-16x9" | Out-Null

    $legacy = Start-ManualScenario -LevelId "level_03" -ScenarioId "e2-legacy-16x9"
    $assertions.legacyCompatibility = $legacy.legacyBattlefield -and $legacy.battlefieldId -eq "legacy_level_03"
    Capture-Screenshot -Name "03-legacy-level-16x9" | Out-Null

    Invoke-TargetAdb -Arguments @("shell", "wm", "size", "1080x2400") | Out-Null
    Start-Sleep -Seconds 2
    $scroll = Start-ManualScenario -LevelId "level_08" -ScenarioId "e2-scroll-wide"
    $scrollShot = Capture-Screenshot -Name "04-scroll-initial-wide"
    $script:ScreenWidth = $scrollShot.width
    $script:ScreenHeight = $scrollShot.height
    $assertions.scrollableLarge = $scroll.battlefieldId -eq "old_well_crossing" `
        -and $scroll.cameraMode -eq "ScrollableLarge" `
        -and ([double]$scroll.cameraMaxFocusX - [double]$scroll.cameraMinFocusX) -gt 1

    $dragCell = Get-VisibleCell -Snapshot $scroll -Order "Center"
    $beforeDragCount = [int]$scroll.towerCount
    $dragEndX = [Math]::Max(120, [int]$dragCell.screenX - 520)
    Invoke-TargetAdb -Arguments @(
        "shell", "input", "swipe", "$($dragCell.screenX)", "$($dragCell.screenY)", "$dragEndX", "$($dragCell.screenY)", "450") | Out-Null
    $scroll = Wait-Snapshot -ScenarioId $scroll.scenarioId -Predicate {
        param($value)
        $value.lastGestureWasDrag
    }
    $assertions.dragSuppressedPlacement = [int]$scroll.towerCount -eq $beforeDragCount

    Swipe-Horizontally -Direction "Right" -Count 10
    $scroll = Wait-Snapshot -ScenarioId $scroll.scenarioId -Predicate {
        param($value)
        [Math]::Abs([double]$value.cameraFocusX - [double]$value.cameraMinFocusX) -lt 0.06
    }
    $assertions.cameraReachedMinimum = $true
    Capture-Screenshot -Name "05-scroll-west-bound-wide" | Out-Null
    $westCell = Get-VisibleCell -Snapshot $scroll -Order "MinX"
    $scroll = Tap-Cell -Snapshot $scroll -Cell $westCell
    $assertions.westEdgePlacement = [int]$scroll.towerCount -eq 1 -and [double]$westCell.worldX -lt -8

    Swipe-Horizontally -Direction "Left" -Count 14
    $scroll = Wait-Snapshot -ScenarioId $scroll.scenarioId -Predicate {
        param($value)
        [Math]::Abs([double]$value.cameraFocusX - [double]$value.cameraMaxFocusX) -lt 0.06
    }
    $assertions.cameraReachedMaximum = $true
    Capture-Screenshot -Name "06-scroll-east-bound-wide" | Out-Null
    $eastCell = Get-VisibleCell -Snapshot $scroll -Order "MaxX"
    $scroll = Tap-Cell -Snapshot $scroll -Cell $eastCell
    $assertions.eastEdgePlacement = [int]$scroll.towerCount -eq 2 -and [double]$eastCell.worldX -gt 8
    Capture-Screenshot -Name "07-scroll-edge-towers-wide" | Out-Null

    $rooftop = Start-ManualScenario -LevelId "level_07" -ScenarioId "e2-rooftop-wide"
    $assertions.rooftopLayout = $rooftop.battlefieldId -eq "rooftop_moonline"
    Capture-Screenshot -Name "08-rooftop-wide" | Out-Null

    $logcatLines = (Invoke-TargetAdb -Arguments @("logcat", "-d", "-v", "time") -AllowFailure).Output
    $logcatPath = Join-Path $script:RunDir "logcat.txt"
    $logcatLines | Set-Content -LiteralPath $logcatPath -Encoding UTF8
    $fatalPattern = "FATAL EXCEPTION|Fatal signal|Abort message|NullReferenceException|MissingMethodException|DllNotFoundException| E/AndroidRuntime"
    $fatalLines = @($logcatLines | Where-Object { $_ -match $fatalPattern })
    $failedScreenshots = @($script:Screenshots | Where-Object { -not $_.landscape })
    $failedAssertions = @($assertions.GetEnumerator() | Where-Object { -not $_.Value } | ForEach-Object { $_.Key })

    $summary = [pscustomobject]@{
        timestampUtc = (Get-Date).ToUniversalTime().ToString("o")
        deviceSerial = $script:TargetSerial
        originalWmSize = $originalSize
        assertions = $assertions
        failedAssertions = $failedAssertions
        screenshots = $script:Screenshots
        fatalPatternCount = $fatalLines.Count
        fatalLines = $fatalLines
        passed = $failedAssertions.Count -eq 0 -and $failedScreenshots.Count -eq 0 -and $fatalLines.Count -eq 0
        runDir = $script:RunDir
    }
    $summaryPath = Join-Path $script:RunDir "qa-summary.json"
    $summary | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $summaryPath -Encoding UTF8

    Write-Host "E2 battlefield QA summary: $summaryPath"
    Write-Host "Failed assertions: $($failedAssertions -join ', ')"
    Write-Host "Fatal errors: $($fatalLines.Count)"
    if (-not $summary.passed) {
        exit 1
    }
}
finally {
    Invoke-TargetAdb -Arguments @("shell", "wm", "size", "reset") -AllowFailure | Out-Null
}

exit 0
