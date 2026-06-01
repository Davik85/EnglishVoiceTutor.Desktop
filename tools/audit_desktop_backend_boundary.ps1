param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"

$desktopProjectPath = Join-Path $RepoRoot "EnglishVoiceTutor.Desktop.csproj"
$backendUxLocalizationPath = Join-Path $RepoRoot "Localization/BackendUxLocalization.cs"
$interfaceOptionsPath = Join-Path $RepoRoot "Models/InterfaceLanguageOptions.cs"
$desktopFiles = Get-ChildItem -Path $RepoRoot -Recurse -File -Include *.cs,*.xaml,*.json,*.config,*.csproj |
    Where-Object {
        $_.FullName -notmatch [regex]::Escape((Join-Path $RepoRoot "backend")) -and
        $_.FullName -notmatch "[\\/]bin[\\/]" -and
        $_.FullName -notmatch "[\\/]obj[\\/]" -and
        $_.FullName -notmatch "[\\/]\.git[\\/]"
    }

foreach ($file in $desktopFiles) {
    $content = Get-Content -Raw -Encoding UTF8 $file.FullName
    if ($content -match "OPENAI_API_KEY") {
        throw "Desktop file contains OPENAI_API_KEY: $($file.FullName)"
    }

    if ($content -match "api\.openai\.com") {
        throw "Desktop file contains a direct OpenAI API endpoint: $($file.FullName)"
    }
}

$trackedProjectFiles = git -C $RepoRoot ls-files "*.csproj" "*.props" "*.targets"
foreach ($relativePath in $trackedProjectFiles) {
    $projectFilePath = Join-Path $RepoRoot $relativePath
    $projectFileContent = Get-Content -Raw -Encoding UTF8 $projectFilePath
    if ($projectFileContent -match '<PackageReference\s+Include="System\.Security\.Cryptography\.ProtectedData"' -or
        $projectFileContent -match '<PackageVersion\s+Include="System\.Security\.Cryptography\.ProtectedData"') {
        throw "Redundant System.Security.Cryptography.ProtectedData package reference found in tracked project file: $relativePath"
    }
}

$desktopProject = Get-Content -Raw -Encoding UTF8 $desktopProjectPath
if ($desktopProject -notmatch '<Compile Remove="backend/\*\*/\*\.cs" />') {
    throw "Desktop project must continue excluding backend source files."
}

$interfaceOptions = Get-Content -Raw -Encoding UTF8 $interfaceOptionsPath
$releaseReadyMatch = [regex]::Match($interfaceOptions, 'ReleaseReadyInterfaceLanguageIds\s*=\s*\[(.*?)\];', [System.Text.RegularExpressions.RegexOptions]::Singleline)
if (-not $releaseReadyMatch.Success) { throw "ReleaseReadyInterfaceLanguageIds list is missing." }
$constants = @{}
[regex]::Matches($interfaceOptions, 'public const string (\w+) = "([^"]+)"') | ForEach-Object { $constants[$_.Groups[1].Value] = $_.Groups[2].Value }
$expectedLanguages = @("en", "es", "fr", "de", "it", "pt", "ru", "pl", "ar", "ja", "ko", "sr", "hr", "bg")
$actualLanguages = @()
[regex]::Matches($releaseReadyMatch.Groups[1].Value, '"([^"]+)"|(\w+Id)') | ForEach-Object {
    if ($_.Groups[1].Success) { $actualLanguages += $_.Groups[1].Value } else { $actualLanguages += $constants[$_.Groups[2].Value] }
}
if (($actualLanguages -join ',') -ne ($expectedLanguages -join ',')) { throw "Unexpected interface language IDs: $($actualLanguages -join ', ')" }

$backendUxLocalization = Get-Content -Raw -Encoding UTF8 $backendUxLocalizationPath
foreach ($languageId in $expectedLanguages) {
    if ($backendUxLocalization -notmatch ('\["{0}"\]\s*=\s*new\(' -f [regex]::Escape($languageId))) {
        throw "Backend UX localization is missing language: $languageId"
    }
}

foreach ($messageKey in @(
    "BackendUnavailable",
    "CouldNotConnect",
    "ActionNeedsBackend",
    "SettingsLoadUnavailable",
    "SettingsSaveUnavailable",
    "LoginFailed",
    "RegisterFailed",
    "SignedIn",
    "SignedOut",
    "SessionRestored",
    "SessionExpired",
    "CredentialsRequired",
    "DisplayNameRequired",
    "VoiceTakingTooLong",
    "VoicePlaybackUnavailable",
    "ActiveLessonExists",
    "ActiveLessonExistsTitle",
    "ActiveLessonExistsMessage",
    "EndOtherLessonAndContinue",
    "Cancel",
    "EndOtherLessonFailed",
    "LessonSessionEndedElsewhere"
)) {
    if ($backendUxLocalization -notmatch [regex]::Escape("nameof(BackendUxLocalizedText.$messageKey)")) {
        throw "Backend UX localization required key is not audited: $messageKey"
    }
}

Write-Host "Desktop backend boundary audit passed."
