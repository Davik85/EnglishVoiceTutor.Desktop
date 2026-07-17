using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Tests.Endpoints;

public sealed class AdminFeedbackReportStatusEndpointsStaticTests
{
    [Fact]
    public void StatusPatchRequiresStatusManageAndNotReadAuthorization()
    {
        var source = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Endpoints/AdminFeedbackReportEndpoints.cs");

        Assert.Contains(nameof(ApiConstants.AdminFeedbackReportStatusRoute), source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization(AdminAuthorizationConstants.FeedbackReportsStatusManagePermissionPolicyName)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPatch(ApiConstants.AdminFeedbackReportStatusRoute, ChangeStatusAsync)\n            .RequireAuthorization(AdminAuthorizationConstants.FeedbackReportsReadPermissionPolicyName)", source, StringComparison.Ordinal);
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
}
