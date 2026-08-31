namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class RestoreCredentialsFoundationStaticTests
{
    [Fact]
    public void FoundationIsDisabledByDefaultAndUsesDiscoverableAssertionOptions()
    {
        var options = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Options/RestoreCredentialsOptions.cs");
        var service = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Services/Auth/RestoreCredentialsService.cs");
        Assert.Contains("public bool Enabled { get; set; }", options, StringComparison.Ordinal);
        var verifier = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Services/Auth/IRestoreCredentialsWebAuthnVerifier.cs");
        Assert.Contains("AllowedCredentials = []", verifier, StringComparison.Ordinal);
        Assert.Contains("ConsumeCeremonyAsync", service, StringComparison.Ordinal);
        Assert.Contains("IssueSessionForActiveUserAsync", service, StringComparison.Ordinal);
        Assert.DoesNotContain("RefreshToken", service, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationIsAdditiveAndCreatesOnlyRestoreCredentialTables()
    {
        var migration = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Migrations/20260831080122_AddRestoreCredentialsFoundation.cs");
        var up = migration[..migration.IndexOf("protected override void Down", StringComparison.Ordinal)];
        Assert.Equal(2, Count(up, "migrationBuilder.CreateTable("));
        Assert.Contains("restore_credentials", up, StringComparison.Ordinal);
        Assert.Contains("restore_credential_ceremonies", up, StringComparison.Ordinal);
        Assert.DoesNotContain("DropTable", up, StringComparison.Ordinal);
        Assert.DoesNotContain("AlterColumn", up, StringComparison.Ordinal);
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
