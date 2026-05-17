# Audit English Voice Tutor lesson JSON and routing consistency.
# Uses only built-in PowerShell/.NET functionality.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Script:LessonsRoot = Join-Path $Script:RepoRoot 'Content\Lessons'
$Script:Errors = New-Object System.Collections.Generic.List[string]
$Script:Warnings = New-Object System.Collections.Generic.List[string]

$Levels = @(
    'A1 Beginner',
    'A2 Elementary',
    'B1 Intermediate',
    'B2 Upper-Intermediate'
)

$ExpectedRegistry = [ordered]@{
    EverydayEnglish = [ordered]@{
        Topic = 'Daily Life'
        Files = [ordered]@{
            'introductions.json' = 'Introductions'
            'small_talk_with_a_neighbor.json' = 'Small talk with a neighbor'
            'asking_for_help.json' = 'Asking for help'
            'making_plans.json' = 'Making plans'
            'talking_about_your_day.json' = 'Talking about your day'
        }
    }
    Travel = [ordered]@{
        Topic = 'Travel'
        Files = [ordered]@{
            'airport_check_in.json' = 'Airport check-in'
            'hotel_check_in.json' = 'Hotel check-in'
            'asking_for_directions.json' = 'Asking for directions'
            'ordering_transport.json' = 'Ordering transport'
            'lost_luggage.json' = 'Lost luggage'
        }
    }
    WorkAndBusiness = [ordered]@{
        Topic = 'Work & Business'
        Files = [ordered]@{
            'first_meeting.json' = 'First meeting'
            'daily_standup.json' = 'Daily standup'
            'phone_call_with_a_client.json' = 'Phone call with a client'
            'asking_for_clarification.json' = 'Asking for clarification'
            'discussing_deadlines.json' = 'Discussing deadlines'
        }
    }
    JobInterview = [ordered]@{
        Topic = 'Job Interview'
        Files = [ordered]@{
            'tell_me_about_yourself.json' = 'Tell me about yourself'
            'work_experience.json' = 'Work experience'
            'strengths_and_weaknesses.json' = 'Strengths and weaknesses'
            'why_do_you_want_this_job.json' = 'Why do you want this job?'
            'asking_questions_at_the_end.json' = 'Asking questions at the end'
        }
    }
    RestaurantAndCafe = [ordered]@{
        Topic = 'Restaurant & Cafe'
        Files = [ordered]@{
            'booking_a_table.json' = 'Booking a table'
            'ordering_food.json' = 'Ordering food'
            'asking_about_ingredients.json' = 'Asking about ingredients'
            'handling_a_wrong_order.json' = 'Handling a wrong order'
            'paying_the_bill.json' = 'Paying the bill'
        }
    }
    FreeConversation = [ordered]@{
        Topic = 'Free Conversation'
        Files = [ordered]@{
            'open_conversation.json' = 'Open conversation'
        }
    }
}

$TopLevelRequired = @(
    'id',
    'metadata',
    'lessonSetup',
    'learningGoal',
    'situation',
    'roles',
    'targetLanguage',
    'levelProfiles',
    'conversationFlow',
    'controlledVariation',
    'offTopicHandling',
    'feedbackRules',
    'hintRules',
    'repetitionLogic',
    'aiTutorPromptInstructions'
)

$MetadataRequired = @(
    'topic',
    'subtopic',
    'lessonType',
    'supportedLevels',
    'softWrapUpAfterUserTurn',
    'finalMessageAtUserTurn',
    'setupAndContextChoiceCountAsLessonTurns'
)

$LevelProfileRequired = @(
    'level',
    'difficultyNotes',
    'tutorLanguageStyle',
    'expectedUserResponse',
    'minimumUserResponse',
    'stretchUserResponse',
    'addedKeyPhrases',
    'addedUsefulConstructions',
    'addedGrammarFocus',
    'feedbackStrictness',
    'hintStrategy',
    'correctionPriority',
    'conversationDepth',
    'exampleGoodAnswer',
    'exampleStretchAnswer',
    'softWrapUpAfterUserTurn',
    'finalMessageAtUserTurn'
)

$AllowedLessonTypes = @('guided_roleplay', 'free_conversation')
$GenericPhrases = @(
    'Use one short',
    'clear subject and verb',
    'simple word order',
    'Make the request, then add one specific detail',
    'Answer the staff question directly',
    'Let us'
)
$FailGenericPhrases = @('Let us')
$ObsoleteLevelFolders = @('A1', 'A2', 'B1', 'B2')
$CyrillicRegexPattern = '[\u0400-\u04FF]'

function Get-RelativePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $root = [System.IO.Path]::GetFullPath($Script:RepoRoot)
    if (-not $root.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $root += [System.IO.Path]::DirectorySeparatorChar
    }

    if ($fullPath.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($root.Length).Replace([System.IO.Path]::DirectorySeparatorChar, '/')
    }

    return $fullPath.Replace([System.IO.Path]::DirectorySeparatorChar, '/')
}

function Add-AuditError {
    param([Parameter(Mandatory = $true)][string]$Message)
    $Script:Errors.Add($Message)
}

function Add-AuditWarning {
    param([Parameter(Mandatory = $true)][string]$Message)
    $Script:Warnings.Add($Message)
}

function Get-JsonProperty {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $Object -or $null -eq $Object.PSObject) {
        return $null
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Test-JsonProperty {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name
    )

    return $null -ne $Object -and $null -ne $Object.PSObject -and $null -ne $Object.PSObject.Properties[$Name]
}

function Test-ArrayExactlyMatches {
    param(
        [Parameter(Mandatory = $true)]$Actual,
        [Parameter(Mandatory = $true)][string[]]$Expected
    )

    if ($null -eq $Actual) {
        return $false
    }

    $actualArray = @($Actual)
    if ($actualArray.Count -ne $Expected.Count) {
        return $false
    }

    for ($index = 0; $index -lt $Expected.Count; $index++) {
        if ([string]$actualArray[$index] -ne $Expected[$index]) {
            return $false
        }
    }

    return $true
}

function Get-ExpectedLimits {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$LessonType,
        [Parameter(Mandatory = $true)][string]$Level
    )

    if ($LessonType -eq 'free_conversation') {
        return @{ Soft = 25; Final = 30 }
    }

    if ($Level -eq 'A1 Beginner' -or $Level -eq 'A2 Elementary') {
        return @{ Soft = 10; Final = 15 }
    }

    return @{ Soft = 20; Final = 25 }
}

function Read-LessonJsonFiles {
    $parsed = [ordered]@{}

    if (-not (Test-Path -LiteralPath $Script:LessonsRoot -PathType Container)) {
        Add-AuditError "Missing lessons root: $(Get-RelativePath $Script:LessonsRoot)"
        return $parsed
    }

    $files = Get-ChildItem -LiteralPath $Script:LessonsRoot -Recurse -File -Filter '*.json' | Sort-Object FullName
    foreach ($file in $files) {
        try {
            $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
            $parsed[$file.FullName] = $text | ConvertFrom-Json
        }
        catch {
            Add-AuditError "Invalid JSON in $(Get-RelativePath $file.FullName): $($_.Exception.Message)"
        }
    }

    return $parsed
}

function Test-FoldersAndRegistry {
    param([Parameter(Mandatory = $true)]$Parsed)

    foreach ($folderName in $ExpectedRegistry.Keys) {
        $registry = $ExpectedRegistry[$folderName]
        $folder = Join-Path $Script:LessonsRoot $folderName
        if (-not (Test-Path -LiteralPath $folder -PathType Container)) {
            Add-AuditError "Expected topic folder is missing: $(Get-RelativePath $folder)"
            continue
        }

        $expectedFiles = @($registry.Files.Keys)
        $actualFiles = @(Get-ChildItem -LiteralPath $folder -File -Filter '*.json' | ForEach-Object { $_.Name })

        foreach ($expectedFile in $expectedFiles) {
            if ($actualFiles -notcontains $expectedFile) {
                Add-AuditError "Expected lesson file is missing: $(Get-RelativePath (Join-Path $folder $expectedFile))"
            }
        }

        foreach ($actualFile in $actualFiles) {
            if ($expectedFiles -notcontains $actualFile) {
                Add-AuditError "Unexpected lesson JSON under known topic folder: $(Get-RelativePath (Join-Path $folder $actualFile))"
            }
        }
    }

    if (Test-Path -LiteralPath $Script:LessonsRoot -PathType Container) {
        $knownFolders = @($ExpectedRegistry.Keys)
        $folders = Get-ChildItem -LiteralPath $Script:LessonsRoot -Directory | Sort-Object FullName
        foreach ($folder in $folders) {
            if ($knownFolders -notcontains $folder.Name -and $ObsoleteLevelFolders -notcontains $folder.Name) {
                Add-AuditWarning "Unregistered topic folder found under Content/Lessons: $(Get-RelativePath $folder.FullName)"
            }
        }
    }

    foreach ($path in $Parsed.Keys) {
        $file = Get-Item -LiteralPath $path
        $folderName = $file.Directory.Name
        if (-not $ExpectedRegistry.Contains($folderName)) {
            continue
        }

        $registry = $ExpectedRegistry[$folderName]
        if (-not $registry.Files.Contains($file.Name)) {
            continue
        }

        $metadata = Get-JsonProperty $Parsed[$path] 'metadata'
        $expectedTopic = $registry.Topic
        $expectedSubtopic = $registry.Files[$file.Name]
        $actualTopic = Get-JsonProperty $metadata 'topic'
        $actualSubtopic = Get-JsonProperty $metadata 'subtopic'

        if ($actualTopic -ne $expectedTopic) {
            Add-AuditError "$(Get-RelativePath $path) metadata.topic is '$actualTopic'; expected '$expectedTopic'"
        }

        if ($actualSubtopic -ne $expectedSubtopic) {
            Add-AuditError "$(Get-RelativePath $path) metadata.subtopic is '$actualSubtopic'; expected '$expectedSubtopic'"
        }
    }
}

function Test-ObsoleteFolders {
    foreach ($folderName in $ObsoleteLevelFolders) {
        $folder = Join-Path $Script:LessonsRoot $folderName
        if (Test-Path -LiteralPath $folder -PathType Container) {
            Add-AuditError "Obsolete per-level lesson folder exists: $(Get-RelativePath $folder)"
        }
    }
}

function Test-TextContent {
    if (-not (Test-Path -LiteralPath $Script:LessonsRoot -PathType Container)) {
        return
    }

    $files = Get-ChildItem -LiteralPath $Script:LessonsRoot -Recurse -File -Filter '*.json' | Sort-Object FullName
    foreach ($file in $files) {
        $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
        $matches = [regex]::Matches($text, $CyrillicRegexPattern)
        foreach ($match in $matches) {
            $line = 1 + [regex]::Matches($text.Substring(0, $match.Index), "`n").Count
            Add-AuditError ("Cyrillic content found in {0}:{1}: '{2}'" -f (Get-RelativePath $file.FullName), $line, $match.Value)
        }

        $relativePath = Get-RelativePath $file.FullName
        $isFreeConversation = $relativePath.StartsWith('Content/Lessons/FreeConversation/', [System.StringComparison]::OrdinalIgnoreCase)
        foreach ($phrase in $GenericPhrases) {
            if ($text.Contains($phrase)) {
                $message = "Generic/copied phrase found in {0}: '{1}'" -f $relativePath, $phrase
                if ($FailGenericPhrases -contains $phrase -or $isFreeConversation) {
                    Add-AuditError $message
                }
                else {
                    Add-AuditWarning $message
                }
            }
        }
    }
}

function Test-RequiredFields {
    param([Parameter(Mandatory = $true)]$Parsed)

    foreach ($path in $Parsed.Keys) {
        $data = $Parsed[$path]

        foreach ($field in $TopLevelRequired) {
            if (-not (Test-JsonProperty $data $field)) {
                Add-AuditError "$(Get-RelativePath $path) missing top-level field: $field"
            }
        }

        $metadata = Get-JsonProperty $data 'metadata'
        if ($null -eq $metadata) {
            Add-AuditError "$(Get-RelativePath $path) metadata must be an object"
            continue
        }

        foreach ($field in $MetadataRequired) {
            if (-not (Test-JsonProperty $metadata $field)) {
                Add-AuditError "$(Get-RelativePath $path) metadata missing field: $field"
            }
        }

        $lessonType = [string](Get-JsonProperty $metadata 'lessonType')
        if ($AllowedLessonTypes -notcontains $lessonType) {
            Add-AuditError "$(Get-RelativePath $path) metadata.lessonType is '$lessonType'; expected one of $($AllowedLessonTypes -join ', ')"
        }

        $supportedLevels = Get-JsonProperty $metadata 'supportedLevels'
        if (-not (Test-ArrayExactlyMatches $supportedLevels $Levels)) {
            Add-AuditError "$(Get-RelativePath $path) metadata.supportedLevels must exactly match $($Levels -join ', ')"
        }

        Test-LevelProfiles $path $data $lessonType
        Test-LessonTypeSpecificContent $path $data $lessonType
    }
}

function Test-LevelProfiles {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Data,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$LessonType
    )

    $profiles = Get-JsonProperty $Data 'levelProfiles'
    if ($null -eq $profiles) {
        Add-AuditError "$(Get-RelativePath $Path) levelProfiles must be an object"
        return
    }

    foreach ($level in $Levels) {
        $profile = Get-JsonProperty $profiles $level
        if ($null -eq $profile) {
            Add-AuditError "$(Get-RelativePath $Path) missing levelProfiles entry: $level"
            continue
        }

        foreach ($field in $LevelProfileRequired) {
            if (-not (Test-JsonProperty $profile $field)) {
                Add-AuditError "$(Get-RelativePath $Path) levelProfiles.$level missing field: $field"
            }
        }

        $profileLevel = Get-JsonProperty $profile 'level'
        if ($profileLevel -ne $level) {
            Add-AuditError "$(Get-RelativePath $Path) levelProfiles.$level.level is '$profileLevel'; expected '$level'"
        }

        $limits = Get-ExpectedLimits $LessonType $level
        $softLimit = Get-JsonProperty $profile 'softWrapUpAfterUserTurn'
        $finalLimit = Get-JsonProperty $profile 'finalMessageAtUserTurn'

        if ([int]$softLimit -ne [int]$limits.Soft) {
            Add-AuditError "$(Get-RelativePath $Path) levelProfiles.$level.softWrapUpAfterUserTurn is '$softLimit'; expected $($limits.Soft)"
        }

        if ([int]$finalLimit -ne [int]$limits.Final) {
            Add-AuditError "$(Get-RelativePath $Path) levelProfiles.$level.finalMessageAtUserTurn is '$finalLimit'; expected $($limits.Final)"
        }
    }
}

function Test-LessonTypeSpecificContent {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Data,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$LessonType
    )

    $controlledVariation = Get-JsonProperty $Data 'controlledVariation'
    $contextVariants = Get-JsonProperty $controlledVariation 'contextVariants'
    $instructions = @(Get-JsonProperty $Data 'aiTutorPromptInstructions')
    $instructionsText = $instructions -join "`n"

    if ($LessonType -eq 'guided_roleplay') {
        if ($null -eq $contextVariants -or @($contextVariants).Count -eq 0) {
            Add-AuditError "$(Get-RelativePath $Path) guided_roleplay lesson must define controlledVariation.contextVariants"
        }
    }
    elseif ($LessonType -eq 'free_conversation') {
        $requiredSafetyTerms = @(
            'safe',
            'harmful',
            'illegal',
            'self-harm',
            'hateful',
            'sexually explicit',
            'professional medical',
            'redirect'
        )

        $missingTerms = @()
        foreach ($term in $requiredSafetyTerms) {
            if ($instructionsText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
                $missingTerms += $term
            }
        }

        if ($missingTerms.Count -gt 0) {
            Add-AuditError "$(Get-RelativePath $Path) free_conversation aiTutorPromptInstructions missing safety terms: $($missingTerms -join ', ')"
        }
    }
}

function Test-RoutingSources {
    $topicTitles = @($ExpectedRegistry.Values | ForEach-Object { $_.Topic })
    $subtopicTitles = @($ExpectedRegistry.Values | ForEach-Object { $_.Files.Values })
    $folderNames = @($ExpectedRegistry.Keys)
    $fileNames = @($ExpectedRegistry.Values | ForEach-Object { $_.Files.Keys })

    $checks = @(
        @{ Path = 'ViewModels/HomeViewModel.cs'; Needles = $topicTitles },
        @{ Path = 'ViewModels/SubtopicsViewModel.cs'; Needles = $subtopicTitles },
        @{ Path = 'ViewModels/MainViewModel.cs'; Needles = @(
            'ContentConstants.EverydayEnglishFolderName',
            'ContentConstants.TravelFolderName',
            'ContentConstants.WorkAndBusinessFolderName',
            'ContentConstants.JobInterviewFolderName',
            'ContentConstants.RestaurantAndCafeFolderName',
            'ContentConstants.FreeConversationFolderName',
            'ContentConstants.IntroductionsFileName',
            'ContentConstants.SmallTalkWithANeighborFileName',
            'ContentConstants.AskingForHelpFileName',
            'ContentConstants.MakingPlansFileName',
            'ContentConstants.TalkingAboutYourDayFileName',
            'ContentConstants.AirportCheckInFileName',
            'ContentConstants.HotelCheckInFileName',
            'ContentConstants.AskingForDirectionsFileName',
            'ContentConstants.OrderingTransportFileName',
            'ContentConstants.LostLuggageFileName',
            'ContentConstants.FirstMeetingFileName',
            'ContentConstants.DailyStandupFileName',
            'ContentConstants.PhoneCallWithAClientFileName',
            'ContentConstants.WorkAskingForClarificationFileName',
            'ContentConstants.DiscussingDeadlinesFileName',
            'ContentConstants.TellMeAboutYourselfFileName',
            'ContentConstants.WorkExperienceFileName',
            'ContentConstants.StrengthsAndWeaknessesFileName',
            'ContentConstants.WhyDoYouWantThisJobFileName',
            'ContentConstants.AskingQuestionsAtTheEndFileName',
            'ContentConstants.BookingATableFileName',
            'ContentConstants.OrderingFoodFileName',
            'ContentConstants.AskingAboutIngredientsFileName',
            'ContentConstants.HandlingAWrongOrderFileName',
            'ContentConstants.PayingTheBillFileName',
            'ContentConstants.OpenConversationFileName'
        ) },
        @{ Path = 'Constants/ContentConstants.cs'; Needles = @($folderNames + $fileNames) }
    )

    foreach ($check in $checks) {
        $path = Join-Path $Script:RepoRoot $check.Path
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            Add-AuditError "Routing source file is missing: $(Get-RelativePath $path)"
            continue
        }

        $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
        foreach ($needle in $check.Needles) {
            if ($text.IndexOf($needle, [System.StringComparison]::Ordinal) -lt 0) {
                Add-AuditError "Routing source check failed: $(Get-RelativePath $path) does not contain '$needle'"
            }
        }
    }
}

function Write-AuditReport {
    param([Parameter(Mandatory = $true)][int]$ParsedCount)

    Write-Host 'Lesson content audit'
    Write-Host '===================='
    Write-Host "Repository: $Script:RepoRoot"
    Write-Host "Lesson JSON files parsed: $ParsedCount"
    Write-Host ''

    if ($Script:Errors.Count -gt 0) {
        Write-Host 'Errors:'
        foreach ($message in $Script:Errors) {
            Write-Host "  [ERROR] $message"
        }
        Write-Host ''
    }

    if ($Script:Warnings.Count -gt 0) {
        Write-Host 'Warnings:'
        foreach ($message in $Script:Warnings) {
            Write-Host "  [WARN] $message"
        }
        Write-Host ''
    }

    if ($Script:Errors.Count -gt 0) {
        Write-Host "FAILED: $($Script:Errors.Count) error(s), $($Script:Warnings.Count) warning(s)."
    }
    else {
        Write-Host "PASSED: 0 errors, $($Script:Warnings.Count) warning(s)."
    }
}

$parsed = Read-LessonJsonFiles
Test-ObsoleteFolders
Test-FoldersAndRegistry $parsed
Test-TextContent
Test-RequiredFields $parsed
Test-RoutingSources
Write-AuditReport $parsed.Count

if ($Script:Errors.Count -gt 0) {
    exit 1
}

exit 0
