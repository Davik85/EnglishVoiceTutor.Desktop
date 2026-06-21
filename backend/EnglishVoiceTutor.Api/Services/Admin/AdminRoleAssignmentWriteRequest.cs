namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminRoleAssignmentWriteRequest(
    Guid ActorAdminUserId,
    Guid TargetAdminUserId,
    string? RoleId,
    IReadOnlyList<string> ActorRoleIds,
    string? Reason,
    string? SafeMetadataJson);
