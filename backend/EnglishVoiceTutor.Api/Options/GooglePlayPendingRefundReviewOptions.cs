namespace EnglishVoiceTutor.Api.Options;

public sealed class GooglePlayPendingRefundReviewOptions
{
    public const string SectionName = "GooglePlayPendingRefundReview";
    public bool Enabled { get; set; }
    public int PollIntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 20;
    public int ProcessingLeaseSeconds { get; set; } = 300;
    public int InitialRetrySeconds { get; set; } = 60;
    public int MaximumRetrySeconds { get; set; } = 3600;
    public int MaximumAttempts { get; set; } = 10;
    public string RefundPreference { get; set; } = string.Empty;
    public bool? SampleContentProvided { get; set; }
    public int TerminalProtectedPayloadRetentionDays { get; set; } = 30;

    public void ValidateForEnabledMode()
    {
        if (PollIntervalSeconds is < 10 or > 3600 || BatchSize is < 1 or > 100 || ProcessingLeaseSeconds is < 30 or > 3600 || InitialRetrySeconds is < 10 or > 3600 || MaximumRetrySeconds < InitialRetrySeconds || MaximumRetrySeconds > 86400 || MaximumAttempts is < 1 or > 100 || TerminalProtectedPayloadRetentionDays is < 1 or > 365 || !string.Equals(RefundPreference, "NEUTRAL", StringComparison.Ordinal) || SampleContentProvided is null)
            throw new InvalidOperationException("Google Play pending-refund review configuration is invalid. Only explicit NEUTRAL review is supported.");
    }
}
