using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Cms;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class CmsDiagnosticsEndpoints
{
    public static void MapCmsDiagnosticsEndpoints(this WebApplication app)
    {
        app.MapGet(ApiConstants.CmsRuntimeContentSourceStatusRoute, GetRuntimeContentSourceStatus);
    }

    private static IResult GetRuntimeContentSourceStatus(
        IOptions<CmsContentOptions> optionsAccessor,
        IWebHostEnvironment environment)
    {
        var options = optionsAccessor.Value;
        return Results.Ok(new CmsRuntimeContentSourceStatusResponse
        {
            EnvironmentName = environment.EnvironmentName,
            RuntimeSource = options.UsePublishedSnapshotForRuntime
                ? CmsContentConstants.Sources.CmsPublishedSnapshot
                : CmsContentConstants.Sources.StaticJson,
            ReadPublishedSnapshotEnabled = options.ReadPublishedSnapshotEnabled,
            UsePublishedSnapshotForRuntime = options.UsePublishedSnapshotForRuntime,
            FallbackToStaticJson = options.FallbackToStaticJson,
            ContentPackSlug = options.ContentPackSlug
        });
    }
}
