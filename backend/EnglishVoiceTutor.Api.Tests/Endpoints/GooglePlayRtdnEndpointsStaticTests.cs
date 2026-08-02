using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Options;

namespace EnglishVoiceTutor.Api.Tests.Endpoints;

public sealed class GooglePlayRtdnEndpointsStaticTests
{
    [Fact]
    public void ReceiverIsConditionallyMappedWithoutApplicationAuthorization()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "EnglishVoiceTutor.Api", "Endpoints", "GooglePlayRtdnEndpoints.cs"));
        Assert.Contains("if (!options.Enabled) return;", source, StringComparison.Ordinal);
        Assert.Contains("options.ValidateForEnabledMode();", source, StringComparison.Ordinal);
        Assert.Contains("Google Play RTDN requires a configured Google Play package name.", source, StringComparison.Ordinal);
        Assert.Contains("app.MapPost(ApiConstants.GooglePlayRtdnPushRoute, ReceiveAsync);", source, StringComparison.Ordinal);
        Assert.Contains("GooglePlayRtdnPushReceiptStatus.TemporarilyUnavailable => Results.StatusCode(StatusCodes.Status503ServiceUnavailable)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RequireAuthorization", source, StringComparison.Ordinal);
    }
}
