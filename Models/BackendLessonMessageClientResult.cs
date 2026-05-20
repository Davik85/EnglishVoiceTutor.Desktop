namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendLessonMessageClientResult(bool Succeeded, BackendLessonMessageResponse? Message, string? SafeErrorMessage)
{
    public static BackendLessonMessageClientResult Success(BackendLessonMessageResponse message)
    {
        return new BackendLessonMessageClientResult(true, message, null);
    }

    public static BackendLessonMessageClientResult Failure(string? safeErrorMessage = null)
    {
        return new BackendLessonMessageClientResult(false, null, safeErrorMessage);
    }
}
