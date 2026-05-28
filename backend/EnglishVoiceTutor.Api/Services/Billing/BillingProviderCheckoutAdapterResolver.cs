using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingProviderCheckoutAdapterResolver : IBillingProviderCheckoutAdapterResolver
{
    private readonly IReadOnlyDictionary<string, IBillingProviderCheckoutAdapter> adapters;
    private readonly IBillingProviderCheckoutAdapter disabledAdapter;

    public BillingProviderCheckoutAdapterResolver(IEnumerable<IBillingProviderCheckoutAdapter> adapters)
    {
        var adapterList = adapters.ToList();
        disabledAdapter = adapterList.First(adapter =>
            string.Equals(adapter.ProviderId, SubscriptionConstants.BillingProviders.None, StringComparison.OrdinalIgnoreCase));

        this.adapters = adapterList
            .GroupBy(adapter => NormalizeProvider(adapter.ProviderId), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IBillingProviderCheckoutAdapter Resolve(string provider)
    {
        var normalizedProvider = NormalizeProvider(provider);
        return adapters.GetValueOrDefault(normalizedProvider, disabledAdapter);
    }

    private static string NormalizeProvider(string provider)
    {
        return string.IsNullOrWhiteSpace(provider)
            ? SubscriptionConstants.BillingProviders.None
            : provider.Trim().ToLowerInvariant();
    }
}
