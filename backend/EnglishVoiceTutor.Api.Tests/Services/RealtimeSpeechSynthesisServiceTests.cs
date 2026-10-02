using System.Buffers.Binary;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Usage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class RealtimeSpeechSynthesisServiceTests
{
    private const string FinalText = "Café is ready. Are you joining?";
    private const string TestKey = "test-api-key";
    private static readonly byte[] Pcm = [1, 2, 3, 4, 5, 6, 7, 8];

    [Theory]
    [InlineData("gpt-realtime-2.1-mini", false, true)]
    [InlineData("gpt-realtime-2.1-mini", true, true)]
    [InlineData("gpt-4o-mini-tts", false, false)]
    [InlineData("gpt-4o-mini-tts", true, false)]
    [InlineData("gpt-realtime", false, false)]
    [InlineData("gpt-realtime", true, false)]
    [InlineData("gpt-realtime-2.1-mini-other", false, false)]
    [InlineData("gpt-realtime-2.1-mini-other", true, false)]
    [InlineData("GPT-REALTIME-2.1-MINI", false, false)]
    [InlineData("GPT-REALTIME-2.1-MINI", true, false)]
    public async Task OnlyExactVerifiedModelSelectsRealtime(string model, bool streaming, bool realtime)
    {
        using var fixture = new Fixture(AiModelSettings.Defaults with
        {
            LessonChatTextToSpeechModel = model,
            RealtimeVoiceModel = OpenAiConstants.RealtimeSpeechSynthesisModel
        });
        if (streaming)
        {
            await using var output = new MemoryStream();
            await fixture.Audio.StreamSpeechAsync(FinalText, output,
                clientCancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(Pcm, output.ToArray());
        }
        else
        {
            await fixture.Audio.CreateSpeechAsync(FinalText, model: "client-model-must-not-win",
                clientCancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(model, Assert.Single(fixture.Usage.Records).Model);
        }
        Assert.Equal(realtime ? 1 : 0, fixture.Connector.Endpoints.Count);
        Assert.Equal(realtime ? 0 : 1, fixture.Http.Requests.Count);
        if (!realtime)
        {
            Assert.Equal(model, Assert.Single(fixture.Http.Requests).GetProperty("model").GetString());
        }
        AssertSafeLogs(fixture.Logger);
    }

    [Theory]
    [InlineData("lesson_chat_tts")]
    [InlineData("conversation_mode_tts")]
    public async Task ActiveRoleVoiceAndStyleReachAudioOnlyNoContextRenderer(string purpose)
    {
        var models = AiModelSettings.Defaults with
        {
            LessonChatTextToSpeechModel = purpose == AudioSpeechService.DefaultPurpose ? OpenAiConstants.RealtimeSpeechSynthesisModel : "legacy-tts",
            ConversationModeTextToSpeechModel = purpose == AudioSpeechService.ConversationModeTtsPurpose ? OpenAiConstants.RealtimeSpeechSynthesisModel : "legacy-tts"
        };
        using var fixture = new Fixture(models);
        fixture.Socket.BeforeReceive = (index, _) =>
        {
            Assert.Equal(index < 2 ? 1 : 2, fixture.Socket.Sent.Count);
            return Task.CompletedTask;
        };
        await fixture.Audio.CreateSpeechAsync(FinalText, purpose, speechVoice: " sage ",
            instructions: "Speak calmly. Do not rush.", targetLanguageName: "French",
            clientCancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("wss://api.openai.com/v1/realtime?model=gpt-realtime-2.1-mini", Assert.Single(fixture.Connector.Endpoints).ToString());
        var sessionEvent = fixture.Socket.Sent[0];
        Assert.Equal("session.update", sessionEvent.GetProperty("type").GetString());
        var session = sessionEvent.GetProperty("session");
        Assert.Equal("realtime", session.GetProperty("type").GetString());
        Assert.Equal(OpenAiConstants.RealtimeSpeechSynthesisModel, session.GetProperty("model").GetString());
        Assert.Equal(["audio"], session.GetProperty("output_modalities").EnumerateArray().Select(value => value.GetString()));
        Assert.False(session.TryGetProperty("tools", out _));
        var audio = session.GetProperty("audio");
        Assert.Equal(["output"], audio.EnumerateObject().Select(property => property.Name));
        var output = audio.GetProperty("output");
        Assert.Equal("sage", output.GetProperty("voice").GetString());
        Assert.Equal("audio/pcm", output.GetProperty("format").GetProperty("type").GetString());
        Assert.Equal(24000, output.GetProperty("format").GetProperty("rate").GetInt32());

        var responseEvent = fixture.Socket.Sent[1];
        Assert.Equal("response.create", responseEvent.GetProperty("type").GetString());
        var response = responseEvent.GetProperty("response");
        Assert.Empty(response.GetProperty("input").EnumerateArray());
        Assert.Equal(["audio"], response.GetProperty("output_modalities").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(["input", "output_modalities", "instructions"], response.EnumerateObject().Select(property => property.Name));
        var instructions = response.GetProperty("instructions").GetString()!;
        foreach (var requirement in new[] { "already the final tutor reply", "Speak exactly", "Do not answer", "continue it", "explain it", "Do not add words", "remove words", "paraphrase", "takes priority", "Speak calmly. Do not rush." })
        {
            Assert.Contains(requirement, instructions);
        }
        Assert.EndsWith(FinalText, instructions);
        var usage = Assert.Single(fixture.Usage.Records);
        Assert.Equal(OpenAiConstants.RealtimeSpeechSynthesisModel, usage.Model);
        Assert.Equal("French", usage.StudyLanguage);
        Assert.Equal(UsageConstants.Operations.Tts, usage.Operation);
        Assert.Equal(UsageConstants.Statuses.Success, usage.Status);
        Assert.Equal(10, usage.InputTokens);
        Assert.Equal(6, usage.AudioOutputTokens);
        Assert.Contains(fixture.Logger.Entries, entry => Equals(entry.Fields.GetValueOrDefault("Purpose"), purpose));
        AssertClosed(fixture.Socket);
        AssertSafeLogs(fixture.Logger);
    }

    [Fact]
    public async Task NonStreamingConcatenatesDeltasAndBuildsValidPcmWav()
    {
        using var fixture = new Fixture();
        fixture.Socket.FragmentSize = 7;
        var wav = await fixture.Audio.CreateSpeechAsync(FinalText, clientCancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
        Assert.Equal(wav.Length - 8, BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(4)));
        Assert.Equal("WAVEfmt ", Encoding.ASCII.GetString(wav, 8, 8));
        Assert.Equal(16, BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(16)));
        Assert.Equal(1, BinaryPrimitives.ReadInt16LittleEndian(wav.AsSpan(20)));
        Assert.Equal(1, BinaryPrimitives.ReadInt16LittleEndian(wav.AsSpan(22)));
        Assert.Equal(24000, BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(24)));
        Assert.Equal(48000, BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(28)));
        Assert.Equal(2, BinaryPrimitives.ReadInt16LittleEndian(wav.AsSpan(32)));
        Assert.Equal(16, BinaryPrimitives.ReadInt16LittleEndian(wav.AsSpan(34)));
        Assert.Equal("data", Encoding.ASCII.GetString(wav, 36, 4));
        Assert.Equal(Pcm.Length, BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(40)));
        Assert.Equal(Pcm, wav[44..]);
        Assert.Equal(wav.Length, Assert.Single(fixture.Usage.Records).OutputBytes);
        AssertClosed(fixture.Socket);
    }

    [Fact]
    public async Task StreamingWritesAndFlushesEachDeltaBeforeCompletion()
    {
        using var fixture = new Fixture();
        await using var output = new ObservedStream(fixture.Socket);
        fixture.Socket.BeforeReceive = (index, _) =>
        {
            if (index == 4) Assert.Equal(4, output.Length);
            if (index == 5) Assert.Equal(8, output.Length);
            return Task.CompletedTask;
        };
        var metrics = await fixture.Audio.StreamSpeechAsync(FinalText, output,
            clientCancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(Pcm, output.ToArray());
        Assert.Equal([false, false], output.CompletionAtWrite);
        Assert.Equal(2, output.Flushes);
        Assert.Equal(8, metrics.TotalBytes);
        Assert.NotNull(metrics.FirstChunkMs);
        Assert.NotNull(metrics.FirstChunkWrittenMs);
        Assert.Empty(fixture.Usage.Records); // Preserve existing stream accounting (diagnostics only).
        AssertClosed(fixture.Socket);
    }

    [Theory]
    [InlineData("Café is ready. Are you joining?", true)]
    [InlineData("  Café is ready.\r\n  Are you joining?  ", true)]
    [InlineData("Cafe\u0301 is ready. Are you joining?", true)]
    [InlineData("Café is ready! Are you joining?", false)]
    [InlineData("Café is ready. Are you leaving?", false)]
    [InlineData("café is ready. Are you joining?", false)]
    public async Task TranscriptDeltasUseNarrowNormalizationAndMismatchOnlyWarns(string transcript, bool expectedMatch)
    {
        using var fixture = new Fixture(events: SuccessEvents(transcript, transcriptDone: false));
        var wav = await fixture.Audio.CreateSpeechAsync(FinalText, clientCancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(Pcm, wav[44..]);
        var fidelity = Assert.Single(fixture.Logger.Entries, entry => entry.Fields.ContainsKey("TranscriptMatches"));
        Assert.Equal(expectedMatch, fidelity.Fields["TranscriptMatches"]);
        Assert.Equal(transcript.Length, fidelity.Fields["TranscriptCharacters"]);
        Assert.Equal(FinalText.Length, fidelity.Fields["InputCharacters"]);
        Assert.Equal(expectedMatch ? LogLevel.Information : LogLevel.Warning, fidelity.Level);
        AssertSafeLogs(fixture.Logger, transcript);
    }

    [Fact]
    public async Task TranscriptDoneIsHandledWithoutDuplicatingDeltas()
    {
        using var fixture = new Fixture();
        await fixture.Audio.CreateSpeechAsync(FinalText, clientCancellationToken: TestContext.Current.CancellationToken);
        var fidelity = Assert.Single(fixture.Logger.Entries, entry => entry.Fields.ContainsKey("TranscriptMatches"));
        Assert.Equal(FinalText.Length, fidelity.Fields["TranscriptCharacters"]);
        Assert.Equal(true, fidelity.Fields["TranscriptMatches"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UsageSupportsCurrentAndCompatibilityDetailFieldNames(bool plural)
    {
        var usage = plural
            ? new { input_tokens = 10, output_tokens = 8, total_tokens = 18, input_tokens_details = new { cached_tokens = 2, audio_tokens = 0 }, output_tokens_details = new { audio_tokens = 6 } }
            : (object)new { input_tokens = 10, output_tokens = 8, total_tokens = 18, input_token_details = new { cached_tokens = 2, audio_tokens = 0 }, output_token_details = new { audio_tokens = 6 } };
        using var fixture = new Fixture(events: [.. SuccessEvents(FinalText).SkipLast(1), Done(usage: usage)]);
        var result = await fixture.Renderer.CreateSpeechAsync(Request(), TestKey, "lesson_chat_tts", null, TestContext.Current.CancellationToken);
        Assert.Equal(10, result.Usage.InputTokens);
        Assert.Equal(8, result.Usage.OutputTokens);
        Assert.Equal(18, result.Usage.TotalTokens);
        Assert.Equal(2, result.Usage.CachedInputTokens);
        Assert.Equal(0, result.Usage.AudioInputTokens);
        Assert.Equal(6, result.Usage.AudioOutputTokens);
        Assert.True(result.Usage.HasExactUsage);
        var diagnostic = Assert.Single(fixture.Logger.Entries, entry => entry.Fields.ContainsKey("CachedInputTokens"));
        Assert.Equal(2L, diagnostic.Fields["CachedInputTokens"]);
        AssertSafeLogs(fixture.Logger);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("wrong-types")]
    public async Task MissingOrMalformedUsageCannotExposeTextOrFailValidAudio(string variant)
    {
        var done = variant switch
        {
            "missing" => "{\"type\":\"response.done\",\"response\":{\"id\":\"resp_test\",\"status\":\"completed\"}}",
            "null" => Done(usage: null),
            _ => Done(usage: new { input_tokens = FinalText, output_tokens = -1, total_tokens = "many", input_token_details = TestKey, output_token_details = new { audio_tokens = "bad" } })
        };
        using var fixture = new Fixture(events: [.. SuccessEvents(FinalText).SkipLast(1), done]);
        var result = await fixture.Renderer.CreateSpeechAsync(Request(), TestKey, "lesson_chat_tts", null, TestContext.Current.CancellationToken);
        Assert.False(result.Usage.HasExactUsage);
        Assert.Null(result.Usage.CachedInputTokens);
        AssertSafeLogs(fixture.Logger);
    }

    [Theory]
    [InlineData("provider-error")]
    [InlineData("failed")]
    [InlineData("cancelled")]
    [InlineData("incomplete")]
    [InlineData("premature-close")]
    [InlineData("empty-audio")]
    [InlineData("invalid-base64")]
    [InlineData("malformed-json")]
    [InlineData("malformed-delta")]
    [InlineData("malformed-transcript")]
    [InlineData("unknown-failed-state")]
    [InlineData("missing-status")]
    [InlineData("odd-pcm-length")]
    [InlineData("before-session-updated")]
    public async Task FailedProviderSequenceNeverReturnsSuccessfulPartialWav(string scenario)
    {
        var events = new List<string?> { SessionCreated, SessionUpdated, ResponseCreated };
        if (scenario != "empty-audio") events.Add(Delta([1, 2, 3, 4]));
        events.Add(scenario switch
        {
            "provider-error" => Event(new { type = "error", error = new { message = FinalText + TestKey } }),
            "failed" or "cancelled" or "incomplete" => Done(scenario),
            "premature-close" => null,
            "invalid-base64" => Event(new { type = "response.output_audio.delta", delta = "invalid%%%" }),
            "malformed-json" => "{invalid " + FinalText + TestKey,
            "malformed-delta" => Event(new { type = "response.output_audio.delta", delta = 5 }),
            "malformed-transcript" => Event(new { type = "response.output_audio_transcript.done", transcript = (string?)null }),
            "unknown-failed-state" => Event(new { type = "response.future_event", response = new { status = "failed" } }),
            "missing-status" => Event(new { type = "response.done", response = new { id = "resp_test" } }),
            "odd-pcm-length" => Delta([5]),
            _ => Done()
        });
        if (scenario == "odd-pcm-length") events.Add(Done());
        if (scenario == "before-session-updated") events = [SessionCreated, Delta([1, 2]), Done()];
        using var fixture = new Fixture(events: events);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => fixture.Audio.CreateSpeechAsync(
            FinalText, clientCancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("OpenAI speech generation request failed.", exception.Message);
        Assert.DoesNotContain(FinalText, exception.ToString());
        Assert.DoesNotContain(TestKey, exception.ToString());
        Assert.Empty(fixture.Usage.Records);
        AssertClosed(fixture.Socket);
        AssertSafeLogs(fixture.Logger);
    }

    [Fact]
    public async Task StreamFailureAfterAudioStillFailsAndClosesProvider()
    {
        using var fixture = new Fixture(events: [SessionCreated, SessionUpdated, ResponseCreated, Delta([1, 2]), null]);
        await using var output = new MemoryStream();
        await Assert.ThrowsAsync<HttpRequestException>(() => fixture.Audio.StreamSpeechAsync(FinalText, output,
            clientCancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(new byte[] { 1, 2 }, output.ToArray());
        Assert.DoesNotContain(fixture.Logger.Entries, entry => entry.Message.Contains("renderer completed", StringComparison.Ordinal));
        AssertClosed(fixture.Socket);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClientCancellationStopsReceiveAndUsesExistingBoundary(bool streaming)
    {
        using var fixture = new Fixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        fixture.Socket.BeforeReceive = async (_, token) =>
        {
            cancellation.Cancel();
            await Task.Delay(Timeout.Infinite, token);
        };
        await using var output = new MemoryStream();
        var exception = await Assert.ThrowsAsync<AudioSpeechRequestCanceledException>(async () =>
        {
            if (streaming) await fixture.Audio.StreamSpeechAsync(FinalText, output, clientCancellationToken: cancellation.Token);
            else await fixture.Audio.CreateSpeechAsync(FinalText, clientCancellationToken: cancellation.Token);
        });
        Assert.True(exception.ClientCancellationRequested);
        Assert.False(exception.InternalTimeoutReached);
        AssertClosed(fixture.Socket, expectCloseFrame: false);
    }

    [Theory]
    [InlineData(false, false, 20)]
    [InlineData(true, false, 5)]
    [InlineData(true, true, 20)]
    public async Task InternalTimeoutUsesExistingBoundaryAndReleasesSocket(bool streaming, bool afterAudio, int seconds)
    {
        var time = new ManualTimeProvider();
        using var fixture = new Fixture(timeProvider: time);
        fixture.Socket.BeforeReceive = (index, token) =>
        {
            if (index == (afterAudio ? 4 : 0))
            {
                time.Advance(TimeSpan.FromSeconds(seconds));
                token.ThrowIfCancellationRequested();
            }
            return Task.CompletedTask;
        };
        await using var output = new MemoryStream();
        var exception = await Assert.ThrowsAsync<AudioSpeechRequestCanceledException>(async () =>
        {
            if (streaming) await fixture.Audio.StreamSpeechAsync(FinalText, output, clientCancellationToken: TestContext.Current.CancellationToken);
            else await fixture.Audio.CreateSpeechAsync(FinalText, clientCancellationToken: TestContext.Current.CancellationToken);
        });
        Assert.True(exception.InternalTimeoutReached);
        Assert.False(exception.ClientCancellationRequested);
        var diagnostic = Assert.Single(fixture.Logger.Entries, entry => entry.Fields.ContainsKey("FirstAudioTimeoutReached"));
        Assert.Equal(streaming && !afterAudio, diagnostic.Fields["FirstAudioTimeoutReached"]);
        AssertClosed(fixture.Socket, expectCloseFrame: false);
    }

    [Fact]
    public async Task EachSpeechRequestUsesAndDisposesItsOwnSocket()
    {
        using var fixture = new Fixture();
        var second = new ScriptedWebSocket(SuccessEvents(FinalText));
        fixture.Connector.Sockets.Enqueue(second);
        await fixture.Audio.CreateSpeechAsync(FinalText, clientCancellationToken: TestContext.Current.CancellationToken);
        await fixture.Audio.CreateSpeechAsync(FinalText, clientCancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, fixture.Connector.Endpoints.Count);
        AssertClosed(fixture.Socket);
        AssertClosed(second);
        Assert.Equal(2, second.Sent.Count);
    }

    [Theory]
    [InlineData(false, "lesson_chat_tts")]
    [InlineData(true, "lesson_chat_tts")]
    [InlineData(false, "conversation_mode_tts")]
    [InlineData(true, "conversation_mode_tts")]
    public async Task BothRealtimeSpeechPathsReceiveFixedSpeedWithoutNumericProviderConfiguration(bool streaming, string purpose)
    {
        using var fixture = new Fixture();
        await using var output = new MemoryStream();
        if (streaming)
        {
            await fixture.Audio.StreamSpeechAsync(FinalText, output, purpose,
                TestContext.Current.CancellationToken, speechSpeed: 1.2);
        }
        else
        {
            await fixture.Audio.CreateSpeechAsync(FinalText, purpose, speechSpeed: 1.2,
                clientCancellationToken: TestContext.Current.CancellationToken);
        }
        Assert.Equal(2, fixture.Socket.Sent.Count);
        Assert.DoesNotContain("\"speed\"", string.Join("", fixture.Socket.Sent));
        Assert.Empty(fixture.Http.Requests);
        var diagnostic = Assert.Single(fixture.Logger.Entries, entry => entry.Fields.ContainsKey("RequestedSpeed"));
        Assert.Equal(1.0, diagnostic.Fields["RequestedSpeed"]);
        Assert.Contains("NumericSpeedApplied=False", diagnostic.Message);
    }

    private static OpenAiAudioSpeechRequest Request() => new()
    {
        Model = OpenAiConstants.RealtimeSpeechSynthesisModel, Input = FinalText,
        Voice = "coral", Speed = 1.0, ResponseFormat = "wav"
    };
    private static string Event(object value) => JsonSerializer.Serialize(value);
    private static readonly string SessionCreated = Event(new { type = "session.created" });
    private static readonly string SessionUpdated = Event(new { type = "session.updated" });
    private static readonly string ResponseCreated = Event(new { type = "response.created", response = new { id = "resp_test", status = "in_progress" } });
    private static string Delta(byte[] bytes) => Event(new { type = "response.output_audio.delta", delta = Convert.ToBase64String(bytes) });
    private static string Done(string status = "completed", object? usage = null) => Event(new { type = "response.done", response = new { id = "resp_test", status, usage } });
    private static List<string?> SuccessEvents(string transcript, bool transcriptDone = true)
    {
        var middle = transcript.Length / 2;
        var result = new List<string?> { SessionCreated, SessionUpdated, ResponseCreated, Delta(Pcm[..4]), Delta(Pcm[4..]),
            Event(new { type = "response.output_audio.done" }), Event(new { type = "rate_limits.updated" }),
            Event(new { type = "response.output_audio_transcript.delta", delta = transcript[..middle] }),
            Event(new { type = "response.output_audio_transcript.delta", delta = transcript[middle..] }) };
        if (transcriptDone) result.Add(Event(new { type = "response.output_audio_transcript.done", transcript }));
        result.Add(Done(usage: new { input_tokens = 10, output_tokens = 8, total_tokens = 18,
            input_token_details = new { cached_tokens = 2, audio_tokens = 0 }, output_token_details = new { audio_tokens = 6 } }));
        return result;
    }

    private static void AssertSafeLogs(RecordingLogger logger, string? transcript = null)
    {
        var logs = string.Join("\n", logger.Entries.Select(entry => entry.Message));
        foreach (var forbidden in new[] { TestKey, FinalText, transcript, Convert.ToBase64String(Pcm[..4]), "Authorization", "Already-final tutor text to speak exactly" }.Where(value => !string.IsNullOrEmpty(value)))
        {
            Assert.DoesNotContain(forbidden!, logs);
        }
    }
    private static void AssertClosed(ScriptedWebSocket socket, bool expectCloseFrame = true)
    {
        Assert.True(socket.Disposed);
        Assert.True(socket.Aborted);
        Assert.Equal(0, socket.ActiveReceives);
        if (expectCloseFrame) Assert.True(socket.CloseFrameSent);
    }

    private sealed class Fixture : IDisposable
    {
        public ScriptedWebSocket Socket { get; }
        public FakeConnector Connector { get; }
        public CapturingHttpClientFactory Http { get; } = new();
        public RecordingLogger Logger { get; } = new();
        public RecordingUsageEventService Usage { get; } = new();
        public RealtimeSpeechSynthesisService Renderer { get; }
        public AudioSpeechService Audio { get; }
        public Fixture(AiModelSettings? settings = null, IEnumerable<string?>? events = null, TimeProvider? timeProvider = null)
        {
            Socket = new ScriptedWebSocket(events ?? SuccessEvents(FinalText));
            Connector = new FakeConnector(Socket);
            Renderer = new RealtimeSpeechSynthesisService(Connector, Logger, timeProvider);
            var models = new FakeAiModelSettingsService(settings ?? AiModelSettings.Defaults with
            {
                LessonChatTextToSpeechModel = OpenAiConstants.RealtimeSpeechSynthesisModel,
                ConversationModeTextToSpeechModel = OpenAiConstants.RealtimeSpeechSynthesisModel
            });
            Audio = new AudioSpeechService(new OpenAiOptionsProvider(models, () => TestKey), Http,
                new FakeRequestUserResolver(), Usage, models, NullLogger<AudioSpeechService>.Instance, Renderer);
        }
        public void Dispose() => Http.Dispose();
    }

    private sealed class FakeConnector(ScriptedWebSocket socket) : IRealtimeSpeechSocketConnector
    {
        public Queue<WebSocket> Sockets { get; } = new([socket]);
        public List<Uri> Endpoints { get; } = [];
        public Task<WebSocket> ConnectAsync(Uri endpoint, string apiKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(TestKey, apiKey);
            Endpoints.Add(endpoint);
            return Task.FromResult(Sockets.Dequeue());
        }
    }

    private sealed class ScriptedWebSocket(IEnumerable<string?> events) : WebSocket
    {
        private readonly Queue<string?> _events = new(events);
        private WebSocketState _state = WebSocketState.Open;
        private byte[]? _current;
        private int _offset;
        private int _eventIndex;
        public int FragmentSize { get; set; } = int.MaxValue;
        public Func<int, CancellationToken, Task>? BeforeReceive { get; set; }
        public List<JsonElement> Sent { get; } = [];
        public bool Disposed { get; private set; }
        public bool Aborted { get; private set; }
        public bool CloseFrameSent { get; private set; }
        public bool DoneReceived { get; private set; }
        public int ActiveReceives { get; private set; }
        public override WebSocketCloseStatus? CloseStatus => _state == WebSocketState.CloseReceived ? WebSocketCloseStatus.NormalClosure : null;
        public override string? CloseStatusDescription => FinalText + TestKey;
        public override WebSocketState State => _state;
        public override string? SubProtocol => null;
        public override void Abort() { Aborted = true; _state = WebSocketState.Aborted; }
        public override void Dispose() { Disposed = true; _state = WebSocketState.Closed; }
        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            CloseFrameSent = true;
            _state = WebSocketState.CloseSent;
            return Task.CompletedTask;
        }
        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(WebSocketMessageType.Text, messageType);
            Assert.True(endOfMessage);
            using var document = JsonDocument.Parse(buffer.AsMemory());
            Sent.Add(document.RootElement.Clone());
            return Task.CompletedTask;
        }
        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            ActiveReceives++;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_current is null)
                {
                    if (BeforeReceive is not null) await BeforeReceive(_eventIndex, cancellationToken);
                    var next = _events.Count == 0 ? null : _events.Dequeue();
                    _eventIndex++;
                    if (next is null)
                    {
                        _state = WebSocketState.CloseReceived;
                        return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
                    }
                    _current = Encoding.UTF8.GetBytes(next);
                    _offset = 0;
                }
                var count = Math.Min(Math.Min(buffer.Count, FragmentSize), _current.Length - _offset);
                _current.AsSpan(_offset, count).CopyTo(buffer.AsSpan());
                _offset += count;
                var end = _offset == _current.Length;
                if (end)
                {
                    DoneReceived |= Encoding.UTF8.GetString(_current).Contains("\"type\":\"response.done\"", StringComparison.Ordinal);
                    _current = null;
                }
                return new WebSocketReceiveResult(count, WebSocketMessageType.Text, end);
            }
            finally { ActiveReceives--; }
        }
    }

    private sealed class ObservedStream(ScriptedWebSocket socket) : MemoryStream
    {
        public List<bool> CompletionAtWrite { get; } = [];
        public int Flushes { get; private set; }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            CompletionAtWrite.Add(socket.DoneReceived);
            return base.WriteAsync(buffer, cancellationToken);
        }
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            Flushes++;
            return base.FlushAsync(cancellationToken);
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Dictionary<string, object?> Fields);
    private sealed class RecordingLogger : ILogger<RealtimeSpeechSynthesisService>
    {
        public List<LogEntry> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var fields = state is IEnumerable<KeyValuePair<string, object?>> values ? values.ToDictionary(pair => pair.Key, pair => pair.Value) : [];
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), fields));
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly List<ManualTimer> _timers = [];
        private TimeSpan _elapsed;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            timer.Change(dueTime, period);
            _timers.Add(timer);
            return timer;
        }
        public void Advance(TimeSpan amount)
        {
            _elapsed += amount;
            foreach (var timer in _timers.ToArray()) timer.Fire();
        }
        private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
        {
            private TimeSpan? _due;
            public bool Change(TimeSpan dueTime, TimeSpan period) { _due = dueTime == Timeout.InfiniteTimeSpan ? null : owner._elapsed + dueTime; return true; }
            public void Fire()
            {
                if (_due is { } due && due <= owner._elapsed) { _due = null; callback(state); }
            }
            public void Dispose() => _due = null;
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }

    private sealed class CapturingHttpClientFactory : IHttpClientFactory, IDisposable
    {
        private readonly HttpClient _client;
        public List<JsonElement> Requests { get; } = [];
        public CapturingHttpClientFactory() => _client = new HttpClient(new Handler(this));
        public HttpClient CreateClient(string name) { Assert.Equal(OpenAiConstants.AudioSpeechHttpClientName, name); return _client; }
        public void Dispose() => _client.Dispose();
        private sealed class Handler(CapturingHttpClientFactory owner) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal(OpenAiConstants.AudioSpeechEndpoint, request.RequestUri?.ToString());
                using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                owner.Requests.Add(document.RootElement.Clone());
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Pcm) };
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
        public Task TryRecordAsync(UsageEventRecord record, CancellationToken cancellationToken = default) { Records.Add(record); return Task.CompletedTask; }
    }
    private sealed class FakeRequestUserResolver : IRequestUserResolver
    {
        public ResolvedRequestUser ResolveCurrentUser() => new(Guid.Parse("55b1c643-1cce-499a-9e83-556bc18d1b37"), RequestUserResolver.AuthenticatedSource);
    }
}
