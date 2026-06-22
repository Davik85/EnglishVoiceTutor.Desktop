using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class TrialClaimEndpoints
{
    private const string AuthenticatedSource = "authenticated";

    public static void MapTrialClaimEndpoints(this WebApplication app)
    {
        var claimTrialEndpoint = app.MapPost(ApiConstants.MeTrialClaimRoute, ClaimTrialAsync)
            .RequireAuthorization();
        if (IsRateLimitingEnabled(app))
        {
            claimTrialEndpoint.RequireRateLimiting(RateLimitingConstants.LessonStatusPolicyName);
        }
    }

    private static bool IsRateLimitingEnabled(WebApplication app) =>
        app.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()?.Enabled == true;

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
