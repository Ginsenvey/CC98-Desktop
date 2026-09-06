$ErrorActionPreference = "Stop"
$bytes = [System.IO.File]::ReadAllBytes($args[0])
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
    if ($len -lt 0) { $pos = $e + $endKw.Length; continue }
    $found++
    $streamBytes = [byte[]]::new($len)
    if ($len -gt 0) { [System.Array]::Copy($bytes, $dataStart, $streamBytes, 0, $len) }
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
    if ($null -eq $decoded -and $len -gt 0) {
        $decoded = [System.Text.Encoding]::Latin1.GetString($streamBytes)
    }
    if ($null -ne $decoded -and $decoded -match "[^\x00-\x08\x0B\x0C\x0E-\x1F]") {
        [void]$sb.Append($decoded)
        [void]$sb.Append("`n---STREAM-BREAK---`n")
    }
    $pos = $e + $endKw.Length
}
[System.IO.File]::WriteAllText($args[1], $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output "streams=$found chars=$($sb.Length) written=$($args[1])"
