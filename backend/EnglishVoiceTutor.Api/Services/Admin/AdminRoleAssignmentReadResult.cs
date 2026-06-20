namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminRoleAssignmentReadResult(
    Guid? AdminUserId,
    bool IsAdminUserFound,
    bool IsDisabled,
    IReadOnlyList<string> RoleIds)
{
    public static AdminRoleAssignmentReadResult NotFound { get; } = new(
        AdminUserId: null,
        IsAdminUserFound: false,
        IsDisabled: false,
        RoleIds: []);

    public static AdminRoleAssignmentReadResult Disabled(Guid adminUserId) => new(
        AdminUserId: adminUserId,
        IsAdminUserFound: true,
        IsDisabled: true,
        RoleIds: []);
}
