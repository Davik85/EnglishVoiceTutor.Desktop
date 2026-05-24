using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using System.Security.Claims;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class LessonAccessDecisionEndpoints
{
    private const string AuthenticatedSource = SubscriptionConstants.LessonAccessSources.Authenticated;
    private const string DevelopmentSource = SubscriptionConstants.LessonAccessSources.Development;

    public static void MapLessonAccessDecisionEndpoints(this WebApplication app)
    {
        app.MapGet(ApiConstants.MeLessonAccessRoute, GetAuthenticatedDecisionAsync).RequireAuthorization();

        app.MapGet(ApiConstants.DevLessonAccessRoute, GetDevDecisionAsync)
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

    private static async Task<IResult> GetAuthenticatedDecisionAsync(
        ClaimsPrincipal principal,
        ILessonAccessDecisionService lessonAccessDecisionService,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var response = await lessonAccessDecisionService.GetDecisionAsync(userId.Value, AuthenticatedSource, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetDevDecisionAsync(
        DevUserProvider devUserProvider,
        ILessonAccessDecisionService lessonAccessDecisionService,
        CancellationToken cancellationToken)
    {
        var userId = devUserProvider.GetDevUserId();
        var response = await lessonAccessDecisionService.GetDecisionAsync(userId, DevelopmentSource, cancellationToken);
        return Results.Ok(response);
    }
}
