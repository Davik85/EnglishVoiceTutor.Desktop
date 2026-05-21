namespace EnglishVoiceTutor.Api.Models;

public sealed class LessonChatRequest
{
    public string SelectedLevel { get; init; } = string.Empty;
    public string TopicTitle { get; init; } = string.Empty;
    public string SubtopicTitle { get; init; } = string.Empty;
    public string UserMessage { get; init; } = string.Empty;
    public string LastBotMessage { get; init; } = string.Empty;
    public string NativeLanguageName { get; init; } = string.Empty;

    public string TargetLanguageId { get; init; } = string.Empty;

    public string TargetLanguageName { get; init; } = string.Empty;

    public string TargetLanguageNativeName { get; init; } = string.Empty;

    public string TargetLanguageCode { get; init; } = string.Empty;
    public string TutorAvatarId { get; init; } = string.Empty;
    public string UserDisplayName { get; init; } = string.Empty;
    public string LearningGoal { get; init; } = string.Empty;
    public int LearnerTurnCount { get; init; }
    public int SoftLearnerTurnLimit { get; init; }
    public int HardLearnerTurnLimit { get; init; }
    public int RemainingLearnerTurns { get; init; }
    public bool ShouldStartWrappingUp { get; init; }
    public bool ShouldEndLessonNow { get; init; }
    public IReadOnlyList<RecentConversationMessage> RecentMessages { get; init; } = [];

    public string LessonPhase { get; init; } = string.Empty;

    public string LessonScenarioId { get; init; } = string.Empty;

    public string Level { get; init; } = string.Empty;

    public string Topic { get; init; } = string.Empty;

    public string Subtopic { get; init; } = string.Empty;

    public string LessonGoal { get; init; } = string.Empty;

    public string LessonType { get; init; } = string.Empty;

    public IReadOnlyList<string> AiTutorPromptInstructions { get; init; } = [];

    public string SelectedContextVariantId { get; init; } = string.Empty;

    public string SelectedContextTitle { get; init; } = string.Empty;

    public string SelectedContextLocalizedTitle { get; init; } = string.Empty;

    public string SelectedContextOpeningLine { get; init; } = string.Empty;

    public string SelectedContextConfirmationLine { get; init; } = string.Empty;

    public string SelectedContextOpeningIntent { get; init; } = string.Empty;

    public int UserTurnNumber { get; init; }

    public int SoftWrapUpAfterUserTurn { get; init; }

    public int FinalMessageAtUserTurn { get; init; }

    public IReadOnlyList<string> TargetLanguageKeyPhrases { get; init; } = [];

    public IReadOnlyList<string> GrammarFocus { get; init; } = [];

    public string ConversationOpening { get; init; } = string.Empty;

    public string ConversationFirstUserTask { get; init; } = string.Empty;

    public IReadOnlyList<string> ConversationGuidedPracticeFollowUpQuestions { get; init; } = [];

    public string ConversationVariationOrComplication { get; init; } = string.Empty;

    public string ConversationCorrectionMoment { get; init; } = string.Empty;

    public string ConversationWrapUpMessage { get; init; } = string.Empty;

    public string ConversationFinalMessage { get; init; } = string.Empty;

    public string ConversationWrapUpIntent { get; init; } = string.Empty;

    public string ConversationFinalMessageIntent { get; init; } = string.Empty;

    public IReadOnlyList<ScenarioRoleplayBeat> RoleplayBeats { get; init; } = [];

    public string ReciprocalQuestionIfUserAsksTutorName { get; init; } = string.Empty;

    public string ReciprocalQuestionIfUserAsksSimplePersonalQuestion { get; init; } = string.Empty;

    public bool ReciprocalQuestionMustNotIgnoreUserQuestion { get; init; }

    public bool ReciprocalQuestionMustNotRefuseScenarioCompatibleQuestions { get; init; }

    public IReadOnlyList<string> ExpectedScenarioProgression { get; init; } = [];

    public string FeedbackRulesSummary { get; init; } = string.Empty;

    public string RequestPurpose { get; set; } = string.Empty;

    public int SourceMessageId { get; init; }
    public Guid? SourcePersistedMessageId { get; init; }
    public Guid? BackendSessionId { get; init; }

    public string SourceMessageKind { get; init; } = string.Empty;

    public string TutorProfileId { get; init; } = string.Empty;

    public string ActiveLevelProfileDifficultyNotes { get; init; } = string.Empty;

    public string ActiveLevelProfileTutorLanguageStyle { get; init; } = string.Empty;

    public string ActiveLevelProfileExpectedUserResponse { get; init; } = string.Empty;

    public string ActiveLevelProfileFeedbackStrictness { get; init; } = string.Empty;

    public string ActiveLevelProfileHintStrategy { get; init; } = string.Empty;

    public string ActiveLevelProfileCorrectionPriority { get; init; } = string.Empty;

    public string ActiveLevelProfileConversationDepth { get; init; } = string.Empty;

    public string ActiveLevelProfileExampleGoodAnswer { get; init; } = string.Empty;

    public string ActiveLevelProfileExampleStretchAnswer { get; init; } = string.Empty;

    public IReadOnlyList<string> ActiveLevelProfileAddedKeyPhrases { get; init; } = [];

    public IReadOnlyList<string> ActiveLevelProfileAddedUsefulConstructions { get; init; } = [];

    public IReadOnlyList<string> ActiveLevelProfileAddedGrammarFocus { get; init; } = [];
}

public sealed class ScenarioRoleplayBeat
{
    public string Id { get; init; } = string.Empty;

    public string Intent { get; init; } = string.Empty;
}
