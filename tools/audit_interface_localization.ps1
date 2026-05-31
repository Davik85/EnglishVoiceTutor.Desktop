param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"

$expectedLanguages = @("en", "es", "fr", "de", "it", "pt", "ru", "pl", "ar", "ja", "ko", "sr", "hr", "bg")
$requiredTexts = @(
    "Your profile",
    "This helps your tutor personalize lessons.",
    "Your name",
    "Learning goal",
    "Lesson history",
    "Daily Life",
    "Small talk, introductions, and daily situations.",
    "Travel",
    "Airports, hotels, directions, and transport.",
    "Work & Business",
    "Meetings, emails, calls, and workplace conversations.",
    "Job Interview",
    "Practice common interview questions and answers.",
    "Restaurant & Cafe",
    "Ordering food, booking tables, and polite requests.",
    "Free Conversation",
    "Open English conversation with safe, respectful boundaries.",
    "Back to chat",
    "View feedback",
    "Feedback",
    "Corrected version",
    "Grammar tip",
    "Vocabulary tip",
    "Culture tip",
    "Recording... Click Stop recording when you finish.",
    "Transcribing your voice...",
    "You completed a short practice dialogue and received AI feedback on your response.",
    "Keep practicing full sentences and apply the feedback tips to improve grammar and vocabulary.",
    "System default",
    "Microphone test completed.",
    "Subscription status: unavailable"
)

$appLocalizationPath = Join-Path $RepoRoot "Localization/AppLocalization.cs"
$interfaceOptionsPath = Join-Path $RepoRoot "Models/InterfaceLanguageOptions.cs"
$studyLanguagePath = Join-Path $RepoRoot "Content/StudyLanguages/study_languages.json"

$appLocalization = Get-Content -Raw -Encoding UTF8 $appLocalizationPath
$interfaceOptions = Get-Content -Raw -Encoding UTF8 $interfaceOptionsPath
$studyLanguages = Get-Content -Raw -Encoding UTF8 $studyLanguagePath | ConvertFrom-Json

$releaseReadyMatch = [regex]::Match($interfaceOptions, 'ReleaseReadyInterfaceLanguageIds\s*=\s*\[(.*?)\];', [System.Text.RegularExpressions.RegexOptions]::Singleline)
if (-not $releaseReadyMatch.Success) { throw "ReleaseReadyInterfaceLanguageIds list is missing." }
$constants = @{}
[regex]::Matches($interfaceOptions, 'public const string (\w+) = "([^"]+)"') | ForEach-Object { $constants[$_.Groups[1].Value] = $_.Groups[2].Value }
$actualLanguages = @()
[regex]::Matches($releaseReadyMatch.Groups[1].Value, '"([^"]+)"|([A-Za-z]+Id)') | ForEach-Object {
    if ($_.Groups[1].Success) { $actualLanguages += $_.Groups[1].Value } else { $actualLanguages += $constants[$_.Groups[2].Value] }
}
if (($actualLanguages -join ',') -ne ($expectedLanguages -join ',')) { throw "Unexpected interface language IDs: $($actualLanguages -join ', ')" }

$studyIds = @($studyLanguages | ForEach-Object { $_.id })
$expectedStudyIds = @("en", "fr", "de", "pt", "es", "it")
if (($studyIds -join ',') -ne ($expectedStudyIds -join ',')) { throw "Unexpected study language IDs: $($studyIds -join ', ')" }

foreach ($languageId in $expectedLanguages) {
    if ($languageId -eq "en") { continue }

    $languageBlockMatch = [regex]::Match($appLocalization, "\[\"$languageId\"\]\s*=\s*new Dictionary<string, string>\(StringComparer\.OrdinalIgnoreCase\)\s*\{(.*?)\n\s*\}", [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $languageBlockMatch.Success) { throw "$languageId is missing learner UI localization coverage." }

    $languageBlock = $languageBlockMatch.Groups[1].Value
    foreach ($englishText in $requiredTexts) {
        $escaped = [regex]::Escape($englishText)
        $entryMatch = [regex]::Match($languageBlock, "\[\"$escaped\"\]\s*=\s*\"([^\"]*)\"")
        if (-not $entryMatch.Success) { throw "$languageId is missing required UI text: $englishText" }
        $value = $entryMatch.Groups[1].Value
        if ([string]::IsNullOrWhiteSpace($value)) { throw "$languageId has blank UI text for: $englishText" }
        if ($value -eq $englishText) { throw "$languageId still uses English text for: $englishText" }
    }
}

Write-Host "Interface localization audit passed."
