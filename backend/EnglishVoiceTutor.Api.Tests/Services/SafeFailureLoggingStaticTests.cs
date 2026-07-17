using EnglishVoiceTutor.Api.Services;
using Microsoft.Extensions.Logging;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class SafeFailureLoggingStaticTests
{
    [Theory]
    [InlineData("backend/EnglishVoiceTutor.Api/Services/Email/SmtpEmailSender.cs")]
    [InlineData("backend/EnglishVoiceTutor.Api/Endpoints/UserFeedbackReportEndpoints.cs")]
    [InlineData("backend/EnglishVoiceTutor.Api/Services/Auth/PasswordResetService.cs")]
    public void ConfirmedFailurePathsUseSafeFailureLoggerWithoutExceptionArguments(string relativePath)
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));

        Assert.Contains("SafeFailureLogger.Log", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LogWarning(exception", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LogError(exception", source, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", source, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.InnerException", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SafeFailureLoggerEmitsOnlyStableCodesWithoutExceptionsOrSensitiveMarkers()
    {
        var logger = new RecordingLogger();
        const string userId = "user-id-9e9a11ef";
        const string recipientEmail = "recipient-4b0aa@example.test";
        const string reportText = "report-text-4c1d2e";
        const string replyText = "reply-text-8b6a4d";
        const string resetUrlAndToken = "https://reset.example.test/?token=reset-token-1c25";
        const string smtpDetails = "smtp-host-2d9a.example.test:2525";

        SafeFailureLogger.LogEmailDeliveryFailed(logger);
        SafeFailureLogger.LogFeedbackReportPersistenceFailed(logger);
        SafeFailureLogger.LogPasswordResetDeliveryFailed(logger);

        var output = string.Join("\n", logger.Entries.Select(entry => entry.Message));
        Assert.Contains(SafeFailureLogger.EmailDeliveryFailedCode, output, StringComparison.Ordinal);
        Assert.Contains(SafeFailureLogger.FeedbackReportPersistenceFailedCode, output, StringComparison.Ordinal);
        Assert.Contains(SafeFailureLogger.PasswordResetDeliveryFailedCode, output, StringComparison.Ordinal);
        Assert.DoesNotContain(userId, output, StringComparison.Ordinal);
        Assert.DoesNotContain(recipientEmail, output, StringComparison.Ordinal);
        Assert.DoesNotContain(reportText, output, StringComparison.Ordinal);
        Assert.DoesNotContain(replyText, output, StringComparison.Ordinal);
        Assert.DoesNotContain(resetUrlAndToken, output, StringComparison.Ordinal);
        Assert.DoesNotContain(smtpDetails, output, StringComparison.Ordinal);
        Assert.All(logger.Entries, entry => Assert.Null(entry.Exception));
    }

    [Fact]
    public void PasswordResetInformationalLogsAreStructuralAndContainNoResetIdentifiers()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "backend", "EnglishVoiceTutor.Api", "Services", "Auth", "PasswordResetService.cs"));

        Assert.Contains("logger.LogInformation(\"Password reset code created and delivery attempted.\");", source, StringComparison.Ordinal);
        Assert.Contains("logger.LogInformation(\"Password reset confirmed.\");", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UserId={UserId}", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TokenId={TokenId}", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpiresAtUtc={ExpiresAtUtc}", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ResetUrl={ResetUrl}", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ResetCode={ResetCode}", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TokenHash={TokenHash}", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend", "EnglishVoiceTutor.Api")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<(string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((formatter(state, exception), exception));
    }
}
