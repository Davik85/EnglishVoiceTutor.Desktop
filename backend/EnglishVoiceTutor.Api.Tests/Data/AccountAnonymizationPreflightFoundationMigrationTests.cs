namespace EnglishVoiceTutor.Api.Tests.Data;

public sealed class AccountAnonymizationPreflightFoundationMigrationTests
{
    [Fact]
    public void MigrationCreatesOnlyTheTwoApprovedFoundationTables()
    {
        var root = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.Parent!;
        var migration = File.ReadAllText(Path.Combine(root.FullName, "EnglishVoiceTutor.Api", "Migrations", "20260722132656_AddAccountAnonymizationPreflightFoundation.cs"));
        Assert.Contains("account_anonymization_policy_snapshots", migration, StringComparison.Ordinal);
        Assert.Contains("account_anonymization_operations", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateData", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteData", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("ReferentialAction.Cascade", migration, StringComparison.Ordinal);
    }
}
