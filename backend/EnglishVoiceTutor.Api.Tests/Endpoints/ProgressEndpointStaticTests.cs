namespace EnglishVoiceTutor.Api.Tests.Endpoints;

public sealed class ProgressEndpointStaticTests
{
    [Fact]
    public void ProgressRouteRequiresAuthorizationAndUsesTheProgressService()
    {
        var program = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Program.cs");

        Assert.Contains("app.MapGet(ApiConstants.MeProgressRoute, HandleGetProgressAsync).RequireAuthorization()", program, StringComparison.Ordinal);
        Assert.Contains("IProgressService progressService", program, StringComparison.Ordinal);
        Assert.DoesNotContain("HandleGetProgressAsync(Guid userId", program, StringComparison.Ordinal);
        Assert.Contains("CreateLessonSessionStorageUnavailableResponse()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void ProgressResponseDoesNotExposeInternalOrSensitiveFields()
    {
        var contract = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Contracts/Progress/ProgressResponse.cs");

        Assert.DoesNotContain("UserId", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SessionId", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Cost", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Provider", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Status", contract, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProgressDoesNotDependOnLessonHistory()
    {
        var service = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Services/ProgressService.cs");

        Assert.DoesNotContain("LessonHistory", service, StringComparison.Ordinal);
        Assert.Contains("CountAsync", service, StringComparison.Ordinal);
        Assert.Contains("AsNoTracking", service, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath))) directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
