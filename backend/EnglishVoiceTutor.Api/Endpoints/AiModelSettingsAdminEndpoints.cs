using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class AiModelSettingsAdminEndpoints
{
    public static void MapAiModelSettingsAdminEndpoints(this WebApplication app)
    {
        app.MapGet(ApiConstants.AdminAiModelSettingsRoute, async (IAiModelSettingsService service, CancellationToken cancellationToken) => Results.Ok(await service.GetAsync(cancellationToken)))
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

        app.MapPost(ApiConstants.AdminAiModelSettingsDraftRoute, async (AiModelSettings request, HttpContext httpContext, IAiModelSettingsService service, CancellationToken cancellationToken) =>
        {
            try { return Results.Ok(await service.SaveDraftAsync(request, ResolveUpdatedBy(httpContext), cancellationToken)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

        app.MapPost(ApiConstants.AdminAiModelSettingsValidateRoute, (AiModelSettings request, IAiModelSettingsService service) => Results.Ok(service.Validate(request)))
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

        app.MapPost(ApiConstants.AdminAiModelSettingsPublishRoute, async (HttpContext httpContext, IAiModelSettingsService service, CancellationToken cancellationToken) =>
        {
            try { return Results.Ok(await service.PublishAsync(ResolveUpdatedBy(httpContext), cancellationToken)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

        app.MapPost(ApiConstants.AdminAiModelSettingsResetDraftRoute, async (HttpContext httpContext, IAiModelSettingsService service, CancellationToken cancellationToken) => Results.Ok(await service.ResetDraftFromActiveAsync(ResolveUpdatedBy(httpContext), cancellationToken)))
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);
    }

    private static string? ResolveUpdatedBy(HttpContext httpContext) =>
        httpContext.User.Identity?.Name ?? httpContext.User.FindFirst("sub")?.Value;
}
