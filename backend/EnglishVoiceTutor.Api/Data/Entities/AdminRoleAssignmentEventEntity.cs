namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class AdminRoleAssignmentEventEntity
{
    public Guid Id { get; set; }
    public Guid? ActorAdminUserId { get; set; }
    public Guid TargetAdminUserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? RoleId { get; set; }
    public string? Reason { get; set; }
    public string? OldRolesJson { get; set; }
    public string? NewRolesJson { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? SafeMetadataJson { get; set; }

    public AdminUserEntity? ActorAdminUser { get; set; }
    public AdminUserEntity TargetAdminUser { get; set; } = null!;
}
