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
ChangesAssociations=yes
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[CustomMessages]
english.LaunchProgram=Launch ComicBookReader
english.FileAssociationsGroup=File associations
english.ChooseDefaultTask=Open Windows Settings to choose ComicBookReader as the default for .cbz and .cbr
english.CbzFileType=ComicBookReader CBZ comic archive
english.CbrFileType=ComicBookReader CBR comic archive
english.CapabilityDescription=ComicBookReader reads CBZ and CBR comic archives.
brazilianportuguese.LaunchProgram=Iniciar o ComicBookReader
brazilianportuguese.FileAssociationsGroup=Associações de arquivos
brazilianportuguese.ChooseDefaultTask=Abrir as Configurações do Windows para escolher o ComicBookReader como padrão para .cbz e .cbr
brazilianportuguese.CbzFileType=Arquivo de HQ CBZ do ComicBookReader
brazilianportuguese.CbrFileType=Arquivo de HQ CBR do ComicBookReader
brazilianportuguese.CapabilityDescription=O ComicBookReader lê arquivos de HQ nos formatos CBZ e CBR.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "chooseComicDefaults"; Description: "{cm:ChooseDefaultTask}"; GroupDescription: "{cm:FileAssociationsGroup}"

[Files]
Source: "..\artifacts\win-x64-distribution\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
Root: HKCU; Subkey: "Software\Classes\ComicBookReader.CBZ"; ValueType: string; ValueName: ""; ValueData: "{cm:CbzFileType}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\ComicBookReader.CBZ\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\ComicBookReader.App.exe,0"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\ComicBookReader.CBZ\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\ComicBookReader.App.exe"" ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\ComicBookReader.CBR"; ValueType: string; ValueName: ""; ValueData: "{cm:CbrFileType}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\ComicBookReader.CBR\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\ComicBookReader.App.exe,0"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\ComicBookReader.CBR\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\ComicBookReader.App.exe"" ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\.cbz\OpenWithProgids"; ValueType: string; ValueName: "ComicBookReader.CBZ"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\.cbr\OpenWithProgids"; ValueType: string; ValueName: "ComicBookReader.CBR"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\ComicBookReader.App.exe"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "ComicBookReader"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\ComicBookReader.App.exe\SupportedTypes"; ValueType: string; ValueName: ".cbz"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\Applications\ComicBookReader.App.exe\SupportedTypes"; ValueType: string; ValueName: ".cbr"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\ComicBookReader\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "ComicBookReader"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\ComicBookReader\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "{cm:CapabilityDescription}"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\ComicBookReader\Capabilities\FileAssociations"; ValueType: string; ValueName: ".cbz"; ValueData: "ComicBookReader.CBZ"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\ComicBookReader\Capabilities\FileAssociations"; ValueType: string; ValueName: ".cbr"; ValueData: "ComicBookReader.CBR"; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "ComicBookReader"; ValueData: "Software\ComicBookReader\Capabilities"; Flags: uninsdeletevalue

[Icons]
Name: "{group}\ComicBookReader"; Filename: "{app}\ComicBookReader.App.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\ComicBookReader"; Filename: "{app}\ComicBookReader.App.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "ms-settings:defaultapps?registeredAppUser=ComicBookReader"; Tasks: chooseComicDefaults; Flags: shellexec nowait skipifsilent
Filename: "{app}\ComicBookReader.App.exe"; Description: "{cm:LaunchProgram}"; Flags: postinstall nowait skipifsilent; Tasks: not chooseComicDefaults
