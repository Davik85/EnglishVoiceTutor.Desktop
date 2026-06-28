using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Website;
using EnglishVoiceTutor.Api.Services.Website;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class WebsiteAdminEndpoints
{
    public static void MapWebsiteAdminEndpoints(this WebApplication app)
    {
        app.MapGet(ApiConstants.AdminWebsiteContentRoute, async (IWebsiteContentService service, CancellationToken cancellationToken) => Results.Ok(await service.GetAsync(cancellationToken)))
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);
        app.MapPost(ApiConstants.AdminWebsiteContentDraftRoute, async (WebsiteContentSet request, IWebsiteContentService service, CancellationToken cancellationToken) => Results.Ok(await service.SaveDraftAsync(request, cancellationToken)))
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);
        app.MapPost(ApiConstants.AdminWebsiteContentPreviewRoute, async (WebsitePreviewRequest request, IWebsiteContentService service, CancellationToken cancellationToken) =>
        {
            try { return Results.Ok(await service.PreviewAsync(request, cancellationToken)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);
        app.MapPost(ApiConstants.AdminWebsiteContentPublishRoute, async (IWebsiteContentService service, CancellationToken cancellationToken) =>
        {
            try { return Results.Ok(await service.PublishAsync(cancellationToken)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);
    }
}
