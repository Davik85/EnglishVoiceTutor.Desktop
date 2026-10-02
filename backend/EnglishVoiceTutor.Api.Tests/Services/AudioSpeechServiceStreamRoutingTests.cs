using System.Net;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Usage;
using Microsoft.Extensions.Logging;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class AudioSpeechServiceStreamRoutingTests
{
    [Theory]
    [InlineData("lesson_chat_tts", "lesson-test-tts", " French ", "fr", "French")]
    [InlineData("CONVERSATION_MODE_TTS", "conversation-test-tts", " ", " fr ", "fr")]
    public async Task StreamUsesActiveRoleAndForwardsOptionalMetadata(
        string purpose, string expectedModel, string languageName, string languageId, string expectedLanguage)
    {
        using var fixture = new Fixture();
        await using var output = new MemoryStream();

        var metrics = await fixture.Service.StreamSpeechAsync(
            "  A tutor reply.  ", output, purpose, TestContext.Current.CancellationToken,
            speechSpeed: 0.8, instructions: "Speak calmly.", speechVoice: " sage ",
            targetLanguageName: languageName, targetLanguageId: languageId);

        var request = Assert.Single(fixture.Http.Requests);
        Assert.Equal(expectedModel, request.GetProperty("model").GetString());
        Assert.Equal("A tutor reply.", request.GetProperty("input").GetString());
        Assert.Equal("sage", request.GetProperty("voice").GetString());
        Assert.Equal(0.8, request.GetProperty("speed").GetDouble());
        Assert.Equal("Speak calmly.", request.GetProperty("instructions").GetString());
        Assert.Equal(OpenAiConstants.PcmSpeechResponseFormat, request.GetProperty("response_format").GetString());
        Assert.Equal(Fixture.Audio, output.ToArray());
        Assert.Equal(Fixture.Audio.Length, metrics.TotalBytes);
        var usage = Assert.Single(fixture.Logger.Entries, entry => entry.ContainsKey("StudyLanguage"));
        Assert.Equal(expectedLanguage, usage["StudyLanguage"]);
        Assert.Equal(purpose.ToLowerInvariant(), usage["Purpose"]);
        Assert.Equal(expectedModel, usage["Model"]);
    }

    [Theory]
    [InlineData("lesson_chat_tts", "lesson-test-tts")]
    [InlineData("conversation_mode_tts", "conversation-test-tts")]
    [InlineData("unknown-purpose", "lesson-test-tts")]
    [InlineData(null, "lesson-test-tts")]
    public async Task MissingOptionalValuesRetainDefaults(string? purpose, string expectedModel)
    {
        using var fixture = new Fixture();
        await using var output = new MemoryStream();

        await fixture.Service.StreamSpeechAsync("Tutor reply.", output, purpose, TestContext.Current.CancellationToken);

        var request = Assert.Single(fixture.Http.Requests);
        Assert.Equal(expectedModel, request.GetProperty("model").GetString());
        Assert.Equal(OpenAiConstants.DefaultSpeechVoice, request.GetProperty("voice").GetString());
        Assert.Equal(OpenAiConstants.DefaultSpeechSpeed, request.GetProperty("speed").GetDouble());
        Assert.False(request.TryGetProperty("instructions", out _));
        Assert.Equal(OpenAiConstants.DefaultBotVoiceStreamResponseFormat, request.GetProperty("response_format").GetString());
        Assert.Equal(Fixture.Audio, output.ToArray());
    }

    [Theory]
    [InlineData("custom-tts", "Speak calmly.", true)]
    [InlineData("custom-speech", "Speak calmly.", false)]
    [InlineData("custom-tts", " ", false)]
    public async Task InstructionsFollowExistingEffectiveModelRules(string model, string instructions, bool expectedInstructions)
    {
        using var fixture = new Fixture(AiModelSettings.Defaults with { ConversationModeTextToSpeechModel = model });
        await using var output = new MemoryStream();

        await fixture.Service.StreamSpeechAsync(
            "Tutor reply.", output, AudioSpeechService.ConversationModeTtsPurpose,
            TestContext.Current.CancellationToken, instructions: instructions, speechVoice: " ");

        var request = Assert.Single(fixture.Http.Requests);
        Assert.Equal(model, request.GetProperty("model").GetString());
        Assert.Equal(OpenAiConstants.DefaultSpeechVoice, request.GetProperty("voice").GetString());
        Assert.Equal(expectedInstructions, request.TryGetProperty("instructions", out var resolved));
        if (expectedInstructions)
        {
            Assert.Equal(instructions, resolved.GetString());
        }
    }

    [Theory]
    [InlineData("conversation_mode_tts", 1.2, 1.0)]
    [InlineData("conversation_mode_tts", 0.0, 1.0)]
    [InlineData("lesson_chat_tts", 1.2, 1.2)]
    public async Task SpeedUsesExistingPurposeRules(string purpose, double speed, double expectedSpeed)
    {
        using var fixture = new Fixture();
        await using var output = new MemoryStream();

        await fixture.Service.StreamSpeechAsync("Tutor reply.", output, purpose,
            TestContext.Current.CancellationToken, speechSpeed: speed);

        Assert.Equal(expectedSpeed, Assert.Single(fixture.Http.Requests).GetProperty("speed").GetDouble());
    }

    [Fact]
    public async Task ClientCancellationRetainsExistingFailureBoundary()
    {
        using var fixture = new Fixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        fixture.Http.BeforeResponse = token =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
        };
        await using var output = new MemoryStream();

        var exception = await Assert.ThrowsAsync<AudioSpeechRequestCanceledException>(() =>
            fixture.Service.StreamSpeechAsync("Tutor reply.", output,
                clientCancellationToken: cancellation.Token));

        Assert.True(exception.ClientCancellationRequested);
        Assert.False(exception.InternalTimeoutReached);
        Assert.Empty(output.ToArray());
    }

    [Theory]
    [InlineData("lesson_chat_tts", "lesson-test-tts")]
    [InlineData("conversation_mode_tts", "conversation-test-tts")]
    public async Task NonStreamingRetainsBackendAuthorityAndWavRequest(string purpose, string expectedModel)
    {
        using var fixture = new Fixture();

        var audio = await fixture.Service.CreateSpeechAsync(
            "  Tutor reply.  ", purpose, speechSpeed: 0.8,
            model: "backend-configured conversation TTS model", instructions: "Speak calmly.",
            speechVoice: "sage", targetLanguageName: "French",
            clientCancellationToken: TestContext.Current.CancellationToken);

        var request = Assert.Single(fixture.Http.Requests);
        Assert.Equal(expectedModel, request.GetProperty("model").GetString());
        Assert.Equal("  Tutor reply.  ", request.GetProperty("input").GetString());
        Assert.Equal(OpenAiConstants.WavSpeechResponseFormat, request.GetProperty("response_format").GetString());
        Assert.Equal("sage", request.GetProperty("voice").GetString());
        Assert.Equal(0.8, request.GetProperty("speed").GetDouble());
        Assert.Equal("Speak calmly.", request.GetProperty("instructions").GetString());
        Assert.Equal(Fixture.Audio, audio);
        var usage = Assert.Single(fixture.Usage.Records);
        Assert.Equal(expectedModel, usage.Model);
        Assert.Equal("French", usage.StudyLanguage);
    }

    private sealed class Fixture : IDisposable
    {
        public static readonly byte[] Audio = [1, 2, 3, 4];
        public CapturingHttpClientFactory Http { get; } = new();
        public RecordingLogger Logger { get; } = new();
        public RecordingUsageEventService Usage { get; } = new();
        public AudioSpeechService Service { get; }

        public Fixture(AiModelSettings? settings = null)
        {
            var models = new FakeAiModelSettingsService(settings ?? AiModelSettings.Defaults with
            {
                LessonChatTextToSpeechModel = "lesson-test-tts",
                ConversationModeTextToSpeechModel = "conversation-test-tts"
            });
            Service = new AudioSpeechService(new OpenAiOptionsProvider(models, () => "test-api-key"),
                Http, new FakeRequestUserResolver(), Usage, models, Logger);
        }

        public void Dispose() => Http.Dispose();
    }

    private sealed class CapturingHttpClientFactory : IHttpClientFactory, IDisposable
    {
        private readonly HttpClient _client;
        public List<JsonElement> Requests { get; } = [];
        public Action<CancellationToken>? BeforeResponse { get; set; }
        public CapturingHttpClientFactory() => _client = new HttpClient(new CapturingHandler(this));
        public HttpClient CreateClient(string name)
        {
            Assert.Equal(OpenAiConstants.AudioSpeechHttpClientName, name);
            return _client;
        }
        public void Dispose() => _client.Dispose();

        private sealed class CapturingHandler(CapturingHttpClientFactory owner) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal(OpenAiConstants.AudioSpeechEndpoint, request.RequestUri?.ToString());
                using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                owner.Requests.Add(document.RootElement.Clone());
                owner.BeforeResponse?.Invoke(cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Fixture.Audio) };
            }
        }
    }

    private sealed class RecordingLogger : ILogger<AudioSpeechService>
    {
        public List<Dictionary<string, object?>> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> values)
            {
                Entries.Add(values.ToDictionary(pair => pair.Key, pair => pair.Value));
            }
        }
    }

    private sealed class FakeAiModelSettingsService(AiModelSettings settings) : IAiModelSettingsService
    {
        public AiModelSettings GetActiveSettings() => settings;
        public Task<AiModelSettingsResponse> GetAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> SaveDraftAsync(AiModelSettings draft, string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
        public AiModelSettingsValidationResponse Validate(AiModelSettings candidate) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> PublishAsync(string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> ResetDraftFromActiveAsync(string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class RecordingUsageEventService : IUsageEventService
    {
        public List<UsageEventRecord> Records { get; } = [];
        public Task TryRecordAsync(UsageEventRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRequestUserResolver : IRequestUserResolver
    {
        public ResolvedRequestUser ResolveCurrentUser() =>
            new(Guid.Parse("55b1c643-1cce-499a-9e83-556bc18d1b37"), RequestUserResolver.AuthenticatedSource);
    }
}
