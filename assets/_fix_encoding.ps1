$path = Join-Path $PSScriptRoot "convert_icons.ps1"
$content = [System.IO.File]::ReadAllText($path)
$bom = New-Object System.Text.UTF8Encoding $true
[System.IO.File]::WriteAllText($path, $content, $bom)
Write-Host "Re-encoded $path with UTF-8 BOM"
