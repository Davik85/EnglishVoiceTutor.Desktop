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
    public const string AudioUploadTimedOutTitle = "Audio upload timed out.";
    public const string AudioUploadTimedOutDetail = "Audio upload timed out. Please try recording again.";
    public const string AudioUploadCanceledTitle = "Audio upload canceled.";
    public const string AudioUploadCanceledDetail = "Audio upload was canceled. Please try recording again.";
    public const string AudioUploadFailedTitle = "Audio upload failed.";
    public const string AudioUploadFailedDetail = "I couldn't process that recording. Please try again.";
    public const string TranslationError = "Could not translate text.";
    public const string AudioSpeechError = "Could not generate speech audio.";
    public const int DefaultLessonSoftLearnerTurnLimit = 10;
    public const int DefaultLessonHardLearnerTurnLimit = 15;
    public const int ExtendedLessonSoftLearnerTurnLimit = 25;
    public const int ExtendedLessonHardLearnerTurnLimit = 30;

    public const string HealthRoute = "/health";
    public const string ApiHealthRoute = "/api/health";
    public const string LessonChatReplyRoute = "/api/lesson-chat/reply";
    public const string LessonChatMockReplyRoute = "/api/lesson-chat/mock-reply";
    public const string LessonChatHintRoute = "/api/lesson-chat/hint";
    public const string BackendConfigStatusRoute = "/api/backend/config-status";
    public const string AudioTranscriptionRoute = "/api/audio/transcribe";
    public const string TranslationRoute = "/api/translate";
    public const string AudioSpeechRoute = "/api/audio/speech";
    public const string AudioSpeechStreamRoute = "/api/audio/speech-stream";
}
