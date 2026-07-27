namespace EnglishVoiceTutor.Api.Tests.Data;

public sealed class GooglePlayPurchaseClaimsMigrationTests
{
    [Fact]
    public void DatabaseUniqueIndexPreventsConcurrentDuplicateClaimsAndMigrationAddsNoOtherTables()
    {
        var migration = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Migrations/20260727045935_AddGooglePlayPurchaseClaims.cs");

        Assert.Contains("CreateTable", migration, StringComparison.Ordinal);
        Assert.Contains("google_play_purchase_claims", migration, StringComparison.Ordinal);
        Assert.Contains("PurchaseTokenFingerprint", migration, StringComparison.Ordinal);
        Assert.Contains("unique: true", migration, StringComparison.Ordinal);
        Assert.Contains("onDelete: ReferentialAction.Restrict", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("subscriptions", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payments", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("entitlements", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BillingEvent", migration, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EntityContainsNoRawTokenOrProviderResponseFields()
    {
        var entity = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Data/Entities/GooglePlayPurchaseClaimEntity.cs");

        Assert.Contains("PurchaseTokenFingerprint", entity, StringComparison.Ordinal);
        foreach (var forbidden in new[] { "PurchaseToken ", "Order", "Price", "Currency", "Acknowledgement", "Response", "Metadata" }) Assert.DoesNotContain(forbidden, entity, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionStillUsesDisabledVerifierAndDoesNotCallClaimServiceFromEndpoint()
    {
        var program = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Program.cs");
        var endpoint = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Endpoints/GooglePlayPurchaseVerificationEndpoints.cs");

        Assert.Contains("AddScoped<IGooglePlayPurchaseVerifier, DisabledGooglePlayPurchaseVerifier>()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("IGooglePlayPurchaseClaimService", endpoint, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath))) directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory!.FullName, relativePath));
    }
}
