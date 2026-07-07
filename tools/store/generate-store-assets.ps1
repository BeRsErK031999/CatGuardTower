param(
    [string]$OutputDir = "docs\store\assets"
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

function New-Color {
    param(
        [string]$Hex,
        [int]$Alpha = 255
    )

    $normalized = $Hex.TrimStart("#")
    if ($normalized.Length -ne 6) {
        throw "Expected 6-digit hex color: $Hex"
    }

    $r = [Convert]::ToInt32($normalized.Substring(0, 2), 16)
    $g = [Convert]::ToInt32($normalized.Substring(2, 2), 16)
    $b = [Convert]::ToInt32($normalized.Substring(4, 2), 16)
    return [System.Drawing.Color]::FromArgb($Alpha, $r, $g, $b)
}

function New-Brush {
    param(
        [string]$Hex,
        [int]$Alpha = 255
    )

    return [System.Drawing.SolidBrush]::new((New-Color $Hex $Alpha))
}

function New-Pen {
    param(
        [string]$Hex,
        [float]$Width = 1,
        [int]$Alpha = 255
    )

    $pen = [System.Drawing.Pen]::new((New-Color $Hex $Alpha), $Width)
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    return $pen
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
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

    return [pscustomobject]@{
        Bitmap = $bitmap
        Graphics = $graphics
    }
}

function New-RoundedPath {
    param(
        [System.Drawing.RectangleF]$Rect,
        [float]$Radius
    )

    $diameter = $Radius * 2
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc($Rect.X, $Rect.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($Rect.Right - $diameter, $Rect.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($Rect.Right - $diameter, $Rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($Rect.X, $Rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function Fill-RoundRect {
    param(
        [System.Drawing.Graphics]$Graphics,
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [float]$Radius,
        [System.Drawing.Brush]$Brush,
        [System.Drawing.Pen]$Pen = $null
    )

    $rect = [System.Drawing.RectangleF]::new($X, $Y, $Width, $Height)
    $path = New-RoundedPath $rect $Radius
    $Graphics.FillPath($Brush, $path)
    if ($Pen -ne $null) {
        $Graphics.DrawPath($Pen, $path)
    }
    $path.Dispose()
}

function Draw-Text {
    param(
        [System.Drawing.Graphics]$Graphics,
        [string]$Text,
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [float]$Size,
        [string]$Color,
        [System.Drawing.FontStyle]$Style = [System.Drawing.FontStyle]::Regular,
        [System.Drawing.StringAlignment]$Align = [System.Drawing.StringAlignment]::Near,
        [System.Drawing.StringAlignment]$LineAlign = [System.Drawing.StringAlignment]::Near
    )

    $font = [System.Drawing.Font]::new("Segoe UI", $Size, $Style, [System.Drawing.GraphicsUnit]::Pixel)
    $brush = New-Brush $Color
    $format = [System.Drawing.StringFormat]::new()
    $format.Alignment = $Align
    $format.LineAlignment = $LineAlign
    $format.Trimming = [System.Drawing.StringTrimming]::EllipsisCharacter
    $format.FormatFlags = [System.Drawing.StringFormatFlags]::NoClip
    $rect = [System.Drawing.RectangleF]::new($X, $Y, $Width, $Height)
    $Graphics.DrawString($Text, $font, $brush, $rect, $format)
    $format.Dispose()
    $brush.Dispose()
    $font.Dispose()
}

function Draw-CatTower {
    param(
        [System.Drawing.Graphics]$Graphics,
        [float]$Cx,
        [float]$Cy,
        [float]$Scale,
        [string]$Body = "#f0c96b",
        [string]$Accent = "#53c3b6"
    )

    $shadow = New-Brush "#000000" 44
    $Graphics.FillEllipse($shadow, $Cx - 60 * $Scale, $Cy + 62 * $Scale, 120 * $Scale, 24 * $Scale)
    $shadow.Dispose()

    $baseBrush = New-Brush "#31505a"
    $basePen = New-Pen "#d8f3f1" (4 * $Scale)
    Fill-RoundRect $Graphics ($Cx - 45 * $Scale) ($Cy + 8 * $Scale) (90 * $Scale) (80 * $Scale) (16 * $Scale) $baseBrush $basePen
    $baseBrush.Dispose()
    $basePen.Dispose()

    $accentBrush = New-Brush $Accent
    $Graphics.FillRectangle($accentBrush, $Cx - 30 * $Scale, $Cy + 45 * $Scale, 60 * $Scale, 14 * $Scale)
    $accentBrush.Dispose()

    $headBrush = New-Brush $Body
    $headPen = New-Pen "#23343a" (4 * $Scale)
    $leftEar = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new($Cx - 39 * $Scale, $Cy - 4 * $Scale),
        [System.Drawing.PointF]::new($Cx - 20 * $Scale, $Cy - 52 * $Scale),
        [System.Drawing.PointF]::new($Cx - 5 * $Scale, $Cy - 10 * $Scale)
    )
    $rightEar = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new($Cx + 39 * $Scale, $Cy - 4 * $Scale),
        [System.Drawing.PointF]::new($Cx + 20 * $Scale, $Cy - 52 * $Scale),
        [System.Drawing.PointF]::new($Cx + 5 * $Scale, $Cy - 10 * $Scale)
    )
    $Graphics.FillPolygon($headBrush, $leftEar)
    $Graphics.FillPolygon($headBrush, $rightEar)
    $Graphics.DrawPolygon($headPen, $leftEar)
    $Graphics.DrawPolygon($headPen, $rightEar)
    $Graphics.FillEllipse($headBrush, $Cx - 46 * $Scale, $Cy - 34 * $Scale, 92 * $Scale, 78 * $Scale)
    $Graphics.DrawEllipse($headPen, $Cx - 46 * $Scale, $Cy - 34 * $Scale, 92 * $Scale, 78 * $Scale)

    $eyeBrush = New-Brush "#1f2e33"
    $Graphics.FillEllipse($eyeBrush, $Cx - 23 * $Scale, $Cy - 6 * $Scale, 10 * $Scale, 14 * $Scale)
    $Graphics.FillEllipse($eyeBrush, $Cx + 13 * $Scale, $Cy - 6 * $Scale, 10 * $Scale, 14 * $Scale)
    $eyeBrush.Dispose()

    $noseBrush = New-Brush "#e87b6d"
    $Graphics.FillEllipse($noseBrush, $Cx - 6 * $Scale, $Cy + 9 * $Scale, 12 * $Scale, 9 * $Scale)
    $noseBrush.Dispose()

    $whiskerPen = New-Pen "#1f2e33" (2.4 * $Scale)
    $Graphics.DrawLine($whiskerPen, $Cx - 8 * $Scale, $Cy + 18 * $Scale, $Cx - 38 * $Scale, $Cy + 13 * $Scale)
    $Graphics.DrawLine($whiskerPen, $Cx - 8 * $Scale, $Cy + 23 * $Scale, $Cx - 38 * $Scale, $Cy + 27 * $Scale)
    $Graphics.DrawLine($whiskerPen, $Cx + 8 * $Scale, $Cy + 18 * $Scale, $Cx + 38 * $Scale, $Cy + 13 * $Scale)
    $Graphics.DrawLine($whiskerPen, $Cx + 8 * $Scale, $Cy + 23 * $Scale, $Cx + 38 * $Scale, $Cy + 27 * $Scale)
    $whiskerPen.Dispose()

    $headBrush.Dispose()
    $headPen.Dispose()
}

function Draw-Enemy {
    param(
        [System.Drawing.Graphics]$Graphics,
        [float]$Cx,
        [float]$Cy,
        [float]$Scale,
        [string]$Color = "#ef6f6c",
        [string]$Kind = "mouse"
    )

    $bodyBrush = New-Brush $Color
    $linePen = New-Pen "#24343a" (3 * $Scale)
    if ($Kind -eq "snail") {
        $Graphics.FillEllipse($bodyBrush, $Cx - 35 * $Scale, $Cy - 16 * $Scale, 58 * $Scale, 34 * $Scale)
        $Graphics.DrawEllipse($linePen, $Cx - 35 * $Scale, $Cy - 16 * $Scale, 58 * $Scale, 34 * $Scale)
        $shellBrush = New-Brush "#8ed0da"
        $Graphics.FillEllipse($shellBrush, $Cx - 3 * $Scale, $Cy - 33 * $Scale, 48 * $Scale, 48 * $Scale)
        $Graphics.DrawEllipse($linePen, $Cx - 3 * $Scale, $Cy - 33 * $Scale, 48 * $Scale, 48 * $Scale)
        $shellBrush.Dispose()
    } elseif ($Kind -eq "moth") {
        $Graphics.FillEllipse($bodyBrush, $Cx - 43 * $Scale, $Cy - 24 * $Scale, 44 * $Scale, 50 * $Scale)
        $Graphics.FillEllipse($bodyBrush, $Cx - 1 * $Scale, $Cy - 24 * $Scale, 44 * $Scale, 50 * $Scale)
        $centerBrush = New-Brush "#f7e7a0"
        $Graphics.FillEllipse($centerBrush, $Cx - 10 * $Scale, $Cy - 21 * $Scale, 20 * $Scale, 42 * $Scale)
        $centerBrush.Dispose()
    } else {
        $Graphics.FillEllipse($bodyBrush, $Cx - 34 * $Scale, $Cy - 22 * $Scale, 68 * $Scale, 44 * $Scale)
        $Graphics.DrawEllipse($linePen, $Cx - 34 * $Scale, $Cy - 22 * $Scale, 68 * $Scale, 44 * $Scale)
        $earBrush = New-Brush "#f2b2a8"
        $Graphics.FillEllipse($earBrush, $Cx - 33 * $Scale, $Cy - 30 * $Scale, 20 * $Scale, 20 * $Scale)
        $Graphics.FillEllipse($earBrush, $Cx + 13 * $Scale, $Cy - 30 * $Scale, 20 * $Scale, 20 * $Scale)
        $earBrush.Dispose()
    }

    $eyeBrush = New-Brush "#172229"
    $Graphics.FillEllipse($eyeBrush, $Cx + 10 * $Scale, $Cy - 5 * $Scale, 7 * $Scale, 7 * $Scale)
    $eyeBrush.Dispose()
    $bodyBrush.Dispose()
    $linePen.Dispose()
}

function Draw-Path {
    param(
        [System.Drawing.Graphics]$Graphics,
        [float]$Scale = 1,
        [float]$OffsetX = 0,
        [float]$OffsetY = 0,
        [float]$Width = 28
    )

    $pathPen = New-Pen "#d9b36d" ($Width * $Scale)
    $pathPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pathPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $points = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new($OffsetX + 40 * $Scale, $OffsetY + 330 * $Scale),
        [System.Drawing.PointF]::new($OffsetX + 230 * $Scale, $OffsetY + 305 * $Scale),
        [System.Drawing.PointF]::new($OffsetX + 330 * $Scale, $OffsetY + 190 * $Scale),
        [System.Drawing.PointF]::new($OffsetX + 530 * $Scale, $OffsetY + 200 * $Scale),
        [System.Drawing.PointF]::new($OffsetX + 690 * $Scale, $OffsetY + 115 * $Scale),
        [System.Drawing.PointF]::new($OffsetX + 900 * $Scale, $OffsetY + 130 * $Scale)
    )
    $Graphics.DrawLines($pathPen, $points)
    $pathPen.Dispose()

    $dashPen = New-Pen "#f6df9d" (4 * $Scale) 180
    $dashPen.DashPattern = [float[]]@(2, 4)
    $Graphics.DrawLines($dashPen, $points)
    $dashPen.Dispose()
}

function Draw-PhoneBackground {
    param(
        [System.Drawing.Graphics]$Graphics,
        [string]$Title,
        [string]$Subtitle
    )

    $bg = New-Brush "#102d36"
    $Graphics.FillRectangle($bg, 0, 0, 1080, 1920)
    $bg.Dispose()

    $sky = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.Rectangle]::new(0, 0, 1080, 1920),
        (New-Color "#163b45"),
        (New-Color "#261f3c"),
        [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
    $Graphics.FillRectangle($sky, 0, 0, 1080, 1920)
    $sky.Dispose()

    $moonBrush = New-Brush "#f6d978" 210
    $Graphics.FillEllipse($moonBrush, 800, 80, 140, 140)
    $moonBrush.Dispose()

    Draw-Text $Graphics $Title 72 76 650 72 46 "#f7f1d2" ([System.Drawing.FontStyle]::Bold)
    Draw-Text $Graphics $Subtitle 76 142 780 52 26 "#a6d7d4"

    $coinBrush = New-Brush "#f0c96b"
    $Graphics.FillEllipse($coinBrush, 810, 98, 54, 54)
    $coinBrush.Dispose()
    Draw-Text $Graphics "Fish Coins 245" 874 105 170 48 24 "#f7f1d2" ([System.Drawing.FontStyle]::Bold)
}

function Draw-LevelScreen {
    param([System.Drawing.Graphics]$Graphics)

    Draw-PhoneBackground $Graphics "Cat Guard" "Choose a cozy path to defend"

    $panelBrush = New-Brush "#f7f1d2"
    $panelPen = New-Pen "#53c3b6" 5
    Fill-RoundRect $Graphics 64 250 952 1400 34 $panelBrush $panelPen
    $panelBrush.Dispose()
    $panelPen.Dispose()

    Draw-Text $Graphics "Level Select" 110 290 500 54 34 "#1b2d35" ([System.Drawing.FontStyle]::Bold)
    Draw-Text $Graphics "10 MVP levels ready for closed testing" 110 342 760 42 24 "#5d6e75"

    $colors = @("#53c3b6", "#f0c96b", "#e87b6d", "#a987d6")
    for ($i = 0; $i -lt 10; $i++) {
        $row = [Math]::Floor($i / 2)
        $col = $i % 2
        $x = 112 + $col * 430
        $y = 430 + $row * 205
        $cardBrush = New-Brush "#ffffff"
        $cardPen = New-Pen $colors[$i % $colors.Count] 5
        Fill-RoundRect $Graphics $x $y 380 150 24 $cardBrush $cardPen
        $cardBrush.Dispose()
        $cardPen.Dispose()
        Draw-Text $Graphics ("Level {0:00}" -f ($i + 1)) ($x + 24) ($y + 22) 180 34 24 "#253942" ([System.Drawing.FontStyle]::Bold)
        $names = @("Garden Gate", "Greenhouse", "Porch Stand", "Lantern Path", "Fish Barrel", "Moonlit Fence", "Roof Corner", "Old Well", "Orchard Wall", "Quiet Alley")
        Draw-Text $Graphics $names[$i] ($x + 24) ($y + 60) 260 34 25 "#253942"
        Draw-Text $Graphics "Reward" ($x + 24) ($y + 104) 95 28 20 "#5d6e75"
        $coin = New-Brush "#f0c96b"
        $Graphics.FillEllipse($coin, $x + 118, $y + 102, 28, 28)
        $coin.Dispose()
        Draw-Text $Graphics ("" + (35 + $i * 20)) ($x + 152) ($y + 100) 90 30 20 "#253942" ([System.Drawing.FontStyle]::Bold)
    }

    $buttonBrush = New-Brush "#53c3b6"
    Fill-RoundRect $Graphics 164 1510 752 84 26 $buttonBrush $null
    $buttonBrush.Dispose()
    Draw-Text $Graphics "Play selected level" 164 1526 752 52 28 "#102d36" ([System.Drawing.FontStyle]::Bold) ([System.Drawing.StringAlignment]::Center)
}

function Draw-GameplayScreen {
    param(
        [System.Drawing.Graphics]$Graphics,
        [switch]$Combat
    )

    Draw-PhoneBackground $Graphics "Garden Gate" $(if ($Combat) { "Wave pressure test" } else { "Place towers before the wave" })

    $fieldBrush = New-Brush "#285448"
    $fieldPen = New-Pen "#79d5a4" 5
    Fill-RoundRect $Graphics 58 250 964 1090 34 $fieldBrush $fieldPen
    $fieldBrush.Dispose()
    $fieldPen.Dispose()

    $gridPen = New-Pen "#c6e9c8" 2 95
    for ($x = 160; $x -le 880; $x += 120) {
        $Graphics.DrawLine($gridPen, $x, 340, $x, 1210)
    }
    for ($y = 340; $y -le 1210; $y += 120) {
        $Graphics.DrawLine($gridPen, 120, $y, 960, $y)
    }
    $gridPen.Dispose()

    $pathPen = New-Pen "#d9b36d" 76
    $pathPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pathPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $points = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(110, 1130),
        [System.Drawing.PointF]::new(330, 1040),
        [System.Drawing.PointF]::new(270, 760),
        [System.Drawing.PointF]::new(560, 700),
        [System.Drawing.PointF]::new(760, 510),
        [System.Drawing.PointF]::new(945, 570)
    )
    $Graphics.DrawLines($pathPen, $points)
    $pathPen.Dispose()

    Draw-CatTower $Graphics 220 920 1.35 "#f0c96b" "#53c3b6"
    Draw-CatTower $Graphics 540 570 1.2 "#f6d2a8" "#e87b6d"
    Draw-CatTower $Graphics 790 900 1.1 "#d7c4ff" "#f0c96b"

    if ($Combat) {
        Draw-Enemy $Graphics 270 1040 1.35 "#ef6f6c" "mouse"
        Draw-Enemy $Graphics 510 720 1.15 "#c58be8" "moth"
        Draw-Enemy $Graphics 745 540 1.25 "#7ccf8a" "snail"
        $boltPen = New-Pen "#f8ed7a" 9
        $Graphics.DrawLine($boltPen, 540, 570, 510, 720)
        $Graphics.DrawLine($boltPen, 790, 900, 745, 540)
        $boltPen.Dispose()
    } else {
        Draw-Enemy $Graphics 140 1130 1.15 "#ef6f6c" "mouse"
        Draw-Enemy $Graphics 370 1040 1.05 "#84d0e0" "snail"
    }

    $hudBrush = New-Brush "#f7f1d2"
    Fill-RoundRect $Graphics 76 1385 928 180 30 $hudBrush $null
    $hudBrush.Dispose()
    Draw-Text $Graphics "Lives 7" 118 1424 180 42 30 "#253942" ([System.Drawing.FontStyle]::Bold)
    Draw-Text $Graphics "Towers 3" 338 1424 200 42 30 "#253942" ([System.Drawing.FontStyle]::Bold)
    Draw-Text $Graphics "Wave 1/1" 598 1424 220 42 30 "#253942" ([System.Drawing.FontStyle]::Bold)
    Draw-Text $Graphics $(if ($Combat) { "Projectiles, VFX, and enemy pressure are visible." } else { "Tap a tile, choose a tower, defend the base." }) 118 1482 800 42 24 "#5d6e75"

    $towerPanel = New-Brush "#17343e"
    Fill-RoundRect $Graphics 76 1608 928 156 30 $towerPanel $null
    $towerPanel.Dispose()
    $names = @("Dart", "Yarn", "Bell", "Laser", "Blanket")
    for ($i = 0; $i -lt 5; $i++) {
        $x = 112 + $i * 180
        Draw-CatTower $Graphics ($x + 58) 1668 0.44 "#f0c96b" $(@("#53c3b6", "#e87b6d", "#a987d6", "#79d5a4", "#f0c96b")[$i])
        Draw-Text $Graphics $names[$i] $x 1712 130 28 20 "#f7f1d2" ([System.Drawing.FontStyle]::Bold) ([System.Drawing.StringAlignment]::Center)
    }
}

function Draw-VictoryScreen {
    param([System.Drawing.Graphics]$Graphics)

    Draw-GameplayScreen $Graphics -Combat
    $overlay = New-Brush "#102d36" 210
    $Graphics.FillRectangle($overlay, 0, 0, 1080, 1920)
    $overlay.Dispose()

    $card = New-Brush "#f7f1d2"
    $cardPen = New-Pen "#f0c96b" 6
    Fill-RoundRect $Graphics 96 390 888 860 38 $card $cardPen
    $card.Dispose()
    $cardPen.Dispose()

    Draw-Text $Graphics "Victory!" 96 454 888 78 58 "#253942" ([System.Drawing.FontStyle]::Bold) ([System.Drawing.StringAlignment]::Center)
    Draw-Text $Graphics "Garden Gate cleared" 96 540 888 42 30 "#5d6e75" ([System.Drawing.FontStyle]::Regular) ([System.Drawing.StringAlignment]::Center)

    $coin = New-Brush "#f0c96b"
    $Graphics.FillEllipse($coin, 445, 625, 190, 190)
    $coin.Dispose()
    Draw-Text $Graphics "+35" 96 665 888 80 62 "#253942" ([System.Drawing.FontStyle]::Bold) ([System.Drawing.StringAlignment]::Center)
    Draw-Text $Graphics "Fish Coins" 96 750 888 38 28 "#5d6e75" ([System.Drawing.FontStyle]::Regular) ([System.Drawing.StringAlignment]::Center)

    $upgradeBrush = New-Brush "#ffffff"
    Fill-RoundRect $Graphics 170 880 740 90 22 $upgradeBrush $null
    Fill-RoundRect $Graphics 170 990 740 90 22 $upgradeBrush $null
    $upgradeBrush.Dispose()
    Draw-Text $Graphics "Claw Training  Level 2" 205 898 520 44 28 "#253942" ([System.Drawing.FontStyle]::Bold)
    Draw-Text $Graphics "Tower damage multiplier" 205 930 520 34 21 "#5d6e75"
    Draw-Text $Graphics "Cozy Cushions  Level 1" 205 1008 520 44 28 "#253942" ([System.Drawing.FontStyle]::Bold)
    Draw-Text $Graphics "Base lives bonus" 205 1040 520 34 21 "#5d6e75"

    $button = New-Brush "#53c3b6"
    Fill-RoundRect $Graphics 230 1135 620 82 26 $button $null
    $button.Dispose()
    Draw-Text $Graphics "Continue" 230 1152 620 44 30 "#102d36" ([System.Drawing.FontStyle]::Bold) ([System.Drawing.StringAlignment]::Center)
}

function Draw-DailyScreen {
    param([System.Drawing.Graphics]$Graphics)

    Draw-PhoneBackground $Graphics "Daily Loop" "Rewards and missions"

    $panel = New-Brush "#f7f1d2"
    Fill-RoundRect $Graphics 64 250 952 1415 34 $panel $null
    $panel.Dispose()

    Draw-Text $Graphics "7-Day Reward Chain" 112 305 680 58 38 "#253942" ([System.Drawing.FontStyle]::Bold)
    for ($i = 0; $i -lt 7; $i++) {
        $x = 118 + ($i % 4) * 220
        $y = 400 + [Math]::Floor($i / 4) * 190
        $active = $i -eq 2
        $brush = New-Brush $(if ($active) { "#f0c96b" } else { "#ffffff" })
        $pen = New-Pen $(if ($active) { "#e87b6d" } else { "#cbd8d6" }) 5
        Fill-RoundRect $Graphics $x $y 168 136 24 $brush $pen
        $brush.Dispose()
        $pen.Dispose()
        Draw-Text $Graphics ("Day " + ($i + 1)) $x ($y + 22) 168 34 24 "#253942" ([System.Drawing.FontStyle]::Bold) ([System.Drawing.StringAlignment]::Center)
        Draw-Text $Graphics ("{0} coins" -f (20 + $i * 10)) $x ($y + 68) 168 32 22 "#5d6e75" ([System.Drawing.FontStyle]::Regular) ([System.Drawing.StringAlignment]::Center)
    }

    Draw-Text $Graphics "Daily Missions" 112 812 680 58 38 "#253942" ([System.Drawing.FontStyle]::Bold)
    $missions = @(
        @("Win 1 Level", "0 / 1", "#53c3b6"),
        @("Place 3 Towers", "2 / 3", "#e87b6d"),
        @("Claim Daily Reward", "Ready", "#a987d6")
    )
    for ($i = 0; $i -lt $missions.Count; $i++) {
        $y = 910 + $i * 155
        $card = New-Brush "#ffffff"
        Fill-RoundRect $Graphics 112 $y 856 112 24 $card $null
        $card.Dispose()
        $dot = New-Brush $missions[$i][2]
        $Graphics.FillEllipse($dot, 148, $y + 34, 44, 44)
        $dot.Dispose()
        Draw-Text $Graphics $missions[$i][0] 220 ($y + 24) 500 40 28 "#253942" ([System.Drawing.FontStyle]::Bold)
        Draw-Text $Graphics $missions[$i][1] 220 ($y + 64) 500 30 23 "#5d6e75"
        $button = New-Brush "#17343e"
        Fill-RoundRect $Graphics 780 ($y + 27) 140 58 20 $button $null
        $button.Dispose()
        Draw-Text $Graphics "Claim" 780 ($y + 40) 140 34 22 "#f7f1d2" ([System.Drawing.FontStyle]::Bold) ([System.Drawing.StringAlignment]::Center)
    }

    $notice = New-Brush "#17343e"
    Fill-RoundRect $Graphics 112 1465 856 120 28 $notice $null
    $notice.Dispose()
    Draw-Text $Graphics "Offline friendly: progress stays local on this device." 150 1496 780 56 26 "#f7f1d2" ([System.Drawing.FontStyle]::Bold) ([System.Drawing.StringAlignment]::Center)
}

function Draw-Icon {
    param([string]$Path)

    $canvas = New-Canvas 512 512 -Alpha
    $g = $canvas.Graphics
    $g.Clear([System.Drawing.Color]::Transparent)

    $clipRect = [System.Drawing.RectangleF]::new(28, 28, 456, 456)
    $clipPath = New-RoundedPath $clipRect 92

    $bg = New-Brush "#123743"
    $g.FillPath($bg, $clipPath)
    $bg.Dispose()

    $g.SetClip($clipPath)

    $hill = New-Brush "#285448"
    $g.FillEllipse($hill, -70, 300, 650, 250)
    $hill.Dispose()

    Draw-CatTower $g 256 242 2.25 "#f0c96b" "#53c3b6"

    $shieldPen = New-Pen "#f7f1d2" 7 210
    $g.DrawArc($shieldPen, 100, 86, 312, 328, 205, 130)
    $shieldPen.Dispose()

    $g.ResetClip()

    $border = New-Pen "#f0c96b" 14
    $g.DrawPath($border, $clipPath)
    $border.Dispose()
    $clipPath.Dispose()

    $canvas.Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $canvas.Bitmap.Dispose()
}

function Draw-FeatureGraphic {
    param([string]$Path)

    $canvas = New-Canvas 1024 500
    $g = $canvas.Graphics
    $gradient = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.Rectangle]::new(0, 0, 1024, 500),
        (New-Color "#123743"),
        (New-Color "#2c2344"),
        [System.Drawing.Drawing2D.LinearGradientMode]::Horizontal)
    $g.FillRectangle($gradient, 0, 0, 1024, 500)
    $gradient.Dispose()

    $moon = New-Brush "#f6d978" 220
    $g.FillEllipse($moon, 784, 44, 110, 110)
    $moon.Dispose()

    $field = New-Brush "#285448"
    Fill-RoundRect $g 40 80 944 360 32 $field $null
    $field.Dispose()

    Draw-Path $g 1 36 25 50
    Draw-CatTower $g 220 254 1.25 "#f0c96b" "#53c3b6"
    Draw-CatTower $g 470 182 1.05 "#f6d2a8" "#e87b6d"
    Draw-CatTower $g 690 282 1.15 "#d7c4ff" "#f0c96b"
    Draw-Enemy $g 330 330 1.15 "#ef6f6c" "mouse"
    Draw-Enemy $g 610 225 1.0 "#c58be8" "moth"
    Draw-Enemy $g 840 160 1.05 "#84d0e0" "snail"

    $spark = New-Brush "#f8ed7a" 190
    $g.FillEllipse($spark, 590, 203, 30, 30)
    $g.FillEllipse($spark, 815, 145, 24, 24)
    $spark.Dispose()

    $canvas.Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $canvas.Bitmap.Dispose()
}

function Draw-Screenshot {
    param(
        [string]$Path,
        [string]$Kind
    )

    $canvas = New-Canvas 1080 1920
    $g = $canvas.Graphics
    switch ($Kind) {
        "menu" { Draw-LevelScreen $g }
        "placement" { Draw-GameplayScreen $g }
        "combat" { Draw-GameplayScreen $g -Combat }
        "victory" { Draw-VictoryScreen $g }
        "daily" { Draw-DailyScreen $g }
        default { throw "Unknown screenshot kind: $Kind" }
    }

    $canvas.Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $canvas.Bitmap.Dispose()
}

$resolvedOutputDir = Resolve-ProjectPath $OutputDir
$iconDir = Join-Path $resolvedOutputDir "icon"
$featureDir = Join-Path $resolvedOutputDir "feature"
$screenshotsDir = Join-Path $resolvedOutputDir "screenshots"
New-Item -ItemType Directory -Force -Path $iconDir, $featureDir, $screenshotsDir | Out-Null

$assets = @(
    @{ Path = Join-Path $iconDir "catguard-store-icon-512.png"; Kind = "icon" },
    @{ Path = Join-Path $featureDir "catguard-feature-1024x500.png"; Kind = "feature" },
    @{ Path = Join-Path $screenshotsDir "01-main-menu-level-select-1080x1920.png"; Kind = "menu" },
    @{ Path = Join-Path $screenshotsDir "02-level-placement-1080x1920.png"; Kind = "placement" },
    @{ Path = Join-Path $screenshotsDir "03-wave-combat-1080x1920.png"; Kind = "combat" },
    @{ Path = Join-Path $screenshotsDir "04-victory-upgrades-1080x1920.png"; Kind = "victory" },
    @{ Path = Join-Path $screenshotsDir "05-daily-loop-1080x1920.png"; Kind = "daily" }
)

foreach ($asset in $assets) {
    $path = [string]$asset.Path
    switch ([string]$asset.Kind) {
        "icon" { Draw-Icon $path }
        "feature" { Draw-FeatureGraphic $path }
        default { Draw-Screenshot $path ([string]$asset.Kind) }
    }

    $file = Get-Item -LiteralPath $path
    Write-Host "$($file.FullName) $($file.Length) bytes"
}
