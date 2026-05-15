; fModLoader BETA v1.0.6 — Inno Setup Installer Script
; Language page appears before Welcome. UAC elevation is enforced.

#define AppName       "fModLoader BETA v1.0.6"
#define AppShortName  "fModLoader"
#define AppVersion    "1.0.6"
#define AppPublisher  "Nexus Tribarixa"
#define AppURL        "https://github.com/nexustribarixa-redaamakrane/fmodloader"
#define AppExeName    "fModLoader.exe"
#define AppCLIName    "fModLoader_CLI.exe"

[Setup]
AppId={{D3A8F2C1-5B4E-4D7F-9E2A-1C6B8F3E7D9A}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases

DefaultDirName={commonpf64}\{#AppShortName}
DefaultGroupName={#AppShortName}

OutputDir=Output
OutputBaseFilename=fModLoader_v1.0.6_Setup

; Wizard appearance
WizardStyle=modern
WizardImageFile=installer_assets\wizard_sidebar.png
WizardImageStretch=yes
WizardSizePercent=100

; Show language dialog BEFORE the welcome page
ShowLanguageDialog=yes

; Compression
Compression=lzma2
SolidCompression=yes

; Architecture
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; UAC — request elevation at startup (triggers Windows UAC prompt)
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline

; Installer window title
SetupMutex=fModLoaderSetupMutex

[Languages]
Name: "english";   MessagesFile: "compiler:Default.isl"
Name: "french";    MessagesFile: "compiler:Languages\French.isl"
Name: "german";    MessagesFile: "compiler:Languages\German.isl"
Name: "spanish";   MessagesFile: "compiler:Languages\Spanish.isl"
Name: "italian";   MessagesFile: "compiler:Languages\Italian.isl"
Name: "portuguese";MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian";   MessagesFile: "compiler:Languages\Russian.isl"

[Messages]
; --- Title bar ---
SetupWindowTitle=Setup - fModLoader BETA v1.0.6

; --- Welcome page ---
WelcomeLabel1=Welcome to the fModLoader%nBETA v1.0.6 Setup Wizard
WelcomeLabel2=This wizard will guide you through the installation of fModLoader BETA v1.0.6 on your computer.%n%nIt is recommended that you close all other applications before continuing.%n%nClick Next to continue, or Cancel to exit Setup.

; --- Finished page ---
FinishedHeadingLabel=Completing the fModLoader%nBETA v1.0.6 Setup Wizard
FinishedLabel=Setup has finished installing fModLoader BETA v1.0.6 on your computer. The application may be launched by selecting the installed icons.


[Tasks]
Name: "desktopicon";   Description: "Create a &desktop shortcut";   GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "startupicon";   Description: "Launch fModLoader on &startup"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "dist\fModLoader\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppShortName}";       Filename: "{app}\{#AppExeName}";  Comment: "Launch fModLoader"
Name: "{group}\{#AppShortName} CLI";   Filename: "{app}\{#AppCLIName}";  Comment: "fModLoader Command-Line Tool"
Name: "{group}\Uninstall {#AppShortName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppShortName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{commonstartup}\{#AppShortName}"; Filename: "{app}\{#AppExeName}"; Tasks: startupicon

[Registry]
Root: HKLM; Subkey: "Software\{#AppPublisher}\{#AppShortName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#AppPublisher}\{#AppShortName}"; ValueType: string; ValueName: "Version";     ValueData: "{#AppVersion}"; Flags: uninsdeletekey

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppShortName}"; Flags: nowait postinstall skipifsilent

[Code]
// ─── UAC elevation check ──────────────────────────────────────────────────────
// If the installer is somehow running without elevation (e.g. UAC was bypassed),
// show a custom error page and offer to restart as administrator.

function IsElevated: Boolean;
begin
  Result := IsAdmin;
end;

var
  UACErrorPage: TWizardPage;
  UACLabel1, UACLabel2, UACStatusLabel: TLabel;
  RestartBtn: TButton;

procedure RestartAsAdmin(Sender: TObject);
var
  ResultCode: Integer;
begin
  if ShellExec('runas', ExpandConstant('{srcexe}'), '', '', SW_SHOWNORMAL, ewNoWait, ResultCode) then
    WizardForm.Close
  else
    MsgBox('Failed to restart with administrator privileges. Please right-click the installer and select "Run as administrator".', mbError, MB_OK);
end;

procedure InitializeWizard;
begin
  // Only show the UAC error page if we're somehow not elevated
  if not IsElevated then
  begin
    UACErrorPage := CreateCustomPage(
      wpWelcome,
      'Error: UAC Privileges Required',
      'The installer requires administrator privileges to continue.'
    );

    UACLabel1 := TLabel.Create(UACErrorPage);
    UACLabel1.Parent := UACErrorPage.Surface;
    UACLabel1.Left := 0;
    UACLabel1.Top := 0;
    UACLabel1.Width := UACErrorPage.SurfaceWidth;
    UACLabel1.AutoSize := False;
    UACLabel1.WordWrap := True;
    UACLabel1.Height := 60;
    UACLabel1.Caption :=
      'The setup cannot proceed because it does not have the necessary ' +
      'system-level access to install and configure required components. ' +
      'To resolve this, you must run the installer with elevated administrative privileges.';

    UACLabel2 := TLabel.Create(UACErrorPage);
    UACLabel2.Parent := UACErrorPage.Surface;
    UACLabel2.Left := 0;
    UACLabel2.Top := 70;
    UACLabel2.Width := UACErrorPage.SurfaceWidth;
    UACLabel2.AutoSize := False;
    UACLabel2.WordWrap := True;
    UACLabel2.Height := 30;
    UACLabel2.Caption := 'Please restart the application, choosing ''Run as administrator''.';

    UACStatusLabel := TLabel.Create(UACErrorPage);
    UACStatusLabel.Parent := UACErrorPage.Surface;
    UACStatusLabel.Left := 0;
    UACStatusLabel.Top := UACErrorPage.SurfaceHeight - 40;
    UACStatusLabel.AutoSize := True;
    UACStatusLabel.Caption := 'Status: Execution Halted.';

    RestartBtn := TButton.Create(UACErrorPage);
    RestartBtn.Parent := UACErrorPage.Surface;
    RestartBtn.Caption := 'Restart as Admin';
    RestartBtn.Width := 130;
    RestartBtn.Height := 26;
    RestartBtn.Left := UACErrorPage.SurfaceWidth - 130;
    RestartBtn.Top := UACErrorPage.SurfaceHeight - 40;
    RestartBtn.OnClick := @RestartAsAdmin;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  // Block Next on the UAC error page — user must restart as admin
  if (UACErrorPage <> nil) and (CurPageID = UACErrorPage.ID) then
  begin
    Result := False;
    MsgBox('You must restart the installer as administrator to continue.', mbError, MB_OK);
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  // If we're elevated, skip the UAC error page entirely
  if (UACErrorPage <> nil) and (PageID = UACErrorPage.ID) and IsElevated then
    Result := True;
end;
