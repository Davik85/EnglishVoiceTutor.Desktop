namespace EnglishVoiceTutor.Api.Tests.Endpoints;

public sealed class AchievementsEndpointStaticTests
{
    [Fact]
    public void AchievementsRouteRequiresAuthorizationAndHasNoUserIdInput()
    {
        var program = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Program.cs");
        Assert.Contains("app.MapGet(ApiConstants.MeAchievementsRoute, HandleGetAchievementsAsync).RequireAuthorization()", program, StringComparison.Ordinal);
        Assert.Contains("IAchievementsService achievementsService", program, StringComparison.Ordinal);
        Assert.DoesNotContain("HandleGetAchievementsAsync(Guid userId", program, StringComparison.Ordinal);
    }

    [Fact]
    public void ContractDoesNotExposeInternalSessionOrSensitiveFields()
    {
        var contract = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Contracts/Achievements/AchievementsResponse.cs");
        Assert.DoesNotContain("UserId", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SessionId", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Cost", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Provider", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Status", contract, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImplementationUsesCanonicalLessonContentIdAndNoHistory()
    {
        var service = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Services/AchievementsService.cs");
        Assert.Contains("session.LessonContentId", service, StringComparison.Ordinal);
        Assert.DoesNotContain("LessonHistory", service, StringComparison.Ordinal);
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
