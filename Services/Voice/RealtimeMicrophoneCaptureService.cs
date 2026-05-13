using System.Diagnostics;
using EnglishVoiceTutor.Desktop.Constants;
using NAudio.Wave;

namespace EnglishVoiceTutor.Desktop.Services.Voice;

public sealed class RealtimeMicrophoneCaptureService : IDisposable
{
    private readonly AudioInputDeviceService audioInputDeviceService = new();
    private WaveInEvent? waveIn;
    private bool disposed;
    private DateTimeOffset? startedAt;

    public bool IsRecording { get; private set; }

    public TimeSpan LastRecordingDuration { get; private set; }

    public event EventHandler<RealtimeMicrophoneAudioChunkEventArgs>? AudioChunkCaptured;

    public void Start(string? audioInputDeviceId = null)
    {
        ThrowIfDisposed();
        if (IsRecording)
        {
            throw new InvalidOperationException(AudioConstants.RecordingAlreadyInProgressMessage);
        }

        var recorder = new WaveInEvent
        {
            WaveFormat = new WaveFormat(AudioConstants.RealtimeInputPcmSampleRate, AudioConstants.RealtimeInputPcmBitsPerSample, AudioConstants.RealtimeInputPcmChannels),
            BufferMilliseconds = AudioConstants.RealtimeMicrophoneBufferMilliseconds
        };
        var deviceNumber = audioInputDeviceService.ResolveDeviceNumber(audioInputDeviceId);
        if (deviceNumber.HasValue)
        {
            recorder.DeviceNumber = deviceNumber.Value;
        }

        recorder.DataAvailable += OnDataAvailable;
        recorder.StartRecording();
        waveIn = recorder;
        startedAt = DateTimeOffset.UtcNow;
        LastRecordingDuration = TimeSpan.Zero;
        IsRecording = true;
        Debug.WriteLine($"Realtime microphone streaming started: SampleRate={recorder.WaveFormat.SampleRate}; Bits={recorder.WaveFormat.BitsPerSample}; Channels={recorder.WaveFormat.Channels}; BufferMilliseconds={recorder.BufferMilliseconds}.");
    }

    public TimeSpan Stop()
    {
        ThrowIfDisposed();
        if (!IsRecording)
        {
            return TimeSpan.Zero;
        }

        LastRecordingDuration = startedAt is null ? TimeSpan.Zero : DateTimeOffset.UtcNow - startedAt.Value;
        waveIn?.StopRecording();
        Cleanup();
        IsRecording = false;
        startedAt = null;
        Debug.WriteLine($"Realtime microphone streaming stopped: DurationMs={LastRecordingDuration.TotalMilliseconds:0}.");
        return LastRecordingDuration;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Cleanup();
        disposed = true;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs args)
    {
        if (args.BytesRecorded <= 0)
        {
            return;
        }

        var chunk = new byte[args.BytesRecorded];
        Buffer.BlockCopy(args.Buffer, 0, chunk, 0, args.BytesRecorded);
        AudioChunkCaptured?.Invoke(this, new RealtimeMicrophoneAudioChunkEventArgs(chunk));
    }

    private void Cleanup()
    {
        if (waveIn is null)
        {
            return;
        }

        waveIn.DataAvailable -= OnDataAvailable;
        waveIn.Dispose();
        waveIn = null;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}

public sealed class RealtimeMicrophoneAudioChunkEventArgs : EventArgs
{
    public RealtimeMicrophoneAudioChunkEventArgs(byte[] audioChunk)
    {
        AudioChunk = audioChunk;
    }

    public byte[] AudioChunk { get; }
}
