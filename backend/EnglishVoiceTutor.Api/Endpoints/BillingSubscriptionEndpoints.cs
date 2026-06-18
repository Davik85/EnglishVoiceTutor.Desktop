using System.Security.Claims;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Billing;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class BillingSubscriptionEndpoints
{
    public static void MapBillingSubscriptionEndpoints(this WebApplication app)
    {
        app.MapPost(ApiConstants.MeBillingSubscriptionCancelRoute, CancelSubscriptionRenewalAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> CancelSubscriptionRenewalAsync(
        ClaimsPrincipal principal,
        IBillingSubscriptionCancellationService cancellationService,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var response = await cancellationService.CancelCurrentUserSubscriptionRenewalAsync(userId.Value, cancellationToken);
        return Results.Ok(response);
    }
}
