using System.Security.Claims;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Admin;
using Microsoft.AspNetCore.RateLimiting;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class AccountAnonymizationEndpoints
{
    public static void MapAccountAnonymizationEndpoints(this WebApplication app)
    {
        var rateLimitingEnabled = app.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()?.Enabled == true;
        var createPreflightEndpoint = app.MapPost(ApiConstants.AdminFeedbackReportAccountAnonymizationPreflightRoute, CreatePreflightAsync)
            .RequireAuthorization(AdminAuthorizationConstants.AccountAnonymizationPreflightReadPermissionPolicyName);
        var statusEndpoint = app.MapGet(ApiConstants.AdminFeedbackReportAccountAnonymizationRoute, GetStatusAsync)
            .RequireAuthorization(AdminAuthorizationConstants.AccountAnonymizationPreflightReadPermissionPolicyName);
        if (rateLimitingEnabled)
        {
            createPreflightEndpoint.RequireRateLimiting(RateLimitingConstants.AdminWritePolicyName);
            statusEndpoint.RequireRateLimiting(RateLimitingConstants.AdminReadPolicyName);
        }
    }

    private static async Task<IResult> CreatePreflightAsync(
        Guid reportId,
        AccountAnonymizationPreflightRequest? request,
        ClaimsPrincipal principal,
        IAdminRoleAssignmentActorResolver actorResolver,
        IAccountAnonymizationPreflightService preflightService,
        CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest(new { error = "account_anonymization_preflight_request_invalid" });
        var actor = await actorResolver.ResolveActorAsync(principal, cancellationToken);
        if (!actor.IsActorMappingFound || !actor.ActorAdminUserId.HasValue)
        {
            return Results.Conflict(new { error = AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode });
        }

        var result = await preflightService.CreateOrRefreshAsync(actor.ActorAdminUserId.Value, reportId, request.Refresh, cancellationToken);
        if (result.IsNotFound) return Results.NotFound(new { error = "account_anonymization_report_not_found" });
        if (result.IsWrongCategory) return Results.Conflict(new { error = "account_anonymization_not_deletion_request" });
        if (result.IsRequestStateBlocked) return Results.Conflict(new { error = "account_anonymization_request_state_blocked" });
        if (result.IsUnavailable) return Results.Json(new { error = "account_anonymization_preflight_unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        return Results.Ok(result.Response);
    }

    private static async Task<IResult> GetStatusAsync(
        Guid reportId,
        ClaimsPrincipal principal,
        IAdminRoleAssignmentActorResolver actorResolver,
        IAccountAnonymizationPreflightService preflightService,
        CancellationToken cancellationToken)
    {
        var actor = await actorResolver.ResolveActorAsync(principal, cancellationToken);
        if (!actor.IsActorMappingFound || !actor.ActorAdminUserId.HasValue)
        {
            return Results.Conflict(new { error = AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode });
        }

        var result = await preflightService.GetStatusAsync(reportId, cancellationToken);
        if (result.IsNotFound) return Results.NotFound(new { error = "account_anonymization_report_not_found" });
        if (result.IsWrongCategory) return Results.Conflict(new { error = "account_anonymization_not_deletion_request" });
        if (result.IsNoOperation) return Results.NotFound(new { error = "account_anonymization_preflight_not_found" });
        return Results.Ok(result.Response);
    }
}
