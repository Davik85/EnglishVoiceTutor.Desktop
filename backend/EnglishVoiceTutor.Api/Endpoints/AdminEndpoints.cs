using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Services.Admin;
using EnglishVoiceTutor.Api.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class AdminEndpoints
{
    private const string EmailQueryKey = "email";
    private const string EmailRequiredError = "Email query parameter is required.";
    private const string UserIdRouteKey = "userId";
    private const string EntitlementIdRouteKey = "entitlementId";

    public static void MapAdminEndpoints(this WebApplication app)
    {
        app.MapGet(ApiConstants.AdminMeRoute, GetAdminMe)
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

        app.MapGet(ApiConstants.AdminUserByEmailRoute, GetAdminUserByEmailAsync)
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

        app.MapPost(ApiConstants.AdminUserPremiumGrantsRoute, GrantManualPremiumAsync)
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

        app.MapPost(ApiConstants.AdminUserPremiumGrantRevokeRoute, RevokeManualPremiumAsync)
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

        app.MapGet(ApiConstants.AdminUserAuditActionsRoute, GetTargetUserAuditActionsAsync)
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

        app.MapPost(ApiConstants.AdminUserFreeLessonAllowanceResetRoute, ResetFreeLessonAllowanceAsync)
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);
    }

    private static IResult GetAdminMe(ClaimsPrincipal principal)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        var email = ClaimsUserAccessor.TryGetUserEmail(principal);

        if (!userId.HasValue || string.IsNullOrWhiteSpace(email))
        {
            return Results.Unauthorized();
        }

        var response = new AdminMeResponse
        {
            UserId = userId.Value,
            Email = email,
            IsAdmin = true,
            AdminSource = AdminAuthorizationConstants.BootstrapAdminSource,
            CheckedAtUtc = DateTimeOffset.UtcNow
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> GetAdminUserByEmailAsync(
        [AsParameters] AdminUserLookupQuery query,
        IAdminUserLookupService adminUserLookupService,
        CancellationToken cancellationToken)
    {
        var lookupResult = await adminUserLookupService.GetByEmailAsync(query.Email, cancellationToken);

        if (lookupResult.IsInvalidEmail)
        {
            return Results.BadRequest(new Dictionary<string, string[]>
            {
                [EmailQueryKey] = [EmailRequiredError]
            });
        }

        if (lookupResult.Response is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(lookupResult.Response);
    }

    private static async Task<IResult> GrantManualPremiumAsync(
        ClaimsPrincipal principal,
        Guid userId,
        AdminManualPremiumGrantRequest request,
        IAdminPremiumGrantService adminPremiumGrantService,
        CancellationToken cancellationToken)
    {
        var adminUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!adminUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await adminPremiumGrantService.GrantPremiumAsync(
            adminUserId.Value,
            userId,
            request,
            cancellationToken);

        if (result.IsInvalid)
        {
            var errorKey = result.ErrorCode == nameof(AdminPremiumGrantConstants.DurationDaysOutOfRangeError)
                ? AdminPremiumGrantConstants.DurationDaysFieldName
                : AdminPremiumGrantConstants.ReasonFieldName;

            return Results.BadRequest(new Dictionary<string, string[]>
            {
                [errorKey] = [result.ErrorMessage ?? string.Empty]
            });
        }

        if (result.IsNotFound)
        {
            return Results.NotFound(new Dictionary<string, string[]>
            {
                [UserIdRouteKey] = [result.ErrorMessage ?? AdminPremiumGrantConstants.TargetUserNotFoundError]
            });
        }

        return Results.Created(
            ApiConstants.AdminUserPremiumGrantsRoute.Replace("{userId:guid}", userId.ToString()),
            result.Response);
    }


    private static async Task<IResult> GetTargetUserAuditActionsAsync(
        Guid userId,
        [AsParameters] AdminAuditActionsQuery query,
        IAdminAuditLogService adminAuditLogService,
        CancellationToken cancellationToken)
    {
        var result = await adminAuditLogService.GetTargetUserAuditActionsAsync(
            userId,
            query.Limit,
            cancellationToken);

        if (result.IsInvalid)
        {
            return Results.BadRequest(new Dictionary<string, string[]>
            {
                [AdminAuditLogConstants.LimitQueryKey] = [result.ErrorMessage ?? string.Empty]
            });
        }

        if (result.IsNotFound)
        {
            return Results.NotFound(new Dictionary<string, string[]>
            {
                [UserIdRouteKey] = [result.ErrorMessage ?? AdminAuditLogConstants.TargetUserNotFoundError]
            });
        }

        return Results.Ok(result.Response);
    }
    private static async Task<IResult> ResetFreeLessonAllowanceAsync(
        ClaimsPrincipal principal,
        Guid userId,
        AdminFreeLessonAllowanceResetRequest request,
        IAdminFreeLessonAllowanceResetService adminFreeLessonAllowanceResetService,
        CancellationToken cancellationToken)
    {
        var adminUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!adminUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await adminFreeLessonAllowanceResetService.ResetFreeLessonAllowanceAsync(
            adminUserId.Value,
            userId,
            request,
            cancellationToken);

        if (result.IsInvalid)
        {
            var errorKey = result.ErrorCode == nameof(AdminFreeLessonAllowanceResetConstants.UsageDateInvalidError)
                ? AdminFreeLessonAllowanceResetConstants.UsageDateFieldName
                : AdminFreeLessonAllowanceResetConstants.ReasonFieldName;

            return Results.BadRequest(new Dictionary<string, string[]>
            {
                [errorKey] = [result.ErrorMessage ?? string.Empty]
            });
        }

        if (result.IsNotFound)
        {
            var errorKey = result.ErrorCode == nameof(AdminFreeLessonAllowanceResetConstants.TargetUserNotFoundError)
                ? UserIdRouteKey
                : AdminFreeLessonAllowanceResetConstants.UsageDateFieldName;

            return Results.NotFound(new Dictionary<string, string[]>
            {
                [errorKey] = [result.ErrorMessage ?? string.Empty]
            });
        }

        return Results.Ok(result.Response);
    }

    private static async Task<IResult> RevokeManualPremiumAsync(
        ClaimsPrincipal principal,
        Guid userId,
        Guid entitlementId,
        AdminManualPremiumRevokeRequest request,
        IAdminPremiumRevokeService adminPremiumRevokeService,
        CancellationToken cancellationToken)
    {
        var adminUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!adminUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await adminPremiumRevokeService.RevokePremiumAsync(
            adminUserId.Value,
            userId,
            entitlementId,
            request,
            cancellationToken);

        if (result.IsInvalid)
        {
            return Results.BadRequest(new Dictionary<string, string[]>
            {
                [AdminPremiumRevokeConstants.ReasonFieldName] = [result.ErrorMessage ?? string.Empty]
            });
        }

        if (result.IsNotFound)
        {
            var routeKey = result.ErrorCode == nameof(AdminPremiumRevokeConstants.TargetUserNotFoundError)
                ? UserIdRouteKey
                : EntitlementIdRouteKey;

            return Results.NotFound(new Dictionary<string, string[]>
            {
                [routeKey] = [result.ErrorMessage ?? string.Empty]
            });
        }

        if (result.IsConflict)
        {
            return Results.Conflict(new Dictionary<string, string[]>
            {
                [EntitlementIdRouteKey] = [result.ErrorMessage ?? AdminPremiumRevokeConstants.EntitlementNotRevokableError]
            });
        }

        return Results.Ok(result.Response);
    }
}

public sealed class AdminUserLookupQuery
{
    public string? Email { get; init; }
}

public sealed class AdminAuditActionsQuery
{
    public int? Limit { get; init; }
}
