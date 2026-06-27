using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Services.WebsiteCms;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class WebsiteEndpoints
{
    public static void MapWebsiteEndpoints(this WebApplication app)
    {
        app.MapGet(ApiConstants.WebsiteTextsRoute, GetWebsiteTextsAsync).AllowAnonymous();
    }

    private static async Task<IResult> GetWebsiteTextsAsync(
        IWebsiteCmsPublicReadService websiteCmsPublicReadService,
        CancellationToken cancellationToken)
    {
        var response = await websiteCmsPublicReadService.GetPublicTextsAsync(cancellationToken);
        return Results.Ok(response);
    }
}
