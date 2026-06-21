namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminRoleAssignmentBootstrapRequest(
    Guid AppUserId,
    string? NormalizedEmail,
    string ActorReason,
    string? SafeMetadataJson);
