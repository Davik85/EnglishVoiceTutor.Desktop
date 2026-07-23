using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Tests.Endpoints;

public sealed class AccountDeletionRequestEndpointStaticTests
{
    [Fact]
    public void EndpointRequiresAuthenticationUsesClaimIdentityAndRateLimitsPasswordConfirmation()
    {
        var source = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Endpoints/UserFeedbackReportEndpoints.cs");

        Assert.Contains("app.MapPost(ApiConstants.MeAccountDeletionRequestsRoute, CreateAccountDeletionRequestAsync)", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains(".RequireRateLimiting(RateLimitingConstants.AuthSessionPolicyName)", source, StringComparison.Ordinal);
        Assert.Contains("ClaimsUserAccessor.TryGetUserId(principal)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RequestDoesNotAcceptClientSuppliedUserIdentity()
    {
        var source = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Contracts/FeedbackReports/CreateAccountDeletionRequest.cs");

        Assert.Contains("CurrentPassword", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UserId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Email", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminEmailIntakeUsesExecuteAuthorizationAndAdminWriteRateLimiting()
    {
        var source = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs");
        var constants = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs");

        Assert.Contains("AdminUserAccountDeletionRequestsRoute", constants, StringComparison.Ordinal);
        Assert.Contains("app.MapPost(ApiConstants.AdminUserAccountDeletionRequestsRoute, CreateAdminAccountDeletionRequestAsync)", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization(AdminAuthorizationConstants.AccountAnonymizationExecutePermissionPolicyName)", source, StringComparison.Ordinal);
        Assert.Contains("ApplyAdminWriteRateLimiting(", source, StringComparison.Ordinal);
        Assert.Contains("SubmitAdminAsync(userId, request?.Comment", source, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath))) directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory!.FullName, relativePath));
    }
}
