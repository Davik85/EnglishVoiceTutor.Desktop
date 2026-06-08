namespace EnglishVoiceTutor.Api.Options;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "SmtpEmail";

    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "support@languagevoicetutor.com";
    public string FromName { get; set; } = "Language Voice Tutor Support";
}
