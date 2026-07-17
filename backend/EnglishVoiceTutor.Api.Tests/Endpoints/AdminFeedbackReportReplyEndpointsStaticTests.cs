using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Tests.Endpoints;

public sealed class AdminFeedbackReportReplyEndpointsStaticTests
{
    [Fact]
    public void ReplyPostUsesOnlyTheReplyPermissionPolicy()
    {
        var source = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Endpoints/AdminFeedbackReportEndpoints.cs");

        Assert.Contains(nameof(ApiConstants.AdminFeedbackReportRepliesRoute), source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization(AdminAuthorizationConstants.FeedbackReportsReplyPermissionPolicyName)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(ApiConstants.AdminFeedbackReportRepliesRoute, SendReplyAsync)\n            .RequireAuthorization(AdminAuthorizationConstants.FeedbackReportsReadPermissionPolicyName)", source, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath))) directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
