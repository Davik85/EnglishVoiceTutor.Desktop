namespace EnglishVoiceTutor.Api.Data.Entities.Cms;

public sealed class ContentAuditLogEntity
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid? ContentPackId { get; set; }
    public string? ContentPackSlug { get; set; }
    public string? StableKey { get; set; }
    public string? BeforeHash { get; set; }
    public string? AfterHash { get; set; }
    public string ChangedFieldsJson { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? RequestMetadataJson { get; set; }

    public ContentPackEntity? ContentPack { get; set; }
}
