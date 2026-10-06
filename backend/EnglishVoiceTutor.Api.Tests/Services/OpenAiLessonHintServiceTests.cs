using System.Net;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Cms;
using EnglishVoiceTutor.Api.Services.Usage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class OpenAiLessonHintServiceTests
{
    private const string TestKey = "test-hint-key-never-log";
    private const string UserText = "Private learner message never to log.";
    private const string HintText = "Je voudrais un café, s'il vous plaît.";
    private const string Model = "hint-distinct-model";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProviderResultBeforeDeadlinePreservesHintRequestAndUsage(bool omitTemperature)
    {
        using var fixture = new Fixture(omitTemperature);
        fixture.Handler.BeforeResponse = token =>
        {
            fixture.Time.Advance(TimeSpan.FromMilliseconds(6999));
            Assert.False(token.IsCancellationRequested);
        };
        var result = await fixture.Service.CreateHintAsync(fixture.Request, TestContext.Current.CancellationToken);
        Assert.Equal(HintText, result.HintText);
        var body = Assert.Single(fixture.Handler.Requests);
        Assert.Equal(Model, body.GetProperty("model").GetString());
        Assert.Equal(OpenAiConstants.LessonHintSystemInstructions, body.GetProperty("instructions").GetString());
        var profile = await fixture.Resolver.ResolveAsync(fixture.Request.TutorAvatarId, TestContext.Current.CancellationToken);
        Assert.Equal(fixture.Builder.BuildHintInput(fixture.Request, profile), body.GetProperty("input").GetString());
        Assert.False(body.TryGetProperty("temperature", out _));
        var format = body.GetProperty("text").GetProperty("format");
        Assert.Equal(OpenAiConstants.JsonSchemaFormatType, format.GetProperty("type").GetString());
        Assert.Equal(OpenAiConstants.LessonHintResponseSchemaName, format.GetProperty("name").GetString());
        Assert.True(format.GetProperty("strict").GetBoolean());
        var schema = format.GetProperty("schema");
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal("hintText", Assert.Single(schema.GetProperty("required").EnumerateArray()).GetString());
        Assert.Equal("string", schema.GetProperty("properties").GetProperty("hintText").GetProperty("type").GetString());
        var usage = Assert.Single(fixture.Usage.Records);
        Assert.Equal(Model, usage.Model);
        Assert.Equal(UsageConstants.Operations.LessonChatHint, usage.Operation);
        Assert.Equal(UsageConstants.Statuses.Success, usage.Status);
        Assert.Equal("French", usage.StudyLanguage);
        Assert.Equal(10, usage.InputTokens);
        Assert.Equal(4, usage.OutputTokens);
        AssertOutcome(fixture, "success", "provider_success", 6999d);
    }

    [Theory]
    [MemberData(nameof(LessonHintLanguageContractTests.FallbackLanguageCases), MemberType = typeof(LessonHintLanguageContractTests))]
    public async Task InternalDeadlineReturnsExistingLanguageFallbackAtSevenSeconds(string languageId, string expectedHint)
    {
        using var fixture = new Fixture(languageId: languageId);
        fixture.Handler.BeforeResponse = token =>
        {
            fixture.Time.Advance(TimeSpan.FromMilliseconds(6999));
            Assert.False(token.IsCancellationRequested);
            fixture.Time.Advance(TimeSpan.FromMilliseconds(1));
            token.ThrowIfCancellationRequested();
        };
        var result = await fixture.Service.CreateHintAsync(fixture.Request, TestContext.Current.CancellationToken);
        Assert.Equal(expectedHint, result.HintText);
        Assert.Equal(TimeSpan.FromSeconds(7), fixture.Time.Elapsed);
        Assert.Single(fixture.Handler.Requests);
        Assert.Empty(fixture.Usage.Records);
        AssertOutcome(fixture, "fallback", "provider_timeout", 7000d);
    }

    [Fact]
    public async Task ProviderDeadlineIncludesReadingResponseBody()
    {
        using var fixture = new Fixture();
        fixture.Handler.ResponseContent = new StalledContent(fixture.Time);
        var result = await fixture.Service.CreateHintAsync(fixture.Request, TestContext.Current.CancellationToken);
        Assert.Equal(await ExpectedFallback(fixture), result.HintText);
        Assert.Empty(fixture.Usage.Records);
        AssertOutcome(fixture, "fallback", "provider_timeout", 7000d);
    }

    [Fact]
    public async Task ProviderFailureStillReturnsFallbackWithoutLoggingExceptionContent()
    {
        using var fixture = new Fixture();
        fixture.Handler.BeforeResponse = _ => throw new HttpRequestException(TestKey + UserText + HintText);
        var result = await fixture.Service.CreateHintAsync(fixture.Request, TestContext.Current.CancellationToken);
        Assert.Equal(await ExpectedFallback(fixture), result.HintText);
        Assert.Single(fixture.Handler.Requests);
        Assert.Empty(fixture.Usage.Records);
        AssertOutcome(fixture, "fallback", nameof(HttpRequestException), 0d);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerCancellationPropagatesWithoutTimeoutFallbackOrRetry(bool simultaneousDeadline)
    {
        using var fixture = new Fixture();
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        fixture.Handler.BeforeResponse = token =>
        {
            caller.Cancel();
            if (simultaneousDeadline) fixture.Time.Advance(TimeSpan.FromSeconds(7));
            token.ThrowIfCancellationRequested();
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.CreateHintAsync(fixture.Request, caller.Token));
        Assert.Single(fixture.Handler.Requests);
        Assert.Empty(fixture.Usage.Records);
        AssertOutcome(fixture, "canceled", "caller_cancellation", simultaneousDeadline ? 7000d : 0d);
    }

    private static async Task<string> ExpectedFallback(Fixture fixture) =>
        (await new MockLessonHintService().CreateHintAsync(fixture.Request, TestContext.Current.CancellationToken)).HintText;

    private static void AssertOutcome(Fixture fixture, string outcome, string reason, double elapsedMs)
    {
        var entry = Assert.Single(fixture.Logger.Entries);
        Assert.Equal(outcome, entry.Fields["Outcome"]);
        Assert.Equal(reason, entry.Fields["Reason"]);
        Assert.Equal(Model, entry.Fields["Model"]);
        Assert.Equal(elapsedMs, entry.Fields["ElapsedMs"]);
        Assert.Null(entry.Exception);
        foreach (var forbidden in new[] { TestKey, UserText, HintText, OpenAiConstants.LessonHintSystemInstructions, "Authorization" })
            Assert.DoesNotContain(forbidden, entry.Message);
    }

    private sealed class Fixture : IDisposable
    {
        public ManualTimeProvider Time { get; } = new();
        public Handler Handler { get; } = new();
        public RecordingLogger Logger { get; } = new();
        public RecordingUsage Usage { get; } = new();
        public LessonPromptBuilder Builder { get; }
        public TutorBehaviorProfileResolver Resolver { get; }
        public OpenAiLessonHintService Service { get; }
        private readonly HttpClient _client;
        public LessonChatRequest Request { get; }
        private static LessonChatRequest CreateRequest(string languageId) => new()
        {
            SelectedLevel = "A1", TopicTitle = "Everyday conversation", SubtopicTitle = "Ordering coffee",
            UserMessage = UserText, LastBotMessage = "What would you like?", TutorAvatarId = "lana",
            TargetLanguageId = languageId, TargetLanguageName = "French"
        };
        public Fixture(bool omitTemperature = true, string languageId = "fr")
        {
            Request = CreateRequest(languageId);
            _client = new HttpClient(Handler);
            var avatars = new TutorAvatarProfileProvider(NullLogger<TutorAvatarProfileProvider>.Instance);
            Builder = new LessonPromptBuilder(avatars);
            Resolver = new TutorBehaviorProfileResolver(new StaticRuntimeService(), avatars);
            Service = new OpenAiLessonHintService(
                new OpenAiOptionsProvider(new FakeSettings(AiModelSettings.Defaults with
                {
                    LessonTutorChatModel = "lesson-chat-distinct-model", LessonHintModel = Model,
                    LessonHintOmitTemperature = omitTemperature
                }), () => TestKey), new MockLessonHintService(), Builder, Resolver,
                new ClientFactory(_client), new FakeUser(), Usage, Logger, Time);
        }
        public void Dispose() => _client.Dispose();
    }

    private sealed class ClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class Handler : HttpMessageHandler
    {
        public List<JsonElement> Requests { get; } = [];
        public Action<CancellationToken>? BeforeResponse { get; set; }
        public HttpContent? ResponseContent { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(OpenAiConstants.ResponsesEndpoint, request.RequestUri?.ToString());
            Assert.Equal(OpenAiConstants.AuthorizationScheme, request.Headers.Authorization?.Scheme);
            Assert.Equal(TestKey, request.Headers.Authorization?.Parameter);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Requests.Add(body.RootElement.Clone());
            BeforeResponse?.Invoke(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = ResponseContent ?? new StringContent(JsonSerializer.Serialize(new
                {
                    output = new[] { new { content = new[] { new { text = JsonSerializer.Serialize(new { hintText = HintText }) } } } },
                    usage = new { input_tokens = 10, output_tokens = 4 }
                }), Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class StalledContent(ManualTimeProvider time) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => throw new NotSupportedException();
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            time.Advance(TimeSpan.FromSeconds(7));
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Provider body deadline was not applied.");
        }
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
    }

    private sealed class FakeSettings(AiModelSettings settings) : IAiModelSettingsService
    {
        public AiModelSettings GetActiveSettings() => settings;
        public Task<AiModelSettingsResponse> GetAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> SaveDraftAsync(AiModelSettings draft, string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
        public AiModelSettingsValidationResponse Validate(AiModelSettings candidate) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> PublishAsync(string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> ResetDraftFromActiveAsync(string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
    private sealed class StaticRuntimeService : ICmsRuntimeLessonContentService
    {
        public Task<CmsRuntimeLessonContentReadResult> ReadRuntimeLessonContentAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new CmsRuntimeLessonContentReadResult { Success = false });
    }
    private sealed class FakeUser : IRequestUserResolver
    {
        public ResolvedRequestUser ResolveCurrentUser() => new(Guid.Parse("55b1c643-1cce-499a-9e83-556bc18d1b37"), RequestUserResolver.AuthenticatedSource);
    }
    private sealed class RecordingUsage : IUsageEventService
    {
        public List<UsageEventRecord> Records { get; } = [];
        public Task TryRecordAsync(UsageEventRecord record, CancellationToken cancellationToken = default) { Records.Add(record); return Task.CompletedTask; }
    }
    private sealed record LogEntry(string Message, Dictionary<string, object?> Fields, Exception? Exception);
    private sealed class RecordingLogger : ILogger<OpenAiLessonHintService>
    {
        public List<LogEntry> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add(new LogEntry(formatter(state, exception), ((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(pair => pair.Key, pair => pair.Value), exception));
    }
    // Same deterministic timer pattern as RealtimeSpeechSynthesisServiceTests.
    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly List<ManualTimer> _timers = [];
        public TimeSpan Elapsed { get; private set; }
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Elapsed.Ticks;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            timer.Change(dueTime, period);
            _timers.Add(timer);
            return timer;
        }
        public void Advance(TimeSpan amount)
        {
            Elapsed += amount;
            foreach (var timer in _timers.ToArray()) timer.Fire();
        }
        private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
        {
            private TimeSpan? _due;
            public bool Change(TimeSpan dueTime, TimeSpan period) { _due = dueTime == Timeout.InfiniteTimeSpan ? null : owner.Elapsed + dueTime; return true; }
            public void Fire() { if (_due is { } due && due <= owner.Elapsed) { _due = null; callback(state); } }
            public void Dispose() => _due = null;
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
