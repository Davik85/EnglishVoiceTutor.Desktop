namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendLessonSessionClientResult(bool IsSuccess, BackendLessonSessionResponse? Value, string? ErrorMessage)
{
    public static BackendLessonSessionClientResult Success(BackendLessonSessionResponse value)
    {
        return new BackendLessonSessionClientResult(true, value, null);
    }

    public static BackendLessonSessionClientResult Failure(string? errorMessage = null)
    {
        return new BackendLessonSessionClientResult(false, null, errorMessage);
    }
}
