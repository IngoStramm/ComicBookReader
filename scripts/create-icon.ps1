[CmdletBinding()]
param(
    [string]$OutputPath,
    [string]$PreviewPath,
    [ValidateSet(16, 32, 48, 256)]
    [int]$PreviewSize = 256,
    [ValidateRange(1, 16)]
    [int]$PreviewScale = 1
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
    $OutputPath = Join-Path $scriptDirectory '..\src\ComicBookReader.App\Assets\comic-reader.ico'
}

function Get-IconColor {
    param([Parameter(Mandatory)][string]$Hex)
    [System.Drawing.ColorTranslator]::FromHtml($Hex)
}

function New-RoundedPath {
    param(
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [float]$Radius
    )

    $diameter = [Math]::Min($Radius * 2, [Math]::Min($Width, $Height))
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    [void]$path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    [void]$path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    [void]$path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    [void]$path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    [void]$path.CloseFigure()
    $path
}

function New-IconPen {
    param(
        [string]$Hex,
        [float]$Width,
        [switch]$RoundCaps
    )

    $pen = [System.Drawing.Pen]::new((Get-IconColor $Hex), $Width)
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    if ($RoundCaps) {
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    }
    $pen
}

function Draw-RoundedPanel {
    param(
        [System.Drawing.Graphics]$Graphics,
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [float]$Radius,
        [string]$FillHex,
        [string]$StrokeHex,
        [float]$StrokeWidth
    )

    $path = New-RoundedPath -X $X -Y $Y -Width $Width -Height $Height -Radius $Radius
    $brush = [System.Drawing.SolidBrush]::new((Get-IconColor $FillHex))
    try {
        $Graphics.FillPath($brush, $path)
        if ($StrokeWidth -gt 0) {
            $pen = New-IconPen -Hex $StrokeHex -Width $StrokeWidth
            try {
                $Graphics.DrawPath($pen, $path)
            }
            finally {
                $pen.Dispose()
            }
        }
    }
    finally {
        $brush.Dispose()
        $path.Dispose()
    }
}

function Get-AdaptiveStroke {
    param(
        [float]$BaseWidth,
        [float]$MinimumPixels,
        [int]$Size
    )

    [Math]::Max($BaseWidth, $MinimumPixels * 256.0 / $Size)
}

function Draw-ComicBook {
    param(
        [System.Drawing.Graphics]$Graphics,
        [int]$Size
    )

    $ink = '#1E2935'
    $outerStroke = Get-AdaptiveStroke -BaseWidth 9 -MinimumPixels 1.45 -Size $Size
    $panelStroke = Get-AdaptiveStroke -BaseWidth 5.5 -MinimumPixels 0.9 -Size $Size
    $bubbleStroke = Get-AdaptiveStroke -BaseWidth 4.5 -MinimumPixels 0.75 -Size $Size
    $fineStroke = Get-AdaptiveStroke -BaseWidth 3.5 -MinimumPixels 0.55 -Size $Size

    # These shapes mirror comic-reader.svg. Strokes grow slightly at small sizes
    # so the panel layout and bubble remain visible in Windows' 16 px view.
    Draw-RoundedPanel $Graphics 32 16 192 224 18 '#FFF3DD' $ink $outerStroke
    Draw-RoundedPanel $Graphics 49 31 34 194 9 '#E95F38' $ink 0

    $bindingPen = New-IconPen -Hex $ink -Width (Get-AdaptiveStroke -BaseWidth 5 -MinimumPixels 0.8 -Size $Size) -RoundCaps
    try {
        $Graphics.DrawLine($bindingPen, 85, 36, 85, 220)
    }
    finally {
        $bindingPen.Dispose()
    }

    Draw-RoundedPanel $Graphics 91 38 119 74 9 '#FFFBF1' $ink $panelStroke
    Draw-RoundedPanel $Graphics 91 122 56 98 9 '#E3ECE7' $ink $panelStroke
    Draw-RoundedPanel $Graphics 154 122 56 98 9 '#F7DEA7' $ink $panelStroke

    # Speech balloon and its tail are one path to keep the silhouette clean.
    $bubble = [System.Drawing.Drawing2D.GraphicsPath]::new()
    [void]$bubble.AddLine(119, 51, 181, 51)
    [void]$bubble.AddBezier(181, 51, 188, 51, 193, 56, 193, 63)
    [void]$bubble.AddLine(193, 63, 193, 73)
    [void]$bubble.AddBezier(193, 73, 193, 80, 188, 85, 181, 85)
    [void]$bubble.AddLine(181, 85, 141, 85)
    [void]$bubble.AddLine(141, 85, 123, 95)
    [void]$bubble.AddBezier(123, 95, 121, 96, 119, 95, 120, 93)
    [void]$bubble.AddLine(120, 93, 124, 85)
    [void]$bubble.AddLine(124, 85, 119, 85)
    [void]$bubble.AddBezier(119, 85, 112, 85, 107, 80, 107, 73)
    [void]$bubble.AddLine(107, 73, 107, 63)
    [void]$bubble.AddBezier(107, 63, 107, 56, 112, 51, 119, 51)
    [void]$bubble.CloseFigure()
    $bubbleBrush = [System.Drawing.SolidBrush]::new((Get-IconColor '#FFFEFA'))
    $bubblePen = New-IconPen -Hex $ink -Width $bubbleStroke
    try {
        $Graphics.FillPath($bubbleBrush, $bubble)
        $Graphics.DrawPath($bubblePen, $bubble)
    }
    finally {
        $bubbleBrush.Dispose()
        $bubblePen.Dispose()
        $bubble.Dispose()
    }

    $textPen = New-IconPen -Hex $ink -Width $fineStroke -RoundCaps
    try {
        $Graphics.DrawLine($textPen, 127, 65, 170, 65)
        $Graphics.DrawLine($textPen, 127, 75, 155, 75)
    }
    finally {
        $textPen.Dispose()
    }

    $bolt = [System.Drawing.Drawing2D.GraphicsPath]::new()
    [void]$bolt.AddPolygon([System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(171, 149),
        [System.Drawing.PointF]::new(188, 149),
        [System.Drawing.PointF]::new(178, 169),
        [System.Drawing.PointF]::new(193, 169),
        [System.Drawing.PointF]::new(168, 201),
        [System.Drawing.PointF]::new(175, 178),
        [System.Drawing.PointF]::new(162, 178)
    ))
    $boltBrush = [System.Drawing.SolidBrush]::new((Get-IconColor '#EA623A'))
    $boltPen = New-IconPen -Hex $ink -Width $fineStroke
    try {
        $Graphics.FillPath($boltBrush, $bolt)
        $Graphics.DrawPath($boltPen, $bolt)
    }
    finally {
        $boltBrush.Dispose()
        $boltPen.Dispose()
        $bolt.Dispose()
    }

    # Reapply the outside contour above the spine so the silhouette stays even.
    $pagePath = New-RoundedPath -X 32 -Y 16 -Width 192 -Height 224 -Radius 18
    $pagePen = New-IconPen -Hex $ink -Width $outerStroke
    try {
        $Graphics.DrawPath($pagePen, $pagePath)
    }
    finally {
        $pagePen.Dispose()
        $pagePath.Dispose()
    }
}

$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$sizes = @(16, 32, 48, 256)
$supersample = 4
$rendered = [System.Collections.Generic.List[object]]::new()

foreach ($size in $sizes) {
    $largeSide = $size * $supersample
    $largeBitmap = [System.Drawing.Bitmap]::new(
        $largeSide,
        $largeSide,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
    )
    $largeGraphics = [System.Drawing.Graphics]::FromImage($largeBitmap)
    try {
        $largeGraphics.Clear([System.Drawing.Color]::Transparent)
        $largeGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $largeGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $largeGraphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $scale = $largeSide / 256.0
        $largeGraphics.ScaleTransform([single]$scale, [single]$scale)
        Draw-ComicBook -Graphics $largeGraphics -Size $size
    }
    finally {
        $largeGraphics.Dispose()
    }

    $smallBitmap = [System.Drawing.Bitmap]::new(
        $size,
        $size,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
    )
    $smallGraphics = [System.Drawing.Graphics]::FromImage($smallBitmap)
    try {
        $smallGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $smallGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $smallGraphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $smallGraphics.DrawImage(
            $largeBitmap,
            [System.Drawing.Rectangle]::new(0, 0, $size, $size),
            [System.Drawing.Rectangle]::new(0, 0, $largeSide, $largeSide),
            [System.Drawing.GraphicsUnit]::Pixel
        )
    }
    finally {
        $smallGraphics.Dispose()
        $largeBitmap.Dispose()
    }

    $memory = [System.IO.MemoryStream]::new()
    try {
        $smallBitmap.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngBytes = $memory.ToArray()
        [void]$rendered.Add([pscustomobject]@{
            Size = $size
            Png = $pngBytes
            Bitmap = $smallBitmap
        })
    }
    finally {
        $memory.Dispose()
    }
}

try {
    if (-not [string]::IsNullOrWhiteSpace($PreviewPath)) {
        $previewPathFull = [System.IO.Path]::GetFullPath($PreviewPath)
        $previewDirectory = Split-Path -Parent $previewPathFull
        if (-not [string]::IsNullOrWhiteSpace($previewDirectory)) {
            New-Item -ItemType Directory -Path $previewDirectory -Force | Out-Null
        }
        $preview = $rendered | Where-Object Size -eq $PreviewSize | Select-Object -First 1
        if ($PreviewScale -eq 1) {
            $preview.Bitmap.Save($previewPathFull, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        else {
            $scaledPreview = [System.Drawing.Bitmap]::new($PreviewSize * $PreviewScale, $PreviewSize * $PreviewScale)
            $previewGraphics = [System.Drawing.Graphics]::FromImage($scaledPreview)
            try {
                $previewGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
                $previewGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
                $previewGraphics.DrawImage(
                    $preview.Bitmap,
                    [System.Drawing.Rectangle]::new(0, 0, $scaledPreview.Width, $scaledPreview.Height),
                    [System.Drawing.Rectangle]::new(0, 0, $PreviewSize, $PreviewSize),
                    [System.Drawing.GraphicsUnit]::Pixel
                )
                $scaledPreview.Save($previewPathFull, [System.Drawing.Imaging.ImageFormat]::Png)
            }
            finally {
                $previewGraphics.Dispose()
                $scaledPreview.Dispose()
            }
        }
    }

    $fileStream = [System.IO.File]::Open(
        $OutputPath,
        [System.IO.FileMode]::Create,
        [System.IO.FileAccess]::Write,
        [System.IO.FileShare]::None
    )
    $writer = [System.IO.BinaryWriter]::new($fileStream)
    try {
        $writer.Write([UInt16]0) # Reserved
        $writer.Write([UInt16]1) # ICO type
        $writer.Write([UInt16]$rendered.Count)

        $nextOffset = 6 + (16 * $rendered.Count)
        foreach ($entry in $rendered) {
            $dimension = if ($entry.Size -eq 256) { [byte]0 } else { [byte]$entry.Size }
            $writer.Write([byte]$dimension)
            $writer.Write([byte]$dimension)
            $writer.Write([byte]0) # Palette size
            $writer.Write([byte]0) # Reserved
            $writer.Write([UInt16]1) # Color planes
            $writer.Write([UInt16]32) # Bits per pixel
            $writer.Write([UInt32]$entry.Png.Length)
            $writer.Write([UInt32]$nextOffset)
            $nextOffset += $entry.Png.Length
        }

        foreach ($entry in $rendered) {
            $writer.Write([byte[]]$entry.Png)
        }
    }
    finally {
        $writer.Dispose()
    }
}
finally {
    foreach ($entry in $rendered) {
        $entry.Bitmap.Dispose()
    }
}

Write-Output "Created $OutputPath with 16, 32, 48, and 256 px images."
