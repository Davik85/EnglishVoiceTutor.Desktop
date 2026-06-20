namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class AdminUserRoleEntity
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public DateTimeOffset AssignedAtUtc { get; set; }
    public Guid AssignedByAdminUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? RevokedByAdminUserId { get; set; }
    public string? RevokeReason { get; set; }

    public AdminUserEntity AdminUser { get; set; } = null!;
    public AdminUserEntity AssignedByAdminUser { get; set; } = null!;
    public AdminUserEntity? RevokedByAdminUser { get; set; }
}
