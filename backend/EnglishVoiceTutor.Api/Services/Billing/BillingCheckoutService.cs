using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Billing;
using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingCheckoutService : IBillingCheckoutService
{
    private readonly IOptions<BillingOptions> billingOptions;
    private readonly IBillingProviderCheckoutAdapterResolver adapterResolver;
    private readonly ILogger<BillingCheckoutService> logger;

    public BillingCheckoutService(
        IOptions<BillingOptions> billingOptions,
        IBillingProviderCheckoutAdapterResolver adapterResolver,
        ILogger<BillingCheckoutService> logger)
    {
        this.billingOptions = billingOptions;
        this.adapterResolver = adapterResolver;
        this.logger = logger;
    }

    public async Task<CreateBillingCheckoutSessionResponse> CreateCheckoutSessionAsync(
        Guid userId,
        CreateBillingCheckoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.PlanId))
        {
            throw new BadHttpRequestException("PlanId is required.", StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(request.PlanId, SubscriptionConstants.Billing.DefaultPremiumPlanId, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadHttpRequestException("Unsupported plan id.", StatusCodes.Status400BadRequest);
        }

        var options = billingOptions.Value;
        var provider = NormalizeProvider(options.Provider);
        var checkoutEnabled = options.CheckoutEnabled
            && !string.Equals(provider, SubscriptionConstants.BillingProviders.None, StringComparison.OrdinalIgnoreCase);

        logger.LogInformation(
            "Billing checkout session requested. UserId={UserId}; Provider={Provider}; CheckoutEnabled={CheckoutEnabled}; PlanId={PlanId}.",
            userId,
            provider,
            options.CheckoutEnabled,
            request.PlanId);

        var providerRequest = new BillingProviderCheckoutRequest
        {
            UserId = userId,
            PlanId = request.PlanId,
            Provider = checkoutEnabled ? provider : SubscriptionConstants.BillingProviders.None,
            ReturnUrl = options.SuccessUrl ?? string.Empty,
            CancelUrl = options.CancelUrl ?? string.Empty,
            Currency = SubscriptionConstants.Billing.DefaultCheckoutCurrency,
            Mode = SubscriptionConstants.Billing.CheckoutModeSubscription,
            RequestedAtUtc = DateTimeOffset.UtcNow
        };

        var adapter = adapterResolver.Resolve(providerRequest.Provider);
        var result = await adapter.CreateCheckoutSessionAsync(providerRequest, cancellationToken);

        return new CreateBillingCheckoutSessionResponse
        {
            Created = result.Created,
            CheckoutEnabled = result.CheckoutEnabled,
            Provider = result.Provider,
            PlanId = result.PlanId,
            CheckoutUrl = result.CheckoutUrl,
            ErrorCode = result.ErrorCode,
            Message = result.Message,
            CheckedAtUtc = result.CheckedAtUtc
        };
    }

    private static string NormalizeProvider(string provider)
    {
        return string.IsNullOrWhiteSpace(provider)
            ? SubscriptionConstants.BillingProviders.None
            : provider.Trim().ToLowerInvariant();
    }
}
