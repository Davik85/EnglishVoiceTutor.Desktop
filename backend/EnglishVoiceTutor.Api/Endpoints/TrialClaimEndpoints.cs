using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using System.Security.Claims;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class TrialClaimEndpoints
{
    private const string AuthenticatedSource = "authenticated";

    public static void MapTrialClaimEndpoints(this WebApplication app)
    {
        app.MapPost(ApiConstants.MeTrialClaimRoute, ClaimTrialAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> ClaimTrialAsync(
        ClaimsPrincipal principal,
        ITrialClaimService trialClaimService,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var response = await trialClaimService.ClaimTrialAsync(userId.Value, AuthenticatedSource, cancellationToken);
        return Results.Ok(response);
    }
}
