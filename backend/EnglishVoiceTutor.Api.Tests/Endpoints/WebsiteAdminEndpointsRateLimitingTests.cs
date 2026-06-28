using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Tests.Endpoints;

public sealed class WebsiteAdminEndpointsRateLimitingTests
{
    [Fact]
    public void WebsiteAdminCmsEndpointsKeepSuperAdminAuthorizationButDoNotUseAdminRateLimiter()
    {
        var source = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Endpoints/WebsiteAdminEndpoints.cs");

        Assert.Contains(nameof(ApiConstants.AdminWebsiteContentRoute), source, StringComparison.Ordinal);
        Assert.Contains(nameof(ApiConstants.AdminWebsiteContentDraftRoute), source, StringComparison.Ordinal);
        Assert.Contains(nameof(ApiConstants.AdminWebsiteContentPreviewRoute), source, StringComparison.Ordinal);
        Assert.Contains(nameof(ApiConstants.AdminWebsiteContentPublishRoute), source, StringComparison.Ordinal);
        Assert.Equal(4, CountOccurrences(source, ".RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName)"));
        Assert.DoesNotContain("RequireRateLimiting", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminReadPolicyName", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminWritePolicyName", source, StringComparison.Ordinal);
    }

    [Fact]
    public void OtherAdminEndpointsStillUseAdminRateLimiterPolicies()
    {
        var source = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs");

        Assert.Contains("RequireRateLimiting(RateLimitingConstants.AdminReadPolicyName)", source, StringComparison.Ordinal);
        Assert.Contains("RequireRateLimiting(RateLimitingConstants.AdminWritePolicyName)", source, StringComparison.Ordinal);
        Assert.Contains("RequireRateLimiting(RateLimitingConstants.AdminRoleManagementPolicyName)", source, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath)))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}
