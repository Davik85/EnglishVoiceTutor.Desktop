namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminRoleAssignmentDisableAdminRequest
{
    public Guid TargetAdminUserId { get; set; }
    public string? Reason { get; set; }
    public string? SafeMetadataJson { get; set; }
}
