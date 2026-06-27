using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Website;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Website;
using Microsoft.AspNetCore.RateLimiting;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class WebsiteAdminEndpoints
{
    public static void MapWebsiteAdminEndpoints(this WebApplication app)
    {
        var rateLimitingEnabled = app.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()?.Enabled == true;
        var get = app.MapGet(ApiConstants.AdminWebsiteContentRoute, async (IWebsiteContentService service, CancellationToken cancellationToken) => Results.Ok(await service.GetAsync(cancellationToken)))
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);
        var save = app.MapPost(ApiConstants.AdminWebsiteContentDraftRoute, async (WebsiteContentSet request, IWebsiteContentService service, CancellationToken cancellationToken) => Results.Ok(await service.SaveDraftAsync(request, cancellationToken)))
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);
        var publish = app.MapPost(ApiConstants.AdminWebsiteContentPublishRoute, async (WebsiteContentSet request, IWebsiteContentService service, CancellationToken cancellationToken) =>
        {
            try { return Results.Ok(await service.PublishAsync(request, cancellationToken)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);
        if (rateLimitingEnabled) { get.RequireRateLimiting(RateLimitingConstants.AdminReadPolicyName); save.RequireRateLimiting(RateLimitingConstants.AdminWritePolicyName); publish.RequireRateLimiting(RateLimitingConstants.AdminWritePolicyName); }
    }
}
