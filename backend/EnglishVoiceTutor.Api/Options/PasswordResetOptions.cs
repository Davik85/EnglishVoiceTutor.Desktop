namespace EnglishVoiceTutor.Api.Options;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";
    public const int DefaultTokenLifetimeMinutes = 60;

    public bool Enabled { get; set; }
    public int TokenLifetimeMinutes { get; set; } = DefaultTokenLifetimeMinutes;
    public string ResetUrlBase { get; set; } = string.Empty;
    public bool RequireConfiguredEmailSender { get; set; } = true;
}
