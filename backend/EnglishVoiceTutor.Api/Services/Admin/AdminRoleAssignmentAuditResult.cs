namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminRoleAssignmentAuditResult(
    Guid EventId,
    DateTimeOffset OccurredAtUtc);
