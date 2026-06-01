namespace EnglishVoiceTutor.Api.Services;

public sealed class LessonSessionEndedElsewhereException : Exception
{
    public const string ErrorCode = "lesson_session_ended_elsewhere";
    public const string UserMessage = "This lesson session is no longer active.";

    public LessonSessionEndedElsewhereException(Guid sessionId, string status)
        : base(UserMessage)
    {
        SessionId = sessionId;
        Status = status;
    }

    public Guid SessionId { get; }
    public string Status { get; }
}
