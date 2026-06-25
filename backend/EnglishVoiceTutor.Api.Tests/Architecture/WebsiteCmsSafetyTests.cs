namespace EnglishVoiceTutor.Api.Tests.Architecture;

public sealed class WebsiteCmsSafetyTests
{
    private static readonly string RepoRoot = LocateRepoRoot();

    [Fact]
    public void WebsiteCmsRoutes_AreAdminOnlyAndDoNotExposePublicEndpoints()
    {
        var apiConstants = File.ReadAllText(Path.Combine(RepoRoot, "backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs"));

        Assert.DoesNotContain("/api/website", apiConstants, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/api/public", apiConstants, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/legal", apiConstants, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/pricing", apiConstants, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SitePublic_HasNoWorkingTreeChanges()
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            ArgumentList = { "diff", "--quiet", "--", "site/public" },
            WorkingDirectory = RepoRoot,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException("Could not start git diff.");

        process.WaitForExit(5000);
        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public void AdminStaticFiles_DoNotContainLiveCheckoutOrDataPaddleStrings()
    {
        var adminRoot = Path.Combine(RepoRoot, "backend/EnglishVoiceTutor.Api/wwwroot/admin");
        var combined = string.Join('\n', Directory.EnumerateFiles(adminRoot, "*", SearchOption.AllDirectories).Select(File.ReadAllText));

        Assert.DoesNotContain("data-paddle", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("checkout.paddle.com", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("paddle_button", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pdl_live", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EfCoreMigrations_HaveDesignerFilesUnlessExplicitlyLegacyManualMigrations()
    {
        var migrationsRoot = Path.Combine(RepoRoot, "backend/EnglishVoiceTutor.Api/Migrations");
        var legacyManualMigrations = new HashSet<string>(StringComparer.Ordinal)
        {
            "20260601120000_AddPasswordResetFoundation",
            "20260603120000_AddCmsContentFoundation",
            "20260604120000_AddCmsScenarioDefinitionJson",
            "20260604121000_AddCmsDraftSaveAuditMetadata"
        };

        var missingDesignerFiles = Directory
            .EnumerateFiles(migrationsRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal))
            .Where(path => !string.Equals(Path.GetFileName(path), "AppDbContextModelSnapshot.cs", StringComparison.Ordinal))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && !legacyManualMigrations.Contains(name))
            .Where(name => !File.Exists(Path.Combine(migrationsRoot, $"{name}.Designer.cs")))
            .ToArray();

        Assert.Empty(missingDesignerFiles);
    }

    private static string LocateRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "EnglishVoiceTutor.Desktop.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
