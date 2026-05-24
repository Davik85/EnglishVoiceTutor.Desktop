using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Subscriptions;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class SubscriptionDiagnosticsEndpoints
{
    private const string DevelopmentSource = "development";

    public static void MapSubscriptionDiagnosticsEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.MapPost(ApiConstants.DevSubscriptionDiagnosticsScenarioRoute, ApplyScenarioAsync);
    }

    private static async Task<IResult> ApplyScenarioAsync(
        string scenario,
        DevUserProvider devUserProvider,
        ISubscriptionDiagnosticsService diagnosticsService,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await diagnosticsService.ApplyScenarioAsync(
                scenario,
                devUserProvider.GetDevUserId(),
                DevelopmentSource,
                cancellationToken);

            return Results.Ok(response);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Results.BadRequest(new { error = "Unknown diagnostics scenario." });
        }
    }
}
