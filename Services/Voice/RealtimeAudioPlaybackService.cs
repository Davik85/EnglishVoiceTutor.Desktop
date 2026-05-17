using System.Diagnostics;
using EnglishVoiceTutor.Desktop.Constants;
using NAudio.Wave;

namespace EnglishVoiceTutor.Desktop.Services.Voice;

public sealed class RealtimeAudioPlaybackService : IDisposable
{
    private readonly object syncRoot = new();
    private BufferedWaveProvider? bufferedWaveProvider;
    private WaveOutEvent? outputDevice;
    private Stopwatch? stopwatch;
    private bool playbackStarted;
    private bool responseCompleted;
    private bool disposed;
    private string sessionId = string.Empty;
    private string responseId = string.Empty;
    private long totalBytes;
    private int totalChunks;
    private int underrunCount;
    private Timer? underrunTimer;

    public int UnderrunCount => underrunCount;

    public bool IsPlaybackActive
    {
        get
        {
            lock (syncRoot)
            {
                return outputDevice is not null && (playbackStarted || (bufferedWaveProvider?.BufferedBytes ?? 0) > 0);
            }
        }
    }

    public event EventHandler<RealtimePlaybackStartedEventArgs>? PlaybackStarted;

    public event EventHandler<RealtimePlaybackCompletedEventArgs>? PlaybackCompleted;

    public void StartSession(string newSessionId, string newResponseId)
    {
        ThrowIfDisposed();
        Stop("new_realtime_playback_session");

        sessionId = newSessionId;
        responseId = newResponseId;
        stopwatch = Stopwatch.StartNew();
        totalBytes = 0;
        totalChunks = 0;
        underrunCount = 0;
        playbackStarted = false;
        responseCompleted = false;

        var waveFormat = new WaveFormat(AudioConstants.RealtimeOutputPcmSampleRate, AudioConstants.RealtimeOutputPcmBitsPerSample, AudioConstants.RealtimeOutputPcmChannels);
        bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferDuration = TimeSpan.FromSeconds(AudioConstants.RealtimePlaybackBufferDurationSeconds),
            DiscardOnBufferOverflow = false
        };
        outputDevice = new WaveOutEvent();
        outputDevice.Init(bufferedWaveProvider);
        underrunTimer = new Timer(CheckPlaybackDrain, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));

        Debug.WriteLine($"Realtime playback session ready: SessionId={sessionId}; ResponseId={responseId}; WaveFormat={waveFormat.SampleRate}Hz/{waveFormat.BitsPerSample}bit/{waveFormat.Channels}ch.");
    }

    public void AddAudioChunk(string chunkSessionId, string chunkResponseId, ReadOnlySpan<byte> audioChunk)
    {
        ThrowIfDisposed();
        if (audioChunk.Length == 0)
        {
            return;
        }

        RealtimePlaybackStartedEventArgs? startedArgs = null;
        lock (syncRoot)
        {
            if (bufferedWaveProvider is null || outputDevice is null || !string.Equals(responseId, chunkResponseId, StringComparison.Ordinal))
            {
                StartSession(chunkSessionId, chunkResponseId);
            }

            var chunkBytes = audioChunk.ToArray();
            bufferedWaveProvider!.AddSamples(chunkBytes, 0, chunkBytes.Length);
            totalBytes += chunkBytes.Length;
            totalChunks++;

            var elapsedMs = stopwatch?.ElapsedMilliseconds ?? 0;
            if (totalChunks == 1)
            {
                Debug.WriteLine($"Realtime first assistant audio chunk received ms: SessionId={sessionId}; ResponseId={responseId}; FirstAudioChunkReceivedMs={elapsedMs}; Bytes={chunkBytes.Length}; StopConversationModeRequested=False.");
            }

            if (!playbackStarted && bufferedWaveProvider.BufferedBytes >= AudioConstants.RealtimePlaybackInitialPrebufferBytes)
            {
                outputDevice!.Play();
                playbackStarted = true;
                var playbackStartedMs = stopwatch?.ElapsedMilliseconds ?? 0;
                Debug.WriteLine($"Realtime playback started ms: SessionId={sessionId}; ResponseId={responseId}; Reason=assistant_playback_started; PlaybackStartedMs={playbackStartedMs}; BufferedBytes={bufferedWaveProvider.BufferedBytes}; TotalAudioBytes={totalBytes}; TotalAudioChunks={totalChunks}; StopConversationModeRequested=False.");
                startedArgs = new RealtimePlaybackStartedEventArgs(sessionId, responseId, playbackStartedMs);
            }
        }

        if (startedArgs is not null)
        {
            PlaybackStarted?.Invoke(this, startedArgs);
        }
    }

    public void CompleteResponse(string completedSessionId, string completedResponseId)
    {
        RealtimePlaybackCompletedEventArgs? completedArgs = null;
        lock (syncRoot)
        {
            responseCompleted = true;
            Debug.WriteLine($"Realtime playback response completed: SessionId={completedSessionId}; ResponseId={completedResponseId}; Reason=assistant_turn_completed; TotalAudioBytes={totalBytes}; TotalAudioChunks={totalChunks}; BufferedBytes={bufferedWaveProvider?.BufferedBytes ?? 0}; BufferUnderrunCount={underrunCount}; StopConversationModeRequested=False.");
            completedArgs = TryCompletePlaybackUnderLock("assistant_response_completed");
        }

        if (completedArgs is not null)
        {
            PlaybackCompleted?.Invoke(this, completedArgs);
        }
    }

    public void Stop(string cancellationReason)
    {
        lock (syncRoot)
        {
            Debug.WriteLine($"Realtime playback stopped: SessionId={sessionId}; ResponseId={responseId}; CancellationReason={cancellationReason}; TotalAudioBytes={totalBytes}; TotalAudioChunks={totalChunks}; BufferUnderrunCount={underrunCount}.");
            StopUnderLock(resetCounters: false);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Stop("dispose");
        disposed = true;
    }

    private void CheckPlaybackDrain(object? state)
    {
        RealtimePlaybackCompletedEventArgs? completedArgs = null;
        lock (syncRoot)
        {
            if (!playbackStarted || bufferedWaveProvider is null || bufferedWaveProvider.BufferedBytes > 0)
            {
                return;
            }

            if (!responseCompleted)
            {
                underrunCount++;
                Debug.WriteLine($"Realtime playback buffer underrun: SessionId={sessionId}; ResponseId={responseId}; BufferUnderrunCount={underrunCount}; TotalAudioBytes={totalBytes}; TotalAudioChunks={totalChunks}.");
                return;
            }

            completedArgs = TryCompletePlaybackUnderLock("assistant_playback_completed");
        }

        if (completedArgs is not null)
        {
            PlaybackCompleted?.Invoke(this, completedArgs);
        }
    }

    private RealtimePlaybackCompletedEventArgs? TryCompletePlaybackUnderLock(string reason)
    {
        if (!responseCompleted || outputDevice is null || bufferedWaveProvider is null || bufferedWaveProvider.BufferedBytes > 0)
        {
            return null;
        }

        var completedSessionId = sessionId;
        var completedResponseId = responseId;
        var elapsedMs = stopwatch?.ElapsedMilliseconds ?? 0;
        Debug.WriteLine($"Realtime playback completed: SessionId={completedSessionId}; ResponseId={completedResponseId}; Reason={reason}; PlaybackCompletedMs={elapsedMs}; TotalAudioBytes={totalBytes}; TotalAudioChunks={totalChunks}; BufferUnderrunCount={underrunCount}; StopConversationModeRequested=False.");
        StopUnderLock(resetCounters: false);
        return new RealtimePlaybackCompletedEventArgs(completedSessionId, completedResponseId, elapsedMs);
    }

    private void StopUnderLock(bool resetCounters)
    {
        underrunTimer?.Dispose();
        underrunTimer = null;
        outputDevice?.Stop();
        outputDevice?.Dispose();
        outputDevice = null;
        bufferedWaveProvider = null;
        playbackStarted = false;
        responseCompleted = false;

        if (resetCounters)
        {
            totalBytes = 0;
            totalChunks = 0;
            underrunCount = 0;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}

public sealed class RealtimePlaybackStartedEventArgs : EventArgs
{
    public RealtimePlaybackStartedEventArgs(string sessionId, string responseId, long elapsedMilliseconds)
    {
        SessionId = sessionId;
        ResponseId = responseId;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public string SessionId { get; }
    public string ResponseId { get; }
    public long ElapsedMilliseconds { get; }
}

public sealed class RealtimePlaybackCompletedEventArgs : EventArgs
{
    public RealtimePlaybackCompletedEventArgs(string sessionId, string responseId, long elapsedMilliseconds)
    {
        SessionId = sessionId;
        ResponseId = responseId;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public string SessionId { get; }
    public string ResponseId { get; }
    public long ElapsedMilliseconds { get; }
}
