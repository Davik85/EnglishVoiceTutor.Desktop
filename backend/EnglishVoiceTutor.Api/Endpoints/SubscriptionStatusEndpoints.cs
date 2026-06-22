using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class SubscriptionStatusEndpoints
{
    private const string AuthenticatedSource = "authenticated";
    private const string DevelopmentSource = "development";

    public static void MapSubscriptionStatusEndpoints(this WebApplication app)
    {
        var authenticatedEndpoint = app.MapGet(ApiConstants.MeSubscriptionStatusRoute, GetAuthenticatedStatusAsync).RequireAuthorization();
        if (IsRateLimitingEnabled(app))
        {
            authenticatedEndpoint.RequireRateLimiting(RateLimitingConstants.LessonStatusPolicyName);
        }

        app.MapGet(ApiConstants.DevSubscriptionStatusRoute, GetDevStatusAsync)
            .AddEndpointFilter(async (context, next) =>
            {
                var environment = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
                if (!environment.IsDevelopment())
                {
                    return Results.NotFound();
                }

                return await next(context);
            });
    }

    private static bool IsRateLimitingEnabled(WebApplication app) =>
        app.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()?.Enabled == true;

    private static async Task<IResult> GetAuthenticatedStatusAsync(
        ClaimsPrincipal principal,
        ISubscriptionStatusService subscriptionStatusService,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var response = await subscriptionStatusService.GetStatusAsync(userId.Value, AuthenticatedSource, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetDevStatusAsync(
        DevUserProvider devUserProvider,
        ISubscriptionStatusService subscriptionStatusService,
        CancellationToken cancellationToken)
    {
        var userId = devUserProvider.GetDevUserId();
        var response = await subscriptionStatusService.GetStatusAsync(userId, DevelopmentSource, cancellationToken);
        return Results.Ok(response);
    }
}
