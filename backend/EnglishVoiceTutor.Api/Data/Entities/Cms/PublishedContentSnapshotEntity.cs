namespace EnglishVoiceTutor.Api.Data.Entities.Cms;

public sealed class PublishedContentSnapshotEntity
{
    public Guid Id { get; set; }
    public Guid ContentVersionId { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string SnapshotHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ContentVersionEntity ContentVersion { get; set; } = null!;
}
