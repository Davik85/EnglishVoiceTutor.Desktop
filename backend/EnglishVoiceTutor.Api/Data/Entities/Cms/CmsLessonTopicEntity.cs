namespace EnglishVoiceTutor.Api.Data.Entities.Cms;

public sealed class CmsLessonTopicEntity
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

    public ContentPackEntity ContentPack { get; set; } = null!;
    public ICollection<CmsLessonScenarioEntity> LessonScenarios { get; set; } = [];
}
