using System.Security.Claims;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Billing;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.AspNetCore.RateLimiting;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class GooglePlayPurchaseVerificationEndpoints
{
    public static void MapGooglePlayPurchaseVerificationEndpoints(this WebApplication app)
    {
        var endpoint = app.MapPost(ApiConstants.MeGooglePlayPurchaseVerificationRoute, VerifyAsync).RequireAuthorization();
        if (app.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()?.Enabled == true)
        {
            endpoint.RequireRateLimiting(RateLimitingConstants.BillingGooglePlayPurchaseVerificationPolicyName);
        }
    }

    private static async Task<IResult> VerifyAsync(ClaimsPrincipal principal, GooglePlayPurchaseVerificationRequest? request, IGooglePlayPurchaseVerificationService verificationService, CancellationToken cancellationToken)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!userId.HasValue) return Results.Unauthorized();

        var result = await verificationService.VerifyAsync(userId.Value, request, cancellationToken);
        return Results.Json(result.Response, statusCode: result.StatusCode);
    }
}
