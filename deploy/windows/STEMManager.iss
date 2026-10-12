#define AppName "STEM Lab Manager"
#define AppVersion "1.0.0"
#define AppExeName "STEMLabManager.Desktop.exe"
[Setup]
AppId={{A23B6D63-1677-4956-99D8-012EB9F49B64}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\STEMManager
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\..\artifacts\windows
OutputBaseFilename=STEMManager-Setup-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExeName}
DisableProgramGroupPage=yes
[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked
[Files]
Source: "..\..\artifacts\windows\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\..\docs\USER_GUIDE.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\artifacts\docs\*"; DestDir: "{app}\Documentation"; Flags: ignoreversion
[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\STEM Demo (separate data)"; Filename: "{app}\{#AppExeName}"; Parameters: "--demo"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
[Run]
Filename: "{app}\{#AppExeName}"; Description: "Open STEM Lab Manager"; Flags: nowait postinstall skipifsilent
; Uninstall deliberately has no UninstallDelete entries for user data.
