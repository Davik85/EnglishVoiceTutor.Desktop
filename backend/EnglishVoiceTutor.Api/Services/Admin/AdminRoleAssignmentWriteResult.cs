namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminRoleAssignmentWriteResult(
    bool IsSuccess,
    string? ErrorCode,
    string? Message,
    Guid? AuditEventId,
    Guid TargetAdminUserId,
    string? RoleId,
    DateTimeOffset OccurredAtUtc);
