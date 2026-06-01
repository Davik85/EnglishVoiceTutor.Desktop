namespace EnglishVoiceTutor.Api.Services;

public sealed class ActiveLessonExistsException : Exception
{
    public const string ErrorCode = "active_lesson_exists";
    public const string UserMessage = "You have not finished a lesson on another device yet. Finish that lesson and try again.";

    public ActiveLessonExistsException(Guid activeSessionId, DateTimeOffset activeSessionStartedAt, DateTimeOffset staleAfterUtc)
        : base(UserMessage)
    {
        ActiveSessionId = activeSessionId;
        ActiveSessionStartedAt = activeSessionStartedAt;
        StaleAfterUtc = staleAfterUtc;
    }

    public Guid ActiveSessionId { get; }
    public DateTimeOffset ActiveSessionStartedAt { get; }
    public DateTimeOffset StaleAfterUtc { get; }
}
