namespace EnglishVoiceTutor.Api.Tests.Endpoints;

public sealed class GooglePlayPurchaseVerificationEndpointsStaticTests
{
    [Fact]
    public void RouteRequiresAuthorizationClaimsIdentityAndBillingVerificationRateLimit()
    {
        var source = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Endpoints/GooglePlayPurchaseVerificationEndpoints.cs");
        var constants = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs");

        Assert.Contains("MeGooglePlayPurchaseVerificationRoute", constants, StringComparison.Ordinal);
        Assert.Contains("app.MapPost(ApiConstants.MeGooglePlayPurchaseVerificationRoute, VerifyAsync).RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("ClaimsUserAccessor.TryGetUserId(principal)", source, StringComparison.Ordinal);
        Assert.Contains("BillingGooglePlayPurchaseVerificationPolicyName", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RequestAndResponseContainOnlySafeContractFields()
    {
        var request = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Contracts/Billing/GooglePlayPurchaseVerificationRequest.cs");
        var response = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Contracts/Billing/GooglePlayPurchaseVerificationResponse.cs");

        Assert.Contains("PurchaseToken", request, StringComparison.Ordinal);
        foreach (var forbidden in new[] { "UserId", "Email", "PackageName", "Provider", "Premium", "Price", "Currency", "Expiry", "Acknowledgement" }) Assert.DoesNotContain(forbidden, request, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Result", response, StringComparison.Ordinal);
        Assert.Contains("Message", response, StringComparison.Ordinal);
        Assert.Contains("SubscriptionStatusRefreshRecommended", response, StringComparison.Ordinal);
        foreach (var forbidden in new[] { "PurchaseToken", "Order", "UserId", "Exception", "Provider" }) Assert.DoesNotContain(forbidden, response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProgramRegistersOnlyDisabledVerifierAndNoGoogleApiClient()
    {
        var program = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Program.cs");
        var registration = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Services/Billing/GooglePlayBillingServiceCollectionExtensions.cs");
        var verifier = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Services/Billing/DisabledGooglePlayPurchaseVerifier.cs");

        Assert.Contains("AddGooglePlayBilling(builder.Configuration)", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<IGooglePlayPurchaseVerifier, DisabledGooglePlayPurchaseVerifier>()", registration, StringComparison.Ordinal);
        Assert.DoesNotContain("AndroidPublisherService", program, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", verifier, StringComparison.Ordinal);
        Assert.DoesNotContain("logger", verifier, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath))) directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory!.FullName, relativePath));
    }
}
