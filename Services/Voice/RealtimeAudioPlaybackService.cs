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
    private bool disposed;
    private string sessionId = string.Empty;
    private string responseId = string.Empty;
    private long totalBytes;
    private int totalChunks;
    private int underrunCount;
    private Timer? underrunTimer;

    public int UnderrunCount => underrunCount;

    public event EventHandler<RealtimePlaybackStartedEventArgs>? PlaybackStarted;

    public void StartSession(string newSessionId, string newResponseId)
    {
        ThrowIfDisposed();
        Stop("new realtime playback session");

        sessionId = newSessionId;
        responseId = newResponseId;
        stopwatch = Stopwatch.StartNew();
        totalBytes = 0;
        totalChunks = 0;
        underrunCount = 0;
        playbackStarted = false;

        var waveFormat = new WaveFormat(AudioConstants.RealtimeOutputPcmSampleRate, AudioConstants.RealtimeOutputPcmBitsPerSample, AudioConstants.RealtimeOutputPcmChannels);
        bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferDuration = TimeSpan.FromSeconds(AudioConstants.RealtimePlaybackBufferDurationSeconds),
            DiscardOnBufferOverflow = false
        };
        outputDevice = new WaveOutEvent();
        outputDevice.Init(bufferedWaveProvider);
        underrunTimer = new Timer(CheckUnderrun, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));

        Debug.WriteLine($"Realtime playback session ready: SessionId={sessionId}; ResponseId={responseId}; WaveFormat={waveFormat.SampleRate}Hz/{waveFormat.BitsPerSample}bit/{waveFormat.Channels}ch.");
    }

    public void AddAudioChunk(string chunkSessionId, string chunkResponseId, ReadOnlySpan<byte> audioChunk)
    {
        ThrowIfDisposed();
        if (audioChunk.Length == 0)
        {
            return;
        }

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
                Debug.WriteLine($"Realtime first assistant audio chunk received ms: SessionId={sessionId}; ResponseId={responseId}; FirstAudioChunkReceivedMs={elapsedMs}; Bytes={chunkBytes.Length}.");
            }

            if (!playbackStarted && bufferedWaveProvider.BufferedBytes >= AudioConstants.RealtimePlaybackInitialPrebufferBytes)
            {
                outputDevice!.Play();
                playbackStarted = true;
                var playbackStartedMs = stopwatch?.ElapsedMilliseconds ?? 0;
                Debug.WriteLine($"Realtime playback started ms: SessionId={sessionId}; ResponseId={responseId}; PlaybackStartedMs={playbackStartedMs}; BufferedBytes={bufferedWaveProvider.BufferedBytes}; TotalAudioBytes={totalBytes}; TotalAudioChunks={totalChunks}.");
                PlaybackStarted?.Invoke(this, new RealtimePlaybackStartedEventArgs(sessionId, responseId, playbackStartedMs));
            }
        }
    }

    public void CompleteResponse(string completedSessionId, string completedResponseId)
    {
        lock (syncRoot)
        {
            Debug.WriteLine($"Realtime playback response completed: SessionId={completedSessionId}; ResponseId={completedResponseId}; TotalAudioBytes={totalBytes}; TotalAudioChunks={totalChunks}; BufferUnderrunCount={underrunCount}.");
        }
    }

    public void Stop(string cancellationReason)
    {
        lock (syncRoot)
        {
            Debug.WriteLine($"Realtime playback stopped: SessionId={sessionId}; ResponseId={responseId}; CancellationReason={cancellationReason}; TotalAudioBytes={totalBytes}; TotalAudioChunks={totalChunks}; BufferUnderrunCount={underrunCount}.");
            underrunTimer?.Dispose();
            underrunTimer = null;
            outputDevice?.Stop();
            outputDevice?.Dispose();
            outputDevice = null;
            bufferedWaveProvider = null;
            playbackStarted = false;
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

    private void CheckUnderrun(object? state)
    {
        lock (syncRoot)
        {
            if (!playbackStarted || bufferedWaveProvider is null || bufferedWaveProvider.BufferedBytes > 0)
            {
                return;
            }

            underrunCount++;
            Debug.WriteLine($"Realtime playback buffer underrun: SessionId={sessionId}; ResponseId={responseId}; BufferUnderrunCount={underrunCount}; TotalAudioBytes={totalBytes}; TotalAudioChunks={totalChunks}.");
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
