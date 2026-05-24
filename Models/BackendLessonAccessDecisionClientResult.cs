using System.Net;

namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendLessonAccessDecisionClientResult(
    bool IsSuccess,
    BackendLessonAccessDecisionResponse? Value,
    string? ErrorMessage,
    HttpStatusCode? StatusCode)
{
    public static BackendLessonAccessDecisionClientResult Success(BackendLessonAccessDecisionResponse value)
    {
        return new BackendLessonAccessDecisionClientResult(true, value, null, null);
    }

    public static BackendLessonAccessDecisionClientResult Failure(string? errorMessage = null, HttpStatusCode? statusCode = null)
    {
        return new BackendLessonAccessDecisionClientResult(false, null, errorMessage, statusCode);
    }
}
