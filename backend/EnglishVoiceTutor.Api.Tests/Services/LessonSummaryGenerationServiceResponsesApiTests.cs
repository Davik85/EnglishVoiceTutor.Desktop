using System.Net;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.LessonSessions;
using EnglishVoiceTutor.Api.Contracts.Subscription;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Cms;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using EnglishVoiceTutor.Api.Services.Usage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class LessonSummaryGenerationServiceResponsesApiTests : IDisposable
{
    private const string ValidSummaryJson = """{"summary":"Good progress.","strengths":["Clear greeting"],"improvements":["Use articles"],"vocabulary":["appointment"],"grammar":["a/an"],"nextSteps":["Practice another greeting"]}""";
    private readonly string? _originalApiKey = Environment.GetEnvironmentVariable(OpenAiConstants.ApiKeyEnvironmentVariableName);

    public LessonSummaryGenerationServiceResponsesApiTests() => Environment.SetEnvironmentVariable(OpenAiConstants.ApiKeyEnvironmentVariableName, "test-summary-api-key");

    [Fact]
    public async Task GeneratesAndPersistsSummaryFromTopLevelOutputText()
    {
        await using var db = CreateDbContext();
        var session = await SeedFinishedSessionWithMessageAsync(db);
        var usage = new RecordingUsageEventService();
        var logger = new RecordingLogger<LessonSummaryGenerationService>();
        var service = CreateGenerator(db, TopLevelEnvelope(ValidSummaryJson), usage, logger);

        await service.TryGenerateForFinishedSessionAsync(session.Id, TestContext.Current.CancellationToken);

        var summary = await db.LessonSummaries.SingleAsync(item => item.SessionId == session.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Good progress.", summary.Summary);
        Assert.Equal(LessonSessionConstants.FinishedStatus, (await db.LessonSessions.FindAsync([session.Id], TestContext.Current.CancellationToken))!.Status);
        Assert.Contains(usage.Records, item => item.Status == UsageConstants.Statuses.Success);
    }

    [Fact]
    public async Task GeneratesAndPersistsSummaryFromNestedResponsesApiOutputWhenTopLevelOutputTextIsEmpty()
    {
        await using var db = CreateDbContext();
        var session = await SeedFinishedSessionWithMessageAsync(db);
        var service = CreateGenerator(db, NestedEnvelope(ValidSummaryJson), new RecordingUsageEventService(), new RecordingLogger<LessonSummaryGenerationService>());

        await service.TryGenerateForFinishedSessionAsync(session.Id, TestContext.Current.CancellationToken);

        var summary = await db.LessonSummaries.SingleOrDefaultAsync(item => item.SessionId == session.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(summary);
        Assert.Equal("Good progress.", summary!.Summary);
    }

    [Theory]
    [InlineData("gpt-5.2", false, true)]
    [InlineData("gpt-5.5", false, false)]
    [InlineData("gpt-5.6-terra", true, false)]
    [InlineData("lesson-chat-distinct-model", true, false)]
    public async Task SummaryRequestUsesLessonTutorChatEffectiveTemperatureAndPersistsValidResponse(
        string modelId,
        bool omitTemperature,
        bool expectsNumericTemperature)
    {
        await using var db = CreateDbContext();
        var session = await SeedFinishedSessionWithMessageAsync(db);
        var settings = AiModelSettings.Defaults with
        {
            LessonTutorChatModel = modelId,
            FeedbackCorrectionModel = "feedback-distinct-model",
            LessonHintModel = "hint-distinct-model",
            TranslationModel = "translation-distinct-model",
            LessonTutorChatOmitTemperature = omitTemperature
        };
        var httpClientFactory = new SingleResponseHttpClientFactory(TopLevelEnvelope(ValidSummaryJson));
        var usage = new RecordingUsageEventService();
        var service = CreateGenerator(
            db,
            TopLevelEnvelope(ValidSummaryJson),
            usage,
            new RecordingLogger<LessonSummaryGenerationService>(),
            settings,
            httpClientFactory);

        await service.TryGenerateForFinishedSessionAsync(session.Id, TestContext.Current.CancellationToken);

        var requestBody = Assert.Single(httpClientFactory.RequestBodies);
        using var requestJson = JsonDocument.Parse(requestBody);
        Assert.Equal(modelId, requestJson.RootElement.GetProperty("model").GetString());
        if (expectsNumericTemperature)
        {
            Assert.Equal(0.3, requestJson.RootElement.GetProperty("temperature").GetDouble());
        }
        else
        {
            Assert.False(requestJson.RootElement.TryGetProperty("temperature", out _));
        }

        var summary = await db.LessonSummaries.SingleAsync(item => item.SessionId == session.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Good progress.", summary.Summary);
        Assert.Equal(modelId, Assert.Single(usage.Records, item => item.Status == UsageConstants.Statuses.Success).Model);
    }

    [Fact]
    public async Task WhitespaceTopLevelOutputFallsBackToNestedResponsesApiOutput()
    {
        await using var db = CreateDbContext();
        var session = await SeedFinishedSessionWithMessageAsync(db);
        var envelope = "{\"id\":\"resp-whitespace-top-level\",\"output_text\":\"   \",\"output\":[{\"type\":\"message\",\"role\":\"assistant\",\"content\":[{\"type\":\"output_text\",\"text\":" + System.Text.Json.JsonSerializer.Serialize(ValidSummaryJson) + "}]}]}";
        var service = CreateGenerator(db, envelope, new RecordingUsageEventService(), new RecordingLogger<LessonSummaryGenerationService>());

        await service.TryGenerateForFinishedSessionAsync(session.Id, TestContext.Current.CancellationToken);

        Assert.Equal("Good progress.", (await db.LessonSummaries.SingleAsync(item => item.SessionId == session.Id, TestContext.Current.CancellationToken)).Summary);
    }

    [Fact]
    public async Task EmptyResponsesApiOutputIsIsolatedAndLeavesFinishedLessonWithoutReadySummary()
    {
        await using var db = CreateDbContext();
        var session = await SeedFinishedSessionWithMessageAsync(db);
        var usage = new RecordingUsageEventService();
        var logger = new RecordingLogger<LessonSummaryGenerationService>();
        var service = CreateGenerator(db, """{"id":"resp-empty","output":[]}""", usage, logger);

        var exception = await Record.ExceptionAsync(() => service.TryGenerateForFinishedSessionAsync(session.Id, TestContext.Current.CancellationToken));

        Assert.Null(exception);
        Assert.Equal(LessonSessionConstants.FinishedStatus, (await db.LessonSessions.FindAsync([session.Id], TestContext.Current.CancellationToken))!.Status);
        Assert.Empty(await db.LessonSummaries.Where(item => item.SessionId == session.Id).ToListAsync(TestContext.Current.CancellationToken));
        Assert.Contains(usage.Records, item => item.Status == UsageConstants.Statuses.Failed);
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("empty_provider_output", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Exception is System.Text.Json.JsonException);
    }

    [Fact]
    public async Task InvalidNestedResponsesApiJsonIsIsolatedWithoutLoggingLearnerTextOrProviderBody()
    {
        await using var db = CreateDbContext();
        var session = await SeedFinishedSessionWithMessageAsync(db, "learner-private-message");
        var logger = new RecordingLogger<LessonSummaryGenerationService>();
        var service = CreateGenerator(db, NestedEnvelope("not-json-provider-body"), new RecordingUsageEventService(), logger);

        var exception = await Record.ExceptionAsync(() => service.TryGenerateForFinishedSessionAsync(session.Id, TestContext.Current.CancellationToken));

        Assert.Null(exception);
        Assert.Equal(LessonSessionConstants.FinishedStatus, (await db.LessonSessions.FindAsync([session.Id], TestContext.Current.CancellationToken))!.Status);
        Assert.Empty(await db.LessonSummaries.Where(item => item.SessionId == session.Id).ToListAsync(TestContext.Current.CancellationToken));
        Assert.DoesNotContain(logger.Entries.Select(entry => entry.Message), message => message.Contains("learner-private-message", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries.Select(entry => entry.Message), message => message.Contains("not-json-provider-body", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AuthenticatedDesktopCompatibleFinishCompletesWhenSummaryGenerationCannotUseEmptyProviderOutput()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var session = await SeedSessionAsync(db, userId, LessonSessionConstants.ActiveStatus);
        db.LessonMessages.Add(new LessonMessageEntity { Id = Guid.NewGuid(), SessionId = session.Id, TurnNumber = 1, Role = "user", Text = "learner-private-message", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var generator = CreateGenerator(db, """{"id":"resp-empty","output":[]}""", new RecordingUsageEventService(), new RecordingLogger<LessonSummaryGenerationService>());
        var finish = new LessonSessionService(db, new FakeRequestUserResolver(userId), new FakeAccessDecisionService(), generator, new RecordingLogger<LessonSessionService>());

        var response = await finish.FinishLessonSessionAsync(session.Id, new FinishLessonSessionRequest(1), TestContext.Current.CancellationToken);

        Assert.Equal(LessonSessionConstants.FinishedStatus, response.Status);
        Assert.Equal(1, response.ValidTurnCount);
        Assert.NotNull(response.FinishedAt);
        Assert.Empty(await db.LessonSummaries.Where(item => item.SessionId == session.Id).ToListAsync(TestContext.Current.CancellationToken));
    }

    public void Dispose() => Environment.SetEnvironmentVariable(OpenAiConstants.ApiKeyEnvironmentVariableName, _originalApiKey);

    private static LessonSummaryGenerationService CreateGenerator(
        AppDbContext db,
        string providerEnvelope,
        RecordingUsageEventService usage,
        RecordingLogger<LessonSummaryGenerationService> logger,
        AiModelSettings? settings = null,
        SingleResponseHttpClientFactory? httpClientFactory = null) =>
        new(
            db,
            new OpenAiOptionsProvider(new FakeAiModelSettingsService(settings)),
            httpClientFactory ?? new SingleResponseHttpClientFactory(providerEnvelope),
            new EmptyRuntimeContentService(),
            usage,
            logger);

    private static string TopLevelEnvelope(string outputText) =>
        "{\"id\":\"resp-top-level\",\"output_text\":" + System.Text.Json.JsonSerializer.Serialize(outputText) + ",\"output\":[]}";

    private static string NestedEnvelope(string outputText) =>
        "{\"id\":\"resp-nested\",\"output\":[{\"type\":\"message\",\"role\":\"assistant\",\"content\":[{\"type\":\"output_text\",\"text\":" + System.Text.Json.JsonSerializer.Serialize(outputText) + "}]}]}";

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    private static async Task<LessonSessionEntity> SeedFinishedSessionWithMessageAsync(AppDbContext db, string message = "I can introduce myself clearly.")
    {
        var session = await SeedSessionAsync(db, Guid.NewGuid(), LessonSessionConstants.FinishedStatus);
        db.LessonMessages.Add(new LessonMessageEntity { Id = Guid.NewGuid(), SessionId = session.Id, TurnNumber = 1, Role = "user", Text = message, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return session;
    }

    private static async Task<LessonSessionEntity> SeedSessionAsync(AppDbContext db, Guid userId, string status)
    {
        db.Users.Add(new UserEntity { Id = userId, Email = $"{userId:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow });
        var session = new LessonSessionEntity { Id = Guid.NewGuid(), UserId = userId, LessonContentId = "lesson-id", StudyLanguage = "English", TopicId = "topic", TopicTitle = "Topic", SubtopicId = "subtopic", SubtopicTitle = "Subtopic", Level = "A1", ModeUsed = LessonSessionConstants.TextMode, Status = status, StartedAt = DateTimeOffset.UtcNow.AddMinutes(-2), LastHeartbeatAtUtc = DateTimeOffset.UtcNow, EstimatedCost = 0m, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2), UpdatedAt = DateTimeOffset.UtcNow, FinishedAt = status == LessonSessionConstants.FinishedStatus ? DateTimeOffset.UtcNow : null };
        db.LessonSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    private sealed class SingleResponseHttpClientFactory(string envelope) : IHttpClientFactory
    {
        public List<string> RequestBodies { get; } = [];
        public HttpClient CreateClient(string name) => new(new SingleResponseHandler(envelope, RequestBodies));
    }

    private sealed class SingleResponseHandler(string envelope, List<string> requestBodies) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            requestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(envelope, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class EmptyRuntimeContentService : ICmsRuntimeLessonContentService
    {
        public Task<CmsRuntimeLessonContentReadResult> ReadRuntimeLessonContentAsync(CancellationToken cancellationToken) => Task.FromResult(new CmsRuntimeLessonContentReadResult());
    }

    private sealed class RecordingUsageEventService : IUsageEventService
    {
        public List<UsageEventRecord> Records { get; } = [];
        public Task TryRecordAsync(UsageEventRecord record, CancellationToken cancellationToken = default) { Records.Add(record); return Task.CompletedTask; }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(string Message, Exception? Exception)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Entries.Add((formatter(state, exception), exception));
    }

    private sealed class FakeAiModelSettingsService(AiModelSettings? settings = null) : IAiModelSettingsService
    {
        public AiModelSettings GetActiveSettings() => settings ?? AiModelSettings.Defaults;
        public Task<AiModelSettingsResponse> GetAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> SaveDraftAsync(AiModelSettings draft, string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
        public AiModelSettingsValidationResponse Validate(AiModelSettings settings) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> PublishAsync(string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> ResetDraftFromActiveAsync(string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeRequestUserResolver(Guid userId) : IRequestUserResolver { public ResolvedRequestUser ResolveCurrentUser() => new(userId, RequestUserResolver.AuthenticatedSource); }
    private sealed class FakeAccessDecisionService : ILessonAccessDecisionService { public Task<LessonAccessDecisionResponse> GetDecisionAsync(Guid userId, string source, CancellationToken cancellationToken) => Task.FromResult(new LessonAccessDecisionResponse()); }
}
