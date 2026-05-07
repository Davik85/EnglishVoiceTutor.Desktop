namespace EnglishVoiceTutor.Api.Constants;

public static class ApiConstants
{
    public const string ServiceName = "EnglishVoiceTutor.Api";
    public const string HealthOkStatus = "ok";
    public const string MockBotReplyText = "Good! I understood your answer. In the next step, AI will give real corrections from the backend.";
    public const string MockHintText = "You can say: Hi, my name is David.";
    public const string EmptyUserMessageError = "User message is required.";
    public const string EmptyAudioFileError = "Audio file is required.";
    public const string EmptyTranslationTextError = "Text is required for translation.";
    public const string EmptyTargetLanguageError = "Target language is required for translation.";
    public const string EmptySpeechTextError = "Text is required for speech generation.";
    public const string AudioTranscriptionFallbackText = "";
    public const string AudioTranscriptionError = "Could not transcribe audio.";
    public const string TranslationError = "Could not translate text.";
    public const string AudioSpeechError = "Could not generate speech audio.";

    public const string HealthRoute = "/health";
    public const string LessonChatReplyRoute = "/api/lesson-chat/reply";
    public const string LessonChatMockReplyRoute = "/api/lesson-chat/mock-reply";
    public const string LessonChatHintRoute = "/api/lesson-chat/hint";
    public const string BackendConfigStatusRoute = "/api/backend/config-status";
    public const string AudioTranscriptionRoute = "/api/audio/transcribe";
    public const string TranslationRoute = "/api/translate";
    public const string AudioSpeechRoute = "/api/audio/speech";
}
