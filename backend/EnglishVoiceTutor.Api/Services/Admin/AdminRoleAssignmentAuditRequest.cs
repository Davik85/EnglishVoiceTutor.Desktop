namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminRoleAssignmentAuditRequest(
    Guid? ActorAdminUserId,
    Guid TargetAdminUserId,
    string ActionType,
    string? RoleId,
    string? Reason,
    IReadOnlyList<string>? OldRoles,
    IReadOnlyList<string>? NewRoles,
    string Result,
    string? SafeMetadataJson);
