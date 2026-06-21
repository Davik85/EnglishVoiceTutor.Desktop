namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminRoleAssignmentSafetyCheckRequest(
    Guid ActorAdminUserId,
    Guid TargetAdminUserId,
    string? RoleId,
    IReadOnlyList<string> ActorRoleIds,
    string? Reason);
