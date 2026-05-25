using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using System.Security.Claims;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class SubscriptionDiagnosticsEndpoints
{
    private const string AuthenticatedSource = SubscriptionConstants.LessonAccessSources.Authenticated;
    private const string DevelopmentSource = SubscriptionConstants.LessonAccessSources.Development;
    private const string UnknownScenarioError = "Unknown diagnostics scenario.";

    public static void MapSubscriptionDiagnosticsEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.MapPost(ApiConstants.DevSubscriptionDiagnosticsScenarioRoute, ApplyDevScenarioAsync);
        app.MapPost(ApiConstants.MeSubscriptionDiagnosticsScenarioRoute, ApplyAuthenticatedScenarioAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> ApplyDevScenarioAsync(
        string scenario,
        DevUserProvider devUserProvider,
        ISubscriptionDiagnosticsService diagnosticsService,
        CancellationToken cancellationToken)
    {
        return await ApplyScenarioAsync(
            scenario,
            devUserProvider.GetDevUserId(),
            DevelopmentSource,
            diagnosticsService,
            cancellationToken);
    }

    private static async Task<IResult> ApplyAuthenticatedScenarioAsync(
        string scenario,
        ClaimsPrincipal principal,
        ISubscriptionDiagnosticsService diagnosticsService,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        return await ApplyScenarioAsync(
            scenario,
            userId.Value,
            AuthenticatedSource,
            diagnosticsService,
            cancellationToken);
    }

    private static async Task<IResult> ApplyScenarioAsync(
        string scenario,
        Guid userId,
        string source,
        ISubscriptionDiagnosticsService diagnosticsService,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await diagnosticsService.ApplyScenarioAsync(
                scenario,
                userId,
                source,
                cancellationToken);

            return Results.Ok(response);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Results.BadRequest(new { error = UnknownScenarioError });
        }
    }
}
