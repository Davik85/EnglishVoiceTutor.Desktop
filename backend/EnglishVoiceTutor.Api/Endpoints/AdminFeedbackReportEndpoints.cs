using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Services.Admin;
using EnglishVoiceTutor.Api.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class AdminFeedbackReportEndpoints
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 100;

    public static void MapAdminFeedbackReportEndpoints(this WebApplication app)
    {
        app.MapGet(ApiConstants.AdminFeedbackReportsRoute, ListAsync)
            .RequireAuthorization(AdminAuthorizationConstants.FeedbackReportsReadPermissionPolicyName);
        app.MapGet(ApiConstants.AdminFeedbackReportByIdRoute, GetByIdAsync)
            .RequireAuthorization(AdminAuthorizationConstants.FeedbackReportsReadPermissionPolicyName);
        app.MapPatch(ApiConstants.AdminFeedbackReportStatusRoute, ChangeStatusAsync)
            .RequireAuthorization(AdminAuthorizationConstants.FeedbackReportsStatusManagePermissionPolicyName);
        app.MapPost(ApiConstants.AdminFeedbackReportRepliesRoute, SendReplyAsync)
            .RequireAuthorization(AdminAuthorizationConstants.FeedbackReportsReplyPermissionPolicyName);
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] AdminFeedbackReportListQuery query,
        IAdminFeedbackReportReadService readService,
        CancellationToken cancellationToken)
    {
        var status = NormalizeFilter(query.Status);
        var category = NormalizeFilter(query.Category);
        var page = query.Page ?? DefaultPage;
        var pageSize = query.PageSize ?? DefaultPageSize;
        if ((status is not null && !UserFeedbackReportConstants.Statuses.Contains(status))
            || (category is not null && !UserFeedbackReportConstants.Categories.Contains(category))
            || page < 1
            || pageSize < 1
            || pageSize > MaxPageSize)
        {
            return Results.BadRequest(new { error = "The feedback report query is invalid." });
        }

        return Results.Ok(await readService.ListAsync(status, category, page, pageSize, cancellationToken));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid reportId,
        IAdminFeedbackReportReadService readService,
        CancellationToken cancellationToken)
    {
        var response = await readService.GetByIdAsync(reportId, cancellationToken);
        return response is null ? Results.NotFound() : Results.Ok(response);
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid reportId,
        AdminFeedbackReportStatusChangeRequest request,
        ClaimsPrincipal principal,
        IAdminFeedbackReportStatusService statusService,
        CancellationToken cancellationToken)
    {
        var adminUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!adminUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await statusService.ChangeStatusAsync(
            adminUserId.Value,
            reportId,
            request.Status,
            cancellationToken);
        if (result.IsInvalid)
        {
            return Results.BadRequest(new { error = "The feedback report status is invalid." });
        }

        if (result.IsNotFound)
        {
            return Results.NotFound();
        }

        return Results.Ok(result.Response);
    }

    private static async Task<IResult> SendReplyAsync(
        Guid reportId,
        AdminFeedbackReportReplyRequest request,
        ClaimsPrincipal principal,
        IAdminRoleAssignmentActorResolver actorResolver,
        IAdminFeedbackReportReplyService replyService,
        CancellationToken cancellationToken)
    {
        var actor = await actorResolver.ResolveActorAsync(principal, cancellationToken);
        if (!actor.IsActorMappingFound || !actor.ActorAdminUserId.HasValue)
        {
            return Results.Conflict(new { error = AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode });
        }

        var result = await replyService.SendAsync(actor.ActorAdminUserId.Value, reportId, request.ReplyText, cancellationToken);
        if (result.IsInvalid) return Results.BadRequest(new { error = "The feedback reply is invalid." });
        if (result.IsNotFound) return Results.NotFound();
        if (result.IsActorUnavailable) return Results.Conflict(new { error = AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode });
        if (result.IsRecipientUnavailable) return Results.Conflict(new { error = "recipient_email_unavailable" });
        if (result.IsDeliveryFailed)
        {
            var response = result.Response!;
            return Results.Json(new { response.ReplyId, response.FeedbackReportId, response.DeliveryStatus, response.FailureCode, response.ReportStatus }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var success = result.Response!;
        return Results.Ok(new { success.ReplyId, success.FeedbackReportId, success.DeliveryStatus, success.CreatedAtUtc, success.SentAtUtc, success.ReportStatus, success.ReviewedAtUtc });
    }

    private static string? NormalizeFilter(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
