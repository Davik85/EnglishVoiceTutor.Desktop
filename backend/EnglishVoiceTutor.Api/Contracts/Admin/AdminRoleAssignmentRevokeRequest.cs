namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminRoleAssignmentRevokeRequest
{
    public Guid TargetAdminUserId { get; set; }
    public string? RoleId { get; set; }
    public string? Reason { get; set; }
    public string? SafeMetadataJson { get; set; }
}
