namespace EnglishVoiceTutor.Api.Constants;

public static class RateLimitingConstants
{
    public const string ErrorCode = "RateLimitExceeded";
    public const string DefaultMessage = "Too many requests. Please wait a moment and try again.";
    public const string LoginMessage = "Too many login attempts. Please wait a few minutes and try again.";
    public const string RegisterMessage = "Too many registration attempts. Please wait and try again.";
    public const string PasswordResetMessage = "Too many password reset requests. Please wait before trying again.";
    public const string AuthSessionMessage = "Too many session requests. Please wait and try again.";
    public const string LessonChatReplyMessage = "You are sending messages too quickly. Please wait a moment and continue the lesson.";
    public const string LessonStartMessage = "Too many lesson start attempts. Please wait a moment and try again.";
    public const string LessonHintMessage = "Too many hint requests. Please wait a moment and try again.";
    public const string LessonFeedbackMessage = "Too many feedback requests. Please wait a moment and try again.";
    public const string LessonPersistedMessageMessage = "Too many lesson messages. Please wait a moment and try again.";
    public const string LessonStatusMessage = "Too many status requests. Please wait and try again.";
    public const string AudioTranscriptionMessage = "Too many recordings. Please wait a moment before recording again.";
    public const string AudioTtsMessage = "Voice playback is being requested too quickly. Please wait a moment and try again.";
    public const string TranslationMessage = "Too many translation requests. Please wait a moment and try again.";
    public const string RealtimeVoiceMessage = "Too many voice sessions. Please close another session or wait a moment.";
    public const string AdminReadMessage = "Too many admin requests. Please wait and try again.";
    public const string AdminWriteMessage = "Too many admin changes. Please wait and try again.";
    public const string AdminRoleManagementMessage = "Too many role-management attempts. Please wait and try again.";
    public const string BillingCheckoutMessage = "Too many checkout requests. Please wait and try again.";
    public const string BillingGooglePlayPurchaseVerificationMessage = "Too many purchase verification requests. Please wait and try again.";
    public const string BillingCancelRenewalMessage = "Too many subscription requests. Please wait and try again.";
    public const string PaddleCheckoutLaunchMessage = "Too many checkout launch requests. Please wait and try again.";
    public const string PaddleWebhookMessage = "Too many provider requests.";

    public const string RetryAfterHeaderName = "Retry-After";

    public const string AuthLoginPolicyName = "auth-login";
    public const string AuthRegisterPolicyName = "auth-register";
    public const string AuthPasswordResetRequestPolicyName = "auth-password-reset-request";
    public const string AuthPasswordResetConfirmPolicyName = "auth-password-reset-confirm";
    public const string AuthSessionPolicyName = "auth-session";
    public const string LessonChatReplyPolicyName = "lesson-chat-reply";
    public const string LessonStartPolicyName = "lesson-start";
    public const string LessonHintPolicyName = "lesson-hint";
    public const string LessonFeedbackPolicyName = "lesson-feedback";
    public const string LessonPersistedMessagePolicyName = "lesson-persisted-message";
    public const string LessonStatusPolicyName = "lesson-status";
    public const string AudioTranscriptionPolicyName = "audio-transcription";
    public const string AudioSpeechPolicyName = "audio-speech";
    public const string AudioSpeechStreamPolicyName = "audio-speech-stream";
    public const string TranslationPolicyName = "translation";
    public const string RealtimeVoicePolicyName = "realtime-voice";
    public const string AdminReadPolicyName = "admin-read";
    public const string AdminWritePolicyName = "admin-write";
    public const string AdminRoleManagementPolicyName = "admin-role-management";
    public const string BillingCheckoutPolicyName = "billing-checkout";
    public const string BillingGooglePlayPurchaseVerificationPolicyName = "billing-google-play-purchase-verification";
    public const string BillingCancelRenewalPolicyName = "billing-cancel-renewal";
    public const string PaddleCheckoutLaunchPolicyName = "paddle-checkout-launch";
    public const string PaddleWebhookPolicyName = "paddle-webhook";

    public const string AuthEndpointGroup = "auth";
    public const string LessonChatEndpointGroup = "lesson-chat";
    public const string LessonStartEndpointGroup = "lesson-start";
    public const string LessonHintEndpointGroup = "lesson-hint";
    public const string LessonFeedbackEndpointGroup = "lesson-feedback";
    public const string LessonPersistedMessageEndpointGroup = "lesson-persisted-message";
    public const string LessonStatusEndpointGroup = "lesson-status";
    public const string AudioEndpointGroup = "audio";
    public const string TranslationEndpointGroup = "translation";
    public const string RealtimeVoiceEndpointGroup = "realtime-voice";
    public const string AdminReadEndpointGroup = "admin-read";
    public const string AdminWriteEndpointGroup = "admin-write";
    public const string AdminRoleManagementEndpointGroup = "admin-role-management";
    public const string BillingCheckoutEndpointGroup = "billing-checkout";
    public const string BillingGooglePlayPurchaseVerificationEndpointGroup = "billing-google-play-purchase-verification";
    public const string BillingCancelRenewalEndpointGroup = "billing-cancel-renewal";
    public const string PaddleCheckoutLaunchEndpointGroup = "paddle-checkout-launch";
    public const string PaddleWebhookEndpointGroup = "paddle-webhook";
    public const string UnknownEndpointGroup = "unknown";
}
