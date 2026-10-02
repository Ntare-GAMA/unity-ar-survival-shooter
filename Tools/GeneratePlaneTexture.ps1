# Generates Assets/Art/PlaneTracker/PlaneTracker_Name.png - the custom AR plane tracker tile.
# Usage: powershell -ExecutionPolicy Bypass -File Tools/GeneratePlaneTexture.ps1 [-Name "Your Name"]
param([string]$Name = "NTARE GAMA Allan")

Add-Type -AssemblyName System.Drawing
$size = 512
$bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.TextRenderingHint = 'AntiAliasGridFit'
$g.Clear([System.Drawing.Color]::FromArgb(80, 8, 22, 70))       # translucent deep-sea navy

$teal = [System.Drawing.Color]::FromArgb(255, 77, 242, 230)
$tealDim = [System.Drawing.Color]::FromArgb(80, 77, 242, 230)
$pink = [System.Drawing.Color]::FromArgb(255, 255, 143, 173)
$yellow = [System.Drawing.Color]::FromArgb(255, 255, 222, 51)

# Cartoon font for the name (bundled font), falling back to Segoe UI.
$fonts = New-Object System.Drawing.Text.PrivateFontCollection
$cartoon = Join-Path $PSScriptRoot '..\Assets\Art\Fonts\LuckiestGuy-Regular.ttf'
$family = 'Segoe UI'
if (Test-Path $cartoon) { $fonts.AddFontFile([System.IO.Path]::GetFullPath($cartoon)); $family = $fonts.Families[0] }

# Soft grid
$gridPen = New-Object System.Drawing.Pen $tealDim, 1
for ($i = 0; $i -le $size; $i += 64) { $g.DrawLine($gridPen, $i, 0, $i, $size); $g.DrawLine($gridPen, 0, $i, $size, $i) }

# Tile border (tiles seamlessly into a bolder grid)
$borderPen = New-Object System.Drawing.Pen $teal, 4
$g.DrawRectangle($borderPen, 2, 2, $size - 4, $size - 4)

# Rounded corner brackets
$bracketPen = New-Object System.Drawing.Pen $yellow, 10
$bracketPen.StartCap = 'Round'; $bracketPen.EndCap = 'Round'
$l = 56; $m = 40
foreach ($c in @(@($m, $m, 1, 1), @(($size - $m), $m, -1, 1), @($m, ($size - $m), 1, -1), @(($size - $m), ($size - $m), -1, -1))) {
    $g.DrawLine($bracketPen, $c[0], $c[1], $c[0] + $l * $c[2], $c[1])
    $g.DrawLine($bracketPen, $c[0], $c[1], $c[0], $c[1] + $l * $c[3])
}

# Five-petal "sky flower" outline behind the name
$points = New-Object 'System.Collections.Generic.List[System.Drawing.PointF]'
for ($i = 0; $i -lt 360; $i++) {
    $t = $i * [Math]::PI / 180
    $r = 120 * (0.62 + 0.38 * [Math]::Abs([Math]::Cos(2.5 * $t)))
    $points.Add((New-Object System.Drawing.PointF ([float](256 + $r * [Math]::Cos($t))), ([float](256 + $r * [Math]::Sin($t)))))
}
$flowerPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(150, 255, 143, 173)), 5
$g.DrawPolygon($flowerPen, $points.ToArray())

# Name + subtitle, centered
$fmt = New-Object System.Drawing.StringFormat
$fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'
$Name = $Name.ToUpperInvariant()   # the cartoon font is designed for capitals
$fontSize = 48
do {
    $font = New-Object System.Drawing.Font($family, $fontSize, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
    $w = $g.MeasureString($Name, $font).Width
    $fontSize -= 2
} while ($w -gt ($size - 60) -and $fontSize -gt 12)

$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(220, 8, 22, 70))
$face = New-Object System.Drawing.SolidBrush $yellow
$g.DrawString($Name, $font, $shadow, (New-Object System.Drawing.RectangleF 4, 4, $size, ($size - 20)), $fmt)
$g.DrawString($Name, $font, $face, (New-Object System.Drawing.RectangleF 0, 0, $size, ($size - 20)), $fmt)

$subFont = New-Object System.Drawing.Font('Segoe UI', 22, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$pinkBrush = New-Object System.Drawing.SolidBrush $pink
$g.DrawString("SAFE SPOT", $subFont, $pinkBrush, (New-Object System.Drawing.RectangleF 0, 56, $size, $size), $fmt)

$out = Join-Path $PSScriptRoot '..\Assets\Art\PlaneTracker\PlaneTracker_Name.png'
$bmp.Save([System.IO.Path]::GetFullPath($out), [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output "Wrote $out for '$Name'"
