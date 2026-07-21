namespace EnglishVoiceTutor.Api.Tests.Data;

public sealed class AddActiveAccountDeletionRequestConstraintMigrationTests
{
    [Fact]
    public void MigrationAddsOnlyThePartialUniqueIndexForActiveDeletionRequests()
    {
        var source = ReadMigrationSource();
        var upMethod = source[..source.IndexOf("protected override void Down", StringComparison.Ordinal)];

        Assert.Contains("CREATE UNIQUE INDEX", upMethod, StringComparison.Ordinal);
        Assert.Contains("IX_user_feedback_reports_ActiveAccountDeletionRequest_UserId", upMethod, StringComparison.Ordinal);
        Assert.Contains("WHERE \"Category\" = 'account_deletion'", upMethod, StringComparison.Ordinal);
        Assert.Contains("'new', 'reviewed', 'needs_information', 'processing'", upMethod, StringComparison.Ordinal);
        Assert.DoesNotContain("DropTable", upMethod, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteData", upMethod, StringComparison.Ordinal);
    }

    private static string ReadMigrationSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend", "EnglishVoiceTutor.Api", "Migrations"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var path = Directory.GetFiles(Path.Combine(directory!.FullName, "backend", "EnglishVoiceTutor.Api", "Migrations"), "*_AddActiveAccountDeletionRequestConstraint.cs").Single();
        return File.ReadAllText(path);
    }
}
