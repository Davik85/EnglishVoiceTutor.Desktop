namespace EnglishVoiceTutor.Desktop.Constants;

public static class BackendConstants
{
    public const string DefaultBackendBaseUrl = "http://localhost:5000";
    public const string LessonChatReplyEndpoint = "/api/lesson-chat/reply";
    public const string MockLessonChatEndpoint = "/api/lesson-chat/mock-reply";
    public const int BackendRequestTimeoutSeconds = 30;

    public const string BackendUnavailableMessage = "Backend is unavailable. Please start the local backend and try again.";
    public const string BackendInvalidResponseMessage = "Backend returned an invalid response.";

    public const string BotStatusReady = "Ready";
    public const string BotStatusThinking = "Thinking";
}
