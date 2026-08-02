namespace EnglishVoiceTutor.Api.Options;

public sealed class GooglePlayReconciliationOptions
{
    public const string SectionName = "GooglePlayReconciliation";
    public bool Enabled { get; set; }
    public int PollIntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 20;
    public int ProcessingLeaseSeconds { get; set; } = 300;
    public int InitialRetrySeconds { get; set; } = 60;
    public int MaximumRetrySeconds { get; set; } = 3600;
    public int MaximumAttempts { get; set; } = 10;

    public void ValidateForEnabledMode()
    {
        if (PollIntervalSeconds is < 10 or > 3600 || BatchSize is < 1 or > 100 || ProcessingLeaseSeconds is < 30 or > 3600 || InitialRetrySeconds is < 10 or > 3600 || MaximumRetrySeconds < InitialRetrySeconds || MaximumRetrySeconds > 86400 || MaximumAttempts is < 1 or > 100)
            throw new InvalidOperationException("Google Play reconciliation configuration is invalid.");
    }
}
