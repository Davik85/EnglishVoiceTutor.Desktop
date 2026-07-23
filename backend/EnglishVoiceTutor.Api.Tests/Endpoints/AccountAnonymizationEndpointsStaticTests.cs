namespace EnglishVoiceTutor.Api.Tests.Endpoints;

public sealed class AccountAnonymizationEndpointsStaticTests
{
    [Fact]
    public void RoutesApplyExecutePermissionAndAdminWriteRateLimiting()
    {
        var source = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Endpoints/AccountAnonymizationEndpoints.cs");

        Assert.Contains("AdminFeedbackReportAccountAnonymizationPreflightRoute", source, StringComparison.Ordinal);
        Assert.Contains("AdminFeedbackReportAccountAnonymizationRoute", source, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(source, "AccountAnonymizationPreflightReadPermissionPolicyName"));
        Assert.Contains("createPreflightEndpoint.RequireRateLimiting(RateLimitingConstants.AdminWritePolicyName)", source, StringComparison.Ordinal);
        Assert.Contains("statusEndpoint.RequireRateLimiting(RateLimitingConstants.AdminReadPolicyName)", source, StringComparison.Ordinal);
        Assert.Contains("AccountAnonymizationExecutePermissionPolicyName", source, StringComparison.Ordinal);
        Assert.Contains("AdminFeedbackReportAccountAnonymizationExecuteRoute", source, StringComparison.Ordinal);
        Assert.Contains("executeEndpoint.RequireRateLimiting(RateLimitingConstants.AdminWritePolicyName)", source, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath))) directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory!.FullName, relativePath));
    }
}
