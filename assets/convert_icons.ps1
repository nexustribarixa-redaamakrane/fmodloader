# SVG → PNG → ICO conversion pipeline for fModLoader icons
# Uses Chrome headless for SVG→PNG rendering and System.Drawing for PNG→ICO
# Usage: .\assets\convert_icons.ps1

Add-Type -AssemblyName System.Drawing

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$AssetsDir = $ScriptDir
$TempDir   = Join-Path $AssetsDir "_iconbuild"

# Clean / create temp dir
if (Test-Path $TempDir) { Remove-Item $TempDir -Recurse -Force }
New-Item -ItemType Directory -Path $TempDir -Force | Out-Null

$Chrome = "C:\Program Files\Google\Chrome\Application\chrome.exe"
if (-not (Test-Path $Chrome)) {
    $Chrome = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
}

# Map SVG base names → ICO base names
# (SVG and ICO names differ for setup→installer, uninstall→uninstaller)
$Icons = [ordered]@{
    "fmodloader"           = "fmodloader"
    "fmodloader_cli"       = "fmodloader_cli"
    "fmodloader_setup"     = "fmodloader_installer"
    "fmodloader_uninstall" = "fmodloader_uninstaller"
    "fmodloader_ttfm"      = "fmodloader_ttfm"
    "fmodloader_otfm"      = "fmodloader_otfm"
}

$Sizes = @(16, 32, 48, 256)

foreach ($entry in $Icons.GetEnumerator()) {
    $svgName = $entry.Key
    $icoName = $entry.Value
    $svgPath = Join-Path $AssetsDir "$svgName.svg"
    $icoPath = Join-Path $AssetsDir "$icoName.ico"

    if (-not (Test-Path $svgPath)) {
        Write-Host "  SKIP: $svgPath not found" -ForegroundColor Yellow
        continue
    }

    Write-Host "Converting $svgName -> $icoName ..." -ForegroundColor Cyan

    # -- Step 1: Create a wrapper HTML that renders the SVG at 256x256 --
    $svgContent = Get-Content $svgPath -Raw
    $htmlPath = Join-Path $TempDir "${svgName}.html"
    $htmlContent = @"
<!DOCTYPE html>
<html>
<head><style>
  * { margin:0; padding:0; }
  body { width:256px; height:256px; overflow:hidden; background:transparent; }
  svg { width:256px; height:256px; }
</style></head>
<body>
$svgContent
</body>
</html>
"@
    Set-Content -Path $htmlPath -Value $htmlContent -Encoding UTF8

    # -- Step 2: SVG -> PNG via Chrome headless --
    $pngPath = Join-Path $TempDir "${svgName}_256.png"
    $fileUri = "file:///$($htmlPath -replace '\\','/')"
    
    $argList = @(
        "--headless=new",
        "--disable-gpu",
        "--no-sandbox",
        "--window-size=256,256",
        "--default-background-color=00000000",
        "--screenshot=""$pngPath""",
        """$fileUri"""
    )
    Start-Process -FilePath $Chrome -ArgumentList $argList -Wait -NoNewWindow | Out-Null

    if (-not (Test-Path $pngPath)) {
        Write-Host "  ERROR: PNG render failed for $svgName" -ForegroundColor Red
        continue
    }
    Write-Host "  SVG -> PNG (256x256) [OK]" -ForegroundColor DarkGray

    # -- Step 3: Create multi-size PNGs by resizing --
    $masterBmp = [System.Drawing.Bitmap]::new($pngPath)
    $bitmaps = @()

    foreach ($size in $Sizes) {
        $resized = [System.Drawing.Bitmap]::new($masterBmp, $size, $size)
        $bitmaps += $resized
    }
    $masterBmp.Dispose()

    # -- Step 4: Build ICO file manually --
    # ICO format: header (6 bytes) + entries (16 bytes each) + PNG data
    $ms = [System.IO.MemoryStream]::new()
    $bw = [System.IO.BinaryWriter]::new($ms)

    # ICO Header
    $bw.Write([UInt16]0)          # Reserved
    $bw.Write([UInt16]1)          # Type: 1 = ICO
    $bw.Write([UInt16]$bitmaps.Count)  # Number of images

    # Prepare PNG data for each size
    $pngDataList = @()
    foreach ($bmp in $bitmaps) {
        $pngMs = [System.IO.MemoryStream]::new()
        $bmp.Save($pngMs, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngDataList += ,($pngMs.ToArray())
        $pngMs.Dispose()
    }

    # Calculate offsets: header=6, each entry=16
    $dataOffset = 6 + (16 * $bitmaps.Count)

    for ($i = 0; $i -lt $bitmaps.Count; $i++) {
        $size = $Sizes[$i]
        $pngData = $pngDataList[$i]

        $bw.Write([byte]$(if ($size -ge 256) { 0 } else { $size }))  # Width (0=256)
        $bw.Write([byte]$(if ($size -ge 256) { 0 } else { $size }))  # Height (0=256)
        $bw.Write([byte]0)          # Color palette
        $bw.Write([byte]0)          # Reserved
        $bw.Write([UInt16]1)        # Color planes
        $bw.Write([UInt16]32)       # Bits per pixel
        $bw.Write([UInt32]$pngData.Length)  # Size of PNG data
        $bw.Write([UInt32]$dataOffset)      # Offset to PNG data

        $dataOffset += $pngData.Length
    }

    # Write PNG data
    foreach ($pngData in $pngDataList) {
        $bw.Write($pngData)
    }

    # Save ICO
    $icoBytes = $ms.ToArray()
    [System.IO.File]::WriteAllBytes($icoPath, $icoBytes)

    $bw.Dispose()
    $ms.Dispose()
    foreach ($bmp in $bitmaps) { $bmp.Dispose() }

    Write-Host "  PNG -> ICO [OK] ($($Sizes -join ', ')px)" -ForegroundColor DarkGray
    $sizeStr = "{0:N0}" -f (Get-Item $icoPath).Length
    Write-Host "  [OK] $icoName.ico created ($sizeStr bytes)" -ForegroundColor Green
}

# Clean up temp
Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`nAll icons converted successfully." -ForegroundColor Green
