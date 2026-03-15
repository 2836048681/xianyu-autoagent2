[Setup]
AppId={{8E2E5B6C-6A15-4C55-9B91-6B3D7A3C28F0}
AppName=XianyuAutoAgent
AppVersion=0.1
AppPublisher=rickxy
DefaultDirName=D:\rickxy
DisableProgramGroupPage=yes
OutputBaseFilename=XianyuAutoAgent_0.1_Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayIcon={app}\v1.exe

[Files]
Source: "..\\..\\dist\\v1.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\\..\\chrome\\*"; DestDir: "{app}\\chrome"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "..\\..\\chromedriver\\*"; DestDir: "{app}\\chromedriver"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\\XianyuAutoAgent"; Filename: "{app}\\v1.exe"
Name: "{commondesktop}\\XianyuAutoAgent"; Filename: "{app}\\v1.exe"; Tasks: desktopicon; Flags: createonlyiffileexists

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; Flags: checkedonce
Name: "autostart"; Description: "Start on &Windows login"; Flags: unchecked

[Registry]
Root: HKCU; Subkey: "Software\\Microsoft\\Windows\\CurrentVersion\\Run"; ValueType: string; ValueName: "XianyuAutoAgent"; ValueData: """{app}\\v1.exe"""; Flags: uninsdeletevalue; Tasks: autostart
