[Setup]
AppId={{8E2E5B6C-6A15-4C55-9B91-6B3D7A3C28F0}
AppName=XianyuAutoAgent
AppVersion=1.0
AppPublisher=rickxy
DefaultDirName={autopf}\XianyuAutoAgent
DisableProgramGroupPage=yes
OutputBaseFilename=XianyuAutoAgent_1.0_Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayIcon={app}\XianyuDesktopApp.exe

[Files]
Source: "..\\..\\desktop\\publish\\app\\XianyuDesktopApp\\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\\XianyuAutoAgent"; Filename: "{app}\\XianyuDesktopApp.exe"
Name: "{commondesktop}\\XianyuAutoAgent"; Filename: "{app}\\XianyuDesktopApp.exe"; Tasks: desktopicon; Flags: createonlyiffileexists

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; Flags: checkedonce
Name: "autostart"; Description: "Start on &Windows login"; Flags: unchecked

[Registry]
Root: HKCU; Subkey: "Software\\Microsoft\\Windows\\CurrentVersion\\Run"; ValueType: string; ValueName: "XianyuAutoAgent"; ValueData: """{app}\\XianyuDesktopApp.exe"""; Flags: uninsdeletevalue; Tasks: autostart
