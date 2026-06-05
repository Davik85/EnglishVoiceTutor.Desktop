namespace EnglishVoiceTutor.Api.Contracts.Cms;

public sealed class CmsContentPackSummaryResponse
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? BaseStaticContentVersion { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int TopicCount { get; set; }
    public int ScenarioCount { get; set; }
    public int PromptTemplateCount { get; set; }
    public int TutorBehaviorProfileCount { get; set; }
    public int? CurrentPublishedVersionNumber { get; set; }
    public DateTimeOffset? CurrentPublishedAtUtc { get; set; }
}

public sealed class CmsContentTopicResponse
{
    public Guid Id { get; set; }
    public Guid ContentPackId { get; set; }
    public string StableTopicKey { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int ScenarioCount { get; set; }
}

public sealed class CmsContentScenarioResponse
{
    public Guid Id { get; set; }
    public Guid ContentPackId { get; set; }
    public Guid TopicId { get; set; }
    public string TopicKey { get; set; } = string.Empty;
    public string StableScenarioKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LessonType { get; set; } = string.Empty;
    public string SupportedLevelIdsJson { get; set; } = string.Empty;
    public string SetupMessage { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = string.Empty;
    public bool IsDefinitionJsonFallback { get; set; }
    public int? SoftWrapUpAfterUserTurn { get; set; }
    public int? FinalMessageAtUserTurn { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CmsPromptTemplateResponse
{
    public Guid Id { get; set; }
    public Guid ContentPackId { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string? TargetStudyLanguageId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string AllowedPlaceholdersJson { get; set; } = string.Empty;
    public string RequiredPlaceholdersJson { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CmsTutorBehaviorProfileResponse
{
    public Guid Id { get; set; }
    public Guid ContentPackId { get; set; }
    public string TutorId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CommunicationStyleJson { get; set; } = string.Empty;
    public string SafetyNotesJson { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CmsContentValidationResponse
{
    public bool Success { get; set; }
    public string ContentPackSlug { get; set; } = string.Empty;
    public CmsContentValidationCountsResponse Counts { get; set; } = new();
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public DateTimeOffset CheckedAtUtc { get; set; }
}

public sealed class CmsContentValidationCountsResponse
{
    public int Topics { get; set; }
    public int Scenarios { get; set; }
    public int PromptTemplates { get; set; }
    public int TutorBehaviorProfiles { get; set; }
}

public sealed class CmsContentPreviewResponse
{
    public string ContentPackSlug { get; set; } = string.Empty;
    public string ContentPackName { get; set; } = string.Empty;
    public string ContentPackStatus { get; set; } = string.Empty;
    public int TopicCount { get; set; }
    public int ScenarioCount { get; set; }
    public int PromptTemplateCount { get; set; }
    public int TutorBehaviorProfileCount { get; set; }
    public int? CurrentPublishedVersionNumber { get; set; }
    public List<CmsContentPreviewTopicSummaryResponse> SampleTopics { get; set; } = [];
    public List<CmsContentPreviewScenarioSummaryResponse> SampleScenarios { get; set; } = [];
    public CmsContentValidationResponse Validation { get; set; } = new();
    public DateTimeOffset GeneratedAtUtc { get; set; }
}

public sealed class CmsContentPreviewTopicSummaryResponse
{
    public Guid Id { get; set; }
    public string StableTopicKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CmsContentPreviewScenarioSummaryResponse
{
    public Guid Id { get; set; }
    public string StableScenarioKey { get; set; } = string.Empty;
    public string TopicKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool DefinitionJsonPresent { get; set; }
    public bool DefinitionJsonValid { get; set; }
}

public sealed class CmsContentUpdateResponse
{
    public bool Success { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid ContentPackId { get; set; }
    public List<string> ChangedFields { get; set; } = [];
    public bool NoChanges { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class UpdateCmsTopicRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public string? Reason { get; set; }
}

public sealed class UpdateCmsScenarioRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? SetupMessage { get; set; }
    public string? DefinitionJson { get; set; }
    public bool? IsActive { get; set; }
    public string? Reason { get; set; }
}

public sealed class UpdateCmsPromptTemplateRequest
{
    public string? Body { get; set; }
    public bool? IsActive { get; set; }
    public string? Reason { get; set; }
}

public sealed class UpdateCmsTutorBehaviorProfileRequest
{
    public string? DisplayName { get; set; }
    public string? CommunicationStyleJson { get; set; }
    public string? SafetyNotesJson { get; set; }
    public bool? IsActive { get; set; }
    public string? Reason { get; set; }
}

public sealed class CmsContentAuditEntriesResponse
{
    public List<CmsContentAuditEntryResponse> Entries { get; set; } = [];
}

public sealed class CmsContentAuditEntryResponse
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public Guid? ContentPackId { get; set; }
    public string? ContentPackSlug { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? StableKey { get; set; }
    public string Operation { get; set; } = string.Empty;
    public List<string> ChangedFields { get; set; } = [];
    public string? BeforeHash { get; set; }
    public string? AfterHash { get; set; }
    public string? Reason { get; set; }
    public string? RequestId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
