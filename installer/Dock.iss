#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#define AppName "Dock"
#define AppPublisher "Maxime Razafinjato"
#define AppExe "Dock.exe"
#define SourceDir "..\publish"
#define WebView2Key "\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"

[Setup]
AppId={{7D3E90A4-4E93-4098-9E57-87BAEA82F32C}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/MaximeRazafinjato/dock-terminal
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
OutputDir=output
OutputBaseFilename=Dock-{#AppVersion}-setup
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
CloseApplications=yes
RestartApplications=no
WizardStyle=modern

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le Bureau"; GroupDescription: "Raccourcis :"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not IsWebView2Installed

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "Installation du runtime WebView2 Evergreen…"; Check: not IsWebView2Installed; Flags: waituntilterminated

[UninstallRun]
Filename: "{app}\{#AppExe}"; Parameters: "--remove-claude-hooks"; RunOnceId: "RemoveClaudeHooks"; Flags: waituntilterminated skipifdoesntexist

[Code]
function HasWebView2Value(Root: Integer; const SubKey: string): Boolean;
var
  Version: string;
begin
  Result := RegQueryStringValue(Root, SubKey, 'pv', Version) and (Version <> '') and (Version <> '0.0.0.0');
end;

function IsWebView2Installed: Boolean;
begin
  Result := HasWebView2Value(HKLM, 'SOFTWARE\WOW6432Node{#WebView2Key}')
    or HasWebView2Value(HKLM, 'SOFTWARE{#WebView2Key}')
    or HasWebView2Value(HKCU, 'Software{#WebView2Key}');
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: string;
begin
  if CurUninstallStep <> usPostUninstall then
    exit;
  DataDir := ExpandConstant('{localappdata}\Dock');
  if UninstallSilent or not DirExists(DataDir) then
    exit;
  if MsgBox('Supprimer aussi vos données Dock (session, texte des terminaux, préférences) ?' + #13#10 + DataDir, mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
    DelTree(DataDir, True, True, True);
end;
