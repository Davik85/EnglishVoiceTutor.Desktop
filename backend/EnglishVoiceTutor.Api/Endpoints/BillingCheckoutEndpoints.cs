using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Billing;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Billing;
using System.Security.Claims;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class BillingCheckoutEndpoints
{
    public static void MapBillingCheckoutEndpoints(this WebApplication app)
    {
        app.MapPost(ApiConstants.MeBillingCheckoutSessionRoute, CreateCheckoutSessionAsync)
            .RequireAuthorization();
    }

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

        var response = await billingCheckoutService.CreateCheckoutSessionAsync(userId.Value, request, cancellationToken);
        return Results.Ok(response);
    }
}
