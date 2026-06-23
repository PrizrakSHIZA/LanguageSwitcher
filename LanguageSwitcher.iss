[Setup]
AppName=Language Switcher
AppVersion=1.0.1
DefaultDirName={autopf}\Language Switcher
DefaultGroupName=Language Switcher
OutputDir=installer
OutputBaseFilename=LanguageSwitcherSetup
Compression=lzma
SolidCompression=yes
UninstallDisplayIcon={app}\LanguageSwitcher.exe

[Files]
Source: "LanguageSwitcher\bin\Release\LanguageSwitcher.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "LanguageSwitcher\bin\Release\LanguageSwitcher.exe.config"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\Language Switcher"; Filename: "{app}\LanguageSwitcher.exe"

[Run]
Filename: "{app}\LanguageSwitcher.exe"; Description: "Launch Language Switcher"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\LanguageSwitcher"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "LanguageSwitcher"; Flags: deletevalue uninsdeletevalue