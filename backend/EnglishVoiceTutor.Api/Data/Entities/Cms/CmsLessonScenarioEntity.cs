namespace EnglishVoiceTutor.Api.Data.Entities.Cms;

public sealed class CmsLessonScenarioEntity
{
    public Guid Id { get; set; }
    public Guid ContentPackId { get; set; }
    public Guid TopicId { get; set; }
    public string StableScenarioKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LessonType { get; set; } = string.Empty;
    public string SupportedLevelIdsJson { get; set; } = string.Empty;
    public string SetupMessage { get; set; } = string.Empty;
    public string ContextSelectionJson { get; set; } = string.Empty;
    public string LearningGoalJson { get; set; } = string.Empty;
    public string SituationJson { get; set; } = string.Empty;
    public string RolesJson { get; set; } = string.Empty;
    public string TargetLanguageJson { get; set; } = string.Empty;
    public string LevelProfilesJson { get; set; } = string.Empty;
    public string ConversationFlowJson { get; set; } = string.Empty;
    public string RoleplayBeatsJson { get; set; } = string.Empty;
    public string ReciprocalQuestionHandlingJson { get; set; } = string.Empty;
    public string ExpectedScenarioProgressionJson { get; set; } = string.Empty;
    public string ControlledVariationJson { get; set; } = string.Empty;
    public string OffTopicHandlingJson { get; set; } = string.Empty;
    public string FeedbackRulesJson { get; set; } = string.Empty;
    public string HintRulesJson { get; set; } = string.Empty;
    public string RepetitionLogicJson { get; set; } = string.Empty;
    public string AiTutorPromptInstructionsJson { get; set; } = string.Empty;
    public string? DefinitionJson { get; set; }
    public int? SoftWrapUpAfterUserTurn { get; set; }
    public int? FinalMessageAtUserTurn { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ContentPackEntity ContentPack { get; set; } = null!;
    public CmsLessonTopicEntity Topic { get; set; } = null!;
}
