namespace EnglishVoiceTutor.Api.Data.Entities.Cms;

public sealed class ContentVersionEntity
{
    public Guid Id { get; set; }
    public Guid ContentPackId { get; set; }
    public int VersionNumber { get; set; }
    public string SnapshotHash { get; set; } = string.Empty;
    public string PublishStatus { get; set; } = string.Empty;
    public Guid? PublishedByUserId { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public string ValidationSummaryJson { get; set; } = string.Empty;
    public string ChangeSummary { get; set; } = string.Empty;
    public Guid? RestoredFromVersionId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ContentPackEntity ContentPack { get; set; } = null!;
    public ContentVersionEntity? RestoredFromVersion { get; set; }
    public PublishedContentSnapshotEntity? PublishedSnapshot { get; set; }
}
