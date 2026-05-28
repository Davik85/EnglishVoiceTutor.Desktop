namespace EnglishVoiceTutor.Api.Options;

public sealed class PaddleWebhookOptions
{
    public const string SectionName = "PaddleWebhook";

    public bool Enabled { get; set; }
    public string SecretKey { get; set; } = string.Empty;
    public int TimestampToleranceSeconds { get; set; } = 300;
}
