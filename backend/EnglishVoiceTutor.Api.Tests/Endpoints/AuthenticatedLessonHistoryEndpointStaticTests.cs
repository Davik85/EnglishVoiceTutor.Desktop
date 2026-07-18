namespace EnglishVoiceTutor.Api.Tests.Endpoints;

public sealed class AuthenticatedLessonHistoryEndpointStaticTests
{
    [Fact]
    public void ProductionLessonHistoryRoutesRequireAuthorization()
    {
        var program = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Program.cs");

        Assert.Contains("app.MapGet(ApiConstants.MeLessonHistoryRoute, HandleGetLessonHistoryAsync).RequireAuthorization()", program, StringComparison.Ordinal);
        Assert.Contains("app.MapGet(ApiConstants.MeLessonHistoryBySessionIdRoute, HandleGetLessonHistoryDetailAsync).RequireAuthorization()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void DevLessonHistoryRoutesRemainMappedToSharedHandlers()
    {
        var program = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Program.cs");

        Assert.Contains("app.MapGet(ApiConstants.DevLessonHistoryRoute, HandleGetLessonHistoryAsync)", program, StringComparison.Ordinal);
        Assert.Contains("app.MapGet(ApiConstants.DevLessonHistoryBySessionIdRoute, HandleGetLessonHistoryDetailAsync)", program, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath))) directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
