# Rebuilds the solution and packages everything into Output/payload for distribution.

# Ensure we are in the script's directory
$ScriptDir = $PSScriptRoot
if (-not $ScriptDir) {
    $ScriptDir = Get-Location
}
Set-Location $ScriptDir

Write-Host "Cleaning Output/ directory..." -ForegroundColor Green
if (Test-Path "Output") {
    Remove-Item -Path "Output/*" -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path "Output/payload" -Force | Out-Null

Write-Host "Publishing fModLoader GUI (app)..." -ForegroundColor Green
dotnet publish app/fModLoader.csproj -c Release -f net8.0 -r win-x64 --self-contained false -p:PublishSingleFile=true -o Output/publish/app

Write-Host "Publishing fModLoader CLI (cli)..." -ForegroundColor Green
dotnet publish cli/fModLoader_CLI.csproj -c Release -f net8.0 -r win-x64 --self-contained true -p:PublishSingleFile=true -o Output/publish/cli

Write-Host "Copying files to payload..." -ForegroundColor Green
# Copy App files
Copy-Item -Path "Output/publish/app/*" -Destination "Output/payload" -Recurse -Force
# Copy CLI files (CLI might have more dependencies or overlap, overwrite is fine)
Copy-Item -Path "Output/publish/cli/*" -Destination "Output/payload" -Recurse -Force

# Clean up *.pdb and publish folders
Get-ChildItem -Path "Output/payload" -Filter "*.pdb" -Recurse | Remove-Item -Force
Remove-Item -Path "Output/publish" -Recurse -Force

# Copy extra folders (fonts, mods) from root
if (Test-Path "fonts") {
    Write-Host "Packaging fonts..." -ForegroundColor Green
    Copy-Item -Path "fonts" -Destination "Output/payload/fonts" -Recurse -Force
}
if (Test-Path "mods") {
    Write-Host "Packaging mods..." -ForegroundColor Green
    Copy-Item -Path "mods" -Destination "Output/payload/mods" -Recurse -Force
}

# Recreate empty/placeholder directories that the installer might expect if not present
@("bugfix", "diagnostics", "plugins") | ForEach-Object {
    $dir = "Output/payload/$_"
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        New-Item -ItemType File -Path "$dir/.keep" -Force | Out-Null
    }
}

Write-Host "Building Installer..." -ForegroundColor Green
dotnet publish installer/FModLoaderInstaller.csproj -c Release -f net8.0-windows -r win-x64 --self-contained false -p:PublishSingleFile=true -o Output/publish/installer

# Stage the release: installer exe + payload/ side by side
Write-Host "Staging release package..." -ForegroundColor Green
New-Item -ItemType Directory -Path "Output/release" -Force | Out-Null
Copy-Item -Path "Output/publish/installer/fModLoader_Setup.exe" -Destination "Output/release/fModLoader_Setup.exe" -Force
Copy-Item -Path "Output/payload" -Destination "Output/release/payload" -Recurse -Force
Remove-Item -Path "Output/publish" -Recurse -Force

# Also keep a standalone copy at the top level for convenience
Copy-Item -Path "Output/release/fModLoader_Setup.exe" -Destination "Output/fModLoader_Setup.exe" -Force

# Create a zip for GitHub release
$zipPath = "Output/fModLoader_Setup.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "Output/release/*" -DestinationPath $zipPath

Write-Host "Payload and Installer built successfully!" -ForegroundColor Green
Write-Host "Setup executable: Output/fModLoader_Setup.exe" -ForegroundColor Green
Write-Host "Release package:  Output/fModLoader_Setup.zip (upload this to GitHub)" -ForegroundColor Green

