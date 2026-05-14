namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class LessonScenario
{
    public string Id { get; set; } = string.Empty;

    public LessonMetadata Metadata { get; set; } = new();

    public LessonSetup LessonSetup { get; set; } = new();

    public LearningGoal LearningGoal { get; set; } = new();

    public LessonSituation Situation { get; set; } = new();

    public LessonRoles Roles { get; set; } = new();

    public TargetLanguage TargetLanguage { get; set; } = new();

    public Dictionary<string, LevelProfile> LevelProfiles { get; set; } = new();

    public ConversationFlow ConversationFlow { get; set; } = new();

    public List<RoleplayBeat> RoleplayBeats { get; set; } = [];

    public ReciprocalQuestionHandling ReciprocalQuestionHandling { get; set; } = new();

    public List<string> ExpectedScenarioProgression { get; set; } = [];

    public ControlledVariation ControlledVariation { get; set; } = new();

    public OffTopicHandling OffTopicHandling { get; set; } = new();

    public FeedbackRules FeedbackRules { get; set; } = new();

    public HintRules HintRules { get; set; } = new();

    public RepetitionLogic RepetitionLogic { get; set; } = new();

    public List<string> AiTutorPromptInstructions { get; set; } = [];
}
