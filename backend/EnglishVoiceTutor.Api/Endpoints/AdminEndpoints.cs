using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Contracts.Cms;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Admin;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Cms;
using EnglishVoiceTutor.Api.Services.WebsiteCms;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        var rateLimitingEnabled = app.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()?.Enabled == true;
        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminMeRoute, GetAdminMe)
            .RequireAuthorization(AdminAuthorizationConstants.AdminSelfReadPermissionPolicyName),
            rateLimitingEnabled);

        app.MapDelete(ApiConstants.AdminSessionRoute, DeleteAdminSessionAsync);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminCapabilitiesRoute, GetAdminCapabilities)
            .RequireAuthorization(AdminAuthorizationConstants.AdminCapabilitiesReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminStatisticsOverviewRoute, GetProductStatisticsOverviewAsync)
            .RequireAuthorization(AdminAuthorizationConstants.ProductStatisticsReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminWebsiteCmsSectionOverviewRoute, GetWebsiteCmsSectionOverviewAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminWebsiteCmsSectionDetailRoute, GetWebsiteCmsSectionDetailAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPut(ApiConstants.AdminWebsiteCmsSectionDraftRoute, SaveWebsiteCmsSectionDraftAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsDraftSavePermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapPost(ApiConstants.AdminWebsiteCmsSectionDraftValidateRoute, ValidateWebsiteCmsSectionDraftAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminWebsiteCmsSectionDraftPreviewRoute, PreviewWebsiteCmsSectionDraftAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPut(ApiConstants.AdminWebsiteCmsSectionReviewStatusRoute, UpdateWebsiteCmsSectionReviewStatusAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsDraftSavePermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPost(ApiConstants.AdminWebsiteCmsSectionPublishRoute, PublishWebsiteCmsSectionAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsDraftSavePermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPost(ApiConstants.AdminWebsiteCmsSectionUnpublishRoute, UnpublishWebsiteCmsSectionAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsDraftSavePermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPost(ApiConstants.AdminWebsiteCmsSectionInitializeMissingRoute, InitializeMissingWebsiteCmsSectionsAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsDraftSavePermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminRoleAssignmentDiagnosticsRoute, GetAdminRoleAssignmentDiagnosticsAsync)
            .RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminRbacCutoverStatusRoute, GetAdminRbacCutoverStatus)
            .RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminRoleAssignmentActorRoute, GetAdminRoleAssignmentActorAsync)
            .RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminRoleManagementRateLimiting(
            app.MapPost(ApiConstants.AdminRoleAssignmentRevokeRoute, RevokeAdminRoleAssignmentAsync)
            .RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminRoleManagementRateLimiting(
            app.MapPost(ApiConstants.AdminRoleAssignmentAssignRoute, AssignAdminRoleAssignmentAsync)
            .RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminRoleManagementRateLimiting(
            app.MapPost(ApiConstants.AdminRoleAssignmentDisableAdminRoute, DisableAdminRoleAssignmentAsync)
            .RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminRoleManagementRateLimiting(
            app.MapPost(ApiConstants.AdminRoleAssignmentEnableAdminRoute, EnableAdminRoleAssignmentAsync)
            .RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminRoleManagementRateLimiting(
            app.MapPost(ApiConstants.AdminRoleAssignmentProvisionAdminUserRoute, ProvisionAdminUserRoleAssignmentAsync)
            .RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminRoleManagementRateLimiting(
            app.MapPost(ApiConstants.AdminRoleAssignmentBootstrapFirstOwnerRoute, BootstrapFirstOwnerAdminRoleAssignmentAsync)
            .RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminUserByEmailRoute, GetAdminUserByEmailAsync)
            .RequireAuthorization(AdminAuthorizationConstants.UserLookupPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminUserByIdRoute, GetAdminUserByIdAsync)
            .RequireAuthorization(AdminAuthorizationConstants.UserOverviewPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPost(ApiConstants.AdminUserPremiumGrantsRoute, GrantManualPremiumAsync)
            .RequireAuthorization(AdminAuthorizationConstants.ManualPremiumGrantPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPost(ApiConstants.AdminUserPremiumGrantRevokeRoute, RevokeManualPremiumAsync)
            .RequireAuthorization(AdminAuthorizationConstants.ManualPremiumRevokePermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminUserAuditActionsRoute, GetTargetUserAuditActionsAsync)
            .RequireAuthorization(AdminAuthorizationConstants.AuditLogViewPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPost(ApiConstants.AdminUserFreeLessonAllowanceResetRoute, ResetFreeLessonAllowanceAsync)
            .RequireAuthorization(AdminAuthorizationConstants.FreeLessonResetPermissionPolicyName),
            rateLimitingEnabled);

        app.MapPost(ApiConstants.AdminUserBillingCancelRenewalRoute, CancelUserBillingRenewalAsync)
            .RequireAuthorization(AdminAuthorizationConstants.BillingCancelRenewalPermissionPolicyName);

        ApplyAdminWriteRateLimiting(
            app.MapPost(ApiConstants.AdminDevCmsStaticContentImportRoute, ImportStaticCmsContentAsync)
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPost(ApiConstants.AdminDevCmsStaticJsonV1InitializeRoute, InitializeStaticJsonV1CmsContentPackAsync)
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsPublishedContentStatusRoute, GetPublishedCmsContentStatusAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsRuntimeContentStatusRoute, GetRuntimeCmsContentStatusAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsRuntimeStatusReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsRuntimeStatusRoute, GetRuntimeCmsContentStatusAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsRuntimeStatusReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPacksRoute, ListCmsContentPacksAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPackRoute, GetCmsContentPackSummaryAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPackTopicsRoute, ListCmsTopicsAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPackTopicRoute, GetCmsTopicAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPut(ApiConstants.AdminDevCmsContentPackTopicRoute, UpdateCmsTopicAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsDraftSavePermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPackScenariosRoute, ListCmsScenariosAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPackScenarioRoute, GetCmsScenarioAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPut(ApiConstants.AdminDevCmsContentPackScenarioRoute, UpdateCmsScenarioAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsDraftSavePermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPackPromptTemplatesRoute, ListCmsPromptTemplatesAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPackPromptTemplateRoute, GetCmsPromptTemplateAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPut(ApiConstants.AdminDevCmsContentPackPromptTemplateRoute, UpdateCmsPromptTemplateAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsDraftSavePermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPackTutorBehaviorProfilesRoute, ListCmsTutorBehaviorProfilesAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPackTutorBehaviorProfileRoute, GetCmsTutorBehaviorProfileAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPut(ApiConstants.AdminDevCmsContentPackTutorBehaviorProfileRoute, UpdateCmsTutorBehaviorProfileAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsDraftSavePermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsAuditEntriesRoute, ListCmsAuditEntriesAsync)
            .RequireAuthorization(AdminAuthorizationConstants.AuditLogViewPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPackAuditEntriesRoute, ListCmsContentPackAuditEntriesAsync)
            .RequireAuthorization(AdminAuthorizationConstants.AuditLogViewPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPost(ApiConstants.AdminDevCmsContentPackValidateRoute, ValidateCmsContentPackAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPackPreviewSummaryRoute, GetCmsPreviewSummaryAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPackVersionsRoute, ListCmsContentVersionsAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminReadRateLimiting(
            app.MapGet(ApiConstants.AdminDevCmsContentPackVersionRoute, GetCmsContentVersionAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPost(ApiConstants.AdminDevCmsContentPackPublishRoute, PublishCmsContentPackAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsPublishPermissionPolicyName),
            rateLimitingEnabled);

        ApplyAdminWriteRateLimiting(
            app.MapPost(ApiConstants.AdminDevCmsContentPackVersionRestoreRoute, RestoreCmsContentVersionAsync)
            .RequireAuthorization(AdminAuthorizationConstants.CmsRestorePermissionPolicyName),
            rateLimitingEnabled);
    }


    private static void ApplyAdminReadRateLimiting(RouteHandlerBuilder builder, bool rateLimitingEnabled)
    {
        if (rateLimitingEnabled)
        {
            builder.RequireRateLimiting(RateLimitingConstants.AdminReadPolicyName);
        }
    }

    private static void ApplyAdminWriteRateLimiting(RouteHandlerBuilder builder, bool rateLimitingEnabled)
    {
        if (rateLimitingEnabled)
        {
            builder.RequireRateLimiting(RateLimitingConstants.AdminWritePolicyName);
        }
    }

    private static void ApplyAdminRoleManagementRateLimiting(RouteHandlerBuilder builder, bool rateLimitingEnabled)
    {
        if (rateLimitingEnabled)
        {
            builder.RequireRateLimiting(RateLimitingConstants.AdminRoleManagementPolicyName);
        }
    }

    private static async Task DeleteAdminSessionAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(AdminAuthorizationConstants.AdminCookieAuthenticationScheme);
        httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private static IResult GetAdminMe(
        ClaimsPrincipal principal,
        IAdminRolePermissionCatalogService adminRolePermissionCatalogService)
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
            Roles = adminRolePermissionCatalogService.GetBootstrapAdminRoles(),
            Permissions = adminRolePermissionCatalogService.GetBootstrapAdminPermissions(),
            IsBootstrapAdmin = true,
            CheckedAtUtc = DateTimeOffset.UtcNow
        };

        return Results.Ok(response);
    }


    private static IResult GetAdminCapabilities(IAdminCapabilitiesService adminCapabilitiesService)
    {
        return Results.Ok(adminCapabilitiesService.GetCapabilities());
    }

    private static async Task<IResult> GetWebsiteCmsSectionOverviewAsync(
        IWebsiteCmsAdminReadService websiteCmsAdminReadService,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await websiteCmsAdminReadService.GetSectionOverviewAsync(cancellationToken));
    }

    private static async Task<IResult> GetWebsiteCmsSectionDetailAsync(
        string sectionKey,
        IWebsiteCmsAdminReadService websiteCmsAdminReadService,
        CancellationToken cancellationToken)
    {
        var result = await websiteCmsAdminReadService.GetSectionDetailAsync(sectionKey, cancellationToken);
        return result is null ? Results.NotFound(new { error = "Unknown Website CMS section key." }) : Results.Ok(result);
    }

    private static async Task<IResult> SaveWebsiteCmsSectionDraftAsync(
        string sectionKey,
        AdminWebsiteCmsSectionDraftSaveRequest request,
        IWebsiteCmsAdminMutationService websiteCmsAdminMutationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await websiteCmsAdminMutationService.SaveDraftAsync(sectionKey, request, cancellationToken);
            return result is null ? Results.NotFound(new { error = "Unknown Website CMS section key." }) : Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }


    private static async Task<IResult> ValidateWebsiteCmsSectionDraftAsync(
        string sectionKey,
        IWebsiteCmsAdminReadService websiteCmsAdminReadService,
        CancellationToken cancellationToken)
    {
        var result = await websiteCmsAdminReadService.ValidateDraftAsync(sectionKey, cancellationToken);
        return result is null ? Results.NotFound(new { error = "Unknown Website CMS section key." }) : Results.Ok(result);
    }

    private static async Task<IResult> PreviewWebsiteCmsSectionDraftAsync(
        string sectionKey,
        IWebsiteCmsAdminReadService websiteCmsAdminReadService,
        CancellationToken cancellationToken)
    {
        var result = await websiteCmsAdminReadService.GetDraftPreviewAsync(sectionKey, cancellationToken);
        return result is null ? Results.NotFound(new { error = "Unknown Website CMS section key." }) : Results.Ok(result);
    }

    private static async Task<IResult> UpdateWebsiteCmsSectionReviewStatusAsync(
        string sectionKey,
        AdminWebsiteCmsSectionReviewStatusUpdateRequest request,
        IWebsiteCmsAdminMutationService websiteCmsAdminMutationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await websiteCmsAdminMutationService.UpdateReviewStatusAsync(sectionKey, request, cancellationToken);
            return result is null ? Results.NotFound(new { error = "Unknown Website CMS section key." }) : Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }


    private static async Task<IResult> PublishWebsiteCmsSectionAsync(
        string sectionKey,
        AdminWebsiteCmsSectionPublishRequest request,
        IWebsiteCmsAdminMutationService websiteCmsAdminMutationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await websiteCmsAdminMutationService.PublishSectionAsync(sectionKey, request, cancellationToken);
            return result is null ? Results.NotFound(new { error = "Unknown Website CMS section key." }) : Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UnpublishWebsiteCmsSectionAsync(
        string sectionKey,
        AdminWebsiteCmsSectionUnpublishRequest request,
        IWebsiteCmsAdminMutationService websiteCmsAdminMutationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await websiteCmsAdminMutationService.UnpublishSectionAsync(sectionKey, request, cancellationToken);
            return result is null ? Results.NotFound(new { error = "Unknown Website CMS section key." }) : Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> InitializeMissingWebsiteCmsSectionsAsync(
        IWebsiteCmsAdminMutationService websiteCmsAdminMutationService,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await websiteCmsAdminMutationService.InitializeMissingSectionsAsync(cancellationToken));
    }

    private static async Task<IResult> GetProductStatisticsOverviewAsync(
        IAdminProductStatisticsService adminProductStatisticsService,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await adminProductStatisticsService.GetOverviewAsync(cancellationToken));
    }

    private static async Task<IResult> GetAdminRoleAssignmentDiagnosticsAsync(
        IAdminRoleAssignmentDiagnosticsService adminRoleAssignmentDiagnosticsService,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await adminRoleAssignmentDiagnosticsService.GetDiagnosticsAsync(cancellationToken));
    }

    private static IResult GetAdminRbacCutoverStatus(IConfiguration configuration)
    {
        return Results.Ok(AdminRbacCutoverStatusReader.GetStatus(configuration));
    }

    private static async Task<IResult> GetAdminRoleAssignmentActorAsync(
        ClaimsPrincipal principal,
        IAdminRoleAssignmentActorResolver adminRoleAssignmentActorResolver,
        CancellationToken cancellationToken)
    {
        var actorResolution = await adminRoleAssignmentActorResolver.ResolveActorAsync(principal, cancellationToken);
        var response = new AdminRoleAssignmentActorResponse
        {
            IsActorMappingFound = actorResolution.IsActorMappingFound,
            ActorAdminUserId = actorResolution.ActorAdminUserId,
            RoleIds = actorResolution.ActorRoleIds,
            ErrorCode = actorResolution.ErrorCode,
            Message = actorResolution.Message,
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };

        return Results.Ok(response);
    }


    private static async Task<IResult> BootstrapFirstOwnerAdminRoleAssignmentAsync(
        [FromBody] AdminRoleAssignmentBootstrapFirstOwnerRequest request,
        ClaimsPrincipal principal,
        IAdminRoleAssignmentBootstrapService adminRoleAssignmentBootstrapService,
        CancellationToken cancellationToken)
    {
        var appUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!appUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await adminRoleAssignmentBootstrapService.BootstrapFirstOwnerAsync(new AdminRoleAssignmentBootstrapRequest(
            appUserId.Value,
            NormalizeTrustedEmail(ClaimsUserAccessor.TryGetUserEmail(principal)),
            request.Reason ?? string.Empty,
            request.SafeMetadataJson), cancellationToken);

        var response = ToAdminRoleAssignmentBootstrapFirstOwnerResponse(result);
        return result.IsSuccess ? Results.Ok(response) : Results.Conflict(response);
    }

    private static AdminRoleAssignmentBootstrapFirstOwnerResponse ToAdminRoleAssignmentBootstrapFirstOwnerResponse(AdminRoleAssignmentBootstrapResult result) => new()
    {
        Success = result.IsSuccess,
        ErrorCode = result.ErrorCode,
        Message = result.Message,
        AdminUserId = result.AdminUserId,
        RoleId = result.RoleId,
        AuditEventId = result.AuditEventId,
        OccurredAtUtc = result.OccurredAtUtc
    };

    private static string? NormalizeTrustedEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }

    private static async Task<IResult> RevokeAdminRoleAssignmentAsync(
        [FromBody] AdminRoleAssignmentRevokeRequest request,
        ClaimsPrincipal principal,
        IAdminRoleAssignmentActorResolver adminRoleAssignmentActorResolver,
        IAdminRoleAssignmentWriteService adminRoleAssignmentWriteService,
        CancellationToken cancellationToken)
    {
        var actorResolution = await adminRoleAssignmentActorResolver.ResolveActorAsync(principal, cancellationToken);
        if (!actorResolution.IsActorMappingFound || !actorResolution.ActorAdminUserId.HasValue)
        {
            return Results.Conflict(new AdminRoleAssignmentRevokeResponse
            {
                Success = false,
                ErrorCode = actorResolution.ErrorCode ?? AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode,
                Message = actorResolution.Message ?? "Persistent Admin actor mapping is not available for the authenticated principal, so role assignment revocation is disabled until safe actor identity and actor role resolution are available.",
                AuditEventId = null,
                TargetAdminUserId = request.TargetAdminUserId,
                RoleId = request.RoleId,
                OccurredAtUtc = DateTimeOffset.UtcNow
            });
        }

        var writeResult = await adminRoleAssignmentWriteService.RevokeRoleAsync(new AdminRoleAssignmentWriteRequest(
            actorResolution.ActorAdminUserId.Value,
            request.TargetAdminUserId,
            request.RoleId,
            actorResolution.ActorRoleIds,
            request.Reason,
            request.SafeMetadataJson), cancellationToken);

        var response = ToAdminRoleAssignmentRevokeResponse(writeResult);
        return writeResult.IsSuccess ? Results.Ok(response) : Results.Conflict(response);
    }

    private static AdminRoleAssignmentRevokeResponse ToAdminRoleAssignmentRevokeResponse(AdminRoleAssignmentWriteResult result) => new()
    {
        Success = result.IsSuccess,
        ErrorCode = result.ErrorCode,
        Message = result.Message,
        AuditEventId = result.AuditEventId,
        TargetAdminUserId = result.TargetAdminUserId,
        RoleId = result.RoleId,
        OccurredAtUtc = result.OccurredAtUtc
    };

    private static async Task<IResult> AssignAdminRoleAssignmentAsync(
        [FromBody] AdminRoleAssignmentAssignRequest request,
        ClaimsPrincipal principal,
        IAdminRoleAssignmentActorResolver adminRoleAssignmentActorResolver,
        IAdminRoleAssignmentWriteService adminRoleAssignmentWriteService,
        CancellationToken cancellationToken)
    {
        var actorResolution = await adminRoleAssignmentActorResolver.ResolveActorAsync(principal, cancellationToken);
        if (!actorResolution.IsActorMappingFound || !actorResolution.ActorAdminUserId.HasValue)
        {
            return Results.Conflict(new AdminRoleAssignmentAssignResponse
            {
                Success = false,
                ErrorCode = actorResolution.ErrorCode ?? AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode,
                Message = actorResolution.Message ?? "Persistent Admin actor mapping is not available for the authenticated principal, so role assignment creation is disabled until safe actor identity and actor role resolution are available.",
                AuditEventId = null,
                TargetAdminUserId = request.TargetAdminUserId,
                RoleId = request.RoleId,
                OccurredAtUtc = DateTimeOffset.UtcNow
            });
        }

        var writeResult = await adminRoleAssignmentWriteService.AssignRoleAsync(new AdminRoleAssignmentWriteRequest(
            actorResolution.ActorAdminUserId.Value,
            request.TargetAdminUserId,
            request.RoleId,
            actorResolution.ActorRoleIds,
            request.Reason,
            request.SafeMetadataJson), cancellationToken);

        var response = ToAdminRoleAssignmentAssignResponse(writeResult);
        return writeResult.IsSuccess ? Results.Ok(response) : Results.Conflict(response);
    }

    private static AdminRoleAssignmentAssignResponse ToAdminRoleAssignmentAssignResponse(AdminRoleAssignmentWriteResult result) => new()
    {
        Success = result.IsSuccess,
        ErrorCode = result.ErrorCode,
        Message = result.Message,
        AuditEventId = result.AuditEventId,
        TargetAdminUserId = result.TargetAdminUserId,
        RoleId = result.RoleId,
        OccurredAtUtc = result.OccurredAtUtc
    };


    private static async Task<IResult> DisableAdminRoleAssignmentAsync(
        [FromBody] AdminRoleAssignmentDisableAdminRequest request,
        ClaimsPrincipal principal,
        IAdminRoleAssignmentActorResolver adminRoleAssignmentActorResolver,
        IAdminRoleAssignmentWriteService adminRoleAssignmentWriteService,
        CancellationToken cancellationToken)
    {
        var actorResolution = await adminRoleAssignmentActorResolver.ResolveActorAsync(principal, cancellationToken);
        if (!actorResolution.IsActorMappingFound || !actorResolution.ActorAdminUserId.HasValue)
        {
            return Results.Conflict(new AdminRoleAssignmentDisableAdminResponse
            {
                Success = false,
                ErrorCode = actorResolution.ErrorCode ?? AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode,
                Message = actorResolution.Message ?? "Persistent Admin actor mapping is not available for the authenticated principal, so admin disablement is disabled until safe actor identity and actor role resolution are available.",
                AuditEventId = null,
                TargetAdminUserId = request.TargetAdminUserId,
                OccurredAtUtc = DateTimeOffset.UtcNow
            });
        }

        var writeResult = await adminRoleAssignmentWriteService.DisableAdminAsync(new AdminRoleAssignmentWriteRequest(
            actorResolution.ActorAdminUserId.Value,
            request.TargetAdminUserId,
            null,
            actorResolution.ActorRoleIds,
            request.Reason,
            request.SafeMetadataJson), cancellationToken);

        var response = ToAdminRoleAssignmentDisableAdminResponse(writeResult);
        return writeResult.IsSuccess ? Results.Ok(response) : Results.Conflict(response);
    }

    private static AdminRoleAssignmentDisableAdminResponse ToAdminRoleAssignmentDisableAdminResponse(AdminRoleAssignmentWriteResult result) => new()
    {
        Success = result.IsSuccess,
        ErrorCode = result.ErrorCode,
        Message = result.Message,
        AuditEventId = result.AuditEventId,
        TargetAdminUserId = result.TargetAdminUserId,
        OccurredAtUtc = result.OccurredAtUtc
    };


    private static async Task<IResult> EnableAdminRoleAssignmentAsync(
        [FromBody] AdminRoleAssignmentEnableAdminRequest request,
        ClaimsPrincipal principal,
        IAdminRoleAssignmentActorResolver adminRoleAssignmentActorResolver,
        IAdminRoleAssignmentWriteService adminRoleAssignmentWriteService,
        CancellationToken cancellationToken)
    {
        var actorResolution = await adminRoleAssignmentActorResolver.ResolveActorAsync(principal, cancellationToken);
        if (!actorResolution.IsActorMappingFound || !actorResolution.ActorAdminUserId.HasValue)
        {
            return Results.Conflict(new AdminRoleAssignmentEnableAdminResponse
            {
                Success = false,
                ErrorCode = actorResolution.ErrorCode ?? AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode,
                Message = actorResolution.Message ?? "Persistent Admin actor mapping is not available for the authenticated principal, so admin enablement is disabled until safe actor identity and actor role resolution are available.",
                AuditEventId = null,
                TargetAdminUserId = request.TargetAdminUserId,
                OccurredAtUtc = DateTimeOffset.UtcNow
            });
        }

        var writeResult = await adminRoleAssignmentWriteService.EnableAdminAsync(new AdminRoleAssignmentWriteRequest(
            actorResolution.ActorAdminUserId.Value,
            request.TargetAdminUserId,
            null,
            actorResolution.ActorRoleIds,
            request.Reason,
            request.SafeMetadataJson), cancellationToken);

        var response = ToAdminRoleAssignmentEnableAdminResponse(writeResult);
        return writeResult.IsSuccess ? Results.Ok(response) : Results.Conflict(response);
    }

    private static AdminRoleAssignmentEnableAdminResponse ToAdminRoleAssignmentEnableAdminResponse(AdminRoleAssignmentWriteResult result) => new()
    {
        Success = result.IsSuccess,
        ErrorCode = result.ErrorCode,
        Message = result.Message,
        AuditEventId = result.AuditEventId,
        TargetAdminUserId = result.TargetAdminUserId,
        OccurredAtUtc = result.OccurredAtUtc
    };


    private static async Task<IResult> ProvisionAdminUserRoleAssignmentAsync(
        [FromBody] AdminRoleAssignmentProvisionAdminUserRequest request,
        ClaimsPrincipal principal,
        IAdminRoleAssignmentActorResolver adminRoleAssignmentActorResolver,
        IAdminRoleAssignmentAdminUserProvisioningService adminRoleAssignmentAdminUserProvisioningService,
        CancellationToken cancellationToken)
    {
        var actorResolution = await adminRoleAssignmentActorResolver.ResolveActorAsync(principal, cancellationToken);
        if (!actorResolution.IsActorMappingFound || !actorResolution.ActorAdminUserId.HasValue)
        {
            return Results.Conflict(new AdminRoleAssignmentProvisionAdminUserResponse
            {
                Success = false,
                ErrorCode = actorResolution.ErrorCode ?? AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode,
                Message = actorResolution.Message ?? "Persistent Admin actor mapping is not available for the authenticated principal, so AdminUser provisioning is disabled until safe actor identity and actor role resolution are available.",
                AdminUserId = null,
                AuditEventId = null,
                OccurredAtUtc = DateTimeOffset.UtcNow
            });
        }

        var provisioningResult = await adminRoleAssignmentAdminUserProvisioningService.ProvisionAdminUserAsync(new AdminRoleAssignmentAdminUserProvisioningRequest(
            actorResolution.ActorAdminUserId.Value,
            actorResolution.ActorRoleIds,
            request.TargetAppUserId,
            null,
            request.Reason ?? string.Empty,
            request.SafeMetadataJson), cancellationToken);

        var response = ToAdminRoleAssignmentProvisionAdminUserResponse(provisioningResult);
        return provisioningResult.IsSuccess ? Results.Ok(response) : Results.Conflict(response);
    }

    private static AdminRoleAssignmentProvisionAdminUserResponse ToAdminRoleAssignmentProvisionAdminUserResponse(AdminRoleAssignmentAdminUserProvisioningResult result) => new()
    {
        Success = result.IsSuccess,
        ErrorCode = result.ErrorCode,
        Message = result.Message,
        AdminUserId = result.AdminUserId,
        AuditEventId = result.AuditEventId,
        OccurredAtUtc = result.OccurredAtUtc
    };

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

        return ToUserLookupResult(lookupResult);
    }

    private static async Task<IResult> GetAdminUserByIdAsync(
        Guid userId,
        IAdminUserLookupService adminUserLookupService,
        CancellationToken cancellationToken)
    {
        var lookupResult = await adminUserLookupService.GetByIdAsync(userId, cancellationToken);
        return ToUserLookupResult(lookupResult);
    }

    private static IResult ToUserLookupResult(AdminUserLookupResult lookupResult)
    {
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
        HttpContext httpContext,
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

        var result = await cmsContentAdminService.UpdateTopicAsync(slug, topicId, request, actorUserId.Value, ClaimsUserAccessor.TryGetUserEmail(principal), httpContext.TraceIdentifier, cancellationToken);
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
        HttpContext httpContext,
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

        try
        {
            var result = await cmsContentAdminService.UpdateScenarioAsync(slug, scenarioId, request, actorUserId.Value, ClaimsUserAccessor.TryGetUserEmail(principal), httpContext.TraceIdentifier, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
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
        HttpContext httpContext,
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

        var result = await cmsContentAdminService.UpdatePromptTemplateAsync(slug, templateId, request, actorUserId.Value, ClaimsUserAccessor.TryGetUserEmail(principal), httpContext.TraceIdentifier, cancellationToken);
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
        HttpContext httpContext,
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
            var result = await cmsContentAdminService.UpdateTutorBehaviorProfileAsync(slug, profileId, request, actorUserId.Value, ClaimsUserAccessor.TryGetUserEmail(principal), httpContext.TraceIdentifier, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ListCmsAuditEntriesAsync(
        [FromQuery] string? contentPackSlug,
        [FromQuery] string? entityType,
        [FromQuery] string? stableKey,
        [FromQuery] int? limit,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await cmsContentAdminService.ListAuditEntriesAsync(contentPackSlug, entityType, stableKey, limit, cancellationToken));
    }

    private static async Task<IResult> ListCmsContentPackAuditEntriesAsync(
        string slug,
        [FromQuery] string? entityType,
        [FromQuery] string? stableKey,
        [FromQuery] int? limit,
        ICmsContentAdminService cmsContentAdminService,
        CancellationToken cancellationToken)
    {
        if (await cmsContentAdminService.GetContentPackSummaryAsync(slug, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(await cmsContentAdminService.ListAuditEntriesAsync(slug, entityType, stableKey, limit, cancellationToken));
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


    private static async Task<IResult> ListCmsContentVersionsAsync(
        string slug,
        ICmsContentPublishingService cmsContentPublishingService,
        CancellationToken cancellationToken)
    {
        var result = await cmsContentPublishingService.ListVersionsAsync(slug, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetCmsContentVersionAsync(
        string slug,
        int versionNumber,
        ICmsContentPublishingService cmsContentPublishingService,
        CancellationToken cancellationToken)
    {
        var result = await cmsContentPublishingService.GetVersionAsync(slug, versionNumber, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> PublishCmsContentPackAsync(
        ClaimsPrincipal principal,
        string slug,
        PublishCmsContentRequest request,
        ICmsContentPublishingService cmsContentPublishingService,
        CancellationToken cancellationToken)
    {
        var actorUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await cmsContentPublishingService.PublishDraftAsync(slug, request, actorUserId.Value, cancellationToken);
        if (result is null)
        {
            return Results.NotFound();
        }

        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> RestoreCmsContentVersionAsync(
        ClaimsPrincipal principal,
        string slug,
        int versionNumber,
        RestoreCmsContentVersionRequest request,
        ICmsContentPublishingService cmsContentPublishingService,
        CancellationToken cancellationToken)
    {
        var actorUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await cmsContentPublishingService.RestoreVersionAsync(slug, versionNumber, request, actorUserId.Value, cancellationToken);
        if (result is null)
        {
            return Results.NotFound();
        }

        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> GetPublishedCmsContentStatusAsync(
        ICmsPublishedContentService cmsPublishedContentService,
        CancellationToken cancellationToken)
    {
        var result = await cmsPublishedContentService.ReadLatestPublishedContentAsync(cancellationToken);
        return Results.Ok(CmsPublishedContentStatusResponse.FromResult(result));
    }

    private static async Task<IResult> GetRuntimeCmsContentStatusAsync(
        ICmsRuntimeLessonContentService cmsRuntimeLessonContentService,
        CancellationToken cancellationToken)
    {
        var result = await cmsRuntimeLessonContentService.ReadRuntimeLessonContentAsync(cancellationToken);
        var response = CmsRuntimeLessonContentStatusResponse.FromResult(result);
        return Results.Ok(response);
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

    private static async Task<IResult> InitializeStaticJsonV1CmsContentPackAsync(
        ClaimsPrincipal principal,
        ICmsContentImportService cmsContentImportService,
        CancellationToken cancellationToken)
    {
        var adminUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!adminUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await cmsContentImportService.InitializeStaticJsonV1DraftAsync(adminUserId.Value, cancellationToken);
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



    private static async Task<IResult> CancelUserBillingRenewalAsync(
        ClaimsPrincipal principal,
        Guid userId,
        AdminBillingCancelRenewalRequest request,
        IAdminBillingCancellationService adminBillingCancellationService,
        CancellationToken cancellationToken)
    {
        var adminUserId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!adminUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await adminBillingCancellationService.CancelRenewalAsync(
            adminUserId.Value,
            userId,
            request,
            cancellationToken);

        if (result.IsInvalid)
        {
            return Results.BadRequest(new Dictionary<string, string[]>
            {
                ["reason"] = [result.ErrorMessage ?? string.Empty]
            });
        }

        if (result.IsNotFound)
        {
            return Results.NotFound(new Dictionary<string, string[]>
            {
                [UserIdRouteKey] = [result.ErrorMessage ?? "Selected user was not found."]
            });
        }

        return Results.Ok(result.Response);
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
