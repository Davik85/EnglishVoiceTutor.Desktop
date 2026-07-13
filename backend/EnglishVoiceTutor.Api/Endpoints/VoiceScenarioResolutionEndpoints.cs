using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class VoiceScenarioResolutionEndpoints
{
    public static IEndpointRouteBuilder MapVoiceScenarioResolutionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(ApiConstants.MeLessonSessionVoiceScenarioResolutionRoute, ResolveAsync)
            .RequireAuthorization();
        return app;
    }

    private static async Task<IResult> ResolveAsync(
        Guid sessionId,
        VoiceScenarioResolutionRequest request,
        ILessonSessionService lessonSessionService,
        IVoiceScenarioResolutionService service,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("VoiceScenarioResolutionEndpoint");
        try
        {
            await lessonSessionService.EnsureActiveLessonSessionAsync(sessionId, cancellationToken);
            var response = await service.ResolveAsync(request, cancellationToken);
            logger.LogInformation(
                "Voice scenario resolution completed. SessionPresent={SessionPresent}; CandidateCount={CandidateCount}; Decision={Decision}; MatchedContextPresent={MatchedContextPresent}; Confidence={Confidence}.",
                sessionId != Guid.Empty, request.Candidates.Count, response.Decision,
                !string.IsNullOrWhiteSpace(response.MatchedContextId), response.Confidence);
            return Results.Ok(response);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { error = "Lesson session was not found." });
        }
        catch (LessonSessionEndedElsewhereException)
        {
            return Results.Conflict(new { error = "Lesson session is no longer active." });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Results.Problem("Scenario resolution timed out. Review or retry the recognized text.", statusCode: StatusCodes.Status504GatewayTimeout);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Voice scenario resolution failed. SessionPresent={SessionPresent}; CandidateCount={CandidateCount}; ExceptionType={ExceptionType}.",
                sessionId != Guid.Empty, request.Candidates.Count, exception.GetType().Name);
            return Results.Problem("Scenario resolution is temporarily unavailable. Review or retry the recognized text.", statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
