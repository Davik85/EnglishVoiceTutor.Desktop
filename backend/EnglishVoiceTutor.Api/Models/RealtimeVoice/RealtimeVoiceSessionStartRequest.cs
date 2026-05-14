namespace EnglishVoiceTutor.Api.Models.RealtimeVoice;

public sealed record RealtimeVoiceSessionStartRequest
{
    public string SessionId { get; init; } = string.Empty;
    public string TutorProfileId { get; init; } = string.Empty;
    public string TutorDisplayName { get; init; } = string.Empty;
    public int TutorProfileAge { get; init; }
    public string TutorProfileHomeCity { get; init; } = string.Empty;
    public string TutorProfileCountryOrRegion { get; init; } = string.Empty;
    public string TutorProfileStudies { get; init; } = string.Empty;
    public IReadOnlyList<string> TutorProfileHobbies { get; init; } = [];
    public IReadOnlyList<string> TutorProfileCommunicationStyle { get; init; } = [];
    public Dictionary<string, string> TutorProfileSpeakingRules { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<string> TutorProfileIdentityRules { get; init; } = [];
    public string SelectedLevel { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
    public string TopicTitle { get; init; } = string.Empty;
    public string Subtopic { get; init; } = string.Empty;
    public string SubtopicTitle { get; init; } = string.Empty;
    public string LessonScenarioId { get; init; } = string.Empty;
    public string LessonType { get; init; } = string.Empty;
    public string LessonGoal { get; init; } = string.Empty;
    public string LessonPhase { get; init; } = string.Empty;
    public string CurrentPhase { get; init; } = string.Empty;
    public string TutorRole { get; init; } = string.Empty;
    public string UserRole { get; init; } = string.Empty;
    public string Situation { get; init; } = string.Empty;
    public string TargetLanguageName { get; init; } = "English";
    public string NativeLanguageName { get; init; } = string.Empty;
    public string UserDisplayName { get; init; } = string.Empty;
    public string LearningGoal { get; init; } = string.Empty;
    public string SelectedContextVariantId { get; init; } = string.Empty;
    public string SelectedContextTitle { get; init; } = string.Empty;
    public string SelectedContextOpeningLine { get; init; } = string.Empty;
    public string LastBotMessage { get; init; } = string.Empty;
    public int LearnerTurnCount { get; init; }
    public int SoftLearnerTurnLimit { get; init; }
    public int HardLearnerTurnLimit { get; init; }
    public IReadOnlyList<string> TargetLanguageKeyPhrases { get; init; } = [];
    public IReadOnlyList<string> GrammarFocus { get; init; } = [];

    public string ConversationOpening { get; init; } = string.Empty;

    public string ConversationFirstUserTask { get; init; } = string.Empty;

    public IReadOnlyList<string> ConversationGuidedPracticeFollowUpQuestions { get; init; } = [];

    public string ConversationVariationOrComplication { get; init; } = string.Empty;

    public string ConversationCorrectionMoment { get; init; } = string.Empty;

    public string ConversationWrapUpMessage { get; init; } = string.Empty;

    public string ConversationFinalMessage { get; init; } = string.Empty;
    public string FeedbackRulesSummary { get; init; } = string.Empty;
    public IReadOnlyList<string> AiTutorPromptInstructions { get; init; } = [];
    public RealtimeLevelProfile ActiveLevelProfile { get; init; } = new();
    public IReadOnlyList<RealtimeRecentConversationMessage> RecentMessages { get; init; } = [];
}

public sealed record RealtimeLevelProfile
{
    public string Level { get; init; } = string.Empty;
    public string DifficultyNotes { get; init; } = string.Empty;
    public string TutorLanguageStyle { get; init; } = string.Empty;
    public string ExpectedUserResponse { get; init; } = string.Empty;
    public string FeedbackStrictness { get; init; } = string.Empty;
    public string HintStrategy { get; init; } = string.Empty;
    public string CorrectionPriority { get; init; } = string.Empty;
    public string ConversationDepth { get; init; } = string.Empty;
    public string ExampleGoodAnswer { get; init; } = string.Empty;
    public string ExampleStretchAnswer { get; init; } = string.Empty;
    public IReadOnlyList<string> AddedKeyPhrases { get; init; } = [];
    public IReadOnlyList<string> AddedUsefulConstructions { get; init; } = [];
    public IReadOnlyList<string> AddedGrammarFocus { get; init; } = [];
}

public sealed record RealtimeRecentConversationMessage
{
    public string Sender { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
}
