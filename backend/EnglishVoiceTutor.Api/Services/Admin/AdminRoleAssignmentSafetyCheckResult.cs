namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminRoleAssignmentSafetyCheckResult(
    bool IsAllowed,
    string? ErrorCode,
    string? Message,
    IReadOnlyList<string> Violations)
{
    public static AdminRoleAssignmentSafetyCheckResult Allowed() => new(
        IsAllowed: true,
        ErrorCode: null,
        Message: null,
        Violations: Array.Empty<string>());

    public static AdminRoleAssignmentSafetyCheckResult Denied(
        string errorCode,
        string message,
        IReadOnlyList<string> violations) => new(
        IsAllowed: false,
        ErrorCode: errorCode,
        Message: message,
        Violations: violations);
}
