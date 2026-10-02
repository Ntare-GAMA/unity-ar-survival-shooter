# Generates the app icon layers in Assets/Art/Icon (ocean-night theme, original artwork):
#   Icon_Background.png  - deep-navy water with glowing sky flowers (adaptive icon background)
#   Icon_Foreground.png  - crosshair + "AR" title, kept inside the adaptive-icon safe zone
#   Icon_Full.png        - both layers combined (legacy / round icons)
# Usage: powershell -ExecutionPolicy Bypass -File Tools/GenerateAppIcon.ps1
Add-Type -AssemblyName System.Drawing
$size = 1024
$outDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Assets\Art\Icon'))
New-Item -ItemType Directory -Force $outDir | Out-Null

function New-Canvas { $b = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb); $g = [System.Drawing.Graphics]::FromImage($b); $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'; return @($b, $g) }
function Color($a, $r, $g, $b) { [System.Drawing.Color]::FromArgb($a, $r, $g, $b) }

function Add-Flower($g, $cx, $cy, $radius, $color, $width) {
    $pts = New-Object 'System.Collections.Generic.List[System.Drawing.PointF]'
    for ($i = 0; $i -lt 360; $i++) {
        $t = $i * [Math]::PI / 180
        $r = $radius * (0.62 + 0.38 * [Math]::Abs([Math]::Cos(2.5 * $t)))
        $pts.Add((New-Object System.Drawing.PointF ([float]($cx + $r * [Math]::Cos($t))), ([float]($cy + $r * [Math]::Sin($t)))))
    }
    foreach ($glow in @(@(70, 4.0), @(120, 2.2), @(255, 1.0))) {   # soft glow, then crisp line
        $pen = New-Object System.Drawing.Pen (Color $glow[0] $color.R $color.G $color.B), ($width * $glow[1])
        $pen.LineJoin = 'Round'
        $g.DrawPolygon($pen, $pts.ToArray())
    }
    $centre = New-Object System.Drawing.Pen $color, $width
    $g.DrawEllipse($centre, $cx - $radius * 0.22, $cy - $radius * 0.22, $radius * 0.44, $radius * 0.44)
}

# --- Background: radial navy gradient + sky flowers ------------------------------------------
$bg = New-Canvas; $b = $bg[0]; $g = $bg[1]
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddEllipse(-300, -300, $size + 600, $size + 600)
$brush = New-Object System.Drawing.Drawing2D.PathGradientBrush $path
$brush.CenterColor = Color 255 22 58 130
$brush.SurroundColors = @(Color 255 6 16 52)
$g.FillRectangle($brush, 0, 0, $size, $size)
Add-Flower $g 150 170 120 (Color 255 77 242 230) 9
Add-Flower $g 880 200 150 (Color 255 173 115 255) 10
Add-Flower $g 870 880 120 (Color 255 255 143 173) 9
Add-Flower $g 140 860 100 (Color 255 77 242 230) 8
$b.Save("$outDir\Icon_Background.png", [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose()

# --- Foreground: crosshair + "AR" (safe zone = central ~66%) ----------------------------------
$fg = New-Canvas; $f = $fg[0]; $h = $fg[1]
$h.Clear([System.Drawing.Color]::Transparent)
$yellow = Color 255 255 222 51
$navy = Color 255 8 22 62
$c = $size / 2; $ring = 235
$outline = New-Object System.Drawing.Pen $navy, 52
$h.DrawEllipse($outline, $c - $ring, $c - $ring, $ring * 2, $ring * 2)
$ringPen = New-Object System.Drawing.Pen $yellow, 30
$h.DrawEllipse($ringPen, $c - $ring, $c - $ring, $ring * 2, $ring * 2)
foreach ($d in @(@(0, -1), @(0, 1), @(-1, 0), @(1, 0))) {    # crosshair ticks crossing the ring
    $x1 = $c + $d[0] * ($ring - 65); $y1 = $c + $d[1] * ($ring - 65)
    $x2 = $c + $d[0] * ($ring + 55); $y2 = $c + $d[1] * ($ring + 55)   # stays inside the ~61% safe zone
    $o = New-Object System.Drawing.Pen $navy, 52; $o.StartCap = 'Round'; $o.EndCap = 'Round'; $h.DrawLine($o, $x1, $y1, $x2, $y2)
    $p = New-Object System.Drawing.Pen $yellow, 30; $p.StartCap = 'Round'; $p.EndCap = 'Round'; $h.DrawLine($p, $x1, $y1, $x2, $y2)
}

$fonts = New-Object System.Drawing.Text.PrivateFontCollection
$fontFile = Join-Path $PSScriptRoot '..\Assets\Art\Fonts\LuckiestGuy-Regular.ttf'
$fonts.AddFontFile([System.IO.Path]::GetFullPath($fontFile))
$font = New-Object System.Drawing.Font($fonts.Families[0], 250, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$text = New-Object System.Drawing.Drawing2D.GraphicsPath
$fmt = New-Object System.Drawing.StringFormat; $fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'
$text.AddString('AR', $font.FontFamily, 0, 250, (New-Object System.Drawing.RectangleF 0, 40, $size, $size), $fmt)
$shadow = $text.Clone(); $m = New-Object System.Drawing.Drawing2D.Matrix; $m.Translate(14, 18); $shadow.Transform($m)
$h.FillPath((New-Object System.Drawing.SolidBrush (Color 200 2 6 25)), $shadow)
$textOutline = New-Object System.Drawing.Pen $navy, 34; $textOutline.LineJoin = 'Round'
$h.DrawPath($textOutline, $text)
$h.FillPath((New-Object System.Drawing.SolidBrush (Color 255 255 143 173)), $text)
$f.Save("$outDir\Icon_Foreground.png", [System.Drawing.Imaging.ImageFormat]::Png)

# --- Full icon: background + foreground --------------------------------------------------------
$full = New-Canvas; $fb = $full[0]; $fgx = $full[1]
$fgx.DrawImage([System.Drawing.Image]::FromFile("$outDir\Icon_Background.png"), 0, 0, $size, $size)
$fgx.DrawImage($f, 0, 0, $size, $size)
$fb.Save("$outDir\Icon_Full.png", [System.Drawing.Imaging.ImageFormat]::Png)
$h.Dispose(); $fgx.Dispose()
Write-Output "Icons written to $outDir"
