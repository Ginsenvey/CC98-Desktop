# PDF text extractor: decompress all FlateDecode streams and search keywords
param(
    [Parameter(Mandatory = $true)][string]$PdfPath,
    [string]$OutText = ""
)
$ErrorActionPreference = "Stop"
$bytes = [System.IO.File]::ReadAllBytes($PdfPath)

function Find-Sequence([byte[]]$data, [byte[]]$seq, [int]$start) {
    for ($i = $start; $i -le $data.Length - $seq.Length; $i++) {
        $match = $true
        for ($j = 0; $j -lt $seq.Length; $j++) {
            if ($data[$i + $j] -ne $seq[$j]) { $match = $false; break }
        }
        if ($match) { return $i }
    }
    return -1
}

$streamKw = [System.Text.Encoding]::ASCII.GetBytes("stream")
$endKw = [System.Text.Encoding]::ASCII.GetBytes("endstream")
$sb = New-Object System.Text.StringBuilder
$found = 0
$pos = 0
while ($true) {
    $s = Find-Sequence $bytes $streamKw $pos
    if ($s -lt 0) { break }
    $dataStart = $s + $streamKw.Length
    if ($dataStart -lt $bytes.Length -and $bytes[$dataStart] -eq 0x0D) { $dataStart++ }
    if ($dataStart -lt $bytes.Length -and $bytes[$dataStart] -eq 0x0A) { $dataStart++ }
    $e = Find-Sequence $bytes $endKw $dataStart
    if ($e -lt 0) { break }
    $len = $e - $dataStart
    $found++
    $streamBytes = New-Object byte[] $len
    [Array]::Copy($bytes, $dataStart, $streamBytes, 0, $len)
    $decoded = $null
    if ($len -gt 2 -and $streamBytes[0] -eq 0x78) {
        try {
            $ms = New-Object System.IO.MemoryStream($streamBytes, 2, $len - 2)
            $ds = New-Object System.IO.Compression.DeflateStream($ms, [System.IO.Compression.CompressionMode]::Decompress)
            $sr = New-Object System.IO.StreamReader($ds, [System.Text.Encoding]::UTF8, $true)
            $decoded = $sr.ReadToEnd()
            $sr.Dispose(); $ds.Dispose(); $ms.Dispose()
        } catch { $decoded = $null }
    }
    if ($null -eq $decoded) {
        try {
            $ms = New-Object System.IO.MemoryStream($streamBytes, 0, $len)
            $ds = New-Object System.IO.Compression.DeflateStream($ms, [System.IO.Compression.CompressionMode]::Decompress)
            $sr = New-Object System.IO.StreamReader($ds, [System.Text.Encoding]::UTF8, $true)
            $decoded = $sr.ReadToEnd()
            $sr.Dispose(); $ds.Dispose(); $ms.Dispose()
        } catch { $decoded = $null }
    }
    if ($null -eq $decoded) {
        $decoded = [System.Text.Encoding]::Latin1.GetString($streamBytes)
    }
    if ($decoded -match "[^\x00-\x08\x0B\x0C\x0E-\x1F]") {
        [void]$sb.Append($decoded)
        [void]$sb.Append("`n---STREAM-BREAK---`n")
    }
    $pos = $e + $endKw.Length
}
$text = $sb.ToString()
Write-Output "streams=$found chars=$($text.Length)"
if ($OutText -ne "") {
    [System.IO.File]::WriteAllText($OutText, $text, (New-Object System.Text.UTF8Encoding($false)))
    Write-Output "written: $OutText"
}
foreach ($kw in @("virtualiz", "StackLayout", "ItemsRepeater", "VirtualizingStackLayout", "ItemsStackLayout")) {
    $idx = $text.IndexOf($kw, [System.StringComparison]::OrdinalIgnoreCase)
    if ($idx -ge 0) {
        Write-Output "`n===== HIT '$kw' @ $idx ====="
        $start = [Math]::Max(0, $idx - 500)
        $len2 = [Math]::Min(1600, $text.Length - $start)
        Write-Output $text.Substring($start, $len2)
    } else {
        Write-Output "`n===== MISS '$kw' ====="
    }
}
