namespace EnglishVoiceTutor.Desktop.Models;

public sealed class LessonChatBackendRequest
{
    public string SelectedLevel { get; init; } = string.Empty;

    public string TopicTitle { get; init; } = string.Empty;

    public string SubtopicTitle { get; init; } = string.Empty;

    public string UserMessage { get; init; } = string.Empty;

    public string LastBotMessage { get; init; } = string.Empty;

    public string NativeLanguageName { get; init; } = string.Empty;

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

    public string SelectedContextOpeningLine { get; init; } = string.Empty;

    public int UserTurnNumber { get; init; }

    public int SoftWrapUpAfterUserTurn { get; init; }

    public int FinalMessageAtUserTurn { get; init; }

    public IReadOnlyList<string> TargetLanguageKeyPhrases { get; init; } = [];

    public IReadOnlyList<string> GrammarFocus { get; init; } = [];

    public string FeedbackRulesSummary { get; init; } = string.Empty;

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
