using System.Diagnostics;
using System.Globalization;
using System.IO;
using EnglishVoiceTutor.Desktop.Constants;
using NAudio.Wave;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class AudioPlaybackService
{
    private readonly object playbackLock = new();
    private WaveOutEvent? currentOutputDevice;
    private CancellationTokenSource? currentPlaybackCancellationTokenSource;

    public async Task<string> SaveBotVoiceAudioAsync(
        byte[] audioBytes,
        string fileExtension = AudioConstants.DefaultBotVoiceFileExtension,
        CancellationToken cancellationToken = default)
    {
        if (audioBytes.Length == 0)
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidSpeechResponseMessage);
        }

        var normalizedExtension = NormalizeAudioFileExtension(fileExtension);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var filePath = await SaveTemporaryAudioFileAsync(audioBytes, normalizedExtension, cancellationToken);
            var fileInfo = new FileInfo(filePath);
            Debug.WriteLine($"Bot voice audio file save completed: AudioBytes={audioBytes.Length}; SavedAudioPath={filePath}; SavedAudioFileExists={fileInfo.Exists}; SavedAudioFileLength={(fileInfo.Exists ? fileInfo.Length : 0)}; FileExtension={normalizedExtension}; ElapsedMilliseconds={stopwatch.ElapsedMilliseconds}.");
            return filePath;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Bot voice audio file save failed: {exception}");
            throw;
        }
    }

    public async Task PlayAudioFileAsync(string filePath, CancellationToken cancellationToken = default, Action<long>? onPlaybackStarted = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new FileNotFoundException("Bot voice audio file path was empty.", filePath);
        }

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Bot voice audio file was not found.", filePath);
        }

        if (fileInfo.Length <= 0)
        {
            throw new InvalidOperationException($"Bot voice audio file is empty: {filePath}");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            Debug.WriteLine($"Bot voice audio playback starting: SavedAudioPath={filePath}; SavedAudioFileExists={fileInfo.Exists}; SavedAudioFileLength={fileInfo.Length}; FileExtension={Path.GetExtension(filePath)}.");
            await PlayTemporaryAudioFileAsync(filePath, cancellationToken, onPlaybackStarted);
            Debug.WriteLine($"Bot voice audio playback completed: SavedAudioPath={filePath}; SavedAudioFileLength={fileInfo.Length}; FileExtension={Path.GetExtension(filePath)}; ElapsedMilliseconds={stopwatch.ElapsedMilliseconds}.");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Bot voice audio playback failed: SavedAudioPath={filePath}; ExceptionType={exception.GetType().FullName}; Message={exception.Message}; {exception}");
            throw;
        }
    }

    public async Task PlayAudioAsync(byte[] audioBytes, CancellationToken cancellationToken = default)
    {
        var filePath = await SaveBotVoiceAudioAsync(audioBytes, AudioConstants.DefaultBotVoiceFileExtension, cancellationToken);

        try
        {
            await PlayAudioFileAsync(filePath, cancellationToken);
        }
        finally
        {
            SafeDeleteFile(filePath);
        }
    }


    public async Task PlayPcmStreamAsync(
        Stream pcmStream,
        int messageId,
        Action<long>? onFirstAudioChunkReceived = null,
        Action<long>? onPlaybackStarted = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var playbackCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(BackendConstants.BotVoiceFirstAudioTimeoutSeconds));

        var waveFormat = new WaveFormat(
            AudioConstants.BotVoicePcmSampleRate,
            AudioConstants.BotVoicePcmBitsPerSample,
            AudioConstants.BotVoicePcmChannels);
        var bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferDuration = TimeSpan.FromSeconds(AudioConstants.BotVoiceStreamBufferDurationSeconds),
            DiscardOnBufferOverflow = false
        };
        using var outputDevice = new WaveOutEvent();
        var buffer = new byte[AudioConstants.BotVoiceStreamReadBufferBytes];
        var leftoverBytes = Array.Empty<byte>();
        var requiredPrebufferBytes = CalculatePcmByteCount(waveFormat, AudioConstants.BotVoiceInitialPrebufferMilliseconds);
        var maximumPrebufferBytes = CalculatePcmByteCount(waveFormat, AudioConstants.BotVoiceMaximumPrebufferMilliseconds);
        var totalRawBytes = 0L;
        var totalAlignedBytes = 0L;
        var playbackStarted = false;
        var streamCompleted = false;
        var firstRawChunkLogged = false;
        var firstAlignedChunkLogged = false;
        var underrunCount = 0;
        var lastUnderrunLogged = false;

        outputDevice.PlaybackStopped += OnPlaybackStopped;

        Debug.WriteLine($"Bot voice stream setup for message {messageId}: WaveFormat={waveFormat.SampleRate}Hz/{waveFormat.BitsPerSample}bit/{waveFormat.Channels}ch; BlockAlign={waveFormat.BlockAlign}; RequiredPrebufferBytes={requiredPrebufferBytes}; MaximumPrebufferBytes={maximumPrebufferBytes}; BufferDurationSeconds={AudioConstants.BotVoiceStreamBufferDurationSeconds}; DiscardOnBufferOverflow={bufferedWaveProvider.DiscardOnBufferOverflow}.");

        lock (playbackLock)
        {
            currentOutputDevice = outputDevice;
            currentPlaybackCancellationTokenSource = linkedCancellationTokenSource;
        }

        try
        {
            outputDevice.Init(bufferedWaveProvider);
            using var cancellationRegistration = linkedCancellationTokenSource.Token.Register(() =>
            {
                try
                {
                    outputDevice.Stop();
                }
                finally
                {
                    playbackCompletion.TrySetCanceled(linkedCancellationTokenSource.Token);
                }
            });

            var underrunMonitorTask = MonitorUnderrunsAsync(linkedCancellationTokenSource.Token);

            while (true)
            {
                var bytesRead = await pcmStream.ReadAsync(buffer, linkedCancellationTokenSource.Token);

                if (bytesRead == 0)
                {
                    streamCompleted = true;
                    Debug.WriteLine($"Bot voice stream input completed before playback drain for message {messageId}: TotalRawBytes={totalRawBytes}; TotalAlignedBytes={totalAlignedBytes}; LeftoverBytes={leftoverBytes.Length}.");
                    break;
                }

                totalRawBytes += bytesRead;

                if (!firstRawChunkLogged)
                {
                    firstRawChunkLogged = true;
                    var firstChunkMs = stopwatch.ElapsedMilliseconds;
                    Debug.WriteLine($"Bot voice stream first raw chunk received for message {messageId}: ElapsedMilliseconds={firstChunkMs}; FirstRawChunkBytes={bytesRead}; BlockAlign={waveFormat.BlockAlign}.");
                    onFirstAudioChunkReceived?.Invoke(firstChunkMs);
                }

                var combinedByteCount = leftoverBytes.Length + bytesRead;
                var alignedByteCount = combinedByteCount - combinedByteCount % waveFormat.BlockAlign;
                var nextLeftoverByteCount = combinedByteCount - alignedByteCount;
                var combinedBuffer = new byte[combinedByteCount];

                if (leftoverBytes.Length > 0)
                {
                    Buffer.BlockCopy(leftoverBytes, 0, combinedBuffer, 0, leftoverBytes.Length);
                }

                Buffer.BlockCopy(buffer, 0, combinedBuffer, leftoverBytes.Length, bytesRead);

                if (alignedByteCount > 0)
                {
                    if (alignedByteCount % waveFormat.BlockAlign != 0)
                    {
                        throw new InvalidOperationException($"PCM alignment failed: aligned byte count {alignedByteCount} is not divisible by block align {waveFormat.BlockAlign}.");
                    }

                    bufferedWaveProvider.AddSamples(combinedBuffer, 0, alignedByteCount);
                    totalAlignedBytes += alignedByteCount;

                    if (!firstAlignedChunkLogged)
                    {
                        firstAlignedChunkLogged = true;
                        Debug.WriteLine($"Bot voice stream first aligned chunk buffered for message {messageId}: FirstAlignedChunkBytes={alignedByteCount}; LeftoverBytes={nextLeftoverByteCount}; BufferedBytes={bufferedWaveProvider.BufferedBytes}.");
                    }
                }

                leftoverBytes = nextLeftoverByteCount > 0
                    ? combinedBuffer[alignedByteCount..combinedByteCount]
                    : Array.Empty<byte>();

                Debug.WriteLine($"Bot voice stream chunk buffered for message {messageId}: RawChunkBytes={bytesRead}; AlignedChunkBytes={alignedByteCount}; LeftoverBytes={leftoverBytes.Length}; TotalRawBytes={totalRawBytes}; TotalAlignedBytes={totalAlignedBytes}; BufferedBytes={bufferedWaveProvider.BufferedBytes}.");

                if (!playbackStarted && bufferedWaveProvider.BufferedBytes >= requiredPrebufferBytes)
                {
                    StartPlayback("prebuffer reached");
                }

                if (!playbackStarted && bufferedWaveProvider.BufferedBytes >= maximumPrebufferBytes)
                {
                    StartPlayback("maximum prebuffer reached");
                }
            }

            if (leftoverBytes.Length > 0)
            {
                Debug.WriteLine($"Bot voice stream discarded incomplete PCM leftover bytes at end for message {messageId}: DiscardedLeftoverBytes={leftoverBytes.Length}; BlockAlign={waveFormat.BlockAlign}.");
                leftoverBytes = Array.Empty<byte>();
            }

            if (!playbackStarted && bufferedWaveProvider.BufferedBytes > 0)
            {
                StartPlayback("stream completed");
            }

            if (!playbackStarted)
            {
                throw new InvalidOperationException(BackendConstants.BackendInvalidSpeechResponseMessage);
            }

            while (bufferedWaveProvider.BufferedBytes > 0 && !linkedCancellationTokenSource.Token.IsCancellationRequested)
            {
                if (!streamCompleted && bufferedWaveProvider.BufferedBytes == 0)
                {
                    LogUnderrun();
                }

                if (bufferedWaveProvider.BufferedBytes > 0)
                {
                    lastUnderrunLogged = false;
                }

                await Task.Delay(50, linkedCancellationTokenSource.Token);
            }

            if (!streamCompleted && bufferedWaveProvider.BufferedBytes == 0)
            {
                LogUnderrun();
            }

            if (streamCompleted)
            {
                outputDevice.Stop();
                Debug.WriteLine($"Bot voice stream playback stop event received for message {messageId}: Exception=False.");
                playbackCompletion.TrySetResult();
            }

            await playbackCompletion.Task;
            linkedCancellationTokenSource.CancelAfter(Timeout.InfiniteTimeSpan);

            try
            {
                await underrunMonitorTask;
            }
            catch (OperationCanceledException)
            {
                // Playback shutdown can cancel the underrun monitor after audio has drained.
            }

            Debug.WriteLine($"Bot voice stream playback completed for message {messageId}: ElapsedMilliseconds={stopwatch.ElapsedMilliseconds}; TotalRawBytes={totalRawBytes}; TotalAlignedBytes={totalAlignedBytes}; DiscardedLeftoverBytes={leftoverBytes.Length}; UnderrunCount={underrunCount}.");
        }
        finally
        {
            outputDevice.PlaybackStopped -= OnPlaybackStopped;
            if (outputDevice.PlaybackState != PlaybackState.Stopped)
            {
                outputDevice.Stop();
            }
            Debug.WriteLine($"Bot voice stream playback stopped/disposed for message {messageId}.");

            lock (playbackLock)
            {
                if (ReferenceEquals(currentOutputDevice, outputDevice))
                {
                    currentOutputDevice = null;
                    currentPlaybackCancellationTokenSource = null;
                }
            }
        }

        void StartPlayback(string reason)
        {
            if (playbackStarted)
            {
                return;
            }

            outputDevice.Play();
            playbackStarted = true;
            linkedCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(BackendConstants.BotVoiceStreamOverallTimeoutSeconds));
            var playbackStartedMs = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine($"Bot voice stream playback started for message {messageId}: PlaybackStartedMs={playbackStartedMs}; BufferedBytes={bufferedWaveProvider.BufferedBytes}; RequiredPrebufferBytes={requiredPrebufferBytes}; Reason={reason}; TotalRawBytes={totalRawBytes}; TotalAlignedBytes={totalAlignedBytes}.");
            onPlaybackStarted?.Invoke(playbackStartedMs);
        }

        async Task MonitorUnderrunsAsync(CancellationToken monitorCancellationToken)
        {
            while (!monitorCancellationToken.IsCancellationRequested && !streamCompleted)
            {
                if (playbackStarted && bufferedWaveProvider.BufferedBytes == 0)
                {
                    LogUnderrun();
                }
                else if (bufferedWaveProvider.BufferedBytes > 0)
                {
                    lastUnderrunLogged = false;
                }

                await Task.Delay(50, monitorCancellationToken);
            }
        }

        void LogUnderrun()
        {
            if (lastUnderrunLogged)
            {
                return;
            }

            underrunCount++;
            lastUnderrunLogged = true;
            Debug.WriteLine($"Bot voice stream buffer underrun for message {messageId}: BufferedBytes={bufferedWaveProvider.BufferedBytes}; StreamCompleted={streamCompleted}; TotalRawBytes={totalRawBytes}; TotalAlignedBytes={totalAlignedBytes}; UnderrunCount={underrunCount}.");
        }

        void OnPlaybackStopped(object? sender, StoppedEventArgs args)
        {
            if (args.Exception is not null)
            {
                Debug.WriteLine($"Bot voice stream playback stop event received for message {messageId}: Exception=True; ExceptionType={args.Exception.GetType().FullName}; Message={args.Exception.Message}.");
                playbackCompletion.TrySetException(args.Exception);
                return;
            }

            if (streamCompleted)
            {
                Debug.WriteLine($"Bot voice stream playback stop event received for message {messageId}: Exception=False.");
                playbackCompletion.TrySetResult();
            }
        }
    }

    private static int CalculatePcmByteCount(WaveFormat waveFormat, int durationMilliseconds)
    {
        return waveFormat.SampleRate
            * waveFormat.Channels
            * waveFormat.BitsPerSample
            / 8
            * durationMilliseconds
            / 1000;
    }

    public void StopPlayback()
    {
        lock (playbackLock)
        {
            try
            {
                currentPlaybackCancellationTokenSource?.Cancel();
                currentOutputDevice?.Stop();
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Bot voice audio playback stop failed: {exception}");
            }
        }
    }

    private static async Task<string> SaveTemporaryAudioFileAsync(
        byte[] audioBytes,
        string fileExtension,
        CancellationToken cancellationToken)
    {
        var botVoiceFolderPath = GetBotVoiceFolderPath();
        Directory.CreateDirectory(botVoiceFolderPath);

        var timestamp = DateTime.Now.ToString(AudioConstants.RecordingTimestampFormat, CultureInfo.InvariantCulture);
        var fileName = $"{AudioConstants.BotVoiceTempFilePrefix}{timestamp}-{Guid.NewGuid():N}{fileExtension}";
        var filePath = Path.Combine(botVoiceFolderPath, fileName);

        await File.WriteAllBytesAsync(filePath, audioBytes, cancellationToken);

        return filePath;
    }

    private async Task PlayTemporaryAudioFileAsync(string filePath, CancellationToken cancellationToken, Action<long>? onPlaybackStarted = null)
    {
        var playbackCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var audioReader = new AudioFileReader(filePath);
        using var outputDevice = new WaveOutEvent();

        outputDevice.PlaybackStopped += OnPlaybackStopped;

        lock (playbackLock)
        {
            currentOutputDevice = outputDevice;
            currentPlaybackCancellationTokenSource = linkedCancellationTokenSource;
        }

        try
        {
            var initializationStopwatch = Stopwatch.StartNew();
            outputDevice.Init(audioReader);
            Debug.WriteLine($"Bot voice audio playback initialized: SavedAudioPath={filePath}; FileSizeBytes={new FileInfo(filePath).Length}; ElapsedMilliseconds={initializationStopwatch.ElapsedMilliseconds}.");
            outputDevice.Play();
            var playbackStartedMs = initializationStopwatch.ElapsedMilliseconds;
            Debug.WriteLine($"Bot voice audio playback started: SavedAudioPath={filePath}; PlaybackStartInitializationMilliseconds={playbackStartedMs}; PlaybackState={outputDevice.PlaybackState}.");
            onPlaybackStarted?.Invoke(playbackStartedMs);
            using var cancellationRegistration = linkedCancellationTokenSource.Token.Register(() =>
            {
                try
                {
                    outputDevice.Stop();
                }
                finally
                {
                    playbackCompletion.TrySetCanceled(linkedCancellationTokenSource.Token);
                }
            });
            await playbackCompletion.Task;
        }
        finally
        {
            outputDevice.PlaybackStopped -= OnPlaybackStopped;
            if (outputDevice.PlaybackState != PlaybackState.Stopped)
            {
                outputDevice.Stop();
            }
            Debug.WriteLine($"Bot voice audio playback stopped/disposed: SavedAudioPath={filePath}; FileExistsAtDispose={File.Exists(filePath)}.");

            lock (playbackLock)
            {
                if (ReferenceEquals(currentOutputDevice, outputDevice))
                {
                    currentOutputDevice = null;
                    currentPlaybackCancellationTokenSource = null;
                }
            }
        }

        void OnPlaybackStopped(object? sender, StoppedEventArgs args)
        {
            if (args.Exception is not null)
            {
                Debug.WriteLine($"Bot voice audio playback stop event received: SavedAudioPath={filePath}; Exception=True; ExceptionType={args.Exception.GetType().FullName}; Message={args.Exception.Message}.");
                playbackCompletion.TrySetException(args.Exception);
                return;
            }

            playbackCompletion.TrySetResult();
        }
    }

    private static string GetBotVoiceFolderPath()
    {
        return Path.Combine(Path.GetTempPath(), AudioConstants.AppTempFolderName, AudioConstants.BotVoiceTempFolderName);
    }

    private static string NormalizeAudioFileExtension(string fileExtension)
    {
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            return AudioConstants.DefaultBotVoiceFileExtension;
        }

        var trimmedExtension = fileExtension.Trim();
        return trimmedExtension.StartsWith('.')
            ? trimmedExtension
            : $".{trimmedExtension}";
    }

    private static void SafeDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore cleanup errors so playback can continue.
        }
    }
}
