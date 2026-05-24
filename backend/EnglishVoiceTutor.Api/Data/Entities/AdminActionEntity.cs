namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class AdminActionEntity
{
    public Guid Id { get; set; }
    public Guid? AdminUserId { get; set; }
    public Guid TargetUserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? SafeMetadataJson { get; set; }

    public UserEntity? AdminUser { get; set; }
    public UserEntity TargetUser { get; set; } = null!;
}
