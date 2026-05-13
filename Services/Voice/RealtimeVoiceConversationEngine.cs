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

    public RealtimeVoiceConversationEngine(LessonChatBackendService backendService)
    {
        this.backendService = backendService;
    }

    public event EventHandler<AssistantAudioChunkReceivedEventArgs>? AssistantAudioChunkReceived;
    public event EventHandler<AssistantTranscriptDeltaEventArgs>? AssistantTranscriptDeltaReceived;
    public event EventHandler<AssistantTurnCompletedEventArgs>? AssistantTurnCompleted;
    public event EventHandler<VoiceSessionErrorEventArgs>? ErrorReceived;

    public async Task StartSessionAsync(VoiceSessionStartRequest request, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        await StopSessionAsync(CancellationToken.None);
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
        return SendBackendEventAsync("user.audio.commit", new { }, cancellationToken);
    }

    public async Task StopSessionAsync(CancellationToken cancellationToken)
    {
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

        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
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
            // Normal shutdown.
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Realtime voice receive loop failed: SessionId={sessionId}; {exception}");
            sessionStartCompletionSource?.TrySetException(exception);
            ErrorReceived?.Invoke(this, new VoiceSessionErrorEventArgs(sessionId, "Realtime voice mode is unavailable. Please try text mode.", activeResponseId, exception));
        }
    }

    private void HandleBackendEvent(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var type = root.GetProperty("type").GetString() ?? string.Empty;
        var eventSessionId = root.TryGetProperty("sessionId", out var sessionProperty) ? sessionProperty.GetString() ?? sessionId : sessionId;
        var responseId = root.TryGetProperty("responseId", out var responseProperty) ? responseProperty.GetString() ?? activeResponseId : activeResponseId;

        if (!string.IsNullOrWhiteSpace(responseId) && !string.Equals(activeResponseId, responseId, StringComparison.Ordinal))
        {
            activeResponseId = responseId;
            activeTranscript.Clear();
        }

        switch (type)
        {
            case "session.started":
                sessionStartCompletionSource?.TrySetResult(true);
                Debug.WriteLine($"Realtime voice session started acknowledgement received: SessionId={eventSessionId}; StartMs={sessionStopwatch.ElapsedMilliseconds}.");
                break;
            case "assistant.audio.delta":
                var audioBase64 = root.GetProperty("audio").GetString() ?? string.Empty;
                var audioBytes = Convert.FromBase64String(audioBase64);
                AssistantAudioChunkReceived?.Invoke(this, new AssistantAudioChunkReceivedEventArgs(eventSessionId, responseId, audioBytes, sessionStopwatch.ElapsedMilliseconds));
                break;
            case "assistant.transcript.delta":
                var delta = root.GetProperty("delta").GetString() ?? string.Empty;
                activeTranscript.Append(delta);
                Debug.WriteLine($"Realtime first transcript delta ms: SessionId={eventSessionId}; ResponseId={responseId}; FirstTranscriptDeltaMs={sessionStopwatch.ElapsedMilliseconds}.");
                AssistantTranscriptDeltaReceived?.Invoke(this, new AssistantTranscriptDeltaEventArgs(eventSessionId, responseId, delta, activeTranscript.ToString(), sessionStopwatch.ElapsedMilliseconds));
                break;
            case "assistant.turn.completed":
                var transcript = root.TryGetProperty("transcript", out var transcriptProperty) ? transcriptProperty.GetString() ?? activeTranscript.ToString() : activeTranscript.ToString();
                Debug.WriteLine($"Realtime assistant turn completed ms: SessionId={eventSessionId}; ResponseId={responseId}; AssistantTurnCompletedMs={sessionStopwatch.ElapsedMilliseconds}; TranscriptLength={transcript.Length}.");
                AssistantTurnCompleted?.Invoke(this, new AssistantTurnCompletedEventArgs(eventSessionId, responseId, transcript, sessionStopwatch.ElapsedMilliseconds));
                break;
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
