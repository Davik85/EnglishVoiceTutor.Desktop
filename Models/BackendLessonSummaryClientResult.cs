namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendLessonSummaryClientResult
{
    public bool Succeeded { get; init; }
    public BackendLessonSummaryResponse? Summary { get; init; }
    public string? SafeErrorMessage { get; init; }

    public static BackendLessonSummaryClientResult Success(BackendLessonSummaryResponse summary)
    {
        return new BackendLessonSummaryClientResult
        {
            Succeeded = true,
            Summary = summary,
            SafeErrorMessage = null
        };
    }

    public static BackendLessonSummaryClientResult Failure(string safeErrorMessage)
    {
        return new BackendLessonSummaryClientResult
        {
            Succeeded = false,
            Summary = null,
            SafeErrorMessage = safeErrorMessage
        };
    }
}
