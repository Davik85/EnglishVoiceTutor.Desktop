namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminRoleAssignmentDisableAdminResponse
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public Guid? AuditEventId { get; set; }
    public Guid TargetAdminUserId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}
