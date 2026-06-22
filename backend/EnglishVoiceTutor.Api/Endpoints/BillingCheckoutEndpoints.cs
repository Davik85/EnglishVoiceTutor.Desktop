using System.Security.Claims;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Billing;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.AspNetCore.RateLimiting;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class BillingCheckoutEndpoints
{
    public static void MapBillingCheckoutEndpoints(this WebApplication app)
    {
        var checkoutEndpoint = app.MapPost(ApiConstants.MeBillingCheckoutSessionRoute, CreateCheckoutSessionAsync)
            .RequireAuthorization();

        if (IsRateLimitingEnabled(app))
        {
            checkoutEndpoint.RequireRateLimiting(RateLimitingConstants.BillingCheckoutPolicyName);
        }
    }

    private static bool IsRateLimitingEnabled(WebApplication app) =>
        app.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()?.Enabled == true;

    private static async Task<IResult> CreateCheckoutSessionAsync(
        ClaimsPrincipal principal,
        CreateBillingCheckoutSessionRequest request,
        IBillingCheckoutService billingCheckoutService,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.PlanId))
        {
            return Results.BadRequest(new
            {
                ErrorCode = SubscriptionConstants.Billing.InvalidBillingCheckoutRequestCode,
                Message = SubscriptionConstants.Billing.PlanIdRequiredMessage
            });
        }

        if (!string.Equals(request.PlanId, SubscriptionConstants.Billing.DefaultPremiumPlanId, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new
            {
                ErrorCode = SubscriptionConstants.Billing.InvalidBillingCheckoutRequestCode,
                Message = SubscriptionConstants.Billing.UnsupportedPlanIdMessage
            });
        }

        var response = await billingCheckoutService.CreateCheckoutSessionAsync(userId.Value, request, cancellationToken);
        return Results.Ok(response);
    }
}
