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
    public string ProviderErrorCode { get; init; } = string.Empty;
    public string ProviderErrorMessageSafe { get; init; } = string.Empty;
    public int? ProviderHttpStatusCode { get; init; }
    public string ProviderRequestId { get; init; } = string.Empty;
    public DateTimeOffset? CancellationAttemptedAtUtc { get; init; }
    public bool ProviderSubscriptionPresent { get; init; }
    public string ProviderSubscriptionIdLast4 { get; init; } = string.Empty;
    public string ProviderSubscriptionIdHash { get; init; } = string.Empty;
}
