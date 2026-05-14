using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models.RealtimeVoice;

namespace EnglishVoiceTutor.Api.Services;

public sealed class RealtimeVoiceSessionService
{
    private const string RealtimeWebSocketEndpoint = "wss://api.openai.com/v1/realtime?model=" + OpenAiConstants.DefaultRealtimeVoiceModel;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenAiOptionsProvider optionsProvider;
    private readonly ILogger<RealtimeVoiceSessionService> logger;
    private ClientWebSocket? openAiSocket;
    private WebSocket? desktopSocket;
    private RealtimeVoiceSessionStartRequest? startRequest;
    private Stopwatch stopwatch = new();
    private string sessionId = string.Empty;
    private string activeResponseId = string.Empty;
    private readonly StringBuilder activeTranscript = new();
    private int learnerTurnCount;
    private bool firstAudioLogged;
    private bool firstTranscriptLogged;
    private long lastUserAudioCommitMs;

    public RealtimeVoiceSessionService(OpenAiOptionsProvider optionsProvider, ILogger<RealtimeVoiceSessionService> logger)
    {
        this.optionsProvider = optionsProvider;
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
        var payload = root.GetProperty("payload");

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
                learnerTurnCount++;
                await SendOpenAiEventAsync(new
                {
                    type = "conversation.item.create",
                    item = new
                    {
                        type = "message",
                        role = "user",
                        content = new[] { new { type = "input_text", text = payload.GetProperty("text").GetString() ?? string.Empty } }
                    }
                }, cancellationToken);
                await CreateResponseAsync(cancellationToken);
                break;
            case "user.audio.start":
                EnforceTurnLimit();
                await SendOpenAiEventAsync(new { type = "input_audio_buffer.clear" }, cancellationToken);
                break;
            case "user.audio.append":
                await SendOpenAiEventAsync(new { type = "input_audio_buffer.append", audio = payload.GetProperty("audio").GetString() ?? string.Empty }, cancellationToken);
                break;
            case "user.audio.commit":
                EnforceTurnLimit();
                learnerTurnCount++;
                lastUserAudioCommitMs = stopwatch.ElapsedMilliseconds;
                logger.LogInformation("Realtime user audio commit sent. SessionId={SessionId}; LearnerTurnCount={LearnerTurnCount}; UserAudioCommitMs={ElapsedMs}.", sessionId, learnerTurnCount, lastUserAudioCommitMs);
                await SendOpenAiEventAsync(new { type = "input_audio_buffer.commit" }, cancellationToken);
                await CreateResponseAsync(cancellationToken);
                break;
            case "session.stop":
                await DisconnectAsync("client_stop", cancellationToken);
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
        stopwatch.Restart();
        logger.LogInformation("Realtime session start time. SessionId={SessionId}; StartedAtUtc={StartedAtUtc:o}; LessonType={LessonType}; CurrentPhase={CurrentPhase}; SelectedContextTitle={SelectedContextTitle}; RecentMessages={RecentMessageCount}; LastBotMessageLength={LastBotMessageLength}; LearnerTurnCount={LearnerTurnCount}.",
            sessionId,
            DateTimeOffset.UtcNow,
            request.LessonType,
            request.CurrentPhase,
            request.SelectedContextTitle,
            request.RecentMessages.Count,
            request.LastBotMessage?.Length ?? 0,
            request.LearnerTurnCount);

        try
        {
            openAiSocket = new ClientWebSocket();
            openAiSocket.Options.SetRequestHeader("Authorization", $"Bearer {options.ApiKey}");
            openAiSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");
            await openAiSocket.ConnectAsync(new Uri(RealtimeWebSocketEndpoint), cancellationToken);
            _ = Task.Run(() => ReceiveOpenAiEventsAsync(openAiSocket, cancellationToken), CancellationToken.None);

            var instructions = BuildInstructions(request);
            await SendOpenAiEventAsync(new
            {
                type = "session.update",
                session = new
                {
                    modalities = new[] { "text", "audio" },
                    instructions,
                    voice = OpenAiConstants.DefaultRealtimeVoice,
                    input_audio_format = "pcm16",
                    output_audio_format = "pcm16",
                    turn_detection = (object?)null,
                    input_audio_transcription = new { model = OpenAiConstants.DefaultTranscriptionModel, language = OpenAiConstants.TranscriptionLanguage },
                    audio = new
                    {
                        input = new
                        {
                            format = new { type = "audio/pcm", rate = 24000 },
                            transcription = new { model = OpenAiConstants.DefaultTranscriptionModel, language = OpenAiConstants.TranscriptionLanguage },
                            turn_detection = (object?)null
                        },
                        output = new
                        {
                            format = new { type = "audio/pcm", rate = 24000 },
                            voice = OpenAiConstants.DefaultRealtimeVoice
                        }
                    }
                }
            }, cancellationToken);

            logger.LogInformation("Realtime session configured. SessionId={SessionId}; SessionConfiguredMs={ElapsedMs}; InputAudioTranscriptionModel={TranscriptionModel}; TranscriptionLanguage={Language}.", sessionId, stopwatch.ElapsedMilliseconds, OpenAiConstants.DefaultTranscriptionModel, OpenAiConstants.TranscriptionLanguage);

            await SeedRecentConversationAsync(request, cancellationToken);

            logger.LogInformation("Realtime session created. SessionId={SessionId}; Model={Model}; Voice={Voice}; LessonType={LessonType}; Topic={Topic}; Subtopic={Subtopic}; Level={Level}.",
                sessionId,
                OpenAiConstants.DefaultRealtimeVoiceModel,
                OpenAiConstants.DefaultRealtimeVoice,
                request.LessonType,
                string.IsNullOrWhiteSpace(request.Topic) ? request.TopicTitle : request.Topic,
                string.IsNullOrWhiteSpace(request.Subtopic) ? request.SubtopicTitle : request.Subtopic,
                request.SelectedLevel);

            await SendDesktopEventAsync(new { type = "session.started", sessionId, model = OpenAiConstants.DefaultRealtimeVoiceModel, voice = OpenAiConstants.DefaultRealtimeVoice }, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Realtime session start failed. SessionId={SessionId}; Endpoint={Endpoint}.", request.SessionId, RealtimeWebSocketEndpoint);
            await SendDesktopEventAsync(new { type = "session.error", sessionId = request.SessionId, message = "Realtime voice mode is unavailable. Please try text mode." }, CancellationToken.None);
        }
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
            logger.LogError(exception, "Realtime OpenAI receive loop failed. SessionId={SessionId}; ResponseId={ResponseId}.", sessionId, activeResponseId);
            await SendDesktopEventAsync(new { type = "session.error", sessionId, responseId = activeResponseId, message = "Realtime voice mode is unavailable. Please try text mode." }, CancellationToken.None);
        }
    }

    private async Task HandleOpenAiEventAsync(string json, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var type = root.GetProperty("type").GetString() ?? string.Empty;
        var responseId = root.TryGetProperty("response_id", out var responseProperty) ? responseProperty.GetString() ?? activeResponseId : activeResponseId;
        if (!string.IsNullOrWhiteSpace(responseId) && !string.Equals(activeResponseId, responseId, StringComparison.Ordinal))
        {
            activeResponseId = responseId;
            activeTranscript.Clear();
            firstAudioLogged = false;
            firstTranscriptLogged = false;
        }

        switch (type)
        {
            case "response.audio.delta":
            case "response.output_audio.delta":
                var audio = root.GetProperty("delta").GetString() ?? string.Empty;
                if (!firstAudioLogged)
                {
                    firstAudioLogged = true;
                    logger.LogInformation("Realtime first assistant audio delta ms. SessionId={SessionId}; ResponseId={ResponseId}; FirstAssistantAudioDeltaMs={ElapsedMs}; SinceUserAudioCommitMs={SinceUserAudioCommitMs}.", sessionId, activeResponseId, stopwatch.ElapsedMilliseconds, lastUserAudioCommitMs > 0 ? stopwatch.ElapsedMilliseconds - lastUserAudioCommitMs : 0);
                }
                await SendDesktopEventAsync(new { type = "assistant.audio.delta", sessionId, responseId = activeResponseId, audio }, cancellationToken);
                break;
            case "response.audio_transcript.delta":
            case "response.output_audio_transcript.delta":
                var delta = root.GetProperty("delta").GetString() ?? string.Empty;
                activeTranscript.Append(delta);
                if (!firstTranscriptLogged)
                {
                    firstTranscriptLogged = true;
                    logger.LogInformation("Realtime first assistant transcript delta ms. SessionId={SessionId}; ResponseId={ResponseId}; FirstAssistantTranscriptDeltaMs={ElapsedMs}; SinceUserAudioCommitMs={SinceUserAudioCommitMs}.", sessionId, activeResponseId, stopwatch.ElapsedMilliseconds, lastUserAudioCommitMs > 0 ? stopwatch.ElapsedMilliseconds - lastUserAudioCommitMs : 0);
                }
                await SendDesktopEventAsync(new { type = "assistant.transcript.delta", sessionId, responseId = activeResponseId, delta }, cancellationToken);
                break;
            case "response.done":
                logger.LogInformation("Realtime assistant response completed ms. SessionId={SessionId}; ResponseId={ResponseId}; AssistantResponseCompletedMs={ElapsedMs}; SinceUserAudioCommitMs={SinceUserAudioCommitMs}; TranscriptLength={TranscriptLength}.", sessionId, activeResponseId, stopwatch.ElapsedMilliseconds, lastUserAudioCommitMs > 0 ? stopwatch.ElapsedMilliseconds - lastUserAudioCommitMs : 0, activeTranscript.Length);
                await SendDesktopEventAsync(new { type = "assistant.turn.completed", sessionId, responseId = activeResponseId, transcript = activeTranscript.ToString() }, cancellationToken);
                break;
            case "input_audio_buffer.committed":
                var itemId = root.TryGetProperty("item_id", out var itemIdProperty) ? itemIdProperty.GetString() ?? string.Empty : string.Empty;
                logger.LogInformation("Realtime user audio committed event received. SessionId={SessionId}; ItemId={ItemId}; UserAudioCommittedEventMs={ElapsedMs}.", sessionId, itemId, stopwatch.ElapsedMilliseconds);
                await SendDesktopEventAsync(new { type = "user.audio.committed", sessionId, itemId }, cancellationToken);
                break;
            case "conversation.item.input_audio_transcription.delta":
                var transcriptDelta = root.TryGetProperty("delta", out var transcriptDeltaProperty) ? transcriptDeltaProperty.GetString() ?? string.Empty : string.Empty;
                var deltaItemId = root.TryGetProperty("item_id", out var deltaItemIdProperty) ? deltaItemIdProperty.GetString() ?? string.Empty : string.Empty;
                logger.LogInformation("Realtime user transcript delta received. SessionId={SessionId}; ItemId={ItemId}; TranscriptLength={TranscriptLength}; EventMs={ElapsedMs}.", sessionId, deltaItemId, transcriptDelta.Length, stopwatch.ElapsedMilliseconds);
                await SendDesktopEventAsync(new { type = "user.transcript.delta", sessionId, itemId = deltaItemId, delta = transcriptDelta }, cancellationToken);
                break;
            case "conversation.item.input_audio_transcription.completed":
                var userTranscript = root.TryGetProperty("transcript", out var userTranscriptProperty) ? userTranscriptProperty.GetString() ?? string.Empty : string.Empty;
                var transcriptItemId = root.TryGetProperty("item_id", out var transcriptItemIdProperty) ? transcriptItemIdProperty.GetString() ?? string.Empty : string.Empty;
                logger.LogInformation("Realtime user transcript completed received. SessionId={SessionId}; ItemId={ItemId}; TranscriptLength={TranscriptLength}; EventMs={ElapsedMs}.", sessionId, transcriptItemId, userTranscript.Trim().Length, stopwatch.ElapsedMilliseconds);
                await SendDesktopEventAsync(new { type = "user.transcript.completed", sessionId, itemId = transcriptItemId, transcript = userTranscript }, cancellationToken);
                break;
            case "error":
                var message = root.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var errorMessage) ? errorMessage.GetString() : "Realtime voice mode is unavailable. Please try text mode.";
                logger.LogWarning("Realtime error event. SessionId={SessionId}; ResponseId={ResponseId}; Error={Error}.", sessionId, activeResponseId, message);
                await SendDesktopEventAsync(new { type = "session.error", sessionId, responseId = activeResponseId, message }, cancellationToken);
                break;
        }
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

            var role = IsTutorSender(message.Sender, request) ? "assistant" : "user";
            await SendOpenAiEventAsync(new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role,
                    content = new[] { new { type = role == "assistant" ? "output_text" : "input_text", text } }
                }
            }, cancellationToken);
        }

        logger.LogInformation("Realtime recent conversation seeded. SessionId={SessionId}; RecentMessages={RecentMessageCount}; LastBotMessageLength={LastBotMessageLength}.", sessionId, request.RecentMessages.Count, request.LastBotMessage?.Length ?? 0);
    }

    private string BuildResponseInstructions()
    {
        if (startRequest is not null && startRequest.LessonType.Equals("guided_roleplay", StringComparison.OrdinalIgnoreCase))
        {
            return $"Respond now in English as the tutor role inside the active guided roleplay scenario '{startRequest.SelectedContextTitle}'. Continue naturally from the last tutor message: '{startRequest.LastBotMessage}'. Produce audio and matching audio transcript from this same response. Ask one simple on-scenario question at a time. Do not ask what topic the learner wants to discuss and do not ask the learner to choose a situation.";
        }

        return "Respond now in English. Produce audio and matching audio transcript from this same response. Ask one question at a time.";
    }

    private async Task CreateResponseAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Realtime response.create sent. SessionId={SessionId}; LearnerTurnCount={LearnerTurnCount}; ResponseCreateSentMs={ElapsedMs}; SinceUserAudioCommitMs={SinceUserAudioCommitMs}.", sessionId, learnerTurnCount, stopwatch.ElapsedMilliseconds, lastUserAudioCommitMs > 0 ? stopwatch.ElapsedMilliseconds - lastUserAudioCommitMs : 0);
        await SendOpenAiEventAsync(new
        {
            type = "response.create",
            response = new
            {
                modalities = new[] { "text", "audio" },
                instructions = BuildResponseInstructions()
            }
        }, cancellationToken);
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
        return SendJsonAsync(socket, value, cancellationToken);
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

    private static bool IsExpectedDesktopDisconnect(WebSocketException exception, CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested
            || exception.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely;
    }

    private async Task DisconnectAsync(string reason, CancellationToken cancellationToken)
    {
        logger.LogInformation("Realtime disconnect. SessionId={SessionId}; ResponseId={ResponseId}; DisconnectReason={DisconnectReason}; ElapsedMs={ElapsedMs}.", sessionId, activeResponseId, reason, stopwatch.ElapsedMilliseconds);
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
            || message.Sender.Contains("tutor", StringComparison.OrdinalIgnoreCase)
            || message.Sender.Contains("Elena", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildInstructions(RealtimeVoiceSessionStartRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are the realtime voice engine for English Voice Tutor Desktop.");
        builder.AppendLine("Voice-first rule: every assistant response must produce audio and a matching transcript from the same response id and same turn. Do not rely on separate TTS or separate text generation.");
        builder.AppendLine("Speak only English. Keep responses appropriate to the selected learner level. Ask one question at a time.");
        builder.AppendLine($"Tutor profile: {request.TutorDisplayName} ({request.TutorProfileId}).");
        builder.AppendLine($"Level: {request.SelectedLevel}.");
        builder.AppendLine($"Topic: {Choose(request.Topic, request.TopicTitle)}.");
        builder.AppendLine($"Subtopic/situation: {Choose(request.Subtopic, request.SubtopicTitle)}.");
        builder.AppendLine($"Lesson type: {request.LessonType}.");
        builder.AppendLine($"Lesson scenario id: {request.LessonScenarioId}.");
        builder.AppendLine($"Lesson goal: {request.LessonGoal}.");
        builder.AppendLine($"Lesson phase: {Choose(request.CurrentPhase, request.LessonPhase)}.");
        if (!string.IsNullOrWhiteSpace(request.TutorRole)) builder.AppendLine($"Tutor role: {request.TutorRole}.");
        if (!string.IsNullOrWhiteSpace(request.UserRole)) builder.AppendLine($"Learner role: {request.UserRole}.");
        if (!string.IsNullOrWhiteSpace(request.Situation)) builder.AppendLine($"Situation: {request.Situation}.");
        if (!string.IsNullOrWhiteSpace(request.LearningGoal)) builder.AppendLine($"Learner personal goal: {request.LearningGoal}.");
        builder.AppendLine($"Target language: {request.TargetLanguageName}.");
        builder.AppendLine($"Turn limits: soft wrap-up after learner turn {request.SoftLearnerTurnLimit}; final learner turn {request.HardLearnerTurnLimit}. Current learner turn count is {request.LearnerTurnCount}. The server also enforces these limits.");
        builder.AppendLine($"Feedback rules: {request.FeedbackRulesSummary}.");
        builder.AppendLine($"Level profile: {request.ActiveLevelProfile.DifficultyNotes} {request.ActiveLevelProfile.TutorLanguageStyle} Expected learner response: {request.ActiveLevelProfile.ExpectedUserResponse} Conversation depth: {request.ActiveLevelProfile.ConversationDepth}.");
        if (request.TargetLanguageKeyPhrases.Count > 0) builder.AppendLine($"Target key phrases: {string.Join(", ", request.TargetLanguageKeyPhrases)}.");
        if (request.GrammarFocus.Count > 0) builder.AppendLine($"Grammar focus: {string.Join(", ", request.GrammarFocus)}.");
        if (request.AiTutorPromptInstructions.Count > 0) builder.AppendLine($"Lesson-specific instructions: {string.Join(" ", request.AiTutorPromptInstructions)}");
        if (!string.IsNullOrWhiteSpace(request.SelectedContextTitle)) builder.AppendLine($"Selected guided roleplay context: {request.SelectedContextTitle}. Variant id: {request.SelectedContextVariantId}. Opening line already shown: {request.SelectedContextOpeningLine}.");
        if (!string.IsNullOrWhiteSpace(request.LastBotMessage)) builder.AppendLine($"Last visible tutor message to continue from: {request.LastBotMessage}.");
        if (request.LessonType.Equals("guided_roleplay", StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendLine("You are in an active guided roleplay, not free conversation.");
            builder.AppendLine($"The selected scenario is: {request.SelectedContextTitle}.");
            builder.AppendLine($"The last tutor message already said: {request.LastBotMessage}.");
            builder.AppendLine("Continue naturally from that message. Stay inside the selected scenario and role.");
            builder.AppendLine("Ask one simple on-scenario question at a time.");
            builder.AppendLine("Never ask the learner what topic they want to discuss.");
            builder.AppendLine("Never say 'How can I assist you today?' or similar generic assistant offers.");
            builder.AppendLine("Do not ask the learner to choose a situation during ActiveRoleplay. The situation is already selected.");
            builder.AppendLine("If the learner asks a meta question, answer briefly and return to the selected scenario.");
        }
        else if (request.LessonType.Equals("free_conversation", StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendLine("Free Conversation: allow safe open topics, keep conversation in English, refuse unsafe content briefly, and redirect to safe everyday English practice.");
        }
        if (request.RecentMessages.Count > 0)
        {
            builder.AppendLine("Recent conversation context:");
            foreach (var message in request.RecentMessages)
            {
                builder.AppendLine($"- {message.Sender}: {message.Text}");
            }
        }
        return builder.ToString();
    }

    private static string Choose(string first, string second)
    {
        return string.IsNullOrWhiteSpace(first) ? second : first;
    }
}
