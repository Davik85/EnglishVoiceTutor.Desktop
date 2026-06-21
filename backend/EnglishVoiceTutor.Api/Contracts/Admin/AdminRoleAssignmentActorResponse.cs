namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminRoleAssignmentActorResponse
{
    public bool IsActorMappingFound { get; set; }
    public Guid? ActorAdminUserId { get; set; }
    public IReadOnlyList<string> RoleIds { get; set; } = [];
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset GeneratedAtUtc { get; set; }
}
