[Setup]
AppName=fModLoader
AppVersion=1.0.6
DefaultDirName={commonpf64}\fModLoader
DefaultGroupName=fModLoader
OutputDir=Output
OutputBaseFilename=fModLoader_v1.0.6_Setup
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin

[Files]
Source: "dist\fModLoader\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\fModLoader"; Filename: "{app}\fModLoader.exe"
Name: "{group}\fModLoader CLI"; Filename: "{app}\fModLoader_CLI.exe"
Name: "{commondesktop}\fModLoader"; Filename: "{app}\fModLoader.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked
