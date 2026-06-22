namespace EnglishVoiceTutor.Api.Options;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public const bool DefaultEnabled = false;
    public const bool DefaultLogThrottledRequests = true;
    public const int DefaultRetryAfterSecondsValue = 60;

    public bool Enabled { get; set; } = DefaultEnabled;
    public bool LogThrottledRequests { get; set; } = DefaultLogThrottledRequests;
    public int DefaultRetryAfterSeconds { get; set; } = DefaultRetryAfterSecondsValue;
    public AuthRateLimitingOptions Auth { get; set; } = new();
    public LessonRateLimitingOptions Lessons { get; set; } = new();
    public AudioRateLimitingOptions Audio { get; set; } = new();
    public TranslationRateLimitingOptions Translation { get; set; } = new();
    public AdminRateLimitingOptions Admin { get; set; } = new();
    public BillingRateLimitingOptions Billing { get; set; } = new();
}

public sealed class AuthRateLimitingOptions
{
    public const int DefaultLoginPerIpLimit = 10;
    public const int DefaultLoginPerEmailLimit = 5;
    public const int DefaultLoginWindowMinutes = 5;
    public const int DefaultRegisterPerIpLimit = 5;
    public const int DefaultRegisterPerEmailLimit = 3;
    public const int DefaultRegisterWindowMinutes = 15;
    public const int DefaultPasswordResetPerIpLimit = 5;
    public const int DefaultPasswordResetPerEmailLimit = 3;
    public const int DefaultPasswordResetWindowMinutes = 15;
    public const int DefaultPasswordResetConfirmPerIpLimit = 10;
    public const int DefaultPasswordResetConfirmPerEmailLimit = 5;
    public const int DefaultPasswordResetConfirmWindowMinutes = 15;

    public int LoginPerIpLimit { get; set; } = DefaultLoginPerIpLimit;
    public int LoginPerEmailLimit { get; set; } = DefaultLoginPerEmailLimit;
    public int LoginWindowMinutes { get; set; } = DefaultLoginWindowMinutes;
    public int RegisterPerIpLimit { get; set; } = DefaultRegisterPerIpLimit;
    public int RegisterPerEmailLimit { get; set; } = DefaultRegisterPerEmailLimit;
    public int RegisterWindowMinutes { get; set; } = DefaultRegisterWindowMinutes;
    public int PasswordResetPerIpLimit { get; set; } = DefaultPasswordResetPerIpLimit;
    public int PasswordResetPerEmailLimit { get; set; } = DefaultPasswordResetPerEmailLimit;
    public int PasswordResetWindowMinutes { get; set; } = DefaultPasswordResetWindowMinutes;
    public int PasswordResetConfirmPerIpLimit { get; set; } = DefaultPasswordResetConfirmPerIpLimit;
    public int PasswordResetConfirmPerEmailLimit { get; set; } = DefaultPasswordResetConfirmPerEmailLimit;
    public int PasswordResetConfirmWindowMinutes { get; set; } = DefaultPasswordResetConfirmWindowMinutes;
}

public sealed class LessonRateLimitingOptions
{
    public const int DefaultChatReplyPerUserLimit = 30;
    public const int DefaultChatReplyPerSessionLimit = 20;
    public const int DefaultChatReplyPerIpFallbackLimit = 30;
    public const int DefaultChatReplyWindowMinutes = 10;

    public int ChatReplyPerUserLimit { get; set; } = DefaultChatReplyPerUserLimit;
    public int ChatReplyPerSessionLimit { get; set; } = DefaultChatReplyPerSessionLimit;
    public int ChatReplyPerIpFallbackLimit { get; set; } = DefaultChatReplyPerIpFallbackLimit;
    public int ChatReplyWindowMinutes { get; set; } = DefaultChatReplyWindowMinutes;
}

public sealed class AudioRateLimitingOptions
{
    public const int DefaultTranscriptionPerUserLimit = 20;
    public const int DefaultTtsPerUserLimit = 60;
    public const int DefaultAudioWindowMinutes = 10;
    public const int DefaultRealtimeVoiceConcurrentPerIpLimit = 3;
    public const int DefaultRealtimeVoiceStartPerIpLimit = 10;
    public const int DefaultRealtimeVoiceWindowMinutes = 10;

    public int TranscriptionPerUserLimit { get; set; } = DefaultTranscriptionPerUserLimit;
    public int TtsPerUserLimit { get; set; } = DefaultTtsPerUserLimit;
    public int AudioWindowMinutes { get; set; } = DefaultAudioWindowMinutes;
    public int RealtimeVoiceConcurrentPerIpLimit { get; set; } = DefaultRealtimeVoiceConcurrentPerIpLimit;
    public int RealtimeVoiceStartPerIpLimit { get; set; } = DefaultRealtimeVoiceStartPerIpLimit;
    public int RealtimeVoiceWindowMinutes { get; set; } = DefaultRealtimeVoiceWindowMinutes;
}

public sealed class TranslationRateLimitingOptions
{
    public const int DefaultPerUserLimit = 30;
    public const int DefaultWindowMinutes = 10;

    public int PerUserLimit { get; set; } = DefaultPerUserLimit;
    public int WindowMinutes { get; set; } = DefaultWindowMinutes;
}

public sealed class AdminRateLimitingOptions
{
    public const int DefaultReadPerAdminLimit = 120;
    public const int DefaultWritePerAdminLimit = 30;
    public const int DefaultRoleManagementPerAdminLimit = 10;
    public const int DefaultWindowMinutes = 10;

    public int ReadPerAdminLimit { get; set; } = DefaultReadPerAdminLimit;
    public int WritePerAdminLimit { get; set; } = DefaultWritePerAdminLimit;
    public int RoleManagementPerAdminLimit { get; set; } = DefaultRoleManagementPerAdminLimit;
    public int WindowMinutes { get; set; } = DefaultWindowMinutes;
}

public sealed class BillingRateLimitingOptions
{
    public const int DefaultCheckoutPerUserLimit = 10;
    public const int DefaultCancelPerUserLimit = 10;
    public const int DefaultPaddleCheckoutLaunchPerIpLimit = 30;
    public const int DefaultPaddleWebhookPerIpLimit = 300;
    public const int DefaultWindowMinutes = 10;
    public const int DefaultWebhookWindowMinutes = 5;

    public int CheckoutPerUserLimit { get; set; } = DefaultCheckoutPerUserLimit;
    public int CancelPerUserLimit { get; set; } = DefaultCancelPerUserLimit;
    public int PaddleCheckoutLaunchPerIpLimit { get; set; } = DefaultPaddleCheckoutLaunchPerIpLimit;
    public int PaddleWebhookPerIpLimit { get; set; } = DefaultPaddleWebhookPerIpLimit;
    public int WindowMinutes { get; set; } = DefaultWindowMinutes;
    public int WebhookWindowMinutes { get; set; } = DefaultWebhookWindowMinutes;
}
