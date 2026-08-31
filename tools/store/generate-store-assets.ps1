param(
    [string]$OutputDir = "docs\store\assets",
    [string]$ArtSourceDir = "docs\store\source",
    [string]$ScreenshotSourceDir = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$script:RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path

function Resolve-ProjectPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return (Join-Path $script:RepoRoot $Path)
}

function New-Canvas {
    param(
        [int]$Width,
        [int]$Height,
        [switch]$Alpha
    )

    $format = if ($Alpha) {
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
    } else {
        [System.Drawing.Imaging.PixelFormat]::Format24bppRgb
    }

    $bitmap = [System.Drawing.Bitmap]::new($Width, $Height, $format)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    return [pscustomobject]@{
        Bitmap = $bitmap
        Graphics = $graphics
    }
}

function Draw-ImageCover {
    param(
        [System.Drawing.Graphics]$Graphics,
        [System.Drawing.Image]$Image,
        [System.Drawing.RectangleF]$Destination
    )

    $destinationRatio = $Destination.Width / $Destination.Height
    $sourceRatio = $Image.Width / $Image.Height

    if ($sourceRatio -gt $destinationRatio) {
        $sourceHeight = [float]$Image.Height
        $sourceWidth = $sourceHeight * $destinationRatio
        $sourceX = ($Image.Width - $sourceWidth) * 0.5
        $sourceY = 0
    } else {
        $sourceWidth = [float]$Image.Width
        $sourceHeight = $sourceWidth / $destinationRatio
        $sourceX = 0
        $sourceY = ($Image.Height - $sourceHeight) * 0.5
    }

    $source = [System.Drawing.RectangleF]::new($sourceX, $sourceY, $sourceWidth, $sourceHeight)
    $Graphics.DrawImage($Image, $Destination, $source, [System.Drawing.GraphicsUnit]::Pixel)
}

function Import-StoreArtwork {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [int]$Width,
        [int]$Height,
        [switch]$Alpha
    )

    if (-not (Test-Path -LiteralPath $SourcePath -PathType Leaf)) {
        throw "Curated store artwork was not found: $SourcePath"
    }

    $source = [System.Drawing.Image]::FromFile($SourcePath)
    try {
        $canvas = New-Canvas $Width $Height -Alpha:$Alpha
        try {
            if ($Alpha) {
                $canvas.Graphics.Clear([System.Drawing.Color]::Transparent)
            }

            Draw-ImageCover $canvas.Graphics $source ([System.Drawing.RectangleF]::new(0, 0, $Width, $Height))
            $canvas.Bitmap.Save($DestinationPath, [System.Drawing.Imaging.ImageFormat]::Png)
        } finally {
            $canvas.Graphics.Dispose()
            $canvas.Bitmap.Dispose()
        }
    } finally {
        $source.Dispose()
    }
}

function Import-RuntimeScreenshot {
    param(
        [string]$SourcePath,
        [string]$DestinationPath
    )

    if (-not (Test-Path -LiteralPath $SourcePath -PathType Leaf)) {
        throw "Runtime screenshot was not found: $SourcePath"
    }

    $source = [System.Drawing.Image]::FromFile($SourcePath)
    try {
        if ($source.Width -ne 1920 -or $source.Height -ne 1080) {
            throw "Runtime screenshot must be exactly 1920 x 1080: $SourcePath ($($source.Width) x $($source.Height))"
        }

        $canvas = New-Canvas 1920 1080
        try {
            $canvas.Graphics.DrawImageUnscaled($source, 0, 0)
            $canvas.Bitmap.Save($DestinationPath, [System.Drawing.Imaging.ImageFormat]::Png)
        } finally {
            $canvas.Graphics.Dispose()
            $canvas.Bitmap.Dispose()
        }
    } finally {
        $source.Dispose()
    }
}

$resolvedOutputDir = Resolve-ProjectPath $OutputDir
$resolvedArtSourceDir = Resolve-ProjectPath $ArtSourceDir
$iconDir = Join-Path $resolvedOutputDir "icon"
$featureDir = Join-Path $resolvedOutputDir "feature"
$screenshotsDir = Join-Path $resolvedOutputDir "screenshots"
New-Item -ItemType Directory -Force -Path $iconDir, $featureDir, $screenshotsDir | Out-Null

$artAssets = @(
    @{
        Source = Join-Path $resolvedArtSourceDir "catguard-store-icon-master.png"
        Destination = Join-Path $iconDir "catguard-store-icon-512.png"
        Width = 512
        Height = 512
        Alpha = $true
    },
    @{
        Source = Join-Path $resolvedArtSourceDir "catguard-feature-master.png"
        Destination = Join-Path $featureDir "catguard-feature-1024x500.png"
        Width = 1024
        Height = 500
        Alpha = $false
    }
)

foreach ($asset in $artAssets) {
    Import-StoreArtwork `
        -SourcePath ([string]$asset.Source) `
        -DestinationPath ([string]$asset.Destination) `
        -Width ([int]$asset.Width) `
        -Height ([int]$asset.Height) `
        -Alpha:([bool]$asset.Alpha)
    $file = Get-Item -LiteralPath ([string]$asset.Destination)
    Write-Host "$($file.FullName) $($file.Length) bytes"
}

if ($ScreenshotSourceDir) {
    $resolvedScreenshotSourceDir = Resolve-ProjectPath $ScreenshotSourceDir
    if (-not (Test-Path -LiteralPath $resolvedScreenshotSourceDir -PathType Container)) {
        throw "Screenshot source directory was not found: $resolvedScreenshotSourceDir"
    }

    $screenshotAssets = @(
        @{ Source = "01-home-hub-campaign.png"; Destination = "01-home-hub-campaign-1920x1080.png" },
        @{ Source = "02-tower-placement.png"; Destination = "02-tower-placement-1920x1080.png" },
        @{ Source = "03-boss-combat.png"; Destination = "03-boss-combat-1920x1080.png" },
        @{ Source = "04-victory-progression.png"; Destination = "04-victory-progression-1920x1080.png" },
        @{ Source = "05-quests-achievements.png"; Destination = "05-quests-achievements-1920x1080.png" }
    )

    foreach ($asset in $screenshotAssets) {
        $sourcePath = Join-Path $resolvedScreenshotSourceDir ([string]$asset.Source)
        $destinationPath = Join-Path $screenshotsDir ([string]$asset.Destination)
        Import-RuntimeScreenshot $sourcePath $destinationPath
        $file = Get-Item -LiteralPath $destinationPath
        Write-Host "$($file.FullName) $($file.Length) bytes"
    }
} else {
    Write-Host "Runtime screenshots were preserved. Pass -ScreenshotSourceDir to import a validated 1920 x 1080 landscape capture set."
}
