namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingProviderCheckoutRequest
{
    public Guid UserId { get; init; }

    public string PlanId { get; init; } = string.Empty;

    public string Provider { get; init; } = string.Empty;

    public string ReturnUrl { get; init; } = string.Empty;

    public string CancelUrl { get; init; } = string.Empty;

    public string Currency { get; init; } = string.Empty;

    public string Mode { get; init; } = string.Empty;

    public DateTimeOffset RequestedAtUtc { get; init; }
}
