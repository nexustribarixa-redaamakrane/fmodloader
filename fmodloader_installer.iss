; fModLoader BETA v1.0.6 — Inno Setup Installer Script
; Full installer: Language → Welcome → License → Dir → StartMenu → Tasks → Ready → Install

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

; License
LicenseFile=LICENSE.txt

OutputDir=Output
OutputBaseFilename=fModLoader_v1.0.6_Setup

; Wizard appearance
WizardStyle=modern
WizardImageFile=installer_assets\wizard_sidebar.png
WizardImageStretch=yes
WizardSizePercent=100

; Language dialog BEFORE welcome
ShowLanguageDialog=yes

; Compression
Compression=lzma2
SolidCompression=yes

; Architecture
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; UAC — triggers Windows UAC prompt before installer launches
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline

SetupMutex=fModLoaderSetupMutex

[Languages]
Name: "english";    MessagesFile: "compiler:Default.isl"
Name: "french";     MessagesFile: "compiler:Languages\French.isl"
Name: "german";     MessagesFile: "compiler:Languages\German.isl"
Name: "spanish";    MessagesFile: "compiler:Languages\Spanish.isl"
Name: "italian";    MessagesFile: "compiler:Languages\Italian.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian";    MessagesFile: "compiler:Languages\Russian.isl"

[Messages]
SetupWindowTitle=Setup - fModLoader BETA v1.0.6

WelcomeLabel1=Welcome to the fModLoader%nBETA v1.0.6 Setup Wizard
WelcomeLabel2=This wizard will guide you through the installation of fModLoader BETA v1.0.6 on your computer.%n%nIt is recommended that you close all other applications before continuing.%n%nClick Next to continue, or Cancel to exit Setup.

LicenseLabel=GNU General Public License version 3.0%n(GNU GPL v3.0)
LicenseLabel3=Please read the following License Agreement. You must accept the terms of this agreement before continuing with the installation.
LicenseAccepted=I accept the terms of the GNU GPL v3.0
LicenseNotAccepted=I decline the terms and conditions

SelectDirDesc=Please select the destination location where you would like to install fModLoader.
SelectDirBrowseLabel=To continue, click Next. If you would like to select a different folder, click Browse.

SelectTasksDesc=Please select the additional tasks you would like Setup to perform while installing {#AppShortName}, then click Next.

ReadyLabel1=Setup is now ready to begin installing {#AppName} on your computer.
ReadyLabel2a=Click Install to continue with the installation.
ReadyLabel2b=Click < Back to review or change any settings.
ReadyMemoTasks=Selected Tasks:

FinishedHeadingLabel=Completing the fModLoader%nBETA v1.0.6 Setup Wizard
FinishedLabel=Setup has finished installing fModLoader BETA v1.0.6 on your computer. The application may be launched by selecting the installed icons.

[Tasks]
; --- File Associations ---
Name: "assoc_modcompat_ttf"; Description: "Associate .MODCOMPAT.TTF files"; GroupDescription: "File Associations:"; Flags: checkedonce
Name: "assoc_modcompat_otf"; Description: "Associate .MODCOMPAT.OTF files"; GroupDescription: "File Associations:"; Flags: checkedonce
Name: "assoc_modcompat_ttc"; Description: "Associate .MODCOMPAT.TTC files"; GroupDescription: "File Associations:"; Flags: checkedonce
Name: "assoc_ttfm";          Description: "Associate .TTFM files";          GroupDescription: "File Associations:"; Flags: checkedonce
Name: "assoc_otfm";          Description: "Associate .OTFM files";          GroupDescription: "File Associations:"; Flags: checkedonce
; --- Shortcuts ---
Name: "desktopicon";  Description: "Create a &desktop shortcut";  GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "startupicon";  Description: "Launch fModLoader on &startup"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "dist\fModLoader\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppShortName}";             Filename: "{app}\{#AppExeName}"; Comment: "Launch fModLoader"
Name: "{group}\{#AppShortName} CLI";         Filename: "{app}\{#AppCLIName}"; Comment: "fModLoader Command-Line Tool"
Name: "{group}\Uninstall {#AppShortName}";   Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppShortName}";     Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{commonstartup}\{#AppShortName}";     Filename: "{app}\{#AppExeName}"; Tasks: startupicon

[Registry]
; --- .TTFM mod packages → fModLoader ---
Root: HKCR; Subkey: ".ttfm";                          ValueType: string; ValueName: ""; ValueData: "fModLoader.ttfm";         Flags: uninsdeletevalue;  Tasks: assoc_ttfm
Root: HKCR; Subkey: "fModLoader.ttfm";                ValueType: string; ValueName: ""; ValueData: "fModLoader Mod Package";   Flags: uninsdeletekeyifempty; Tasks: assoc_ttfm
Root: HKCR; Subkey: "fModLoader.ttfm\DefaultIcon";    ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0";    Flags: uninsdeletekeyifempty; Tasks: assoc_ttfm
Root: HKCR; Subkey: "fModLoader.ttfm\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletekeyifempty; Tasks: assoc_ttfm

; --- .OTFM mod packages → fModLoader ---
Root: HKCR; Subkey: ".otfm";                          ValueType: string; ValueName: ""; ValueData: "fModLoader.otfm";         Flags: uninsdeletevalue;  Tasks: assoc_otfm
Root: HKCR; Subkey: "fModLoader.otfm";                ValueType: string; ValueName: ""; ValueData: "fModLoader OTF Mod Package"; Flags: uninsdeletekeyifempty; Tasks: assoc_otfm
Root: HKCR; Subkey: "fModLoader.otfm\DefaultIcon";    ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0";    Flags: uninsdeletekeyifempty; Tasks: assoc_otfm
Root: HKCR; Subkey: "fModLoader.otfm\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletekeyifempty; Tasks: assoc_otfm

; --- .MODCOMPAT.TTF → OpenWithProgIds (fModLoader) ---
Root: HKCR; Subkey: ".ttf\OpenWithProgids"; ValueType: string; ValueName: "fModLoader.modcompat"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc_modcompat_ttf
Root: HKCR; Subkey: ".otf\OpenWithProgids"; ValueType: string; ValueName: "fModLoader.modcompat"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc_modcompat_otf
Root: HKCR; Subkey: ".ttc\OpenWithProgids"; ValueType: string; ValueName: "fModLoader.modcompat"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc_modcompat_ttc

Root: HKCR; Subkey: "fModLoader.modcompat";               ValueType: string; ValueName: ""; ValueData: "fModLoader ModCompat Font"; Flags: uninsdeletekeyifempty; Tasks: assoc_modcompat_ttf assoc_modcompat_otf assoc_modcompat_ttc
Root: HKCR; Subkey: "fModLoader.modcompat\DefaultIcon";   ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0";      Flags: uninsdeletekeyifempty; Tasks: assoc_modcompat_ttf assoc_modcompat_otf assoc_modcompat_ttc
Root: HKCR; Subkey: "fModLoader.modcompat\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletekeyifempty; Tasks: assoc_modcompat_ttf assoc_modcompat_otf assoc_modcompat_ttc

; --- App path registration ---
Root: HKLM; Subkey: "Software\{#AppPublisher}\{#AppShortName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#AppPublisher}\{#AppShortName}"; ValueType: string; ValueName: "Version";     ValueData: "{#AppVersion}"; Flags: uninsdeletekey

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppShortName}"; Flags: nowait postinstall skipifsilent

[Code]
// ─── Globals ──────────────────────────────────────────────────────────────────
var
  // UAC error page
  UACErrorPage: TWizardPage;
  UACLabel1, UACLabel2, UACStatusLabel: TLabel;
  RestartBtn: TButton;

  // File association viewer dropdown (on wpSelectTasks)
  AssocViewerLabel: TLabel;
  AssocViewerCombo: TComboBox;
  AssocViewerHint: TLabel;

// ─── UAC helpers ─────────────────────────────────────────────────────────────
function IsElevated: Boolean;
begin
  Result := IsAdmin;
end;

procedure RestartAsAdmin(Sender: TObject);
var
  ResultCode: Integer;
begin
  if ShellExec('runas', ExpandConstant('{srcexe}'), '', '', SW_SHOWNORMAL, ewNoWait, ResultCode) then
    WizardForm.Close
  else
    MsgBox('Failed to restart with administrator privileges.' + #13#10 +
           'Please right-click the installer and select "Run as administrator".',
           mbError, MB_OK);
end;

// ─── Wizard initialization ────────────────────────────────────────────────────
procedure InitializeWizard;
begin
  // -- UAC error page (only shown if not elevated) --
  if not IsElevated then
  begin
    UACErrorPage := CreateCustomPage(wpWelcome,
      'Error: UAC Privileges Required',
      'The installer requires administrator privileges to continue.');

    UACLabel1 := TLabel.Create(UACErrorPage);
    UACLabel1.Parent := UACErrorPage.Surface;
    UACLabel1.Left := 0;
    UACLabel1.Top := 0;
    UACLabel1.Width := UACErrorPage.SurfaceWidth;
    UACLabel1.Height := 80;
    UACLabel1.AutoSize := False;
    UACLabel1.WordWrap := True;
    UACLabel1.Caption :=
      'The setup cannot proceed because it does not have the necessary ' +
      'system-level access to install and configure required components. ' +
      'To resolve this, you must run the installer with elevated administrative privileges.';

    UACLabel2 := TLabel.Create(UACErrorPage);
    UACLabel2.Parent := UACErrorPage.Surface;
    UACLabel2.Left := 0;
    UACLabel2.Top := 88;
    UACLabel2.Width := UACErrorPage.SurfaceWidth;
    UACLabel2.Height := 30;
    UACLabel2.AutoSize := False;
    UACLabel2.WordWrap := True;
    UACLabel2.Caption := 'Please restart the application, choosing ''Run as administrator''.';

    UACStatusLabel := TLabel.Create(UACErrorPage);
    UACStatusLabel.Parent := UACErrorPage.Surface;
    UACStatusLabel.Left := 0;
    UACStatusLabel.Top := UACErrorPage.SurfaceHeight - 26;
    UACStatusLabel.AutoSize := True;
    UACStatusLabel.Caption := 'Status: Execution Halted.';

    RestartBtn := TButton.Create(UACErrorPage);
    RestartBtn.Parent := UACErrorPage.Surface;
    RestartBtn.Caption := 'Restart as Admin';
    RestartBtn.Width := 130;
    RestartBtn.Height := 26;
    RestartBtn.Left := UACErrorPage.SurfaceWidth - 130;
    RestartBtn.Top := UACErrorPage.SurfaceHeight - 26;
    RestartBtn.OnClick := @RestartAsAdmin;
  end;

  // -- File viewer combobox on the Select Tasks page --
  // We add a label + combobox to the right side of the TasksList area
  AssocViewerLabel := TLabel.Create(WizardForm);
  AssocViewerLabel.Parent := WizardForm.SelectTasksPage;
  AssocViewerLabel.Caption := 'Associate with application:';
  AssocViewerLabel.Left := WizardForm.TasksList.Left + WizardForm.TasksList.Width div 2 + 8;
  AssocViewerLabel.Top := WizardForm.TasksList.Top - 20;
  AssocViewerLabel.AutoSize := True;
  AssocViewerLabel.Visible := False;

  AssocViewerCombo := TComboBox.Create(WizardForm);
  AssocViewerCombo.Parent := WizardForm.SelectTasksPage;
  AssocViewerCombo.Style := csDropDownList;
  AssocViewerCombo.Left := AssocViewerLabel.Left;
  AssocViewerCombo.Top := WizardForm.TasksList.Top;
  AssocViewerCombo.Width := WizardForm.TasksList.Width div 2 - 8;
  AssocViewerCombo.Items.Add('Windows Font Viewer');
  AssocViewerCombo.Items.Add('Adobe Font Manager');
  AssocViewerCombo.Items.Add('None');
  AssocViewerCombo.ItemIndex := 0;
  AssocViewerCombo.Visible := False;

  AssocViewerHint := TLabel.Create(WizardForm);
  AssocViewerHint.Parent := WizardForm.SelectTasksPage;
  AssocViewerHint.Caption := 'Applies to: .MODCOMPAT.TTF / OTF / TTC';
  AssocViewerHint.Left := AssocViewerCombo.Left;
  AssocViewerHint.Top := AssocViewerCombo.Top + AssocViewerCombo.Height + 6;
  AssocViewerHint.AutoSize := True;
  AssocViewerHint.Visible := False;
end;

// ─── Show/hide combo when on the tasks page ───────────────────────────────────
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectTasks then
  begin
    AssocViewerLabel.Visible := True;
    AssocViewerCombo.Visible := True;
    AssocViewerHint.Visible := True;
  end else begin
    if AssocViewerLabel <> nil then AssocViewerLabel.Visible := False;
    if AssocViewerCombo <> nil then AssocViewerCombo.Visible := False;
    if AssocViewerHint  <> nil then AssocViewerHint.Visible  := False;
  end;
end;

// ─── Block next on UAC error page ─────────────────────────────────────────────
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if (UACErrorPage <> nil) and (CurPageID = UACErrorPage.ID) then
  begin
    Result := False;
    MsgBox('You must restart the installer as administrator to continue.', mbError, MB_OK);
  end;
end;

// ─── Skip UAC page if already elevated ───────────────────────────────────────
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if (UACErrorPage <> nil) and (PageID = UACErrorPage.ID) and IsElevated then
    Result := True;
end;

// ─── Apply viewer association after install ───────────────────────────────────
procedure RegisterFontViewer(Ext, ProgId: String);
var
  ViewerExe: String;
begin
  if AssocViewerCombo = nil then Exit;
  if AssocViewerCombo.ItemIndex = 2 then Exit; // "None"

  if AssocViewerCombo.ItemIndex = 0 then
    // Windows Font Viewer
    ViewerExe := ExpandConstant('{sys}\fontview.exe "%1"')
  else
    // Adobe Font Manager — best-effort path
    ViewerExe := '"C:\Program Files (x86)\Adobe\Adobe Font Manager\AFM.exe" "%1"';

  RegWriteStringValue(HKCR, ProgId + '\shell\preview\command', '', ViewerExe);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if WizardIsTaskSelected('assoc_modcompat_ttf') then
      RegisterFontViewer('.ttf', 'fModLoader.modcompat');
    if WizardIsTaskSelected('assoc_modcompat_otf') then
      RegisterFontViewer('.otf', 'fModLoader.modcompat');
    if WizardIsTaskSelected('assoc_modcompat_ttc') then
      RegisterFontViewer('.ttc', 'fModLoader.modcompat');
  end;
end;
