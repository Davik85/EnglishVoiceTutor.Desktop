namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminRoleAssignmentBootstrapFirstOwnerRequest
{
    public string? Reason { get; set; }
    public string? SafeMetadataJson { get; set; }
}
