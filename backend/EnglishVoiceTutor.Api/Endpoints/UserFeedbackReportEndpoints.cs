using System.Security.Claims;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.FeedbackReports;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class UserFeedbackReportEndpoints
{
    private const int MessageMaxLength = 4000;
    private const int ReportedAiTextMaxLength = 4000;
    private const int ClientPlatformMaxLength = 32;
    private const int ClientVersionMaxLength = 64;
    public static void MapUserFeedbackReportEndpoints(this WebApplication app)
    {
        app.MapPost(ApiConstants.MeFeedbackReportsRoute, CreateAsync).RequireAuthorization();
        app.MapPost(ApiConstants.MeAccountDeletionRequestsRoute, CreateAccountDeletionRequestAsync)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingConstants.AuthSessionPolicyName);
    }

    private static async Task<IResult> CreateAsync(CreateFeedbackReportRequest request, ClaimsPrincipal principal, AppDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!userId.HasValue) return Results.Unauthorized();
        var category = request.Category.Trim().ToLowerInvariant();
        var message = request.Message.Trim();
        var reportedAiText = string.IsNullOrWhiteSpace(request.ReportedAiText) ? null : request.ReportedAiText.Trim();
        var clientPlatform = request.ClientPlatform.Trim();
        var clientVersion = request.ClientVersion.Trim();
        if (!UserFeedbackReportConstants.Categories.Contains(category)
            || category == UserFeedbackReportConstants.AccountDeletionCategory
            || message.Length == 0 || message.Length > MessageMaxLength || (reportedAiText?.Length ?? 0) > ReportedAiTextMaxLength || clientPlatform.Length > ClientPlatformMaxLength || clientVersion.Length > ClientVersionMaxLength)
            return Results.BadRequest(new { error = "The feedback report is invalid." });
        var report = new UserFeedbackReportEntity { Id = Guid.NewGuid(), UserId = userId.Value, Category = category, Message = message, ReportedAiText = reportedAiText, Status = UserFeedbackReportConstants.NewStatus, ClientPlatform = clientPlatform, ClientVersion = clientVersion, CreatedAtUtc = DateTimeOffset.UtcNow };
        try
        {
            dbContext.UserFeedbackReports.Add(report);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Created(ApiConstants.MeFeedbackReportsRoute, new CreateFeedbackReportResponse { ReportId = report.Id });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SafeFailureLogger.LogFeedbackReportPersistenceFailed(loggerFactory.CreateLogger("UserFeedbackReportEndpoints"));
            return Results.Json(new { error = "Feedback is temporarily unavailable." }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static async Task<IResult> CreateAccountDeletionRequestAsync(
        CreateAccountDeletionRequest request,
        ClaimsPrincipal principal,
        IAccountDeletionRequestService deletionRequestService,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!userId.HasValue) return Results.Unauthorized();

        var result = await deletionRequestService.SubmitAsync(userId.Value, request.CurrentPassword, request.Reason, cancellationToken);
        if (result.IsInvalid) return Results.BadRequest(new { error = "The account deletion request is invalid." });
        if (result.IsPasswordRejected) return Results.Json(new { error = "The password confirmation was not accepted." }, statusCode: StatusCodes.Status401Unauthorized);
        if (result.IsUserUnavailable) return Results.Unauthorized();
        return result.IsAlreadyRequested ? Results.Ok(result.Response) : Results.Created(ApiConstants.MeAccountDeletionRequestsRoute, result.Response);
    }
}
