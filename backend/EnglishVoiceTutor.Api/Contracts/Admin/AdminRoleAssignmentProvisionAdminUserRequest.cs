namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminRoleAssignmentProvisionAdminUserRequest
{
    public Guid TargetAppUserId { get; set; }
    public string? Reason { get; set; }
    public string? SafeMetadataJson { get; set; }
}
