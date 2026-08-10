param(
    [string]$AssetDir = "docs\store\assets"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$resolvedAssetDir = if ([System.IO.Path]::IsPathRooted($AssetDir)) {
    $AssetDir
} else {
    Join-Path $repoRoot $AssetDir
}

$errors = New-Object System.Collections.Generic.List[string]

function Test-ImageAsset {
    param(
        [string]$RelativePath,
        [int]$ExpectedWidth,
        [int]$ExpectedHeight,
        [bool]$RequireAlpha,
        [long]$MaximumBytes = 0,
        [bool]$ValidateScreenshotRatio = $false
    )

    $path = Join-Path $resolvedAssetDir $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("Missing asset: $RelativePath")
        return
    }

    $file = Get-Item -LiteralPath $path
    if ($MaximumBytes -gt 0 -and $file.Length -gt $MaximumBytes) {
        $errors.Add("Asset exceeds $MaximumBytes bytes: $RelativePath ($($file.Length) bytes)")
    }

    $image = [System.Drawing.Image]::FromFile($path)
    try {
        if ($image.RawFormat.Guid -ne [System.Drawing.Imaging.ImageFormat]::Png.Guid) {
            $errors.Add("Asset must be PNG: $RelativePath")
        }

        if ($image.Width -ne $ExpectedWidth -or $image.Height -ne $ExpectedHeight) {
            $errors.Add("Unexpected dimensions: $RelativePath ($($image.Width) x $($image.Height))")
        }

        $hasAlpha = [System.Drawing.Image]::IsAlphaPixelFormat($image.PixelFormat)
        if ($RequireAlpha -and -not $hasAlpha) {
            $errors.Add("Asset must include an alpha channel: $RelativePath ($($image.PixelFormat))")
        }

        if (-not $RequireAlpha -and $hasAlpha) {
            $errors.Add("Asset must not include an alpha channel: $RelativePath ($($image.PixelFormat))")
        }

        if ($ValidateScreenshotRatio) {
            $shortestSide = [Math]::Min($image.Width, $image.Height)
            $longestSide = [Math]::Max($image.Width, $image.Height)
            if ($longestSide -gt ($shortestSide * 2)) {
                $errors.Add("Longest side is more than twice the shortest side: $RelativePath")
            }
        }

        Write-Host "$RelativePath $($image.Width)x$($image.Height) $($image.PixelFormat) $($file.Length) bytes"
    } finally {
        $image.Dispose()
    }
}

Test-ImageAsset "icon\catguard-store-icon-512.png" 512 512 $true 1048576
Test-ImageAsset "feature\catguard-feature-1024x500.png" 1024 500 $false
Test-ImageAsset "screenshots\01-home-hub-campaign-1920x1080.png" 1920 1080 $false 0 $true
Test-ImageAsset "screenshots\02-tower-placement-1920x1080.png" 1920 1080 $false 0 $true
Test-ImageAsset "screenshots\03-boss-combat-1920x1080.png" 1920 1080 $false 0 $true
Test-ImageAsset "screenshots\04-victory-progression-1920x1080.png" 1920 1080 $false 0 $true
Test-ImageAsset "screenshots\05-quests-achievements-1920x1080.png" 1920 1080 $false 0 $true

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Store asset validation passed."
exit 0
