Add-Type -AssemblyName System.Drawing

$srcPath = "E:\HomeworkMemo\tb.png"
$outPath = "E:\HomeworkMemo\app.ico"
$sizes = @(16, 24, 32, 48, 64, 128, 256)

$src = [System.Drawing.Image]::FromFile($srcPath)
$entries = New-Object System.Collections.Generic.List[object]

foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($src, 0, 0, $s, $s)
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $data = $ms.ToArray()
    $entries.Add([PSCustomObject]@{ Size = $s; Data = $data })
    $bmp.Dispose()
    $ms.Dispose()
}
$src.Dispose()

$count = $entries.Count
$offset = 6 + 16 * $count

$msOut = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($msOut)
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$count)

foreach ($e in $entries) {
    $s = [int]$e.Size
    $w = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([Byte]$w)
    $bw.Write([Byte]$w)
    $bw.Write([Byte]0)
    $bw.Write([Byte]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]$e.Data.Length)
    $bw.Write([UInt32]$offset)
    $offset += $e.Data.Length
}

foreach ($e in $entries) {
    $bw.Write([byte[]]$e.Data)
}

$bw.Flush()
[System.IO.File]::WriteAllBytes($outPath, $msOut.ToArray())
$bw.Dispose()
$msOut.Dispose()

Write-Output "ICO written: $outPath ($([Math]::Round((Get-Item $outPath).Length/1KB,1)) KB)"
