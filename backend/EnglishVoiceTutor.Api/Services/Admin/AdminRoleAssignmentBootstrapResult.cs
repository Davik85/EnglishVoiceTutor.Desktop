namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminRoleAssignmentBootstrapResult(
    bool IsSuccess,
    string? ErrorCode,
    string? Message,
    Guid? AdminUserId,
    string? RoleId,
    Guid? AuditEventId,
    DateTimeOffset OccurredAtUtc);
