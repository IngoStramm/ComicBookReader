#if Ver < EncodeVer(6, 7, 3, 0) || Ver >= EncodeVer(6, 7, 4, 0)
  #error This build requires Inno Setup 6.7.3.
#endif

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

[Setup]
AppId={{3E53FBD8-7CF4-4E2D-9353-1F8DD2A7651C}
AppName=ComicBookReader
AppVersion={#AppVersion}
AppVerName=ComicBookReader {#AppVersion}
DefaultDirName={localappdata}\Programs\ComicBookReader
DefaultGroupName=ComicBookReader
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
CreateUninstallRegKey=yes
OutputBaseFilename=ComicBookReader-{#AppVersion}-Setup
SetupIconFile=..\src\ComicBookReader.App\Assets\comic-reader.ico
UninstallDisplayIcon={app}\ComicBookReader.App.exe
CloseApplications=yes
CloseApplicationsFilter=ComicBookReader.App.exe
RestartApplications=no
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[CustomMessages]
english.LaunchProgram=Launch ComicBookReader
brazilianportuguese.LaunchProgram=Iniciar o ComicBookReader

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\artifacts\win-x64-distribution\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\ComicBookReader"; Filename: "{app}\ComicBookReader.App.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\ComicBookReader"; Filename: "{app}\ComicBookReader.App.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\ComicBookReader.App.exe"; Description: "{cm:LaunchProgram}"; Flags: postinstall nowait skipifsilent
