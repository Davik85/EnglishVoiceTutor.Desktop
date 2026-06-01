param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"

$expectedLanguages = @("en", "es", "fr", "de", "it", "pt", "ru", "pl", "ar", "ja", "ko", "sr", "hr", "bg")
$expectedStudyIds = @("en", "fr", "de", "pt", "es", "it")
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
    "Subscription status: unavailable",
    "Backend is unavailable. Please start the local backend and try again.",
    "Based on completed lessons on this device.",
    "Total completed lessons",
    "Lessons completed today",
    "Current streak",
    "Last completed lesson",
    "No completed lessons yet.",
    "Sign in to view your account status.",
    "Not signed in"
)
$dailyLifeSubtopics = @(
    "Introductions",
    "Small talk with a neighbor",
    "Asking for help",
    "Making plans",
    "Talking about your day"
)
$spanishFallbackTexts = @(
    "Presentaciones",
    "Preséntate y haz preguntas personales básicas.",
    "Hablar con un vecino",
    "Ten una conversación breve y amable cerca de casa.",
    "Pedir ayuda",
    "Pide ayuda en una situación cotidiana sencilla.",
    "Hacer planes",
    "Planifica una actividad y acuerda hora y lugar.",
    "Hablar de tu día",
    "Describe tu día y tu rutina diaria."
)

$appLocalizationPath = Join-Path $RepoRoot "Localization/AppLocalization.cs"
$subtopicsLocalizationPath = Join-Path $RepoRoot "Localization/SubtopicsLocalization.cs"
$subtopicsViewModelPath = Join-Path $RepoRoot "ViewModels/SubtopicsViewModel.cs"
$interfaceOptionsPath = Join-Path $RepoRoot "Models/InterfaceLanguageOptions.cs"
$studyLanguagePath = Join-Path $RepoRoot "Content/StudyLanguages/study_languages.json"

$appLocalization = Get-Content -Raw -Encoding UTF8 $appLocalizationPath
$subtopicsLocalization = Get-Content -Raw -Encoding UTF8 $subtopicsLocalizationPath
$subtopicsViewModel = Get-Content -Raw -Encoding UTF8 $subtopicsViewModelPath
$interfaceOptions = Get-Content -Raw -Encoding UTF8 $interfaceOptionsPath
$studyLanguages = Get-Content -Raw -Encoding UTF8 $studyLanguagePath | ConvertFrom-Json

$settingsLocalizationPath = Join-Path $RepoRoot "Localization/SettingsLocalizedText.cs"
$settingsViewModelPath = Join-Path $RepoRoot "ViewModels/SettingsViewModel.cs"
$settingsViewPath = Join-Path $RepoRoot "Views/SettingsView.xaml"
$settingsLocalization = Get-Content -Raw -Encoding UTF8 $settingsLocalizationPath
$settingsViewModel = Get-Content -Raw -Encoding UTF8 $settingsViewModelPath
$settingsView = Get-Content -Raw -Encoding UTF8 $settingsViewPath

$releaseReadyMatch = [regex]::Match($interfaceOptions, 'ReleaseReadyInterfaceLanguageIds\s*=\s*\[(.*?)\];', [System.Text.RegularExpressions.RegexOptions]::Singleline)
if (-not $releaseReadyMatch.Success) { throw "ReleaseReadyInterfaceLanguageIds list is missing." }
$constants = @{}
[regex]::Matches($interfaceOptions, 'public const string (\w+) = "([^"]+)"') | ForEach-Object { $constants[$_.Groups[1].Value] = $_.Groups[2].Value }
$actualLanguages = @()
[regex]::Matches($releaseReadyMatch.Groups[1].Value, '"([^"]+)"|(\w+Id)') | ForEach-Object {
    if ($_.Groups[1].Success) { $actualLanguages += $_.Groups[1].Value } else { $actualLanguages += $constants[$_.Groups[2].Value] }
}
if (($actualLanguages -join ',') -ne ($expectedLanguages -join ',')) { throw "Unexpected interface language IDs: $($actualLanguages -join ', ')" }

$studyIds = @($studyLanguages | ForEach-Object { $_.id })
if (($studyIds -join ',') -ne ($expectedStudyIds -join ',')) { throw "Unexpected study language IDs: $($studyIds -join ', ')" }

foreach ($languageId in $expectedLanguages) {
    if ($languageId -eq "en") { continue }

    $languageBlockPattern = ('\["{0}"\]\s*=\s*new Dictionary<string, string>\(StringComparer\.OrdinalIgnoreCase\)\s*\{{(.*?)\n\s*\}}' -f [regex]::Escape($languageId))
    $languageBlockMatch = [regex]::Match($appLocalization, $languageBlockPattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $languageBlockMatch.Success) { throw "$languageId is missing learner UI localization coverage." }

    $languageBlock = $languageBlockMatch.Groups[1].Value
    foreach ($englishText in $requiredTexts) {
        $entryPattern = ('\["{0}"\]\s*=\s*"([^"]*)"' -f [regex]::Escape($englishText))
        $entryMatch = [regex]::Match($languageBlock, $entryPattern)
        if (-not $entryMatch.Success) { throw "$languageId is missing required UI text: $englishText" }
        $value = $entryMatch.Groups[1].Value
        if ([string]::IsNullOrWhiteSpace($value)) { throw "$languageId has blank UI text for: $englishText" }
        if ($value -eq $englishText) { throw "$languageId still uses English text for: $englishText" }
    }
}


foreach ($requiredSettingsMember in @(
    "LearningStatisticsTitle",
    "LearningStatisticsSubtitle",
    "TotalCompletedLessonsLabel",
    "LessonsTodayLabel",
    "CurrentStreakLabel",
    "LastCompletedLessonLabel",
    "NoCompletedLessonsText",
    "SaveButtonText",
    "BackButtonText",
    "AccountTitle",
    "AccountSubtitle",
    "CurrentAccountLabel",
    "SubscriptionStatusTitle")) {
    if ($settingsLocalization -notmatch [regex]::Escape($requiredSettingsMember)) { throw "SettingsLocalizedText is missing $requiredSettingsMember." }
    if ($settingsViewModel -notmatch [regex]::Escape($requiredSettingsMember)) { throw "SettingsViewModel is missing $requiredSettingsMember." }
}
if ($settingsViewModel -notmatch 'LocalizeUiText\(SignedOutSubscriptionPromptText\)') { throw "Signed-out account status must use interface localization." }
if ($settingsView -notmatch 'MinWidth="132"[\s\S]*Content="\{Binding SaveButtonText\}"') { throw "Settings Save button must have enough width for long localized text." }
$ruBlockMatch = [regex]::Match($appLocalization, '\["ru"\]\s*=\s*new Dictionary<string, string>\(StringComparer\.OrdinalIgnoreCase\)\s*\{(.*?)
\s*\}', [System.Text.RegularExpressions.RegexOptions]::Singleline)
if (-not $ruBlockMatch.Success) { throw "ru is missing learner UI localization coverage." }
$ruBlock = $ruBlockMatch.Groups[1].Value
if ($ruBlock -match 'Basado en lecciones completadas en este dispositivo') { throw "Russian Progress helper contains Spanish fallback." }
$ruAccountStatusText = "Sign in to view your account status."
$ruAccountStatusEntryPattern = ('\["{0}"\]\s*=\s*"([^"]*)"' -f [regex]::Escape($ruAccountStatusText))
$ruAccountStatusEntryMatch = [regex]::Match($ruBlock, $ruAccountStatusEntryPattern)
if (-not $ruAccountStatusEntryMatch.Success) { throw "Russian Account status is missing." }
if ($ruAccountStatusEntryMatch.Groups[1].Value -eq $ruAccountStatusText) { throw "Russian Account status contains English fallback." }
foreach ($progressText in @("Based on completed lessons on this device.", "Total completed lessons", "Lessons completed today", "Current streak", "Last completed lesson", "No completed lessons yet.")) {
    $entryPattern = ('\["{0}"\]\s*=\s*"([^"]*)"' -f [regex]::Escape($progressText))
    $entryMatch = [regex]::Match($ruBlock, $entryPattern)
    if (-not $entryMatch.Success) { throw "Russian Progress text is missing: $progressText" }
    if ($entryMatch.Groups[1].Value -eq $progressText) { throw "Russian Progress text uses English fallback: $progressText" }
}

if ($appLocalization -notmatch 'return InterfaceLanguageOptions\.GetById\(languageId\)\.Id;') { throw "Interface normalization must use InterfaceLanguageOptions." }
if ($appLocalization -notmatch 'TextByLanguageId\.Value\[InterfaceLanguageOptions\.EnglishId\]') { throw "English fallback is missing." }
if ($appLocalization -notmatch 'SubtopicsLocalization\.GetTitleTemplate\(languageId\)') { throw "Subtopics title template is not wired." }
if ($appLocalization -notmatch 'SubtopicsLocalization\.GetSubtitle\(languageId\)') { throw "Subtopics subtitle is not wired." }
if ($subtopicsViewModel -notmatch 'SubtopicsLocalization\.GetFreeConversationTitle\(localizedText\.LanguageId\)') { throw "Free Conversation title is not wired." }
if ($subtopicsViewModel -notmatch 'SubtopicsLocalization\.GetFreeConversationSubtitle\(localizedText\.LanguageId\)') { throw "Free Conversation subtitle is not wired." }
foreach ($memberName in @("StartLessonButtonText", "BackButtonText", "CurrentLevelText", "TopicText")) {
    if ($subtopicsViewModel -notmatch [regex]::Escape($memberName)) { throw "SubtopicsViewModel is missing $memberName." }
}

$screenTextMatches = [regex]::Matches($subtopicsLocalization, '\["([^"]+)"\]\s*=\s*new\("([^"]*)",\s*"([^"]*)",\s*"([^"]*)",\s*"([^"]*)"\)')
$screenTextByLanguage = @{}
foreach ($match in $screenTextMatches) {
    $screenTextByLanguage[$match.Groups[1].Value] = @($match.Groups[2].Value, $match.Groups[3].Value, $match.Groups[4].Value, $match.Groups[5].Value)
}
foreach ($languageId in $expectedLanguages) {
    if (-not $screenTextByLanguage.ContainsKey($languageId)) { throw "$languageId is missing Subtopics screen text." }
    $titleTemplate = $screenTextByLanguage[$languageId][0]
    $subtitle = $screenTextByLanguage[$languageId][1]
    foreach ($value in $screenTextByLanguage[$languageId]) {
        if ([string]::IsNullOrWhiteSpace($value)) { throw "$languageId has blank Subtopics screen text." }
    }
    if (-not $titleTemplate.Contains("{0}")) { throw "$languageId title template must preserve {0}." }
    if ($languageId -ne "en" -and $titleTemplate.ToLowerInvariant().Contains(" for ")) { throw "$languageId title template contains hardcoded English 'for': $titleTemplate" }
    if ($languageId -ne "en" -and $subtitle -eq "Choose a realistic situation for your short speaking lesson.") { throw "$languageId subtitle uses English fallback." }
    if (($languageId -ne "en") -and ($languageId -ne "es") -and ($subtitle -eq "Elige una situación realista para tu lección oral corta.")) { throw "$languageId subtitle uses Spanish fallback." }
}

foreach ($languageId in $expectedLanguages) {
    $mapStartPattern = ('\["{0}"\]\s*=\s*Map\(' -f [regex]::Escape($languageId))
    $mapStartMatch = [regex]::Match($subtopicsLocalization, $mapStartPattern)
    if (-not $mapStartMatch.Success) { throw "$languageId is missing Subtopics/Situations card localization." }
    $remaining = $subtopicsLocalization.Substring($mapStartMatch.Index + $mapStartMatch.Length)
    $nextMapMatch = [regex]::Match($remaining, '\n\s*\["[^"]+"\]\s*=\s*Map\(')
    if ($nextMapMatch.Success) { $block = $remaining.Substring(0, $nextMapMatch.Index) } else { $block = $remaining.Substring(0, $remaining.IndexOf("`n        };")) }
    $entries = [regex]::Matches($block, '\("([^"]+)",\s*"([^"]*)",\s*"([^"]*)"\)')
    if ($entries.Count -ne 26) { throw "$languageId must localize all 26 visible Subtopics/Situations; found $($entries.Count)." }
    foreach ($subtopicKey in $dailyLifeSubtopics) {
        $entryPattern = ('\("{0}",\s*"([^"]*)",\s*"([^"]*)"\)' -f [regex]::Escape($subtopicKey))
        $entryMatch = [regex]::Match($block, $entryPattern)
        if (-not $entryMatch.Success) { throw "$languageId is missing Daily Life subtopic: $subtopicKey" }
        $title = $entryMatch.Groups[1].Value
        $description = $entryMatch.Groups[2].Value
        if ([string]::IsNullOrWhiteSpace($title) -or [string]::IsNullOrWhiteSpace($description)) { throw "$languageId has blank Daily Life subtopic text: $subtopicKey" }
        if (($languageId -ne "en") -and ($languageId -ne "es")) {
            if ($spanishFallbackTexts -contains $title) { throw "$languageId title for $subtopicKey uses Spanish fallback: $title" }
            if ($spanishFallbackTexts -contains $description) { throw "$languageId description for $subtopicKey uses Spanish fallback: $description" }
        }
    }
}

Write-Host "Interface localization audit passed."
