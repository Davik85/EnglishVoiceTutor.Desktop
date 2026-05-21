namespace EnglishVoiceTutor.Api.Constants;

public static class UsageConstants
{
    public const string UnknownStudyLanguage = "unknown";

    public static class Operations
    {
        public const string LessonChatReply = "lesson_chat_reply";
        public const string LessonChatHint = "lesson_chat_hint";
        public const string LessonChatFeedback = "lesson_chat_feedback";
        public const string LessonSummary = "lesson_summary";
        public const string Translation = "translation";
        public const string AudioTranscription = "audio_transcription";
        public const string Tts = "tts";
    }

    public static class Statuses
    {
        public const string Success = "success";
        public const string Failed = "failed";
        public const string Skipped = "skipped";
    }
}
