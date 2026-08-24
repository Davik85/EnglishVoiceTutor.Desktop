#ifndef AppVersion
  #error AppVersion must be provided by the packaging script with /DAppVersion=...
#endif

#define AppPublisher "Language Voice Tutor"
#define AppExeName "LanguageVoiceTutor.Desktop.exe"
#define LegacyAppExeName "EnglishVoiceTutor.Desktop.exe"
#define PublishDir "..\..\artifacts\publish\win-x64-inno"
#define AppIconFile "..\..\Assets\Branding\app-icon.ico"
#define InstalledAppIconFile "{app}\Assets\Branding\app-icon.ico"
#define InstalledShortcutIconFile "{app}\Assets\Branding\app-icon-" + AppVersion + ".ico"

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
UninstallDisplayIcon={#InstalledAppIconFile}
SetupIconFile={#AppIconFile}
SetupLogging=yes
CloseApplications=yes
CloseApplicationsFilter={#AppExeName},{#LegacyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#AppIconFile}"; DestDir: "{app}\Assets\Branding"; Flags: ignoreversion
Source: "{#AppIconFile}"; DestDir: "{app}\Assets\Branding"; DestName: "app-icon-{#AppVersion}.ico"; Flags: ignoreversion

[InstallDelete]
Type: files; Name: "{commondesktop}\Language Voice Tutor.lnk"
Type: files; Name: "{app}\Assets\Branding\app-icon-*.ico"
Type: files; Name: "{app}\EnglishVoiceTutor.Desktop.exe"
Type: files; Name: "{app}\EnglishVoiceTutor.Desktop.dll"
Type: files; Name: "{app}\EnglishVoiceTutor.Desktop.deps.json"
Type: files; Name: "{app}\EnglishVoiceTutor.Desktop.runtimeconfig.json"
Type: files; Name: "{app}\EnglishVoiceTutor.Desktop.pdb"

[Icons]
Name: "{group}\Language Voice Tutor"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{#InstalledShortcutIconFile}"
Name: "{commondesktop}\Language Voice Tutor"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{#InstalledShortcutIconFile}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch Language Voice Tutor"; Flags: nowait postinstall skipifsilent


[Code]
const
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\LanguageVoiceTutor.Desktop_is1';

function MinInt(Left: Integer; Right: Integer): Integer;
begin
  if Left < Right then
    Result := Left
  else
    Result := Right;
end;

function IsDigit(Character: String): Boolean;
begin
  Result := (Length(Character) = 1) and (Character >= '0') and (Character <= '9');
end;

function IsNumericToken(Value: String): Boolean;
var
  Index: Integer;
begin
  Result := Value <> '';
  for Index := 1 to Length(Value) do
  begin
    if not IsDigit(Copy(Value, Index, 1)) then
    begin
      Result := False;
      exit;
    end;
  end;
end;

function ReadNumberToken(var Value: String): Integer;
var
  Token: String;
begin
  Token := '';
  while (Value <> '') and IsDigit(Copy(Value, 1, 1)) do
  begin
    Token := Token + Copy(Value, 1, 1);
    Delete(Value, 1, 1);
  end;

  if Token = '' then
    Result := 0
  else
    Result := StrToIntDef(Token, 0);
end;

function ReadCoreSegment(Version: String; SegmentIndex: Integer): Integer;
var
  Index: Integer;
  Work: String;
begin
  Work := Version;

  for Index := 0 to SegmentIndex do
  begin
    Result := ReadNumberToken(Work);

    if Index = SegmentIndex then
      exit;

    if (Work <> '') and (Copy(Work, 1, 1) = '.') then
      Delete(Work, 1, 1)
    else
    begin
      Result := 0;
      exit;
    end;
  end;
end;

function GetPrerelease(Version: String): String;
var
  DashPos: Integer;
  PlusPos: Integer;
begin
  Result := '';
  DashPos := Pos('-', Version);
  if DashPos = 0 then
    exit;

  Result := Copy(Version, DashPos + 1, Length(Version) - DashPos);
  PlusPos := Pos('+', Result);
  if PlusPos > 0 then
    Result := Copy(Result, 1, PlusPos - 1);
end;

function ReadPrereleaseToken(var Value: String): String;
var
  SeparatorPos: Integer;
begin
  SeparatorPos := Pos('.', Value);
  if SeparatorPos = 0 then
  begin
    Result := Value;
    Value := '';
  end
  else
  begin
    Result := Copy(Value, 1, SeparatorPos - 1);
    Delete(Value, 1, SeparatorPos);
  end;
end;

function CompareAlphaTokens(Left: String; Right: String): Integer;
var
  Index: Integer;
  LeftChar: String;
  RightChar: String;
begin
  Left := LowerCase(Left);
  Right := LowerCase(Right);

  for Index := 1 to MinInt(Length(Left), Length(Right)) do
  begin
    LeftChar := Copy(Left, Index, 1);
    RightChar := Copy(Right, Index, 1);

    if LeftChar < RightChar then
    begin
      Result := -1;
      exit;
    end;

    if LeftChar > RightChar then
    begin
      Result := 1;
      exit;
    end;
  end;

  if Length(Left) < Length(Right) then
    Result := -1
  else if Length(Left) > Length(Right) then
    Result := 1
  else
    Result := 0;
end;

function ComparePrerelease(Left: String; Right: String): Integer;
var
  LeftToken: String;
  RightToken: String;
  LeftIsNumeric: Boolean;
  RightIsNumeric: Boolean;
  LeftNumber: Integer;
  RightNumber: Integer;
begin
  if (Left = '') and (Right = '') then
  begin
    Result := 0;
    exit;
  end;

  if Left = '' then
  begin
    Result := 1;
    exit;
  end;

  if Right = '' then
  begin
    Result := -1;
    exit;
  end;

  while (Left <> '') or (Right <> '') do
  begin
    LeftToken := ReadPrereleaseToken(Left);
    RightToken := ReadPrereleaseToken(Right);

    if LeftToken = '' then
    begin
      Result := -1;
      exit;
    end;

    if RightToken = '' then
    begin
      Result := 1;
      exit;
    end;

    LeftIsNumeric := IsNumericToken(LeftToken);
    RightIsNumeric := IsNumericToken(RightToken);

    if LeftIsNumeric and RightIsNumeric then
    begin
      LeftNumber := StrToIntDef(LeftToken, 0);
      RightNumber := StrToIntDef(RightToken, 0);

      if LeftNumber < RightNumber then
      begin
        Result := -1;
        exit;
      end;

      if LeftNumber > RightNumber then
      begin
        Result := 1;
        exit;
      end;
    end
    else if LeftIsNumeric and not RightIsNumeric then
    begin
      Result := -1;
      exit;
    end
    else if not LeftIsNumeric and RightIsNumeric then
    begin
      Result := 1;
      exit;
    end
    else
    begin
      Result := CompareAlphaTokens(LeftToken, RightToken);
      if Result <> 0 then
        exit;
    end;
  end;

  Result := 0;
end;

function CompareVersions(InstalledVersion: String; InstallerVersion: String): Integer;
var
  Index: Integer;
  InstalledSegment: Integer;
  InstallerSegment: Integer;
begin
  for Index := 0 to 2 do
  begin
    InstalledSegment := ReadCoreSegment(InstalledVersion, Index);
    InstallerSegment := ReadCoreSegment(InstallerVersion, Index);

    if InstalledSegment < InstallerSegment then
    begin
      Result := -1;
      exit;
    end;

    if InstalledSegment > InstallerSegment then
    begin
      Result := 1;
      exit;
    end;
  end;

  Result := ComparePrerelease(GetPrerelease(InstalledVersion), GetPrerelease(InstallerVersion));
end;

function TryReadInstalledVersionFromRoot(RootKey: Integer; var InstalledVersion: String): Boolean;
begin
  Result := RegQueryStringValue(RootKey, UninstallKey, 'DisplayVersion', InstalledVersion);
  if not Result then
    Result := RegQueryStringValue(RootKey, UninstallKey, 'Inno Setup: App Version', InstalledVersion);

  if Result then
    InstalledVersion := Trim(InstalledVersion);
end;

function TryReadInstalledVersion(var InstalledVersion: String): Boolean;
begin
  Result := TryReadInstalledVersionFromRoot(HKLM, InstalledVersion);

  if not Result then
    Result := TryReadInstalledVersionFromRoot(HKCU, InstalledVersion);
end;

function InitializeSetup(): Boolean;
var
  InstalledVersion: String;
  VersionComparison: Integer;
begin
  Result := True;

  if not TryReadInstalledVersion(InstalledVersion) then
    exit;

  VersionComparison := CompareVersions(InstalledVersion, '{#AppVersion}');

  if VersionComparison < 0 then
  begin
    MsgBox(
      'An older version of Language Voice Tutor is already installed.' + #13#10 + #13#10 +
      'Installed version: ' + InstalledVersion + #13#10 +
      'Installer version: {#AppVersion}' + #13#10 + #13#10 +
      'Setup will update Language Voice Tutor to this version.',
      mbInformation,
      MB_OK);
    exit;
  end;

  if VersionComparison = 0 then
  begin
    Result := MsgBox(
      'Language Voice Tutor version ' + InstalledVersion + ' is already installed. Do you want to reinstall the same version?',
      mbConfirmation,
      MB_YESNO) = IDYES;
    exit;
  end;

  MsgBox(
    'A newer version of Language Voice Tutor is already installed. Installing this older version may downgrade the app.' + #13#10 + #13#10 +
    'Installed version: ' + InstalledVersion + #13#10 +
    'Installer version: {#AppVersion}' + #13#10 + #13#10 +
    'Setup will now close without making changes.',
    mbCriticalError,
    MB_OK);
  Result := False;
end;
