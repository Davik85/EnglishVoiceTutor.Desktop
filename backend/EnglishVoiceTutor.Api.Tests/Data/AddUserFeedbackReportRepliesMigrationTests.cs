namespace EnglishVoiceTutor.Api.Tests.Data;

public sealed class AddUserFeedbackReportRepliesMigrationTests
{
    [Fact]
    public void MigrationCreatesOnlyReplyTableWithRequiredForeignKeysAndIndexes()
    {
        var source = ReadMigrationSource();

        Assert.Equal(1, CountOccurrences(source, "migrationBuilder.CreateTable("));
        Assert.Contains("name: \"user_feedback_report_replies\"", source, StringComparison.Ordinal);
        Assert.Contains("principalTable: \"user_feedback_reports\"", source, StringComparison.Ordinal);
        Assert.Contains("principalTable: \"admin_users\"", source, StringComparison.Ordinal);
        Assert.Contains("IX_user_feedback_report_replies_FeedbackReportId", source, StringComparison.Ordinal);
        Assert.Contains("IX_user_feedback_report_replies_AdminUserId", source, StringComparison.Ordinal);
        Assert.Contains("IX_user_feedback_report_replies_FeedbackReportId_CreatedAtUtc", source, StringComparison.Ordinal);
        Assert.DoesNotContain("migrationBuilder.DropTable(", UpMethod(source), StringComparison.Ordinal);
        Assert.DoesNotContain("migrationBuilder.DropColumn(", UpMethod(source), StringComparison.Ordinal);
        Assert.DoesNotContain("migrationBuilder.DeleteData(", UpMethod(source), StringComparison.Ordinal);
        Assert.DoesNotContain("migrationBuilder.Sql(", UpMethod(source), StringComparison.Ordinal);
    }

    private static string ReadMigrationSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend", "EnglishVoiceTutor.Api", "Migrations")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var migrationPath = Directory.GetFiles(Path.Combine(directory.FullName, "backend", "EnglishVoiceTutor.Api", "Migrations"), "*_AddUserFeedbackReportReplies.cs").Single();
        return File.ReadAllText(migrationPath);
    }

    private static string UpMethod(string source) => source[..source.IndexOf("protected override void Down", StringComparison.Ordinal)];

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
