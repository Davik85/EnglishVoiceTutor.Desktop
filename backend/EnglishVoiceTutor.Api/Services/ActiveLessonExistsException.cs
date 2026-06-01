namespace EnglishVoiceTutor.Api.Services;

public sealed class ActiveLessonExistsException : Exception
{
    public const string ErrorCode = "active_lesson_exists";
    public const string UserMessage = "You already have an active lesson on another device. To continue here, end the lesson on that device first.";

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
