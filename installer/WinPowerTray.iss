; WinPowerTray Inno Setup Script
; Build with:  iscc /DAppVersion=1.2.3 WinPowerTray.iss
; The CI passes /DAppVersion automatically from the release tag.

#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif

#define AppName        "WinPowerTray"
#define AppPublisher   "hello-world-dot-c"
#define AppURL         "https://github.com/hello-world-dot-c/WinPowerTray"
#define AppExeName     "WinPowerTray.exe"
#define AppMutexName   "Global\WinPowerTraySingleInstance"

[Setup]
AppId={{A3F2B1C0-9D4E-4F8A-B6C2-1E5D7A3F0B9C}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
; Install into %LOCALAPPDATA% — no UAC elevation required
DefaultDirName={localappdata}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Require Windows 10 2004+ (build 19041) for power overlay API
MinVersion=10.0.19041
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=output
OutputBaseFilename={#AppName}-{#AppVersion}-Setup
SetupIconFile=
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
; Prevent launching a second setup while the app is running
AppMutex={#AppMutexName}
UninstallDisplayIcon={app}\{#AppExeName}
CloseApplications=yes
CloseApplicationsFilter=*.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Start {#AppName} automatically with &Windows"; GroupDescription: "Additional options:"; Flags: unchecked

[Files]
; Self-contained single-file publish from CI (dotnet publish -r win-x64 --self-contained)
Source: "..\publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"

[Registry]
; Add/remove the Run key only when the "startup" task is selected
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#AppName}"; \
  ValueData: """{app}\{#AppExeName}"""; \
  Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#AppExeName}"; \
  Description: "Launch {#AppName} now"; \
  Flags: nowait postinstall skipifsilent
