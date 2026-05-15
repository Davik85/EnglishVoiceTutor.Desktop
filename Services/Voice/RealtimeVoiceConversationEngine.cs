using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Services;

namespace EnglishVoiceTutor.Desktop.Services.Voice;

public sealed class RealtimeVoiceConversationEngine : IVoiceConversationEngine, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly LessonChatBackendService backendService;
    private readonly SemaphoreSlim sendLock = new(1, 1);
    private ClientWebSocket? webSocket;
    private CancellationTokenSource? receiveCancellationTokenSource;
    private Task? receiveTask;
    private Stopwatch sessionStopwatch = new();
    private string sessionId = string.Empty;
    private string activeResponseId = string.Empty;
    private readonly StringBuilder activeTranscript = new();
    private TaskCompletionSource<bool>? sessionStartCompletionSource;
    private bool disposed;
    private bool stopRequested;

    public RealtimeVoiceConversationEngine(LessonChatBackendService backendService)
    {
        this.backendService = backendService;
    }

    public event EventHandler<AssistantAudioChunkReceivedEventArgs>? AssistantAudioChunkReceived;
    public event EventHandler<AssistantTranscriptDeltaEventArgs>? AssistantTranscriptDeltaReceived;
    public event EventHandler<AssistantTurnCompletedEventArgs>? AssistantTurnCompleted;
    public event EventHandler<UserAudioCommittedEventArgs>? UserAudioCommitted;
    public event EventHandler<UserTranscriptDeltaEventArgs>? UserTranscriptDeltaReceived;
    public event EventHandler<UserTranscriptCompletedEventArgs>? UserTranscriptCompleted;
    public event EventHandler<UserTranscriptFailedEventArgs>? UserTranscriptFailed;
    public event EventHandler<VoiceSessionErrorEventArgs>? ErrorReceived;
    public event EventHandler<VoiceSessionDisconnectedEventArgs>? Disconnected;

    public async Task StartSessionAsync(VoiceSessionStartRequest request, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        await StopSessionAsync(CancellationToken.None);
        stopRequested = false;
        sessionId = request.SessionId;
        activeTranscript.Clear();
        activeResponseId = string.Empty;
        sessionStopwatch = Stopwatch.StartNew();

        var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader(BackendConstants.NgrokSkipBrowserWarningHeaderName, BackendConstants.NgrokSkipBrowserWarningHeaderValue);
        webSocket = socket;
        receiveCancellationTokenSource = new CancellationTokenSource();
        sessionStartCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var endpoint = backendService.CreateRealtimeVoiceWebSocketUri();
        Debug.WriteLine($"Realtime voice session connecting: SessionId={sessionId}; Endpoint={endpoint}; LessonType={request.LessonType}; Topic={request.Topic}; Subtopic={request.Subtopic}; Level={request.SelectedLevel}.");
        await socket.ConnectAsync(endpoint, cancellationToken);
        receiveTask = ReceiveLoopAsync(socket, receiveCancellationTokenSource.Token);
        await SendBackendEventAsync("session.start", request, cancellationToken);

        var startCompletion = sessionStartCompletionSource;
        if (startCompletion is not null)
        {
            await startCompletion.Task.WaitAsync(TimeSpan.FromSeconds(BackendConstants.BackendRequestTimeoutSeconds), cancellationToken);
        }

        Debug.WriteLine($"Realtime voice session start ms: SessionId={sessionId}; StartMs={sessionStopwatch.ElapsedMilliseconds}.");
    }

    public Task SendUserTextAsync(string text, CancellationToken cancellationToken)
    {
        return SendBackendEventAsync("user.text", new { text }, cancellationToken);
    }

    public Task StartUserAudioAsync(CancellationToken cancellationToken)
    {
        return SendBackendEventAsync("user.audio.start", new { }, cancellationToken);
    }

    public Task AppendUserAudioAsync(ReadOnlyMemory<byte> audioChunk, CancellationToken cancellationToken)
    {
        return SendBackendEventAsync("user.audio.append", new { audio = Convert.ToBase64String(audioChunk.Span) }, cancellationToken);
    }

    public Task CommitUserAudioAsync(CancellationToken cancellationToken)
    {
        Debug.WriteLine($"Realtime user audio commit requested: SessionId={sessionId}; UserAudioCommitRequestedMs={sessionStopwatch.ElapsedMilliseconds}.");
        return SendBackendEventAsync("user.audio.commit", new { }, cancellationToken);
    }

    public async Task StopSessionAsync(CancellationToken cancellationToken)
    {
        stopRequested = true;
        var socket = webSocket;
        receiveCancellationTokenSource?.Cancel();
        if (socket is not null)
        {
            try
            {
                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    await SendBackendEventAsync("session.stop", new { reason = "client_stop" }, CancellationToken.None);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "client_stop", cancellationToken);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Realtime voice session stop warning: SessionId={sessionId}; CancellationReason=client_stop; {exception}");
            }

            socket.Dispose();
        }

        webSocket = null;
        receiveCancellationTokenSource?.Dispose();
        receiveCancellationTokenSource = null;
        sessionStartCompletionSource = null;
        Debug.WriteLine($"Realtime voice session stopped: SessionId={sessionId}; CancellationReason=client_stop; ElapsedMs={sessionStopwatch.ElapsedMilliseconds}.");
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        _ = StopSessionAsync(CancellationToken.None);
        sendLock.Dispose();
        disposed = true;
    }

    private async Task SendBackendEventAsync(string type, object payload, CancellationToken cancellationToken)
    {
        var socket = webSocket ?? throw new InvalidOperationException("Realtime voice session is not started.");
        if (socket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("Realtime voice mode is unavailable. Please try text mode.");
        }

        var json = JsonSerializer.Serialize(new { type, sessionId, payload }, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await sendLock.WaitAsync(cancellationToken);
        try
        {
            await socket.SendAsync(bytes, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, cancellationToken);
        }
        finally
        {
            sendLock.Release();
        }
    }

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        var builder = new StringBuilder();

        var disconnectReason = "receive_loop_ended";
        var expectedDisconnect = false;

        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    disconnectReason = "websocket_close_received";
                    expectedDisconnect = stopRequested;
                    break;
                }

                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (!result.EndOfMessage)
                {
                    continue;
                }

                var json = builder.ToString();
                builder.Clear();
                HandleBackendEvent(json);
            }
        }
        catch (OperationCanceledException)
        {
            disconnectReason = stopRequested ? "client_stop" : "receive_loop_canceled";
            expectedDisconnect = stopRequested;
        }
        catch (Exception exception)
        {
            disconnectReason = "receive_loop_failed";
            expectedDisconnect = false;
            Debug.WriteLine($"Realtime voice receive loop failed: SessionId={sessionId}; {exception}");
            sessionStartCompletionSource?.TrySetException(exception);
            ErrorReceived?.Invoke(this, new VoiceSessionErrorEventArgs(sessionId, "Realtime voice mode is unavailable. Please try text mode.", activeResponseId, exception));
        }
        finally
        {
            var socketState = socket.State.ToString();
            Debug.WriteLine($"Realtime voice receive loop ended: SessionId={sessionId}; Reason={disconnectReason}; Expected={expectedDisconnect}; SocketState={socketState}; ElapsedMs={sessionStopwatch.ElapsedMilliseconds}.");
            if (!expectedDisconnect)
            {
                Disconnected?.Invoke(this, new VoiceSessionDisconnectedEventArgs(sessionId, activeResponseId, disconnectReason, expectedDisconnect, socketState));
            }
        }
    }

    private void HandleBackendEvent(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var type = root.GetProperty("type").GetString() ?? string.Empty;
        var eventSessionId = root.TryGetProperty("sessionId", out var sessionProperty) ? sessionProperty.GetString() ?? sessionId : sessionId;
        var responseId = root.TryGetProperty("responseId", out var responseProperty) ? responseProperty.GetString() ?? activeResponseId : activeResponseId;

        if (!string.IsNullOrWhiteSpace(eventSessionId) && !string.Equals(eventSessionId, sessionId, StringComparison.Ordinal))
        {
            Debug.WriteLine($"Ignoring stale realtime event: ActiveSessionId={sessionId}; EventSessionId={eventSessionId}; EventType={type}.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(responseId) && !string.Equals(activeResponseId, responseId, StringComparison.Ordinal))
        {
            activeResponseId = responseId;
            activeTranscript.Clear();
        }

        switch (type)
        {
            case "session.started":
            case "session.ready":
                sessionStartCompletionSource?.TrySetResult(true);
                Debug.WriteLine($"Realtime voice session ready acknowledgement received: SessionId={eventSessionId}; SessionConfiguredMs={sessionStopwatch.ElapsedMilliseconds}.");
                break;
            case "assistant.audio.delta":
                var audioBase64 = root.GetProperty("audio").GetString() ?? string.Empty;
                var audioBytes = Convert.FromBase64String(audioBase64);
                Debug.WriteLine($"Realtime assistant audio delta received: SessionId={eventSessionId}; ResponseId={responseId}; AudioDeltaMs={sessionStopwatch.ElapsedMilliseconds}; Bytes={audioBytes.Length}.");
                AssistantAudioChunkReceived?.Invoke(this, new AssistantAudioChunkReceivedEventArgs(eventSessionId, responseId, audioBytes, sessionStopwatch.ElapsedMilliseconds));
                break;
            case "assistant.transcript.delta":
                var delta = root.GetProperty("delta").GetString() ?? string.Empty;
                activeTranscript.Append(delta);
                Debug.WriteLine($"Realtime assistant transcript delta received: SessionId={eventSessionId}; ResponseId={responseId}; TranscriptDeltaMs={sessionStopwatch.ElapsedMilliseconds}; DeltaLength={delta.Length}; TranscriptLength={activeTranscript.Length}.");
                AssistantTranscriptDeltaReceived?.Invoke(this, new AssistantTranscriptDeltaEventArgs(eventSessionId, responseId, delta, activeTranscript.ToString(), sessionStopwatch.ElapsedMilliseconds));
                break;
            case "assistant.turn.completed":
                var transcript = root.TryGetProperty("transcript", out var transcriptProperty) ? transcriptProperty.GetString() ?? activeTranscript.ToString() : activeTranscript.ToString();
                Debug.WriteLine($"Realtime assistant transcript finalized: SessionId={eventSessionId}; ResponseId={responseId}; AssistantTurnCompletedMs={sessionStopwatch.ElapsedMilliseconds}; TranscriptLength={transcript.Length}.");
                AssistantTurnCompleted?.Invoke(this, new AssistantTurnCompletedEventArgs(eventSessionId, responseId, transcript, sessionStopwatch.ElapsedMilliseconds));
                break;
            case "user.audio.committed":
                var committedItemId = root.TryGetProperty("itemId", out var committedItemProperty) ? committedItemProperty.GetString() ?? string.Empty : string.Empty;
                Debug.WriteLine($"Realtime user audio committed: SessionId={eventSessionId}; ItemId={committedItemId}; UserAudioCommittedMs={sessionStopwatch.ElapsedMilliseconds}.");
                UserAudioCommitted?.Invoke(this, new UserAudioCommittedEventArgs(eventSessionId, committedItemId, sessionStopwatch.ElapsedMilliseconds));
                break;
            case "user.transcript.delta":
                var userDelta = root.TryGetProperty("delta", out var userDeltaProperty) ? userDeltaProperty.GetString() ?? string.Empty : string.Empty;
                var userDeltaItemId = root.TryGetProperty("itemId", out var userDeltaItemProperty) ? userDeltaItemProperty.GetString() ?? string.Empty : string.Empty;
                Debug.WriteLine($"Realtime user transcript delta received: SessionId={eventSessionId}; ItemId={userDeltaItemId}; TranscriptLength={userDelta.Length}; UserTranscriptDeltaMs={sessionStopwatch.ElapsedMilliseconds}.");
                UserTranscriptDeltaReceived?.Invoke(this, new UserTranscriptDeltaEventArgs(eventSessionId, userDeltaItemId, userDelta, sessionStopwatch.ElapsedMilliseconds));
                break;
            case "user.transcript.completed":
                var userTranscript = root.TryGetProperty("transcript", out var userTranscriptProperty) ? userTranscriptProperty.GetString() ?? string.Empty : string.Empty;
                var userTranscriptItemId = root.TryGetProperty("itemId", out var userTranscriptItemProperty) ? userTranscriptItemProperty.GetString() ?? string.Empty : string.Empty;
                Debug.WriteLine($"Realtime user transcript complete received: SessionId={eventSessionId}; ItemId={userTranscriptItemId}; TranscriptLength={userTranscript.Trim().Length}; UserTranscriptCompletedMs={sessionStopwatch.ElapsedMilliseconds}.");
                UserTranscriptCompleted?.Invoke(this, new UserTranscriptCompletedEventArgs(eventSessionId, userTranscriptItemId, userTranscript, sessionStopwatch.ElapsedMilliseconds));
                break;
            case "user.audio.ignored":
                Debug.WriteLine($"Realtime user audio ignored: SessionId={eventSessionId}; Reason=too_short; ElapsedMs={sessionStopwatch.ElapsedMilliseconds}.");
                UserTranscriptFailed?.Invoke(this, new UserTranscriptFailedEventArgs(eventSessionId, string.Empty, "Audio too short.", sessionStopwatch.ElapsedMilliseconds));
                break;
            case "user.transcript.failed":
                var failedItemId = root.TryGetProperty("itemId", out var failedItemProperty) ? failedItemProperty.GetString() ?? string.Empty : string.Empty;
                var failureMessage = root.TryGetProperty("message", out var failureMessageProperty) ? failureMessageProperty.GetString() ?? "Transcription unavailable." : "Transcription unavailable.";
                Debug.WriteLine($"Realtime user transcript failed received: SessionId={eventSessionId}; ItemId={failedItemId}; UserTranscriptFailedMs={sessionStopwatch.ElapsedMilliseconds}.");
                UserTranscriptFailed?.Invoke(this, new UserTranscriptFailedEventArgs(eventSessionId, failedItemId, failureMessage, sessionStopwatch.ElapsedMilliseconds));
                break;
            case "session.startup_failed":
            case "session.error":
                var message = root.TryGetProperty("message", out var messageProperty) ? messageProperty.GetString() ?? "Realtime voice mode is unavailable. Please try text mode." : "Realtime voice mode is unavailable. Please try text mode.";
                sessionStartCompletionSource?.TrySetException(new InvalidOperationException(message));
                ErrorReceived?.Invoke(this, new VoiceSessionErrorEventArgs(eventSessionId, message, responseId));
                break;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
