namespace EnglishVoiceTutor.Desktop.Constants;

public static class BackendConstants
{
    public const string DefaultBackendBaseUrl = "http://localhost:5000";
    public const string LessonChatReplyEndpoint = "/api/lesson-chat/reply";
    public const string MockLessonChatEndpoint = "/api/lesson-chat/mock-reply";
    public const string LessonChatHintEndpoint = "/api/lesson-chat/hint";
    public const string LessonChatFeedbackEndpoint = "/api/lesson-chat/feedback";
    public const string AudioTranscriptionEndpoint = "/api/audio/transcribe";
    public const string TranslationEndpoint = "/api/translate";
    public const string AudioSpeechEndpoint = "/api/audio/speech";
    public const string AudioSpeechStreamEndpoint = "/api/audio/speech-stream";
    public const string RealtimeVoiceEndpoint = "/api/realtime-voice";
    public const string HealthEndpoint = "/api/health";
    public const string DatabaseHealthEndpoint = "/api/health/database";
    public const string BackendConfigStatusEndpoint = "/api/backend/config-status";
    public const int BackendRequestTimeoutSeconds = 30;
    public const int BackendHealthTimeoutSeconds = 5;
    public const int BotVoiceRequestTimeoutSeconds = 20;
    public const int BotVoiceFirstAudioTimeoutSeconds = 5;
    public const int BotVoiceSegmentTimeoutSeconds = 15;
    public const int BotVoiceStreamOverallTimeoutSeconds = 20;
    public const string DefaultConversationModeVoiceProvider = "Tts1";
    public const double ConversationModeTtsSpeechSpeed = 1.0;
    public const string MultipartFileFieldName = "file";
    public const string WavContentType = "audio/wav";
    public const string PcmContentType = "audio/pcm";
    public const string SpeechResponseContentType = WavContentType;
    public const string NgrokSkipBrowserWarningHeaderName = "ngrok-skip-browser-warning";
    public const string NgrokSkipBrowserWarningHeaderValue = "1";
    public const string BackendUserAgentProductName = "EnglishVoiceTutor.Desktop";
    public const string BackendUserAgentVersion = "1.0";
    public const string LessonChatModelName = "configured OpenAI Responses model";
    public const string FeedbackModelName = LessonChatModelName;
    public const string SummaryModelName = "desktop summary generator";
    public const string TranscriptionModelName = "gpt-4o-mini-transcribe";
    public const string LessonChatTtsModel = "tts-1";
    public const string ConversationModeTtsModel = "gpt-4o-mini-tts";
    public const string TtsModelName = LessonChatTtsModel;
    public const string RealtimeModelName = "gpt-realtime";
    public const string LessonChatTtsPurpose = "lesson_chat_tts";
    public const string RealtimePreStartOpeningSpeechPurpose = "realtime_pre_start_opening";
    public const string ConversationModeTtsPurpose = "conversation_mode_tts";

    public const string ConversationModeTtsInstructions = "Speak in a calm, friendly target-language tutor voice. Use an even pace and steady volume. Do not shout. Do not rush near the end of sentences. Keep the tone warm, patient, and encouraging. Use natural pauses between sentences. Pronounce clearly for a target-language learner. Speak only in the selected target language unless quoting the learner.";

    public const string BackendUnavailableMessage = "Backend is unavailable. Please start the local backend and try again.";
    public const string BackendInvalidResponseMessage = "Backend returned an invalid response.";
    public const string BackendInvalidTranscriptionResponseMessage = "Backend returned an invalid transcription response.";
    public const string BackendInvalidTranslationResponseMessage = "Backend returned an invalid translation response.";
    public static bool SpeechModelSupportsInstructions(string model)
    {
        return string.Equals(model, ConversationModeTtsModel, StringComparison.Ordinal);
    }

    public const string BackendInvalidSpeechResponseMessage = "Backend returned an invalid speech response.";
    public const string RealtimeUnavailableMessage = "Realtime voice mode is unavailable. Please try text mode.";
    public const string VoicePlaybackUnavailableMessage = "Voice playback is unavailable. You can continue by reading the message.";
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
