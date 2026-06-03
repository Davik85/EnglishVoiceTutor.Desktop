namespace EnglishVoiceTutor.Api.Data.Entities.Cms;

public sealed class TutorBehaviorProfileEntity
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

    public ContentPackEntity ContentPack { get; set; } = null!;
}
