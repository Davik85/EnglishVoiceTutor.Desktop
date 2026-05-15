using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Models.RealtimeVoice;
using EnglishVoiceTutor.Shared.LessonPolicies;

namespace EnglishVoiceTutor.Api.Services;

public sealed class RealtimeVoiceSessionService
{
    private const string RealtimeWebSocketEndpoint = "wss://api.openai.com/v1/realtime?model=" + OpenAiConstants.DefaultRealtimeVoiceModel;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenAiOptionsProvider optionsProvider;
    private readonly ILogger<RealtimeVoiceSessionService> logger;
    private readonly LessonPromptBuilder lessonPromptBuilder;
    private ClientWebSocket? openAiSocket;
    private WebSocket? desktopSocket;
    private RealtimeVoiceSessionStartRequest? startRequest;
    private Stopwatch stopwatch = new();
    private string sessionId = string.Empty;
    private string activeResponseId = string.Empty;
    private string activeResponseUserTurnId = string.Empty;
    private readonly StringBuilder activeTranscript = new();
    private int learnerTurnCount;
    private string currentRealtimeUserTurnId = string.Empty;
    private int currentRealtimeUserTurnNumber;
    private int currentAudioChunkCount;
    private long currentRecordingStartMs;
    private bool currentRecordingFirstAudioChunkLogged;
    private bool firstAudioLogged;
    private bool firstTranscriptLogged;
    private long lastUserAudioCommitMs;
    private int inputAudioBytesBuffered;
    private long totalInputAudioBytes;
    private long totalCommittedAudioBytes;
    private long totalAssistantAudioBytes;
    private int audioCommitCount;
    private int realtimeUserTranscriptCharacters;
    private bool isResponseInProgress;
    private string pendingUserAudioItemId = string.Empty;
    private bool isAwaitingUserTranscript;
    private bool isStartupInProgress;
    private bool isSessionReady;
    private bool isDisconnecting;
    private bool startupFailureNotified;
    private bool softWrapUpLogged;
    private bool finalMessageRequiredLogged;
    private TaskCompletionSource<bool>? startupCompletionSource;
    private CancellationTokenSource? transcriptionTimeoutCancellationTokenSource;
    private const int InputAudioSampleRate = OpenAiConstants.RealtimeInputAudioSampleRate;
    private const int InputAudioBytesPerSample = 2;
    private const int MinimumInputAudioDurationMs = 500;
    private const int UserTranscriptTimeoutMilliseconds = 8000;
    private const int AudioAppendDebugLogEveryChunkCount = 25;
    private const string UnknownResponseId = "unknown";
    private const string RealtimeUserTurnIdPrefix = "rtut";
    private const int MinimumInputAudioBytes = InputAudioSampleRate * InputAudioBytesPerSample * MinimumInputAudioDurationMs / 1000;

    public RealtimeVoiceSessionService(
        OpenAiOptionsProvider optionsProvider,
        LessonPromptBuilder lessonPromptBuilder,
        ILogger<RealtimeVoiceSessionService> logger)
    {
        this.optionsProvider = optionsProvider;
        this.lessonPromptBuilder = lessonPromptBuilder;
        this.logger = logger;
    }

    public async Task RunGatewayAsync(WebSocket desktopWebSocket, CancellationToken cancellationToken)
    {
        desktopSocket = desktopWebSocket;
        stopwatch = Stopwatch.StartNew();
        try
        {
            await ReceiveDesktopEventsAsync(cancellationToken);
        }
        catch (WebSocketException exception) when (IsExpectedDesktopDisconnect(exception, cancellationToken))
        {
            logger.LogInformation("Realtime desktop socket disconnected without a full close handshake. SessionId={SessionId}; ResponseId={ResponseId}; SocketState={SocketState}; Message={Message}.",
                sessionId,
                activeResponseId,
                desktopSocket?.State,
                exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Realtime desktop socket receive loop canceled. SessionId={SessionId}; ResponseId={ResponseId}; SocketState={SocketState}.",
                sessionId,
                activeResponseId,
                desktopSocket?.State);
        }
        finally
        {
            await DisconnectAsync("desktop_disconnected", CancellationToken.None);
        }
    }

    private async Task ReceiveDesktopEventsAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[128 * 1024];
        var builder = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested && desktopSocket?.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await desktopSocket.ReceiveAsync(buffer, cancellationToken);
            }
            catch (WebSocketException exception) when (IsExpectedDesktopDisconnect(exception, cancellationToken))
            {
                logger.LogInformation("Realtime desktop receive ended because the client disconnected. SessionId={SessionId}; ResponseId={ResponseId}; SocketState={SocketState}; Message={Message}.",
                    sessionId,
                    activeResponseId,
                    desktopSocket?.State,
                    exception.Message);
                return;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            if (!result.EndOfMessage)
            {
                continue;
            }

            var json = builder.ToString();
            builder.Clear();
            await HandleDesktopEventAsync(json, cancellationToken);
        }
    }

    private async Task HandleDesktopEventAsync(string json, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var type = root.GetProperty("type").GetString() ?? string.Empty;
        sessionId = root.TryGetProperty("sessionId", out var sessionProperty) ? sessionProperty.GetString() ?? sessionId : sessionId;
        var payload = root.TryGetProperty("payload", out var payloadProperty) ? payloadProperty : default;

        switch (type)
        {
            case "session.start":
                var request = payload.Deserialize<RealtimeVoiceSessionStartRequest>(JsonOptions) ?? new RealtimeVoiceSessionStartRequest();
                logger.LogInformation("Realtime session start endpoint call received. SessionId={SessionId}; LessonType={LessonType}; Topic={Topic}; Subtopic={Subtopic}; Level={Level}.",
                    request.SessionId,
                    request.LessonType,
                    string.IsNullOrWhiteSpace(request.Topic) ? request.TopicTitle : request.Topic,
                    string.IsNullOrWhiteSpace(request.Subtopic) ? request.SubtopicTitle : request.Subtopic,
                    request.SelectedLevel);
                await StartOpenAiSessionAsync(request, cancellationToken);
                break;
            case "user.text":
                EnforceTurnLimit();
                var userText = payload.GetProperty("text").GetString() ?? string.Empty;
                var textValidation = LessonTranscriptValidator.Validate(userText);
                if (!textValidation.IsValid)
                {
                    logger.LogInformation("Realtime user text rejected by transcript policy. SessionId={SessionId}; Reason={Reason}; LearnerTurnCountBefore={LearnerTurnCount}; NormalAssistantResponseCreated=False; RetryPromptShown=True.", sessionId, textValidation.Reason, learnerTurnCount);
                    await SendDesktopEventAsync(new { type = "user.transcript.failed", sessionId, itemId = string.Empty, message = LessonTranscriptValidator.RetryMessage, reason = textValidation.Reason.ToString() }, cancellationToken);
                    break;
                }

                learnerTurnCount++;
                if (startRequest is not null)
                {
                    startRequest = startRequest with { LearnerTurnCount = learnerTurnCount };
                }

                logger.LogInformation("Realtime user text accepted. SessionId={SessionId}; LearnerTurnCountAfter={LearnerTurnCount}; ValidationReason={Reason}.", sessionId, learnerTurnCount, textValidation.Reason);
                LogLessonTurnThresholds();
                await SendOpenAiEventAsync(new
                {
                    type = OpenAiConstants.RealtimeConversationItemCreateEventType,
                    item = new
                    {
                        type = "message",
                        role = "user",
                        content = new[] { new { type = OpenAiConstants.RealtimeInputTextContentType, text = textValidation.NormalizedTranscript } }
                    }
                }, cancellationToken);
                await CreateResponseAsync(cancellationToken);
                break;
            case "user.audio.start":
                currentRealtimeUserTurnId = CreateRealtimeUserTurnId();
                currentRealtimeUserTurnNumber = learnerTurnCount + 1;
                currentAudioChunkCount = 0;
                currentRecordingStartMs = stopwatch.ElapsedMilliseconds;
                currentRecordingFirstAudioChunkLogged = false;
                logger.LogInformation("Realtime desktop start_recording event received. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; LearnerTurnCount={LearnerTurnCount}; SessionReady={SessionReady}.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, learnerTurnCount, isSessionReady);
                EnforceTurnLimit();
                inputAudioBytesBuffered = 0;
                logger.LogInformation("Realtime input_audio_buffer.clear sent. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; SessionReady={SessionReady}.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, isSessionReady);
                await SendOpenAiEventAsync(new { type = "input_audio_buffer.clear" }, cancellationToken);
                break;
            case "user.audio.append":
                var audioBase64 = payload.GetProperty("audio").GetString() ?? string.Empty;
                var appendedBytes = GetBase64DecodedByteCount(audioBase64);
                currentAudioChunkCount++;
                inputAudioBytesBuffered += appendedBytes;
                totalInputAudioBytes += appendedBytes;
                if (!currentRecordingFirstAudioChunkLogged)
                {
                    currentRecordingFirstAudioChunkLogged = true;
                    logger.LogInformation("Realtime first audio chunk received. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; AppendedBytes={AppendedBytes}; BufferedBytes={BufferedBytes}; TotalInputAudioBytes={TotalInputAudioBytes}; SessionReady={SessionReady}.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, appendedBytes, inputAudioBytesBuffered, totalInputAudioBytes, isSessionReady);
                }

                if (currentAudioChunkCount == 1 || currentAudioChunkCount % AudioAppendDebugLogEveryChunkCount == 0)
                {
                    logger.LogDebug("Realtime desktop audio append received. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; AudioChunkCount={AudioChunkCount}; AppendedBytes={AppendedBytes}; BufferedBytes={BufferedBytes}; TotalInputAudioBytes={TotalInputAudioBytes}; SessionReady={SessionReady}.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, currentAudioChunkCount, appendedBytes, inputAudioBytesBuffered, totalInputAudioBytes, isSessionReady);
                }

                await SendOpenAiEventAsync(new { type = "input_audio_buffer.append", audio = audioBase64 }, cancellationToken);
                break;
            case "user.audio.commit":
                var commitElapsedMs = stopwatch.ElapsedMilliseconds;
                var bufferedBytesAtCommit = inputAudioBytesBuffered;
                logger.LogInformation("Realtime desktop commit received. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; AudioChunkCount={AudioChunkCount}; BufferedBytes={BufferedBytes}; TotalInputAudioBytes={TotalInputAudioBytes}; EstimatedBufferedAudioDurationSeconds={EstimatedBufferedAudioDurationSeconds}; TimeFromStartRecordingToCommitMs={TimeFromStartRecordingToCommitMs}; SessionReady={SessionReady}.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, currentAudioChunkCount, bufferedBytesAtCommit, totalInputAudioBytes, EstimateRealtimePcmDurationSeconds(bufferedBytesAtCommit), currentRecordingStartMs > 0 ? commitElapsedMs - currentRecordingStartMs : 0, isSessionReady);
                EnforceTurnLimit();
                if (inputAudioBytesBuffered < MinimumInputAudioBytes)
                {
                    logger.LogInformation("Realtime user audio commit ignored because the buffered audio is too short. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; AudioChunkCount={AudioChunkCount}; BufferedBytes={BufferedBytes}; MinimumBytes={MinimumBytes}; MinimumDurationMs={MinimumDurationMs}.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, currentAudioChunkCount, inputAudioBytesBuffered, MinimumInputAudioBytes, MinimumInputAudioDurationMs);
                    inputAudioBytesBuffered = 0;
                    logger.LogInformation("Realtime input_audio_buffer.clear sent. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; SessionReady={SessionReady}.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, isSessionReady);
                    await SendOpenAiEventAsync(new { type = "input_audio_buffer.clear" }, cancellationToken);
                    await SendDesktopEventAsync(new { type = "user.audio.ignored", sessionId, reason = "too_short", minimumDurationMs = MinimumInputAudioDurationMs }, cancellationToken);
                    break;
                }

                lastUserAudioCommitMs = stopwatch.ElapsedMilliseconds;
                totalCommittedAudioBytes += inputAudioBytesBuffered;
                audioCommitCount++;
                isAwaitingUserTranscript = true;
                pendingUserAudioItemId = string.Empty;
                logger.LogInformation("Realtime user audio commit sent; waiting for transcript before response.create. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; AudioChunkCount={AudioChunkCount}; BufferedBytes={BufferedBytes}; TotalInputAudioBytes={TotalInputAudioBytes}; EstimatedBufferedAudioDurationSeconds={EstimatedBufferedAudioDurationSeconds}; TimeFromStartRecordingToCommitMs={TimeFromStartRecordingToCommitMs}; SessionReady={SessionReady}; UserAudioCommitMs={ElapsedMs}; TranscriptionTimeoutMs={TimeoutMs}.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, currentAudioChunkCount, inputAudioBytesBuffered, totalInputAudioBytes, EstimateRealtimePcmDurationSeconds(inputAudioBytesBuffered), currentRecordingStartMs > 0 ? lastUserAudioCommitMs - currentRecordingStartMs : 0, isSessionReady, lastUserAudioCommitMs, UserTranscriptTimeoutMilliseconds);
                inputAudioBytesBuffered = 0;
                await SendOpenAiEventAsync(new { type = "input_audio_buffer.commit" }, cancellationToken);
                StartTranscriptionTimeout(cancellationToken);
                break;
            case "session.stop":
                logger.LogInformation("Realtime desktop stop/cancel received. SessionId={SessionId}; ResponseId={ResponseId}; SessionReady={SessionReady}.", sessionId, activeResponseId, isSessionReady);
                await DisconnectAsync("client_stop", cancellationToken);
                break;
            default:
                logger.LogWarning("Realtime ignored unknown desktop message. SessionId={SessionId}; MessageType={MessageType}; Reason=unknown_desktop_message_type.", sessionId, type);
                break;
        }
    }

    private async Task StartOpenAiSessionAsync(RealtimeVoiceSessionStartRequest request, CancellationToken cancellationToken)
    {
        var options = optionsProvider.GetOptions();
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            await SendDesktopEventAsync(new { type = "session.error", sessionId = request.SessionId, message = "Realtime voice mode is unavailable. Please try text mode." }, cancellationToken);
            logger.LogWarning("Realtime session rejected because OPENAI_API_KEY is missing. SessionId={SessionId}.", request.SessionId);
            return;
        }

        var validationError = ValidateGuidedRoleplayStartRequest(request);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            await SendDesktopEventAsync(new { type = "session.error", sessionId = request.SessionId, message = validationError }, cancellationToken);
            logger.LogWarning("Realtime guided roleplay session rejected. SessionId={SessionId}; ValidationError={ValidationError}; LessonType={LessonType}; CurrentPhase={CurrentPhase}; SelectedContextTitle={SelectedContextTitle}.", request.SessionId, validationError, request.LessonType, request.CurrentPhase, request.SelectedContextTitle);
            return;
        }

        startRequest = request;
        sessionId = request.SessionId;
        learnerTurnCount = request.LearnerTurnCount;
        firstAudioLogged = false;
        firstTranscriptLogged = false;
        lastUserAudioCommitMs = 0;
        inputAudioBytesBuffered = 0;
        totalInputAudioBytes = 0;
        totalCommittedAudioBytes = 0;
        totalAssistantAudioBytes = 0;
        audioCommitCount = 0;
        realtimeUserTranscriptCharacters = 0;
        activeResponseId = string.Empty;
        activeResponseUserTurnId = string.Empty;
        currentRealtimeUserTurnId = string.Empty;
        currentRealtimeUserTurnNumber = learnerTurnCount + 1;
        currentAudioChunkCount = 0;
        currentRecordingStartMs = 0;
        currentRecordingFirstAudioChunkLogged = false;
        softWrapUpLogged = false;
        finalMessageRequiredLogged = false;
        activeTranscript.Clear();
        isResponseInProgress = false;
        pendingUserAudioItemId = string.Empty;
        isAwaitingUserTranscript = false;
        isStartupInProgress = false;
        isSessionReady = false;
        transcriptionTimeoutCancellationTokenSource?.Cancel();
        transcriptionTimeoutCancellationTokenSource?.Dispose();
        transcriptionTimeoutCancellationTokenSource = null;
        stopwatch.Restart();
        logger.LogInformation("Realtime session start time. SessionId={SessionId}; StartedAtUtc={StartedAtUtc:o}; TutorProfileId={TutorProfileId}; TutorDisplayName={TutorDisplayName}; Level={Level}; LessonType={LessonType}; Topic={Topic}; Subtopic={Subtopic}; CurrentPhase={CurrentPhase}; SelectedContextTitle={SelectedContextTitle}; RecentMessages={RecentMessageCount}; LastBotMessageLength={LastBotMessageLength}; LearnerTurnCount={LearnerTurnCount}.",
            sessionId,
            DateTimeOffset.UtcNow,
            request.TutorProfileId,
            request.TutorDisplayName,
            request.SelectedLevel,
            request.LessonType,
            string.IsNullOrWhiteSpace(request.Topic) ? request.TopicTitle : request.Topic,
            string.IsNullOrWhiteSpace(request.Subtopic) ? request.SubtopicTitle : request.Subtopic,
            request.CurrentPhase,
            request.SelectedContextTitle,
            request.RecentMessages.Count,
            request.LastBotMessage?.Length ?? 0,
            request.LearnerTurnCount);

        try
        {
            isStartupInProgress = true;
            isSessionReady = false;
            isDisconnecting = false;
            startupFailureNotified = false;
            startupCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            openAiSocket = new ClientWebSocket();
            openAiSocket.Options.SetRequestHeader("Authorization", $"Bearer {options.ApiKey}");
            await openAiSocket.ConnectAsync(new Uri(RealtimeWebSocketEndpoint), cancellationToken);
            _ = Task.Run(() => ReceiveOpenAiEventsAsync(openAiSocket, cancellationToken), CancellationToken.None);

            var instructions = lessonPromptBuilder.BuildRealtimeInstructions(request);
            var sessionUpdateEvent = CreateRealtimeSessionUpdateEvent(instructions);
            LogRealtimeSessionUpdateShape(sessionUpdateEvent, instructions.Length);
            await SendOpenAiEventAsync(sessionUpdateEvent, cancellationToken);

            await startupCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            logger.LogInformation("Realtime session configured. SessionId={SessionId}; SessionConfiguredMs={ElapsedMs}; InputAudioTranscriptionModel={TranscriptionModel}; TranscriptionLanguage={Language}.", sessionId, stopwatch.ElapsedMilliseconds, OpenAiConstants.DefaultTranscriptionModel, OpenAiConstants.TranscriptionLanguage);

            await SeedRecentConversationAsync(request, cancellationToken);
            isStartupInProgress = false;
            isSessionReady = true;

            logger.LogInformation("Realtime session created. SessionId={SessionId}; Model={Model}; Voice={Voice}; LessonType={LessonType}; Topic={Topic}; Subtopic={Subtopic}; Level={Level}.",
                sessionId,
                OpenAiConstants.DefaultRealtimeVoiceModel,
                OpenAiConstants.DefaultRealtimeVoice,
                request.LessonType,
                string.IsNullOrWhiteSpace(request.Topic) ? request.TopicTitle : request.Topic,
                string.IsNullOrWhiteSpace(request.Subtopic) ? request.SubtopicTitle : request.Subtopic,
                request.SelectedLevel);

            await SendDesktopEventAsync(new { type = "session.ready", sessionId, model = OpenAiConstants.DefaultRealtimeVoiceModel, voice = OpenAiConstants.DefaultRealtimeVoice }, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            isStartupInProgress = false;
            isSessionReady = false;
            startupCompletionSource?.TrySetException(exception);
            logger.LogError(exception, "Realtime session start failed. SessionId={SessionId}; Endpoint={Endpoint}; FailureKind=StartupFailed.", request.SessionId, RealtimeWebSocketEndpoint);
            if (!startupFailureNotified)
            {
                startupFailureNotified = true;
                await SendDesktopEventAsync(new { type = "session.startup_failed", sessionId = request.SessionId, reason = "StartupFailed", message = "Realtime voice mode is unavailable. Please try text mode." }, CancellationToken.None);
            }
            await DisconnectAsync("startup_failed", CancellationToken.None);
        }
    }


    private static object CreateRealtimeSessionUpdateEvent(string instructions)
    {
        return new
        {
            type = "session.update",
            session = new
            {
                type = "realtime",
                model = OpenAiConstants.DefaultRealtimeVoiceModel,
                output_modalities = new[] { OpenAiConstants.RealtimeAudioOutputModality },
                instructions,
                audio = new
                {
                    input = new
                    {
                        format = new
                        {
                            type = OpenAiConstants.RealtimeAudioPcmFormatType,
                            rate = OpenAiConstants.RealtimeInputAudioSampleRate
                        },
                        turn_detection = (object?)null,
                        transcription = new
                        {
                            model = OpenAiConstants.DefaultTranscriptionModel,
                            language = OpenAiConstants.TranscriptionLanguage
                        }
                    },
                    output = new
                    {
                        format = new
                        {
                            type = OpenAiConstants.RealtimeAudioPcmFormatType,
                            rate = OpenAiConstants.RealtimeOutputAudioSampleRate
                        },
                        voice = OpenAiConstants.DefaultRealtimeVoice
                    }
                }
            }
        };
    }

    private void LogRealtimeSessionUpdateShape(object sessionUpdateEvent, int instructionsLength)
    {
        var json = JsonSerializer.Serialize(sessionUpdateEvent, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var session = document.RootElement.GetProperty("session");
        var audio = session.GetProperty("audio");
        var sanitizedShape = new
        {
            type = document.RootElement.GetProperty("type").GetString(),
            session = new
            {
                type = session.GetProperty("type").GetString(),
                model = session.GetProperty("model").GetString(),
                output_modalities = session.GetProperty("output_modalities").EnumerateArray().Select(value => value.GetString()).ToArray(),
                voice = audio.GetProperty("output").GetProperty("voice").GetString(),
                instructionsLength,
                audio = new
                {
                    input = new
                    {
                        format = new
                        {
                            type = audio.GetProperty("input").GetProperty("format").GetProperty("type").GetString(),
                            rate = audio.GetProperty("input").GetProperty("format").GetProperty("rate").GetInt32()
                        },
                        turn_detection = (string?)null,
                        transcription = new
                        {
                            model = audio.GetProperty("input").GetProperty("transcription").GetProperty("model").GetString(),
                            language = audio.GetProperty("input").GetProperty("transcription").GetProperty("language").GetString()
                        }
                    },
                    output = new
                    {
                        format = new
                        {
                            type = audio.GetProperty("output").GetProperty("format").GetProperty("type").GetString(),
                            rate = audio.GetProperty("output").GetProperty("format").GetProperty("rate").GetInt32()
                        }
                    }
                }
            }
        };

        logger.LogInformation("Realtime session.update sanitized shape. SessionId={SessionId}; Shape={SessionUpdateShape}.", sessionId, JsonSerializer.Serialize(sanitizedShape, JsonOptions));
    }

    private async Task ReceiveOpenAiEventsAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[128 * 1024];
        var builder = new StringBuilder();

        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (!result.EndOfMessage)
                {
                    continue;
                }

                var json = builder.ToString();
                builder.Clear();
                await HandleOpenAiEventAsync(json, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Realtime OpenAI receive loop failed. SessionId={SessionId}; ResponseId={ResponseId}; StartupInProgress={StartupInProgress}.", sessionId, activeResponseId, isStartupInProgress);
            if (isStartupInProgress || !isSessionReady)
            {
                startupCompletionSource?.TrySetException(exception);
                if (!startupFailureNotified)
                {
                    startupFailureNotified = true;
                    await SendDesktopEventAsync(new { type = "session.startup_failed", sessionId, responseId = activeResponseId, reason = "UpstreamRealtimeError", message = "Realtime voice mode is unavailable. Please try text mode." }, CancellationToken.None);
                }
                return;
            }

            await SendDesktopEventAsync(new { type = "session.runtime_failed", sessionId, responseId = activeResponseId, reason = "ReceiveLoopFailed", message = "Realtime voice mode is unavailable. Please try text mode." }, CancellationToken.None);
            await DisconnectAsync("runtime_error", CancellationToken.None);
        }
    }

    private async Task HandleOpenAiEventAsync(string json, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var type = root.GetProperty("type").GetString() ?? string.Empty;
        var responseId = ExtractRealtimeResponseId(root);
        var logResponseId = string.IsNullOrWhiteSpace(responseId) ? UnknownResponseId : responseId;
        logger.LogDebug("Realtime OpenAI event received. SessionId={SessionId}; EventType={EventType}; ResponseId={ResponseId}; RealtimeUserTurnId={RealtimeUserTurnId}; IsResponseInProgress={IsResponseInProgress}.", sessionId, type, logResponseId, activeResponseUserTurnId, isResponseInProgress);

        switch (type)
        {
            case "session.created":
                logger.LogInformation("Realtime upstream session.created received. SessionId={SessionId}; EventMs={ElapsedMs}.", sessionId, stopwatch.ElapsedMilliseconds);
                break;
            case "session.updated":
                logger.LogInformation("Realtime upstream session.updated received. SessionId={SessionId}; StartupInProgress={StartupInProgress}; EventMs={ElapsedMs}.", sessionId, isStartupInProgress, stopwatch.ElapsedMilliseconds);
                startupCompletionSource?.TrySetResult(true);
                break;
            case "response.created":
                if (string.IsNullOrWhiteSpace(responseId))
                {
                    activeResponseId = string.Empty;
                    activeTranscript.Clear();
                    firstAudioLogged = false;
                    firstTranscriptLogged = false;
                }
                else
                {
                    BindActiveResponseId(responseId, "response.created");
                }

                isResponseInProgress = true;
                logger.LogInformation("Realtime assistant response created. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ResponseId={ResponseId}; ResponseCreatedMs={ElapsedMs}.", sessionId, activeResponseUserTurnId, currentRealtimeUserTurnNumber, GetLogResponseId(responseId), stopwatch.ElapsedMilliseconds);
                break;
            case "response.output_audio.delta":
                var audio = root.GetProperty("delta").GetString() ?? string.Empty;
                var assistantAudioBytes = GetBase64DecodedByteCount(audio);
                totalAssistantAudioBytes += assistantAudioBytes;
                if (!firstAudioLogged)
                {
                    BindActiveResponseId(responseId, "response.output_audio.delta");
                    firstAudioLogged = true;
                    logger.LogInformation("Realtime first assistant audio delta ms. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ResponseId={ResponseId}; FirstAssistantAudioDeltaMs={ElapsedMs}; SinceUserAudioCommitMs={SinceUserAudioCommitMs}; AssistantAudioBytes={AssistantAudioBytes}.", sessionId, activeResponseUserTurnId, currentRealtimeUserTurnNumber, GetOutboundResponseId(responseId), stopwatch.ElapsedMilliseconds, lastUserAudioCommitMs > 0 ? stopwatch.ElapsedMilliseconds - lastUserAudioCommitMs : 0, assistantAudioBytes);
                }
                await SendDesktopEventAsync(new { type = "assistant.audio.delta", sessionId, responseId = GetOutboundResponseId(responseId), audio }, cancellationToken);
                break;
            case "response.output_audio_transcript.delta":
                var delta = root.GetProperty("delta").GetString() ?? string.Empty;
                activeTranscript.Append(delta);
                if (!firstTranscriptLogged)
                {
                    BindActiveResponseId(responseId, "response.output_audio_transcript.delta");
                    firstTranscriptLogged = true;
                    logger.LogInformation("Realtime first assistant transcript delta ms. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ResponseId={ResponseId}; FirstAssistantTranscriptDeltaMs={ElapsedMs}; SinceUserAudioCommitMs={SinceUserAudioCommitMs}.", sessionId, activeResponseUserTurnId, currentRealtimeUserTurnNumber, GetOutboundResponseId(responseId), stopwatch.ElapsedMilliseconds, lastUserAudioCommitMs > 0 ? stopwatch.ElapsedMilliseconds - lastUserAudioCommitMs : 0);
                }
                await SendDesktopEventAsync(new { type = "assistant.transcript.delta", sessionId, responseId = GetOutboundResponseId(responseId), delta }, cancellationToken);
                break;
            case "response.failed":
                isResponseInProgress = false;
                logger.LogWarning("Realtime assistant response failed. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; ResponseId={ResponseId}; ResponseFailedMs={ElapsedMs}.", sessionId, activeResponseUserTurnId, GetOutboundResponseId(responseId), stopwatch.ElapsedMilliseconds);
                break;
            case "response.cancelled":
                isResponseInProgress = false;
                logger.LogInformation("Realtime assistant response cancelled. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; ResponseId={ResponseId}; ResponseCancelledMs={ElapsedMs}.", sessionId, activeResponseUserTurnId, GetOutboundResponseId(responseId), stopwatch.ElapsedMilliseconds);
                break;
            case "response.done":
                BindActiveResponseId(responseId, "response.done");
                isResponseInProgress = false;
                LogRealtimeResponseUsage(root, GetOutboundResponseId(responseId));
                var finalTranscript = activeTranscript.ToString();
                if (AssistantOutputLanguageGuard.IsClearlyNonEnglishTutorOutput(finalTranscript))
                {
                    logger.LogWarning("RealtimeAssistantLanguageViolation SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; ResponseId={ResponseId}; Model={Model}; LessonId={LessonId}; Level={Level}; Topic={Topic}; Subtopic={Subtopic}; TranscriptLength={TranscriptLength}.", sessionId, activeResponseUserTurnId, GetOutboundResponseId(responseId), OpenAiConstants.DefaultRealtimeVoiceModel, startRequest?.LessonScenarioId, startRequest?.SelectedLevel, startRequest?.Topic, startRequest?.Subtopic, finalTranscript.Length);
                    await SendOpenAiEventAsync(new { type = "session.update", session = new { type = "realtime", instructions = BuildCorrectiveEnglishOnlyInstructions() } }, cancellationToken);
                }

                logger.LogInformation("Realtime assistant response completed ms. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ResponseId={ResponseId}; AssistantResponseCompletedMs={ElapsedMs}; SinceUserAudioCommitMs={SinceUserAudioCommitMs}; TranscriptLength={TranscriptLength}; AssistantAudioBytes={AssistantAudioBytes}.", sessionId, activeResponseUserTurnId, currentRealtimeUserTurnNumber, GetOutboundResponseId(responseId), stopwatch.ElapsedMilliseconds, lastUserAudioCommitMs > 0 ? stopwatch.ElapsedMilliseconds - lastUserAudioCommitMs : 0, activeTranscript.Length, totalAssistantAudioBytes);
                await SendDesktopEventAsync(new { type = "assistant.turn.completed", sessionId, responseId = GetOutboundResponseId(responseId), transcript = finalTranscript }, cancellationToken);
                break;
            case "input_audio_buffer.committed":
                var itemId = root.TryGetProperty("item_id", out var itemIdProperty) ? itemIdProperty.GetString() ?? string.Empty : string.Empty;
                pendingUserAudioItemId = itemId;
                logger.LogInformation("Realtime user audio committed event received. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ItemId={ItemId}; UserAudioCommittedEventMs={ElapsedMs}; WaitingForTranscript={WaitingForTranscript}.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, itemId, stopwatch.ElapsedMilliseconds, isAwaitingUserTranscript);
                await SendDesktopEventAsync(new { type = "user.audio.committed", sessionId, itemId }, cancellationToken);
                break;
            case "conversation.item.input_audio_transcription.delta":
                var transcriptDelta = root.TryGetProperty("delta", out var transcriptDeltaProperty) ? transcriptDeltaProperty.GetString() ?? string.Empty : string.Empty;
                var deltaItemId = root.TryGetProperty("item_id", out var deltaItemIdProperty) ? deltaItemIdProperty.GetString() ?? string.Empty : string.Empty;
                logger.LogDebug("Realtime user transcript delta received. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ItemId={ItemId}; TranscriptLength={TranscriptLength}; EventMs={ElapsedMs}.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, deltaItemId, transcriptDelta.Length, stopwatch.ElapsedMilliseconds);
                await SendDesktopEventAsync(new { type = "user.transcript.delta", sessionId, itemId = deltaItemId, delta = transcriptDelta }, cancellationToken);
                break;
            case "conversation.item.created":
                var createdItemId = root.TryGetProperty("item", out var createdItem) && createdItem.TryGetProperty("id", out var createdItemIdProperty) ? createdItemIdProperty.GetString() ?? string.Empty : string.Empty;
                logger.LogDebug("Realtime conversation item created. SessionId={SessionId}; ItemId={ItemId}; EventMs={ElapsedMs}.", sessionId, createdItemId, stopwatch.ElapsedMilliseconds);
                break;
            case "conversation.item.input_audio_transcription.completed":
                var userTranscript = root.TryGetProperty("transcript", out var userTranscriptProperty) ? userTranscriptProperty.GetString() ?? string.Empty : string.Empty;
                var transcriptItemId = root.TryGetProperty("item_id", out var transcriptItemIdProperty) ? transcriptItemIdProperty.GetString() ?? string.Empty : string.Empty;
                logger.LogInformation("Realtime user transcript completed received. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ItemId={ItemId}; TranscriptLength={TranscriptLength}; EventMs={ElapsedMs}.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, transcriptItemId, userTranscript.Trim().Length, stopwatch.ElapsedMilliseconds);
                await HandleUserTranscriptCompletedAsync(transcriptItemId, userTranscript, cancellationToken);
                break;
            case "conversation.item.input_audio_transcription.failed":
                var failedItemId = root.TryGetProperty("item_id", out var failedItemIdProperty) ? failedItemIdProperty.GetString() ?? string.Empty : string.Empty;
                var failureMessage = root.TryGetProperty("error", out var transcriptionError) && transcriptionError.TryGetProperty("message", out var transcriptionErrorMessage) ? transcriptionErrorMessage.GetString() ?? "Transcription unavailable." : "Transcription unavailable.";
                logger.LogWarning("Realtime user transcript failed. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ItemId={ItemId}; Error={Error}; EventMs={ElapsedMs}; LearnerTurnCountBefore={LearnerTurnCount}; NormalAssistantResponseCreated=False; RetryPromptShown=True.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, failedItemId, failureMessage, stopwatch.ElapsedMilliseconds, learnerTurnCount);
                await HandleUserTranscriptFailedAsync(failedItemId, failureMessage, cancellationToken);
                break;
            case "error":
                isResponseInProgress = false;
                var message = root.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var errorMessage) ? errorMessage.GetString() : "Realtime voice mode is unavailable. Please try text mode.";
                logger.LogWarning("Realtime error event. SessionId={SessionId}; ResponseId={ResponseId}; Error={Error}; StartupInProgress={StartupInProgress}.", sessionId, activeResponseId, message, isStartupInProgress);
                if (isStartupInProgress || !isSessionReady)
                {
                    var exception = new InvalidOperationException(message);
                    startupCompletionSource?.TrySetException(exception);
                    isStartupInProgress = false;
                    isSessionReady = false;
                    if (!startupFailureNotified)
                    {
                        startupFailureNotified = true;
                        await SendDesktopEventAsync(new { type = "session.startup_failed", sessionId, responseId = activeResponseId, reason = "UpstreamRealtimeError", message = "Realtime voice mode is unavailable. Please try text mode." }, cancellationToken);
                    }
                    await DisconnectAsync("upstream_realtime_error", CancellationToken.None);
                    break;
                }

                await SendDesktopEventAsync(new { type = "session.runtime_failed", sessionId, responseId = activeResponseId, reason = "UpstreamRealtimeError", message }, cancellationToken);
                await DisconnectAsync("upstream_realtime_error", CancellationToken.None);
                break;
        }
    }

    private async Task HandleUserTranscriptCompletedAsync(string itemId, string transcript, CancellationToken cancellationToken)
    {
        transcriptionTimeoutCancellationTokenSource?.Cancel();
        var validation = LessonTranscriptValidator.Validate(transcript);
        var resolvedItemId = string.IsNullOrWhiteSpace(itemId) ? pendingUserAudioItemId : itemId;
        if (!validation.IsValid)
        {
            logger.LogInformation("Realtime user transcript rejected. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ItemId={ItemId}; Reason={Reason}; TranscriptLength={TranscriptLength}; LearnerTurnCountBefore={LearnerTurnCount}; LearnerTurnCountAfter={LearnerTurnCountAfter}; NormalAssistantResponseCreated=False; RetryPromptShown=True.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, resolvedItemId, validation.Reason, validation.NormalizedTranscript.Length, learnerTurnCount, learnerTurnCount);
            isAwaitingUserTranscript = false;
            await SendDesktopEventAsync(new { type = "user.transcript.completed", sessionId, itemId = resolvedItemId, transcript = validation.NormalizedTranscript }, cancellationToken);
            await SendDesktopEventAsync(new { type = "user.transcript.failed", sessionId, itemId = resolvedItemId, message = LessonTranscriptValidator.RetryMessage, reason = validation.Reason.ToString() }, cancellationToken);
            return;
        }

        EnforceTurnLimit();
        learnerTurnCount++;
        if (startRequest is not null)
        {
            startRequest = startRequest with { LearnerTurnCount = learnerTurnCount };
        }

        isAwaitingUserTranscript = false;
        realtimeUserTranscriptCharacters += validation.NormalizedTranscript.Length;
        logger.LogInformation("Realtime user transcript accepted. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ItemId={ItemId}; Reason={Reason}; TranscriptLength={TranscriptLength}; LearnerTurnCountAfter={LearnerTurnCount}; NormalAssistantResponseCreated=True; RetryPromptShown=False.", sessionId, currentRealtimeUserTurnId, learnerTurnCount, resolvedItemId, validation.Reason, validation.NormalizedTranscript.Length, learnerTurnCount);
        LogLessonTurnThresholds();
        await SendDesktopEventAsync(new { type = "user.transcript.completed", sessionId, itemId = resolvedItemId, transcript = validation.NormalizedTranscript }, cancellationToken);
        await CreateResponseAsync(cancellationToken);
    }

    private async Task HandleUserTranscriptFailedAsync(string itemId, string message, CancellationToken cancellationToken)
    {
        transcriptionTimeoutCancellationTokenSource?.Cancel();
        isAwaitingUserTranscript = false;
        var resolvedItemId = string.IsNullOrWhiteSpace(itemId) ? pendingUserAudioItemId : itemId;
        await SendDesktopEventAsync(new { type = "user.transcript.failed", sessionId, itemId = resolvedItemId, message = LessonTranscriptValidator.RetryMessage, reason = message }, cancellationToken);
    }

    private void StartTranscriptionTimeout(CancellationToken cancellationToken)
    {
        transcriptionTimeoutCancellationTokenSource?.Cancel();
        transcriptionTimeoutCancellationTokenSource?.Dispose();
        transcriptionTimeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var timeoutToken = transcriptionTimeoutCancellationTokenSource.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(UserTranscriptTimeoutMilliseconds, timeoutToken);
                if (!timeoutToken.IsCancellationRequested && isAwaitingUserTranscript)
                {
                    logger.LogWarning("Realtime user transcript timed out. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ItemId={ItemId}; TimeoutMs={TimeoutMs}; LearnerTurnCountBefore={LearnerTurnCount}; NormalAssistantResponseCreated=False; RetryPromptShown=True.", sessionId, currentRealtimeUserTurnId, currentRealtimeUserTurnNumber, pendingUserAudioItemId, UserTranscriptTimeoutMilliseconds, learnerTurnCount);
                    await HandleUserTranscriptFailedAsync(pendingUserAudioItemId, "transcription_timeout", CancellationToken.None);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, CancellationToken.None);
    }

    private async Task SeedRecentConversationAsync(RealtimeVoiceSessionStartRequest request, CancellationToken cancellationToken)
    {
        foreach (var message in request.RecentMessages.TakeLast(12))
        {
            var text = message.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var role = IsTutorSender(message, request) ? "assistant" : "user";
            await SendOpenAiEventAsync(new
            {
                type = OpenAiConstants.RealtimeConversationItemCreateEventType,
                item = new
                {
                    type = "message",
                    role,
                    content = new[] { new { type = GetRealtimeTextContentTypeForRole(role), text } }
                }
            }, cancellationToken);
        }

        logger.LogInformation("Realtime recent conversation seeded. SessionId={SessionId}; RecentMessages={RecentMessageCount}; LastBotMessageLength={LastBotMessageLength}.", sessionId, request.RecentMessages.Count, request.LastBotMessage?.Length ?? 0);
    }

    private static string GetRealtimeTextContentTypeForRole(string role)
    {
        return role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            ? OpenAiConstants.RealtimeOutputTextContentType
            : OpenAiConstants.RealtimeInputTextContentType;
    }

    private string BuildResponseInstructions()
    {
        return startRequest is null
            ? "Respond now in English. Produce audio and matching audio transcript from this same response. Ask one question at a time."
            : lessonPromptBuilder.BuildRealtimeResponseInstructions(startRequest);
    }

    private string BuildCorrectiveEnglishOnlyInstructions()
    {
        return (startRequest is null ? BuildResponseInstructions() : lessonPromptBuilder.BuildRealtimeInstructions(startRequest))
            + "\nEnglish-only correction: continue the lesson in English only. If the learner asks for another language, refuse briefly in English and continue the lesson.";
    }

    private async Task CreateResponseAsync(CancellationToken cancellationToken)
    {
        if (isResponseInProgress)
        {
            logger.LogInformation("Realtime response.create skipped because a response is already active. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; ActiveResponseId={ResponseId}; LearnerTurnCount={LearnerTurnCount}.", sessionId, currentRealtimeUserTurnId, GetLogResponseId(activeResponseId), learnerTurnCount);
            return;
        }

        isResponseInProgress = true;
        activeResponseUserTurnId = currentRealtimeUserTurnId;
        logger.LogInformation("Realtime response.create sent. SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; LearnerTurnCount={LearnerTurnCount}; ResponseCreateSentMs={ElapsedMs}; SinceUserAudioCommitMs={SinceUserAudioCommitMs}.", sessionId, activeResponseUserTurnId, learnerTurnCount, learnerTurnCount, stopwatch.ElapsedMilliseconds, lastUserAudioCommitMs > 0 ? stopwatch.ElapsedMilliseconds - lastUserAudioCommitMs : 0);
        await SendOpenAiEventAsync(new
        {
            type = OpenAiConstants.RealtimeResponseCreateEventType,
            response = new
            {
                output_modalities = new[] { OpenAiConstants.RealtimeAudioOutputModality },
                instructions = BuildResponseInstructions()
            }
        }, cancellationToken);
    }

    private void LogRealtimeResponseUsage(JsonElement responseDoneEvent, string responseId)
    {
        if (!responseDoneEvent.TryGetProperty("response", out var response) || !response.TryGetProperty("usage", out var usage))
        {
            logger.LogInformation("Developer usage summary: Operation=realtime_response; SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ResponseId={ResponseId}; Model={Model}; HasExactUsage=False; CostEstimateApproximate=True; MissingUsageFields=realtime_response_usage; MissingCostFields={MissingCostFields}.", sessionId, activeResponseUserTurnId, learnerTurnCount, GetOutboundResponseId(responseId), OpenAiConstants.DefaultRealtimeVoiceModel, GetRealtimeResponseMissingCostFields());
            return;
        }

        long? GetLong(string name) => usage.TryGetProperty(name, out var property) && property.TryGetInt64(out var value) ? value : null;
        long? inputTokens = GetLong("input_tokens");
        long? outputTokens = GetLong("output_tokens");
        long? totalTokens = GetLong("total_tokens");
        long? inputAudioTokens = null;
        long? outputAudioTokens = null;
        JsonElement inputDetails;
        if (usage.TryGetProperty("input_token_details", out inputDetails) || usage.TryGetProperty("input_tokens_details", out inputDetails))
        {
            inputAudioTokens = inputDetails.TryGetProperty("audio_tokens", out var value) && value.TryGetInt64(out var parsed) ? parsed : null;
        }

        JsonElement outputDetails;
        if (usage.TryGetProperty("output_token_details", out outputDetails) || usage.TryGetProperty("output_tokens_details", out outputDetails))
        {
            outputAudioTokens = outputDetails.TryGetProperty("audio_tokens", out var value) && value.TryGetInt64(out var parsed) ? parsed : null;
        }

        logger.LogInformation("Developer usage summary: Operation=realtime_response; SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ResponseId={ResponseId}; Model={Model}; InputTokens={InputTokens}; OutputTokens={OutputTokens}; TotalTokens={TotalTokens}; AudioInputTokens={AudioInputTokens}; AudioOutputTokens={AudioOutputTokens}; HasExactUsage={HasExactUsage}; CostEstimateApproximate=True; MissingCostFields={MissingCostFields}.", sessionId, activeResponseUserTurnId, learnerTurnCount, GetOutboundResponseId(responseId), OpenAiConstants.DefaultRealtimeVoiceModel, inputTokens, outputTokens, totalTokens, inputAudioTokens, outputAudioTokens, inputTokens.HasValue || outputTokens.HasValue || totalTokens.HasValue || inputAudioTokens.HasValue || outputAudioTokens.HasValue, GetRealtimeResponseMissingCostFields());
    }


    private void BindActiveResponseId(string responseId, string eventType)
    {
        if (string.IsNullOrWhiteSpace(responseId))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(activeResponseId) && !string.Equals(activeResponseId, responseId, StringComparison.Ordinal) && isResponseInProgress)
        {
            logger.LogWarning("Realtime multiple active response ids detected. SessionId={SessionId}; PreviousResponseId={PreviousResponseId}; NewResponseId={NewResponseId}; EventType={EventType}; RealtimeUserTurnId={RealtimeUserTurnId}.", sessionId, activeResponseId, responseId, eventType, activeResponseUserTurnId);
        }

        if (!string.Equals(activeResponseId, responseId, StringComparison.Ordinal))
        {
            activeResponseId = responseId;
            activeTranscript.Clear();
            firstAudioLogged = false;
            firstTranscriptLogged = false;
        }
    }

    private static string ExtractRealtimeResponseId(JsonElement root)
    {
        if (root.TryGetProperty("response_id", out var responseIdProperty))
        {
            return responseIdProperty.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("response", out var responseProperty) && responseProperty.TryGetProperty("id", out var nestedIdProperty))
        {
            return nestedIdProperty.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private string GetOutboundResponseId(string parsedResponseId)
    {
        if (!string.IsNullOrWhiteSpace(parsedResponseId))
        {
            return parsedResponseId;
        }

        return string.IsNullOrWhiteSpace(activeResponseId) ? UnknownResponseId : activeResponseId;
    }

    private string GetLogResponseId(string responseId)
    {
        return string.IsNullOrWhiteSpace(responseId) ? UnknownResponseId : responseId;
    }

    private static string CreateRealtimeUserTurnId()
    {
        return $"{RealtimeUserTurnIdPrefix}_{Guid.NewGuid():N}";
    }

    private static string GetRealtimeResponseMissingCostFields()
    {
        return PricingConstants.OpenAi.RealtimeTextInputPerMillionTokensUsd == 0m || PricingConstants.OpenAi.RealtimeTextOutputPerMillionTokensUsd == 0m
            ? "realtime_pricing"
            : string.Empty;
    }

    private void LogLessonTurnThresholds()
    {
        if (startRequest is null)
        {
            return;
        }

        if (!softWrapUpLogged && startRequest.SoftLearnerTurnLimit > 0 && learnerTurnCount >= startRequest.SoftLearnerTurnLimit && learnerTurnCount < startRequest.HardLearnerTurnLimit)
        {
            softWrapUpLogged = true;
            LogLessonTurnThreshold("SoftWrapUpStarted", isFinalTurn: false);
        }

        if (!finalMessageRequiredLogged && startRequest.HardLearnerTurnLimit > 0 && learnerTurnCount >= startRequest.HardLearnerTurnLimit)
        {
            finalMessageRequiredLogged = true;
            LogLessonTurnThreshold("FinalMessageRequired", isFinalTurn: true);
        }
    }

    private void LogLessonTurnThreshold(string thresholdName, bool isFinalTurn)
    {
        if (startRequest is null)
        {
            return;
        }

        logger.LogInformation("Realtime lesson turn threshold crossed. Threshold={Threshold}; SessionId={SessionId}; LessonType={LessonType}; Level={Level}; Topic={Topic}; Subtopic={Subtopic}; LearnerTurnCount={LearnerTurnCount}; SoftWrapUpAfterUserTurn={SoftWrapUpAfterUserTurn}; FinalMessageAtUserTurn={FinalMessageAtUserTurn}; IsFinalTurn={IsFinalTurn}; ShouldDisableFurtherInput={ShouldDisableFurtherInput}.",
            thresholdName,
            sessionId,
            startRequest.LessonType,
            startRequest.SelectedLevel,
            string.IsNullOrWhiteSpace(startRequest.Topic) ? startRequest.TopicTitle : startRequest.Topic,
            string.IsNullOrWhiteSpace(startRequest.Subtopic) ? startRequest.SubtopicTitle : startRequest.Subtopic,
            learnerTurnCount,
            startRequest.SoftLearnerTurnLimit,
            startRequest.HardLearnerTurnLimit,
            isFinalTurn,
            isFinalTurn);
    }

    private void EnforceTurnLimit()
    {
        if (startRequest is not null && learnerTurnCount >= startRequest.HardLearnerTurnLimit)
        {
            throw new InvalidOperationException("The realtime lesson turn limit has been reached.");
        }
    }

    private Task SendOpenAiEventAsync(object value, CancellationToken cancellationToken)
    {
        var socket = openAiSocket ?? throw new InvalidOperationException("OpenAI realtime session is not started.");
        LogRealtimeOutboundEventShape(value);
        return SendJsonAsync(socket, value, cancellationToken);
    }

    private void LogRealtimeOutboundEventShape(object value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var eventType = root.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() ?? string.Empty : string.Empty;
            var role = string.Empty;
            var itemKind = string.Empty;
            var contentPartTypes = Array.Empty<string?>();
            var outputModalities = Array.Empty<string?>();

            if (eventType.Equals(OpenAiConstants.RealtimeConversationItemCreateEventType, StringComparison.Ordinal)
                && root.TryGetProperty("item", out var item))
            {
                role = item.TryGetProperty("role", out var roleProperty) ? roleProperty.GetString() ?? string.Empty : string.Empty;
                itemKind = DetermineRealtimeOutboundItemKind(role);
                if (item.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                {
                    contentPartTypes = content.EnumerateArray()
                        .Select(part => part.TryGetProperty("type", out var contentTypeProperty) ? contentTypeProperty.GetString() : string.Empty)
                        .ToArray();
                }
            }
            else if (eventType.Equals(OpenAiConstants.RealtimeResponseCreateEventType, StringComparison.Ordinal)
                && root.TryGetProperty("response", out var response))
            {
                itemKind = "generated_response";
                if (response.TryGetProperty("output_modalities", out var modalities) && modalities.ValueKind == JsonValueKind.Array)
                {
                    outputModalities = modalities.EnumerateArray().Select(modality => modality.GetString()).ToArray();
                }

                if (response.TryGetProperty("input", out var input))
                {
                    contentPartTypes = ExtractRealtimeInputContentTypes(input);
                }
            }
            else if (eventType.Equals("session.update", StringComparison.Ordinal) && root.TryGetProperty("session", out var session))
            {
                itemKind = session.TryGetProperty("instructions", out _) && isSessionReady ? "correction" : "session_configuration";
                if (session.TryGetProperty("output_modalities", out var sessionModalities) && sessionModalities.ValueKind == JsonValueKind.Array)
                {
                    outputModalities = sessionModalities.EnumerateArray().Select(modality => modality.GetString()).ToArray();
                }
            }
            else
            {
                itemKind = eventType switch
                {
                    "input_audio_buffer.append" => "user_audio",
                    "input_audio_buffer.commit" => "user_audio",
                    "input_audio_buffer.clear" => "user_audio",
                    "response.cancel" => "response_control",
                    _ => "control"
                };
            }

            logger.LogDebug("Realtime outbound event shape. SessionId={SessionId}; EventType={EventType}; Role={Role}; ItemKind={ItemKind}; ContentPartTypes={ContentPartTypes}; OutputModalities={OutputModalities}; IsSeed={IsSeed}; IsCorrection={IsCorrection}.",
                sessionId,
                eventType,
                string.IsNullOrWhiteSpace(role) ? "none" : role,
                itemKind,
                string.Join(",", contentPartTypes.Where(value => !string.IsNullOrWhiteSpace(value))),
                string.Join(",", outputModalities.Where(value => !string.IsNullOrWhiteSpace(value))),
                itemKind.StartsWith("seed_", StringComparison.Ordinal),
                itemKind.Equals("correction", StringComparison.Ordinal));
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Realtime outbound event shape logging skipped. SessionId={SessionId}.", sessionId);
        }
    }

    private string DetermineRealtimeOutboundItemKind(string role)
    {
        if (isSessionReady)
        {
            return role.Equals("user", StringComparison.OrdinalIgnoreCase) ? "live_user_text" : "live_conversation_item";
        }

        return role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            ? "seed_assistant"
            : role.Equals("system", StringComparison.OrdinalIgnoreCase)
                ? "seed_system"
                : "seed_user";
    }

    private static string?[] ExtractRealtimeInputContentTypes(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string?>();
        }

        return input.EnumerateArray()
            .SelectMany(item => item.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array
                ? content.EnumerateArray().Select(part => part.TryGetProperty("type", out var contentTypeProperty) ? contentTypeProperty.GetString() : string.Empty)
                : Enumerable.Empty<string?>())
            .ToArray();
    }

    private Task SendDesktopEventAsync(object value, CancellationToken cancellationToken)
    {
        var socket = desktopSocket ?? throw new InvalidOperationException("Desktop realtime socket is not connected.");
        return SendJsonAsync(socket, value, cancellationToken);
    }

    private static async Task SendJsonAsync(WebSocket socket, object value, CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var json = JsonSerializer.Serialize(value, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, cancellationToken);
    }

    private static int GetBase64DecodedByteCount(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return 0;
        }

        try
        {
            return Convert.FromBase64String(base64).Length;
        }
        catch (FormatException)
        {
            return 0;
        }
    }

    private static double EstimateRealtimePcmDurationSeconds(long bytes)
    {
        return Math.Max(0, bytes) / (double)(InputAudioSampleRate * InputAudioBytesPerSample);
    }

    private static bool IsExpectedDesktopDisconnect(WebSocketException exception, CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested
            || exception.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely;
    }

    private async Task DisconnectAsync(string reason, CancellationToken cancellationToken)
    {
        if (isDisconnecting)
        {
            return;
        }

        isDisconnecting = true;
        logger.LogInformation("Realtime disconnect. SessionId={SessionId}; ResponseId={ResponseId}; DisconnectReason={DisconnectReason}; ElapsedMs={ElapsedMs}.", sessionId, activeResponseId, reason, stopwatch.ElapsedMilliseconds);
        logger.LogInformation("Developer usage summary: Operation=realtime_session; SessionId={SessionId}; Model={Model}; Voice={Voice}; InputTranscriptionModel={InputTranscriptionModel}; Language={Language}; TotalInputAudioBytes={TotalInputAudioBytes}; EstimatedInputAudioDurationSeconds={EstimatedInputAudioDurationSeconds}; TotalCommittedAudioBytes={TotalCommittedAudioBytes}; AudioCommits={AudioCommits}; UserTranscriptCharacters={UserTranscriptCharacters}; AssistantTranscriptCharacters={AssistantTranscriptCharacters}; AssistantAudioBytes={AssistantAudioBytes}; EstimatedAssistantAudioDurationSeconds={EstimatedAssistantAudioDurationSeconds}; DisconnectReason={DisconnectReason}; CostEstimateApproximate=True; MissingCostFields={MissingCostFields}.", sessionId, OpenAiConstants.DefaultRealtimeVoiceModel, OpenAiConstants.DefaultRealtimeVoice, OpenAiConstants.DefaultTranscriptionModel, OpenAiConstants.TranscriptionLanguage, totalInputAudioBytes, EstimateRealtimePcmDurationSeconds(totalInputAudioBytes), totalCommittedAudioBytes, audioCommitCount, realtimeUserTranscriptCharacters, activeTranscript.Length, totalAssistantAudioBytes, EstimateRealtimePcmDurationSeconds(totalAssistantAudioBytes), reason, PricingConstants.OpenAi.RealtimeAudioInputPerMillionTokensUsd == 0m || PricingConstants.OpenAi.RealtimeAudioOutputPerMillionTokensUsd == 0m ? "realtime_pricing" : string.Empty);
        transcriptionTimeoutCancellationTokenSource?.Cancel();
        transcriptionTimeoutCancellationTokenSource?.Dispose();
        transcriptionTimeoutCancellationTokenSource = null;
        isAwaitingUserTranscript = false;
        isStartupInProgress = false;
        isSessionReady = false;
        startupCompletionSource = null;
        if (openAiSocket is not null)
        {
            try
            {
                if (openAiSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    await openAiSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, cancellationToken);
                }
            }
            catch
            {
                // Ignore disconnect cleanup errors.
            }
            openAiSocket.Dispose();
            openAiSocket = null;
        }
    }

    private static string ValidateGuidedRoleplayStartRequest(RealtimeVoiceSessionStartRequest request)
    {
        if (!request.LessonType.Equals("guided_roleplay", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var missing = new List<string>();
        if (!request.CurrentPhase.Equals("ActiveRoleplay", StringComparison.OrdinalIgnoreCase)) missing.Add(nameof(request.CurrentPhase));
        if (string.IsNullOrWhiteSpace(request.SelectedLevel)) missing.Add(nameof(request.SelectedLevel));
        if (string.IsNullOrWhiteSpace(Choose(request.Topic, request.TopicTitle))) missing.Add(nameof(request.Topic));
        if (string.IsNullOrWhiteSpace(Choose(request.Subtopic, request.SubtopicTitle))) missing.Add(nameof(request.Subtopic));
        if (string.IsNullOrWhiteSpace(request.SelectedContextTitle)) missing.Add(nameof(request.SelectedContextTitle));
        if (string.IsNullOrWhiteSpace(request.SelectedContextVariantId)) missing.Add(nameof(request.SelectedContextVariantId));
        if (string.IsNullOrWhiteSpace(request.TutorRole)) missing.Add(nameof(request.TutorRole));
        if (string.IsNullOrWhiteSpace(request.UserRole)) missing.Add(nameof(request.UserRole));
        if (string.IsNullOrWhiteSpace(request.Situation)) missing.Add(nameof(request.Situation));
        if (string.IsNullOrWhiteSpace(request.LessonGoal) && string.IsNullOrWhiteSpace(request.LearningGoal)) missing.Add("LearningGoal");
        if (string.IsNullOrWhiteSpace(request.TargetLanguageName)) missing.Add(nameof(request.TargetLanguageName));
        if (string.IsNullOrWhiteSpace(request.ActiveLevelProfile.DifficultyNotes) && string.IsNullOrWhiteSpace(request.ActiveLevelProfile.TutorLanguageStyle)) missing.Add(nameof(request.ActiveLevelProfile));
        if (string.IsNullOrWhiteSpace(request.LastBotMessage)) missing.Add(nameof(request.LastBotMessage));
        if (request.RecentMessages.Count == 0) missing.Add(nameof(request.RecentMessages));

        return missing.Count == 0
            ? string.Empty
            : $"Guided roleplay realtime cannot start without selected scenario context: {string.Join(", ", missing)}.";
    }

    private static bool IsTutorSender(RealtimeRecentConversationMessage message, RealtimeVoiceSessionStartRequest request)
    {
        return message.Sender.Equals(request.TutorDisplayName, StringComparison.OrdinalIgnoreCase)
            || message.Sender.Contains("tutor", StringComparison.OrdinalIgnoreCase);
    }

    private static string Choose(string first, string second)
    {
        return string.IsNullOrWhiteSpace(first) ? second : first;
    }
}
