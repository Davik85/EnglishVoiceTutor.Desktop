namespace EnglishVoiceTutor.Api.Options;

public sealed class GooglePlayRtdnOptions
{
    public const string SectionName = "GooglePlayRtdn";

    public bool Enabled { get; set; }
    public string ExpectedAudience { get; set; } = string.Empty;
    public string ExpectedServiceAccountEmail { get; set; } = string.Empty;
    public string ExpectedPubSubSubscription { get; set; } = string.Empty;

    public void ValidateForEnabledMode()
    {
        if (string.IsNullOrWhiteSpace(ExpectedAudience) ||
            string.IsNullOrWhiteSpace(ExpectedServiceAccountEmail) ||
            string.IsNullOrWhiteSpace(ExpectedPubSubSubscription))
        {
            throw new InvalidOperationException("Google Play RTDN configuration is incomplete.");
        }
    }
}
