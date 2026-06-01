namespace EnglishVoiceTutor.Desktop.Services;

public sealed class LessonSessionEndedElsewhereException : Exception
{
    public const string ErrorCode = "lesson_session_ended_elsewhere";

    public LessonSessionEndedElsewhereException()
        : base(ErrorCode)
    {
    }
}
