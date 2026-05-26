using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminFreeLessonAllowanceResetService
{
    Task<AdminFreeLessonAllowanceResetResult> ResetFreeLessonAllowanceAsync(
        Guid adminUserId,
        Guid targetUserId,
        AdminFreeLessonAllowanceResetRequest request,
        CancellationToken cancellationToken);
}

public sealed class AdminFreeLessonAllowanceResetResult
{
    public bool IsInvalid { get; init; }
    public bool IsNotFound { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public AdminFreeLessonAllowanceResetResponse? Response { get; init; }
}
