namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingProviderSubscriptionCancelRequest
{
    public Guid UserId { get; init; }
    public string ProviderSubscriptionId { get; init; } = string.Empty;
    public DateTimeOffset? CurrentPeriodEndUtc { get; init; }
    public DateTimeOffset RequestedAtUtc { get; init; }
}
