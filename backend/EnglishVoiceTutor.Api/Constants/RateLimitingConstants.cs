namespace EnglishVoiceTutor.Api.Constants;

public static class RateLimitingConstants
{
    public const string ErrorCode = "RateLimitExceeded";
    public const string DefaultMessage = "Too many requests. Please wait a moment and try again.";
    public const string LoginMessage = "Too many login attempts. Please wait a few minutes and try again.";
    public const string RegisterMessage = "Too many registration attempts. Please wait and try again.";
    public const string PasswordResetMessage = "Too many password reset requests. Please wait before trying again.";
    public const string LessonChatReplyMessage = "You are sending messages too quickly. Please wait a moment and continue the lesson.";

    public const string RetryAfterHeaderName = "Retry-After";

    public const string AuthLoginPolicyName = "auth-login";
    public const string AuthRegisterPolicyName = "auth-register";
    public const string AuthPasswordResetRequestPolicyName = "auth-password-reset-request";
    public const string AuthPasswordResetConfirmPolicyName = "auth-password-reset-confirm";
    public const string LessonChatReplyPolicyName = "lesson-chat-reply";

    public const string AuthEndpointGroup = "auth";
    public const string LessonChatEndpointGroup = "lesson-chat";
    public const string UnknownEndpointGroup = "unknown";
}
