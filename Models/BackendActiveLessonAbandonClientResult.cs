namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendActiveLessonAbandonClientResult(
    bool IsSuccess,
    bool Released,
    Guid? SessionId,
    string Status,
    string? ErrorMessage,
    bool BackendWasReached = false,
    bool IsBackendReachabilityFailure = false)
{
    public static BackendActiveLessonAbandonClientResult Success(BackendActiveLessonAbandonResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new BackendActiveLessonAbandonClientResult(
            IsSuccess: true,
            Released: response.Released,
            SessionId: response.SessionId,
            Status: response.Status,
            ErrorMessage: null,
            BackendWasReached: true);
    }

    public static BackendActiveLessonAbandonClientResult Failure(
        string? errorMessage = null,
        bool backendWasReached = false,
        bool isBackendReachabilityFailure = false)
    {
        return new BackendActiveLessonAbandonClientResult(
            IsSuccess: false,
            Released: false,
            SessionId: null,
            Status: string.Empty,
            ErrorMessage: errorMessage,
            BackendWasReached: backendWasReached,
            IsBackendReachabilityFailure: isBackendReachabilityFailure);
    }
}
