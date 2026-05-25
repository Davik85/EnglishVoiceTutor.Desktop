namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendLessonSessionClientResult(
    bool IsSuccess,
    BackendLessonSessionResponse? Value,
    string? ErrorMessage,
    bool IsLessonAccessDenied = false,
    string AccessDeniedReason = "",
    string AccessDeniedDecision = "",
    bool EnforcementEnabled = false,
    bool FreeLessonUsedToday = false,
    int FreeLessonRemainingToday = 0)
{
    public static BackendLessonSessionClientResult Success(BackendLessonSessionResponse value)
    {
        return new BackendLessonSessionClientResult(true, value, null);
    }

    public static BackendLessonSessionClientResult Failure(string? errorMessage = null)
    {
        return new BackendLessonSessionClientResult(false, null, errorMessage);
    }

    public static BackendLessonSessionClientResult LessonAccessDenied(BackendLessonAccessDeniedResponse deniedResponse)
    {
        ArgumentNullException.ThrowIfNull(deniedResponse);

        return new BackendLessonSessionClientResult(
            IsSuccess: false,
            Value: null,
            ErrorMessage: null,
            IsLessonAccessDenied: true,
            AccessDeniedReason: deniedResponse.Reason,
            AccessDeniedDecision: deniedResponse.Decision,
            EnforcementEnabled: deniedResponse.EnforcementEnabled,
            FreeLessonUsedToday: deniedResponse.FreeLessonUsedToday,
            FreeLessonRemainingToday: deniedResponse.FreeLessonRemainingToday);
    }
}
