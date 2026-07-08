using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Tests.Endpoints;

public sealed class LessonSessionReplyEndpointStaticTests
{
    [Fact]
    public void MobileLessonReplyEndpointRequiresAuthorizationAndUsesOwnedRequestDto()
    {
        var source = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Program.cs");
        var requestSource = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Contracts/LessonSessions/LessonSessionReplyRequest.cs");

        Assert.Contains("app.MapPost(ApiConstants.MeLessonSessionReplyRoute, HandleCreateLessonSessionReplyAsync).RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("LessonSessionReplyRequest request", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LessonChatRequest request,\n    ILessonSessionReplyService", source.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("public sealed record LessonSessionReplyRequest(string? MessageText);", requestSource, StringComparison.Ordinal);
        Assert.DoesNotContain("PromptTemplates", requestSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RecentMessages", requestSource, StringComparison.Ordinal);
        Assert.DoesNotContain("LessonScenarioId", requestSource, StringComparison.Ordinal);
    }

    [Fact]
    public void MobileLessonReplyEndpointKeepsDesktopLessonChatEndpointUnchanged()
    {
        var source = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Program.cs");

        Assert.Contains("var lessonChatReplyEndpoint = app.MapPost(ApiConstants.LessonChatReplyRoute, HandleLessonChatReplyAsync);", source, StringComparison.Ordinal);
        Assert.Contains("static async Task<IResult> HandleLessonChatReplyAsync(", source, StringComparison.Ordinal);
        Assert.Contains("LessonChatRequest request,", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ApiConstants.LessonChatReplyRoute, HandleCreateLessonSessionReplyAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MobileLessonReplyEndpointUsesLessonChatRateLimitPolicyAndSafeConflictResponse()
    {
        var programSource = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Program.cs");
        var rateLimitSource = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/RateLimiting/RateLimitingServiceCollectionExtensions.cs");
        var serviceSource = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Services/LessonSessionReplyResult.cs");

        Assert.Contains("authenticatedLessonReplyEndpoint.RequireRateLimiting(RateLimitingConstants.LessonChatReplyPolicyName);", programSource, StringComparison.Ordinal);
        Assert.Contains("IsAuthenticatedLessonReplyRequest", rateLimitSource, StringComparison.Ordinal);
        Assert.Contains("EndsWith(\"/reply\", StringComparison.OrdinalIgnoreCase)", rateLimitSource, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status409Conflict", programSource, StringComparison.Ordinal);
        Assert.Contains("mobile_lesson_reply_not_implemented", serviceSource, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenAI", serviceSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prompt", serviceSource, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath)))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
