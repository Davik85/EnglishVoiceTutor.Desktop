namespace EnglishVoiceTutor.Api.Services;

public enum LessonSessionReplyResultStatus
{
    NotImplemented
}

public sealed class LessonSessionReplyResult
{
    private LessonSessionReplyResult(
        LessonSessionReplyResultStatus status,
        LessonSessionReplyUnavailableResponse unavailableResponse)
    {
        Status = status;
        UnavailableResponse = unavailableResponse;
    }

    public LessonSessionReplyResultStatus Status { get; }
    public LessonSessionReplyUnavailableResponse UnavailableResponse { get; }

    public static LessonSessionReplyResult NotImplemented(Guid sessionId)
    {
        return new LessonSessionReplyResult(
            LessonSessionReplyResultStatus.NotImplemented,
            new LessonSessionReplyUnavailableResponse
            {
                SessionId = sessionId
            });
    }
}

public sealed class LessonSessionReplyUnavailableResponse
{
    public const string ErrorCodeValue = "mobile_lesson_reply_not_implemented";
    public const string UserMessage = "Mobile lesson text replies are not available yet. Please continue this lesson in a supported client.";

    public string Error { get; init; } = ErrorCodeValue;
    public string ErrorCode { get; init; } = ErrorCodeValue;
    public string Code { get; init; } = ErrorCodeValue;
    public string Message { get; init; } = UserMessage;
    public Guid SessionId { get; init; }
}
