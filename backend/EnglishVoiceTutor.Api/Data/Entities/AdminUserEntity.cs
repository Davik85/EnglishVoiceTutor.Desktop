namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class AdminUserEntity
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? NormalizedEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? DisabledAtUtc { get; set; }
    public Guid? CreatedByAdminUserId { get; set; }

    public UserEntity? User { get; set; }
    public AdminUserEntity? CreatedByAdminUser { get; set; }
    public ICollection<AdminUserEntity> CreatedAdminUsers { get; set; } = [];
    public ICollection<AdminUserRoleEntity> RoleAssignments { get; set; } = [];
    public ICollection<AdminUserRoleEntity> RoleAssignmentsCreated { get; set; } = [];
    public ICollection<AdminUserRoleEntity> RoleAssignmentsRevoked { get; set; } = [];
    public ICollection<AdminRoleAssignmentEventEntity> ActorEvents { get; set; } = [];
    public ICollection<AdminRoleAssignmentEventEntity> TargetEvents { get; set; } = [];
}
