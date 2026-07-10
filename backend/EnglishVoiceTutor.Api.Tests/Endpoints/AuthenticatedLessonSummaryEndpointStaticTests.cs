namespace EnglishVoiceTutor.Api.Tests.Endpoints;

public sealed class AuthenticatedLessonSummaryEndpointStaticTests
{
    [Fact]
    public void ProductionSummaryRouteRequiresAuthorizationAndDoesNotAcceptClientSummaryContent()
    {
        var program = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Program.cs");
        var finishRequest = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Contracts/LessonSessions/FinishLessonSessionRequest.cs");

        Assert.Contains("app.MapGet(ApiConstants.MeLessonSessionSummaryRoute, HandleGetAuthenticatedLessonSummaryAsync).RequireAuthorization()", program, StringComparison.Ordinal);
        Assert.Contains("app.MapPut(ApiConstants.MeLessonSessionFinishRoute, HandleFinishLessonSessionAsync).RequireAuthorization()", program, StringComparison.Ordinal);
        Assert.Equal("namespace EnglishVoiceTutor.Api.Contracts.LessonSessions;\n\npublic sealed record FinishLessonSessionRequest(int ValidTurnCount);\n", finishRequest.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void SummaryGenerationUsesPersistedMessagesAndSafeStructuredOutput()
    {
        var source = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Services/LessonSummaryGenerationService.cs");
        Assert.Contains("dbContext.LessonMessages.Where(item => item.SessionId == sessionId)", source, StringComparison.Ordinal);
        Assert.Contains("JsonSchemaFormatType", source, StringComparison.Ordinal);
        Assert.DoesNotContain("raw runtime JSON", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath))) directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
