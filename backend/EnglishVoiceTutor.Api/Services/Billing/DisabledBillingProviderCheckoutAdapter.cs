using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class DisabledBillingProviderCheckoutAdapter : IBillingProviderCheckoutAdapter
{
    public string ProviderId => SubscriptionConstants.BillingProviders.None;

    public Task<BillingProviderCheckoutResult> CreateCheckoutSessionAsync(
        BillingProviderCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedProvider = string.IsNullOrWhiteSpace(request.Provider)
            ? SubscriptionConstants.BillingProviders.None
            : request.Provider.Trim().ToLowerInvariant();

        var provider = string.Equals(normalizedProvider, SubscriptionConstants.BillingProviders.None, StringComparison.OrdinalIgnoreCase)
            ? SubscriptionConstants.BillingProviders.None
            : normalizedProvider;

        return Task.FromResult(new BillingProviderCheckoutResult
        {
            Created = false,
            CheckoutEnabled = false,
            Provider = provider,
            PlanId = request.PlanId,
            CheckoutUrl = string.Empty,
            ErrorCode = SubscriptionConstants.Billing.BillingProviderNotConfiguredCode,
            Message = SubscriptionConstants.Billing.BillingCheckoutDisabledMessage,
            CheckedAtUtc = DateTimeOffset.UtcNow
        });
    }
}
