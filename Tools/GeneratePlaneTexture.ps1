# Generates Assets/Art/PlaneTracker/PlaneTracker_Name.png — the custom AR plane tracker tile.
# Usage: powershell -ExecutionPolicy Bypass -File Tools/GeneratePlaneTexture.ps1 [-Name "Your Name"]
param([string]$Name = "NTARE GAMA Allan")

Add-Type -AssemblyName System.Drawing
$size = 512
$bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.TextRenderingHint = 'AntiAliasGridFit'
$g.Clear([System.Drawing.Color]::FromArgb(70, 10, 30, 40))

$accent = [System.Drawing.Color]::FromArgb(255, 0, 229, 200)
$dim = [System.Drawing.Color]::FromArgb(90, 0, 229, 200)

# Fine grid
$gridPen = New-Object System.Drawing.Pen $dim, 1
for ($i = 0; $i -le $size; $i += 64) { $g.DrawLine($gridPen, $i, 0, $i, $size); $g.DrawLine($gridPen, 0, $i, $size, $i) }

# Tile border (tiles seamlessly into a bolder grid)
$borderPen = New-Object System.Drawing.Pen $accent, 4
$g.DrawRectangle($borderPen, 2, 2, $size - 4, $size - 4)

# Corner brackets
$bracketPen = New-Object System.Drawing.Pen $accent, 8
$l = 60; $m = 40
foreach ($c in @(@($m, $m, 1, 1), @(($size - $m), $m, -1, 1), @($m, ($size - $m), 1, -1), @(($size - $m), ($size - $m), -1, -1))) {
    $g.DrawLine($bracketPen, $c[0], $c[1], $c[0] + $l * $c[2], $c[1])
    $g.DrawLine($bracketPen, $c[0], $c[1], $c[0], $c[1] + $l * $c[3])
}

# Crosshair
$ringPen = New-Object System.Drawing.Pen $dim, 3
$g.DrawEllipse($ringPen, 156, 156, 200, 200)

# Name + subtitle, centered
$fmt = New-Object System.Drawing.StringFormat
$fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'
$fontSize = 44
do {
    $font = New-Object System.Drawing.Font('Segoe UI', $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $w = $g.MeasureString($Name, $font).Width
    $fontSize -= 2
} while ($w -gt ($size - 60) -and $fontSize -gt 12)

$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 0, 0, 0))
$white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$g.DrawString($Name, $font, $shadow, (New-Object System.Drawing.RectangleF 3, 3, $size, ($size - 20)), $fmt)
$g.DrawString($Name, $font, $white, (New-Object System.Drawing.RectangleF 0, 0, $size, ($size - 20)), $fmt)

$subFont = New-Object System.Drawing.Font('Segoe UI', 20, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$accentBrush = New-Object System.Drawing.SolidBrush $accent
$g.DrawString("AR SURVIVAL ZONE", $subFont, $accentBrush, (New-Object System.Drawing.RectangleF 0, 50, $size, $size), $fmt)

$out = Join-Path $PSScriptRoot '..\Assets\Art\PlaneTracker\PlaneTracker_Name.png'
$bmp.Save([System.IO.Path]::GetFullPath($out), [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output "Wrote $out for '$Name'"
