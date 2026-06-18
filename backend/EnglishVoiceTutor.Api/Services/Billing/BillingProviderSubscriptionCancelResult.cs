namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingProviderSubscriptionCancelResult
{
    public bool Accepted { get; init; }
    public bool ProviderEnabled { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string SubscriptionStatus { get; init; } = string.Empty;
    public bool CancelAtPeriodEnd { get; init; }
    public DateTimeOffset? ScheduledChangeEffectiveAtUtc { get; init; }
    public DateTimeOffset? CurrentPeriodEndUtc { get; init; }
}
