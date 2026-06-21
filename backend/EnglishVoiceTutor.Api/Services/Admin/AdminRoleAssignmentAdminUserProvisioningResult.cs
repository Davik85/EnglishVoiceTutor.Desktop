namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminRoleAssignmentAdminUserProvisioningResult(
    bool IsSuccess,
    string? ErrorCode,
    string? Message,
    Guid? AdminUserId,
    Guid? AuditEventId,
    DateTimeOffset OccurredAtUtc);
