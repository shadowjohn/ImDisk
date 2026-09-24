#define AppName "ImDisk RAM Disk Manager"
#ifndef AppVersion
  #define AppVersion "1.0.1"
#endif

[Setup]
AppId={{C79B50F8-5D95-4D80-9B38-2A77F880D137}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=FeatherMountain (shadowjohn)
AppPublisherURL=https://github.com/shadowjohn/ImDisk
AppSupportURL=https://github.com/shadowjohn/ImDisk/issues
AppUpdatesURL=https://github.com/shadowjohn/ImDisk/releases
DefaultDirName={autopf}\ImDisk RAM Disk Manager
DefaultGroupName={#AppName}
OutputDir=output
OutputBaseFilename=ImDiskGui-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
ShowLanguageDialog=yes
UninstallDisplayIcon={app}\ImDiskGui.exe
LicenseFile=..\LICENSE.md

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "zh"; MessagesFile: "ChineseTraditional.isl"

[CustomMessages]
en.ForkNotice=Independent fork maintained by FeatherMountain (shadowjohn). The original ImDisk driver is by Olof Lagerkvist (LTR Data), whose work we deeply appreciate. This setup installs the GUI and driver files; driver installation is requested separately in the app.
zh.ForkNotice=本安裝程式由羽山（shadowjohn）獨立維護，屬於 ImDisk 的第三方 fork。原始 ImDisk 驅動由 Olof Lagerkvist（LTR Data）開發，我們相當敬佩他的貢獻。本安裝程式安裝 GUI 與驅動檔案；驅動安裝會在程式內另外詢問。

[Files]
Source: "..\ImDiskGui_Release\ImDiskGui.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ImDiskGui_Release\ImDiskGui.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ImDiskGui_Release\uninstall_imdisk.cmd"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ImDiskGui_Release\driver\*"; DestDir: "{app}\driver"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.en.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\ImDiskGui.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\ImDiskGui.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Run]
Filename: "{app}\ImDiskGui.exe"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure InitializeWizard;
begin
  WizardForm.WelcomeLabel2.Caption := CustomMessage('ForkNotice');
end;
