namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminRoleAssignmentProvisionAdminUserResponse
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public Guid? AdminUserId { get; set; }
    public Guid? AuditEventId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}
