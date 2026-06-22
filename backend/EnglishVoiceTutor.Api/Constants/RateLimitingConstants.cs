namespace EnglishVoiceTutor.Api.Constants;

public static class RateLimitingConstants
{
    public const string ErrorCode = "RateLimitExceeded";
    public const string DefaultMessage = "Too many requests. Please wait a moment and try again.";
    public const string LoginMessage = "Too many login attempts. Please wait a few minutes and try again.";
    public const string RegisterMessage = "Too many registration attempts. Please wait and try again.";
    public const string PasswordResetMessage = "Too many password reset requests. Please wait before trying again.";
    public const string LessonChatReplyMessage = "You are sending messages too quickly. Please wait a moment and continue the lesson.";
    public const string AudioTranscriptionMessage = "Too many recordings. Please wait a moment before recording again.";
    public const string AudioTtsMessage = "Voice playback is being requested too quickly. Please wait a moment and try again.";
    public const string TranslationMessage = "Too many translation requests. Please wait a moment and try again.";
    public const string RealtimeVoiceMessage = "Too many voice sessions. Please close another session or wait a moment.";

    public const string RetryAfterHeaderName = "Retry-After";

    public const string AuthLoginPolicyName = "auth-login";
    public const string AuthRegisterPolicyName = "auth-register";
    public const string AuthPasswordResetRequestPolicyName = "auth-password-reset-request";
    public const string AuthPasswordResetConfirmPolicyName = "auth-password-reset-confirm";
    public const string LessonChatReplyPolicyName = "lesson-chat-reply";
    public const string AudioTranscriptionPolicyName = "audio-transcription";
    public const string AudioSpeechPolicyName = "audio-speech";
    public const string AudioSpeechStreamPolicyName = "audio-speech-stream";
    public const string TranslationPolicyName = "translation";
    public const string RealtimeVoicePolicyName = "realtime-voice";

    public const string AuthEndpointGroup = "auth";
    public const string LessonChatEndpointGroup = "lesson-chat";
    public const string AudioEndpointGroup = "audio";
    public const string TranslationEndpointGroup = "translation";
    public const string RealtimeVoiceEndpointGroup = "realtime-voice";
    public const string UnknownEndpointGroup = "unknown";
}
