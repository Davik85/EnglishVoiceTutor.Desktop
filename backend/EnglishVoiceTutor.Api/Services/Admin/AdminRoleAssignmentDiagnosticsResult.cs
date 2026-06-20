namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminRoleAssignmentDiagnosticsResult(
    int TotalAdminUsers,
    int ActiveAdminUsers,
    int DisabledAdminUsers,
    int PendingInviteAdminUsers,
    int TotalRoleAssignments,
    int ActiveRoleAssignments,
    int RevokedRoleAssignments,
    int TotalRoleAssignmentEvents,
    IReadOnlyList<string> RolesInUse,
    IReadOnlyList<AdminRoleAssignmentDiagnosticsUserResult> AdminUsers,
    DateTimeOffset GeneratedAtUtc);

public sealed record AdminRoleAssignmentDiagnosticsUserResult(
    Guid AdminUserId,
    Guid? LinkedUserId,
    string Status,
    IReadOnlyList<string> RoleIds,
    int ActiveRoleCount,
    DateTimeOffset? DisabledAtUtc,
    DateTimeOffset CreatedAtUtc);
