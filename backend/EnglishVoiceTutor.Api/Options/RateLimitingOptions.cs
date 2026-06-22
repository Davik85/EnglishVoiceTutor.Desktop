namespace EnglishVoiceTutor.Api.Options;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public const bool DefaultEnabled = false;
    public const bool DefaultLogThrottledRequests = true;
    public const int DefaultRetryAfterSeconds = 60;

    public bool Enabled { get; set; } = DefaultEnabled;
    public bool LogThrottledRequests { get; set; } = DefaultLogThrottledRequests;
    public int DefaultRetryAfterSeconds { get; set; } = DefaultRetryAfterSeconds;
    public AuthRateLimitingOptions Auth { get; set; } = new();
    public LessonRateLimitingOptions Lessons { get; set; } = new();
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
