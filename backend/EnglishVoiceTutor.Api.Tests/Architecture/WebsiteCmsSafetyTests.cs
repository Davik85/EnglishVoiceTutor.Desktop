namespace EnglishVoiceTutor.Api.Tests.Architecture;

public sealed class WebsiteCmsSafetyTests
{
    private static readonly string RepoRoot = LocateRepoRoot();

    [Fact]
    public void WebsiteCmsRoutes_AreAdminOnlyAndDoNotExposePublicEndpoints()
    {
        var apiConstants = File.ReadAllText(Path.Combine(RepoRoot, "backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs"));

        Assert.Contains("/api/admin/website-cms/sections/overview", apiConstants, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/api/website", apiConstants, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/api/public", apiConstants, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/api/website-cms", apiConstants, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/legal", apiConstants, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/pricing", apiConstants, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WebsiteCmsOverviewEndpoint_UsesAdminReadRouteAndCmsReadAuthorization()
    {
        var adminEndpoints = File.ReadAllText(Path.Combine(RepoRoot, "backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs"));
        var apiConstants = File.ReadAllText(Path.Combine(RepoRoot, "backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs"));

        Assert.Contains("AdminWebsiteCmsSectionOverviewRoute = \"/api/admin/website-cms/sections/overview\"", apiConstants, StringComparison.Ordinal);
        Assert.Contains("app.MapGet(ApiConstants.AdminWebsiteCmsSectionOverviewRoute, GetWebsiteCmsSectionOverviewAsync)", adminEndpoints, StringComparison.Ordinal);
        Assert.Contains("RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName)", adminEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(ApiConstants.AdminWebsiteCmsSectionOverviewRoute", adminEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut(ApiConstants.AdminWebsiteCmsSectionOverviewRoute", adminEndpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminWebsiteTab_IsTopLevelAndDisplaysReadOnlyMetadataWording()
    {
        var adminIndex = File.ReadAllText(Path.Combine(RepoRoot, "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html"));
        var adminScript = File.ReadAllText(Path.Combine(RepoRoot, "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js"));

        Assert.Contains("data-tab-id=\"website\"", adminIndex, StringComparison.Ordinal);
        Assert.Contains("Read-only metadata view", adminIndex, StringComparison.Ordinal);
        Assert.Contains("Editing, save draft, and publish are not implemented yet", adminIndex, StringComparison.Ordinal);
        Assert.Contains("Public site still renders static", adminIndex, StringComparison.Ordinal);
        Assert.Contains("/api/admin/website-cms/sections/overview", adminScript, StringComparison.Ordinal);
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
        var candidates = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        foreach (var candidate in candidates)
        {
            var current = new DirectoryInfo(candidate);
            while (current is not null)
            {
                if (IsRepoRoot(current.FullName))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private static bool IsRepoRoot(string directory)
    {
        return Directory.Exists(Path.Combine(directory, ".git"))
            && (File.Exists(Path.Combine(directory, "EnglishVoiceTutor.Desktop.sln"))
                || File.Exists(Path.Combine(directory, "EnglishVoiceTutor.Desktop.slnx")))
            && Directory.Exists(Path.Combine(directory, "backend/EnglishVoiceTutor.Api"))
            && Directory.Exists(Path.Combine(directory, "site/public"));
    }
}
