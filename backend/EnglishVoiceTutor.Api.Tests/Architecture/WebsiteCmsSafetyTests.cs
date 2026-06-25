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
