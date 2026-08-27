namespace EnglishVoiceTutor.Api.Tests.Data;

public sealed class GooglePlayTrialDeferralMigrationTests
{
    private const string MigrationPath = "backend/EnglishVoiceTutor.Api/Migrations/20260827105749_AddGooglePlayTrialDeferralFoundation.cs";

    [Fact]
    public void MigrationIsAdditiveAndCreatesOnlyTheDedicatedDeferralTable()
    {
        var migration = ReadRepositoryFile(MigrationPath);
        var up = migration[..migration.IndexOf("protected override void Down", StringComparison.Ordinal)];

        Assert.Contains("CreateTable", up, StringComparison.Ordinal);
        Assert.Contains("google_play_initial_premium_deferrals", up, StringComparison.Ordinal);
        Assert.Equal(1, Count(up, "migrationBuilder.CreateTable("));
        Assert.Equal(3, Count(up, "migrationBuilder.CreateIndex("));
        Assert.DoesNotContain("DropTable", up, StringComparison.Ordinal);
        Assert.DoesNotContain("DropColumn", up, StringComparison.Ordinal);
        Assert.DoesNotContain("Rename", up, StringComparison.Ordinal);
        Assert.DoesNotContain("AlterColumn", up, StringComparison.Ordinal);
        Assert.DoesNotContain("Sql(", up, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationHasPerClaimExactlyOnceGuaranteeAndExplainsImmutableCoverageTail()
    {
        var migration = ReadRepositoryFile(MigrationPath);

        Assert.Contains("IX_google_play_initial_premium_deferrals_GooglePlayPurchaseClaimId", migration, StringComparison.Ordinal);
        Assert.Contains("ExistingCoverageStartsAtUtc", migration, StringComparison.Ordinal);
        Assert.Contains("ExistingCoverageTailUtc", migration, StringComparison.Ordinal);
        Assert.Contains("TargetProviderExpiryUtc", migration, StringComparison.Ordinal);
        Assert.Contains("IsLicenseTestPurchase", migration, StringComparison.Ordinal);
        Assert.Equal(1, Count(migration, "unique: true"));
        Assert.DoesNotContain("PurchaseToken =", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("ProtectedPurchaseToken", migration, StringComparison.Ordinal);
    }

    private static int Count(string value, string term) => value.Split(term, StringSplitOptions.None).Length - 1;

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath))) directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory!.FullName, relativePath));
    }
}
