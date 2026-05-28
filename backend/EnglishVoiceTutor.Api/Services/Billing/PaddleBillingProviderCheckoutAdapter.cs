using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class PaddleBillingProviderCheckoutAdapter : IBillingProviderCheckoutAdapter
{
    private readonly PaddleBillingOptions options;
    private readonly ILogger<PaddleBillingProviderCheckoutAdapter> logger;

    public PaddleBillingProviderCheckoutAdapter(
        IOptions<PaddleBillingOptions> options,
        ILogger<PaddleBillingProviderCheckoutAdapter> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    public string ProviderId => SubscriptionConstants.BillingProviders.Paddle;

    public Task<BillingProviderCheckoutResult> CreateCheckoutSessionAsync(
        BillingProviderCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!options.CheckoutAdapterEnabled)
        {
            logger.LogInformation("Paddle checkout adapter resolved but disabled. PlanId={PlanId}; Environment={Environment}.", request.PlanId, GetNormalizedEnvironment());
            return Task.FromResult(CreateResult(request.PlanId, SubscriptionConstants.Billing.PaddleCheckoutAdapterDisabledMessage));
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            logger.LogInformation("Paddle checkout adapter resolved but API key is not configured. PlanId={PlanId}; Environment={Environment}.", request.PlanId, GetNormalizedEnvironment());
            return Task.FromResult(CreateResult(request.PlanId, SubscriptionConstants.Billing.PaddleCheckoutNotConfiguredMessage));
        }

        if (string.Equals(request.PlanId, SubscriptionConstants.Plans.PremiumPlanId, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(options.PremiumPriceId))
        {
            logger.LogInformation("Paddle checkout adapter resolved but Premium price id is not configured. PlanId={PlanId}; Environment={Environment}.", request.PlanId, GetNormalizedEnvironment());
            return Task.FromResult(CreateResult(request.PlanId, SubscriptionConstants.Billing.PaddleCheckoutNotConfiguredMessage));
        }

        logger.LogInformation("Paddle checkout adapter resolved. External checkout creation is not implemented yet. PlanId={PlanId}; Environment={Environment}.", request.PlanId, GetNormalizedEnvironment());
        return Task.FromResult(CreateResult(request.PlanId, SubscriptionConstants.Billing.PaddleCheckoutNotConfiguredMessage));
    }

    private BillingProviderCheckoutResult CreateResult(string planId, string message)
    {
        return new BillingProviderCheckoutResult
        {
            Created = false,
            CheckoutEnabled = false,
            Provider = SubscriptionConstants.BillingProviders.Paddle,
            PlanId = planId,
            CheckoutUrl = string.Empty,
            ErrorCode = SubscriptionConstants.Billing.PaddleCheckoutNotConfiguredCode,
            Message = message,
            CheckedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private string GetNormalizedEnvironment()
    {
        return string.IsNullOrWhiteSpace(options.Environment)
            ? SubscriptionConstants.Billing.DefaultPaddleEnvironment
            : options.Environment.Trim().ToLowerInvariant();
    }
}
