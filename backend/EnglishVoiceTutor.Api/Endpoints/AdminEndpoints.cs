using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Contracts.Cms;
using EnglishVoiceTutor.Api.Services.Admin;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Cms;
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

        app.MapGet(ApiConstants.AdminCapabilitiesRoute, GetAdminCapabilities)
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

        if (app.Environment.IsDevelopment())
        {
            app.MapPost(ApiConstants.AdminDevCmsStaticContentImportRoute, ImportStaticCmsContentAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapGet(ApiConstants.AdminDevCmsPublishedContentStatusRoute, GetPublishedCmsContentStatusAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapGet(ApiConstants.AdminDevCmsContentPacksRoute, ListCmsContentPacksAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapGet(ApiConstants.AdminDevCmsContentPackRoute, GetCmsContentPackSummaryAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapGet(ApiConstants.AdminDevCmsContentPackTopicsRoute, ListCmsTopicsAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapGet(ApiConstants.AdminDevCmsContentPackTopicRoute, GetCmsTopicAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapPut(ApiConstants.AdminDevCmsContentPackTopicRoute, UpdateCmsTopicAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapGet(ApiConstants.AdminDevCmsContentPackScenariosRoute, ListCmsScenariosAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapGet(ApiConstants.AdminDevCmsContentPackScenarioRoute, GetCmsScenarioAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapPut(ApiConstants.AdminDevCmsContentPackScenarioRoute, UpdateCmsScenarioAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapGet(ApiConstants.AdminDevCmsContentPackPromptTemplatesRoute, ListCmsPromptTemplatesAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapGet(ApiConstants.AdminDevCmsContentPackPromptTemplateRoute, GetCmsPromptTemplateAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapPut(ApiConstants.AdminDevCmsContentPackPromptTemplateRoute, UpdateCmsPromptTemplateAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapGet(ApiConstants.AdminDevCmsContentPackTutorBehaviorProfilesRoute, ListCmsTutorBehaviorProfilesAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapGet(ApiConstants.AdminDevCmsContentPackTutorBehaviorProfileRoute, GetCmsTutorBehaviorProfileAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapPut(ApiConstants.AdminDevCmsContentPackTutorBehaviorProfileRoute, UpdateCmsTutorBehaviorProfileAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapPost(ApiConstants.AdminDevCmsContentPackValidateRoute, ValidateCmsContentPackAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

            app.MapGet(ApiConstants.AdminDevCmsContentPackPreviewSummaryRoute, GetCmsPreviewSummaryAsync)
                .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);
        }
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


    private static IResult GetAdminCapabilities(IAdminCapabilitiesService adminCapabilitiesService)
    {
        return Results.Ok(adminCapabilitiesService.GetCapabilities());
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



    private static async Task<IResult> ListCmsContentPacksAsync(
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await cmsContentAdminService.ListContentPacksAsync(cancellationToken));
    }

    private static async Task<IResult> GetCmsContentPackSummaryAsync(
        string slug,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        var result = await cmsContentAdminService.GetContentPackSummaryAsync(slug, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> ListCmsTopicsAsync(
        string slug,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        if (await cmsContentAdminService.GetContentPackSummaryAsync(slug, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(await cmsContentAdminService.ListTopicsAsync(slug, cancellationToken));
    }

    private static async Task<IResult> GetCmsTopicAsync(
        string slug,
        string topicId,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        var result = await cmsContentAdminService.GetTopicAsync(slug, topicId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpdateCmsTopicAsync(
        ClaimsPrincipal principal,
        string slug,
        string topicId,
        UpdateCmsTopicRequest request,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        var actorUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await cmsContentAdminService.UpdateTopicAsync(slug, topicId, request, actorUserId.Value, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> ListCmsScenariosAsync(
        string slug,
        [FromQuery] string? topic,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        if (await cmsContentAdminService.GetContentPackSummaryAsync(slug, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(await cmsContentAdminService.ListScenariosAsync(slug, topic, cancellationToken));
    }

    private static async Task<IResult> GetCmsScenarioAsync(
        string slug,
        string scenarioId,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        var result = await cmsContentAdminService.GetScenarioAsync(slug, scenarioId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpdateCmsScenarioAsync(
        ClaimsPrincipal principal,
        string slug,
        string scenarioId,
        UpdateCmsScenarioRequest request,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        var actorUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await cmsContentAdminService.UpdateScenarioAsync(slug, scenarioId, request, actorUserId.Value, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> ListCmsPromptTemplatesAsync(
        string slug,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        if (await cmsContentAdminService.GetContentPackSummaryAsync(slug, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(await cmsContentAdminService.ListPromptTemplatesAsync(slug, cancellationToken));
    }

    private static async Task<IResult> GetCmsPromptTemplateAsync(
        string slug,
        string templateId,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        var result = await cmsContentAdminService.GetPromptTemplateAsync(slug, templateId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpdateCmsPromptTemplateAsync(
        ClaimsPrincipal principal,
        string slug,
        string templateId,
        UpdateCmsPromptTemplateRequest request,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        var actorUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await cmsContentAdminService.UpdatePromptTemplateAsync(slug, templateId, request, actorUserId.Value, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> ListCmsTutorBehaviorProfilesAsync(
        string slug,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        if (await cmsContentAdminService.GetContentPackSummaryAsync(slug, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(await cmsContentAdminService.ListTutorBehaviorProfilesAsync(slug, cancellationToken));
    }

    private static async Task<IResult> GetCmsTutorBehaviorProfileAsync(
        string slug,
        string profileId,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        var result = await cmsContentAdminService.GetTutorBehaviorProfileAsync(slug, profileId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpdateCmsTutorBehaviorProfileAsync(
        ClaimsPrincipal principal,
        string slug,
        string profileId,
        UpdateCmsTutorBehaviorProfileRequest request,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        var actorUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        try
        {
            var result = await cmsContentAdminService.UpdateTutorBehaviorProfileAsync(slug, profileId, request, actorUserId.Value, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ValidateCmsContentPackAsync(
        string slug,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        var result = await cmsContentAdminService.ValidateDraftAsync(slug, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetCmsPreviewSummaryAsync(
        string slug,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        var result = await cmsContentAdminService.GetPreviewSummaryAsync(slug, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetPublishedCmsContentStatusAsync(
        ICmsPublishedContentService cmsPublishedContentService,
        CancellationToken cancellationToken)
    {
        var result = await cmsPublishedContentService.ReadLatestPublishedContentAsync(cancellationToken);
        return Results.Ok(CmsPublishedContentStatusResponse.FromResult(result));
    }

    private static async Task<IResult> ImportStaticCmsContentAsync(
        ClaimsPrincipal principal,
        ICmsContentImportService cmsContentImportService,
        CancellationToken cancellationToken)
    {
        var adminUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!adminUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await cmsContentImportService.ImportStaticContentAsync(adminUserId.Value, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
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
