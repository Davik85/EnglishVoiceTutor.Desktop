using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Tests.Endpoints;

public sealed class AdminFeedbackReportEndpointsStaticTests
{
    [Fact]
    public void BothFeedbackReportEndpointsRequireTheFeedbackReportsReadPolicy()
    {
        var source = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Endpoints/AdminFeedbackReportEndpoints.cs");

        Assert.Contains(nameof(ApiConstants.AdminFeedbackReportsRoute), source, StringComparison.Ordinal);
        Assert.Contains(nameof(ApiConstants.AdminFeedbackReportByIdRoute), source, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(source, ".RequireAuthorization(AdminAuthorizationConstants.FeedbackReportsReadPermissionPolicyName)"));
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
