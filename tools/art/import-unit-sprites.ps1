param(
    [string]$TowerSheetPath = "docs\art\source\tower-guardian-sheet.png",
    [string]$EnemySheetPath = "docs\art\source\garden-enemy-sheet.png",
    [string]$OutputRoot = "Assets\_Project\Art\Units",
    [int]$SpriteSize = 256
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

if (-not ("CatGuardAlphaCleaner" -as [type])) {
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;

public static class CatGuardAlphaCleaner
{
    public static int RemoveSmallIslands(Bitmap bitmap, byte alphaThreshold, double minimumComponentRatio)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        bool[] visited = new bool[width * height];
        var components = new List<List<int>>();
        int largestComponent = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int start = (y * width) + x;
                if (visited[start] || bitmap.GetPixel(x, y).A <= alphaThreshold)
                {
                    continue;
                }

                var pixels = new List<int>();
                var queue = new Queue<int>();
                queue.Enqueue(start);
                visited[start] = true;

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    pixels.Add(current);
                    int currentX = current % width;
                    int currentY = current / width;

                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        for (int offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            if (offsetX == 0 && offsetY == 0)
                            {
                                continue;
                            }

                            int nextX = currentX + offsetX;
                            int nextY = currentY + offsetY;
                            if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height)
                            {
                                continue;
                            }

                            int next = (nextY * width) + nextX;
                            if (visited[next] || bitmap.GetPixel(nextX, nextY).A <= alphaThreshold)
                            {
                                continue;
                            }

                            visited[next] = true;
                            queue.Enqueue(next);
                        }
                    }
                }

                components.Add(pixels);
                largestComponent = Math.Max(largestComponent, pixels.Count);
            }
        }

        int minimumComponentSize = Math.Max(2, (int)Math.Ceiling(largestComponent * minimumComponentRatio));
        int removedPixels = 0;
        foreach (List<int> component in components)
        {
            if (component.Count >= minimumComponentSize)
            {
                continue;
            }

            foreach (int pixel in component)
            {
                bitmap.SetPixel(pixel % width, pixel / width, Color.Transparent);
                removedPixels++;
            }
        }

        return removedPixels;
    }
}
'@
}

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path

function Resolve-ProjectPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return (Join-Path $repoRoot $Path)
}

function Get-VisibleBounds {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [int]$CellX,
        [int]$CellWidth
    )

    $left = $Bitmap.Width
    $top = $Bitmap.Height
    $right = -1
    $bottom = -1
    $cellRight = [Math]::Min($Bitmap.Width, $CellX + $CellWidth)

    for ($y = 0; $y -lt $Bitmap.Height; $y++) {
        for ($x = $CellX; $x -lt $cellRight; $x++) {
            if ($Bitmap.GetPixel($x, $y).A -le 8) {
                continue
            }

            $left = [Math]::Min($left, $x)
            $top = [Math]::Min($top, $y)
            $right = [Math]::Max($right, $x)
            $bottom = [Math]::Max($bottom, $y)
        }
    }

    if ($right -lt $left -or $bottom -lt $top) {
        throw "No visible sprite pixels found in cell starting at x=$CellX."
    }

    $width = $right - $left + 1
    $height = $bottom - $top + 1
    $padding = [Math]::Max(4, [int][Math]::Ceiling([Math]::Max($width, $height) * 0.04))
    $paddedLeft = [Math]::Max($CellX, $left - $padding)
    $paddedTop = [Math]::Max(0, $top - $padding)
    $paddedRight = [Math]::Min($cellRight - 1, $right + $padding)
    $paddedBottom = [Math]::Min($Bitmap.Height - 1, $bottom + $padding)

    return [System.Drawing.Rectangle]::new(
        $paddedLeft,
        $paddedTop,
        $paddedRight - $paddedLeft + 1,
        $paddedBottom - $paddedTop + 1)
}

function Export-SpriteSheet {
    param(
        [string]$SourcePath,
        [string]$OutputDirectory,
        [string[]]$SpriteNames
    )

    if (-not (Test-Path -LiteralPath $SourcePath -PathType Leaf)) {
        throw "Transparent sprite sheet was not found: $SourcePath"
    }

    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $source = [System.Drawing.Bitmap]::FromFile($SourcePath)
    try {
        if (-not [System.Drawing.Image]::IsAlphaPixelFormat($source.PixelFormat)) {
            throw "Sprite sheet must include an alpha channel: $SourcePath"
        }

        $cellWidth = [int][Math]::Floor($source.Width / $SpriteNames.Count)
        for ($index = 0; $index -lt $SpriteNames.Count; $index++) {
            $cellX = $index * $cellWidth
            $currentCellWidth = if ($index -eq $SpriteNames.Count - 1) {
                $source.Width - $cellX
            } else {
                $cellWidth
            }
            $sourceBounds = Get-VisibleBounds $source $cellX $currentCellWidth
            $scale = [Math]::Min(
                ($SpriteSize * 0.88) / $sourceBounds.Width,
                ($SpriteSize * 0.88) / $sourceBounds.Height)
            $destinationWidth = [Math]::Max(1, [int][Math]::Round($sourceBounds.Width * $scale))
            $destinationHeight = [Math]::Max(1, [int][Math]::Round($sourceBounds.Height * $scale))
            $destinationX = [int][Math]::Round(($SpriteSize - $destinationWidth) * 0.5)
            $destinationY = [int][Math]::Round(($SpriteSize - $destinationHeight) * 0.5)

            $sprite = [System.Drawing.Bitmap]::new(
                $SpriteSize,
                $SpriteSize,
                [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            try {
                $graphics = [System.Drawing.Graphics]::FromImage($sprite)
                try {
                    $graphics.Clear([System.Drawing.Color]::Transparent)
                    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                    $destination = [System.Drawing.Rectangle]::new(
                        $destinationX,
                        $destinationY,
                        $destinationWidth,
                        $destinationHeight)
                    $graphics.DrawImage($source, $destination, $sourceBounds, [System.Drawing.GraphicsUnit]::Pixel)
                } finally {
                    $graphics.Dispose()
                }

                [void][CatGuardAlphaCleaner]::RemoveSmallIslands($sprite, 8, 0.015)

                $outputPath = Join-Path $OutputDirectory ($SpriteNames[$index] + ".png")
                $sprite.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
                Write-Host "$outputPath $SpriteSize x $SpriteSize"
            } finally {
                $sprite.Dispose()
            }
        }
    } finally {
        $source.Dispose()
    }
}

$resolvedOutputRoot = Resolve-ProjectPath $OutputRoot
Export-SpriteSheet `
    -SourcePath (Resolve-ProjectPath $TowerSheetPath) `
    -OutputDirectory (Join-Path $resolvedOutputRoot "Towers") `
    -SpriteNames @("cat_dart", "yarn_cannon", "bell_sniper", "laser_pointer", "blanket_boom")
Export-SpriteSheet `
    -SourcePath (Resolve-ProjectPath $EnemySheetPath) `
    -OutputDirectory (Join-Path $resolvedOutputRoot "Enemies") `
    -SpriteNames @("mouse_scout", "rat_bruiser", "beetle_guard", "moth_swarm", "snail_tank")
