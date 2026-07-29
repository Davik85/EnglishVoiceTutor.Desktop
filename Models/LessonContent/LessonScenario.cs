namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class LessonScenario
{
    public string Id { get; set; } = string.Empty;

    public LessonMetadata Metadata { get; set; } = new();

    public LessonSetup LessonSetup { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, LessonSetupLocalization>? SetupLocalizations { get; set; }

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

    public List<TutorRuntimeMetadata> TutorProfiles { get; set; } = [];

    public Dictionary<string, string> PromptTemplates { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public RuntimeContentDiagnostics RuntimeContent { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public LocalizedLessonSetup? LocalizedSetup { get; set; }
}

public sealed class RuntimeContentDiagnostics
{
    public string Source { get; set; } = string.Empty;
    public string EffectiveSource { get; set; } = string.Empty;
    public string EffectiveRuntimeSource { get; set; } = string.Empty;
    public string ContentPackSlug { get; set; } = string.Empty;
    public int? VersionNumber { get; set; }
    public int? PublishedVersionNumber { get; set; }
    public string SnapshotHash { get; set; } = string.Empty;
    public bool FallbackUsed { get; set; }
    public string ScenarioKey { get; set; } = string.Empty;
    public string ResolvedLevelId { get; set; } = string.Empty;
    public int SoftWrapUpAfterUserTurn { get; set; }
    public int FinalMessageAtUserTurn { get; set; }
    public string LessonPhase { get; set; } = string.Empty;
    public bool HasWrapUpStarted { get; set; }
}
