namespace EnglishVoiceTutor.Desktop.Constants;

public static class BackendConstants
{
    public const string DefaultBackendBaseUrl = "http://localhost:5000";
    public const string LessonChatReplyEndpoint = "/api/lesson-chat/reply";
    public const string MockLessonChatEndpoint = "/api/lesson-chat/mock-reply";
    public const string HealthEndpoint = "/health";
    public const string BackendConfigStatusEndpoint = "/api/backend/config-status";
    public const int BackendRequestTimeoutSeconds = 30;

    public const string BackendUnavailableMessage = "Backend is unavailable. Please start the local backend and try again.";
    public const string BackendInvalidResponseMessage = "Backend returned an invalid response.";
    public const string BackendStatusChecking = "Backend: checking...";
    public const string BackendStatusConnected = "Backend: connected";
    public const string BackendStatusUnavailable = "Backend: unavailable";
    public const string BackendHealthCheckFailedMessage = "Backend health check failed. Please start the local backend.";

    public const string AiStatusChecking = "AI: checking...";
    public const string AiStatusConfiguredPrefix = "AI: configured";
    public const string AiStatusNotConfigured = "AI: not configured";
    public const string AiStatusUnavailable = "AI: unavailable";
    public const string OpenAiConfiguredStatus = "configured";

    public const string BotStatusReady = "Ready";
    public const string BotStatusThinking = "Thinking";
}
