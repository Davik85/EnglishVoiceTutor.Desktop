namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendLessonSessionClientResult(
    bool IsSuccess,
    BackendLessonSessionResponse? Value,
    string? ErrorMessage,
    bool IsLessonAccessDenied = false,
    bool IsActiveLessonBlocked = false,
    string ActiveLessonMessage = "",
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
            IsActiveLessonBlocked: false,
            ActiveLessonMessage: string.Empty,
            AccessDeniedReason: deniedResponse.Reason,
            AccessDeniedDecision: deniedResponse.Decision,
            EnforcementEnabled: deniedResponse.EnforcementEnabled,
            FreeLessonUsedToday: deniedResponse.FreeLessonUsedToday,
            FreeLessonRemainingToday: deniedResponse.FreeLessonRemainingToday);
    }

    public static BackendLessonSessionClientResult ActiveLessonBlocked(BackendActiveLessonExistsResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new BackendLessonSessionClientResult(
            IsSuccess: false,
            Value: null,
            ErrorMessage: response.Message,
            IsLessonAccessDenied: false,
            IsActiveLessonBlocked: true,
            ActiveLessonMessage: response.Message);
    }
}
