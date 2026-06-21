namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminRoleAssignmentAdminUserProvisioningRequest(
    Guid ActorAdminUserId,
    IReadOnlyList<string> ActorRoleIds,
    Guid TargetAppUserId,
    string? TargetNormalizedEmail,
    string Reason,
    string? SafeMetadataJson);
