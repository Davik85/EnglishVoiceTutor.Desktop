using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Billing;
using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingCheckoutService : IBillingCheckoutService
{
    private readonly IOptions<BillingOptions> billingOptions;
    private readonly ILogger<BillingCheckoutService> logger;

    public BillingCheckoutService(IOptions<BillingOptions> billingOptions, ILogger<BillingCheckoutService> logger)
    {
        this.billingOptions = billingOptions;
        this.logger = logger;
    }

    public Task<CreateBillingCheckoutSessionResponse> CreateCheckoutSessionAsync(
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
        var provider = string.IsNullOrWhiteSpace(options.Provider)
            ? SubscriptionConstants.BillingProviders.None
            : options.Provider.Trim().ToLowerInvariant();

        logger.LogInformation(
            "Billing checkout session requested. UserId={UserId}; Provider={Provider}; CheckoutEnabled={CheckoutEnabled}; PlanId={PlanId}.",
            userId,
            provider,
            options.CheckoutEnabled,
            request.PlanId);

        var checkoutEnabled = options.CheckoutEnabled && !string.Equals(provider, SubscriptionConstants.BillingProviders.None, StringComparison.OrdinalIgnoreCase);

        if (!checkoutEnabled)
        {
            return Task.FromResult(new CreateBillingCheckoutSessionResponse
            {
                Created = false,
                CheckoutEnabled = false,
                Provider = SubscriptionConstants.BillingProviders.None,
                PlanId = request.PlanId,
                CheckoutUrl = string.Empty,
                ErrorCode = SubscriptionConstants.Billing.BillingProviderNotConfiguredCode,
                Message = SubscriptionConstants.Billing.BillingCheckoutDisabledMessage,
                CheckedAtUtc = DateTimeOffset.UtcNow
            });
        }

        return Task.FromResult(new CreateBillingCheckoutSessionResponse
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
