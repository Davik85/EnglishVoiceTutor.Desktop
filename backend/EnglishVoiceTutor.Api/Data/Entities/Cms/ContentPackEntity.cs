namespace EnglishVoiceTutor.Api.Data.Entities.Cms;

public sealed class ContentPackEntity
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? BaseStaticContentVersion { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<CmsLessonTopicEntity> LessonTopics { get; set; } = [];
    public ICollection<CmsLessonScenarioEntity> LessonScenarios { get; set; } = [];
    public ICollection<PromptTemplateEntity> PromptTemplates { get; set; } = [];
    public ICollection<TutorBehaviorProfileEntity> TutorBehaviorProfiles { get; set; } = [];
    public ICollection<ContentVersionEntity> ContentVersions { get; set; } = [];
    public ICollection<ContentAuditLogEntity> AuditLogs { get; set; } = [];
}
