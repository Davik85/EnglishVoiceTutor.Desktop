namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class AdminAuthAuditEventEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public Guid? ActorAdminUserId { get; set; }
    public string? ActorEmail { get; set; }
    public string? AttemptedEmail { get; set; }
    public string? AdminSource { get; set; }
    public string? RoleIdsJson { get; set; }
    public string? FailureReasonCode { get; set; }
    public string? SafeMetadataJson { get; set; }

    public UserEntity? ActorUser { get; set; }
    public AdminUserEntity? ActorAdminUser { get; set; }
}
