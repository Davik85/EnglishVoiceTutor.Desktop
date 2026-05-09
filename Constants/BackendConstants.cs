namespace EnglishVoiceTutor.Desktop.Constants;

public static class BackendConstants
{
    public const string DefaultBackendBaseUrl = "http://localhost:5000";
    public const string LessonChatReplyEndpoint = "/api/lesson-chat/reply";
    public const string MockLessonChatEndpoint = "/api/lesson-chat/mock-reply";
    public const string LessonChatHintEndpoint = "/api/lesson-chat/hint";
    public const string AudioTranscriptionEndpoint = "/api/audio/transcribe";
    public const string TranslationEndpoint = "/api/translate";
    public const string AudioSpeechEndpoint = "/api/audio/speech";
    public const string HealthEndpoint = "/health";
    public const string BackendConfigStatusEndpoint = "/api/backend/config-status";
    public const int BackendRequestTimeoutSeconds = 30;
    public const string MultipartFileFieldName = "file";
    public const string WavContentType = "audio/wav";

    public const string BackendUnavailableMessage = "Backend is unavailable. Please start the local backend and try again.";
    public const string BackendInvalidResponseMessage = "Backend returned an invalid response.";
    public const string BackendInvalidTranscriptionResponseMessage = "Backend returned an invalid transcription response.";
    public const string BackendInvalidTranslationResponseMessage = "Backend returned an invalid translation response.";
    public const string BackendInvalidSpeechResponseMessage = "Backend returned an invalid speech response.";
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

    public const string StatusIndicatorReadyBrush = "#FF34A853";
    public const string StatusIndicatorUnavailableBrush = "#FFE05252";
    public const string StatusIndicatorCheckingBrush = "#FF9AA7B5";
}
