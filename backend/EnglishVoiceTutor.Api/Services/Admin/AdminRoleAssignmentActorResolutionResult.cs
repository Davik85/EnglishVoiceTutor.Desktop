namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminRoleAssignmentActorResolutionResult(
    Guid? ActorAdminUserId,
    IReadOnlyList<string> ActorRoleIds,
    bool IsActorMappingFound,
    string? ErrorCode,
    string? Message)
{
    public static AdminRoleAssignmentActorResolutionResult Success(Guid actorAdminUserId, IReadOnlyList<string> actorRoleIds) => new(
        ActorAdminUserId: actorAdminUserId,
        ActorRoleIds: actorRoleIds,
        IsActorMappingFound: true,
        ErrorCode: null,
        Message: null);

    public static AdminRoleAssignmentActorResolutionResult Unavailable(string message) => new(
        ActorAdminUserId: null,
        ActorRoleIds: [],
        IsActorMappingFound: false,
        ErrorCode: AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode,
        Message: message);
}
