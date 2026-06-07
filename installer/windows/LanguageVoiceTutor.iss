#ifndef AppVersion
  #error AppVersion must be provided by the packaging script with /DAppVersion=...
#endif

#define AppPublisher "Language Voice Tutor"
#define AppExeName "EnglishVoiceTutor.Desktop.exe"
#define PublishDir "..\..\artifacts\publish\win-x64-inno"

[Setup]
AppId=LanguageVoiceTutor.Desktop
AppName=Language Voice Tutor
AppVerName=Language Voice Tutor {#AppVersion}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\Language Voice Tutor
DefaultGroupName=Language Voice Tutor
DisableProgramGroupPage=yes
OutputDir=..\..\artifacts\installers\windows
OutputBaseFilename=LanguageVoiceTutorSetup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayName=Language Voice Tutor
UninstallDisplayIcon={app}\{#AppExeName}
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Language Voice Tutor"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\Language Voice Tutor"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch Language Voice Tutor"; Flags: nowait postinstall skipifsilent
