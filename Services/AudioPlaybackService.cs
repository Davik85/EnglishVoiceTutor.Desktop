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
            Debug.WriteLine($"Bot voice audio file save completed: AudioBytes={audioBytes.Length}; FileExtension={normalizedExtension}; ElapsedMilliseconds={stopwatch.ElapsedMilliseconds}.");
            return filePath;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Bot voice audio file save failed: {exception}");
            throw;
        }
    }

    public async Task PlayAudioFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            throw new FileNotFoundException("Bot voice audio file was not found.", filePath);
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            Debug.WriteLine($"Bot voice audio playback starting: FileExtension={Path.GetExtension(filePath)}.");
            await PlayTemporaryAudioFileAsync(filePath, cancellationToken);
            Debug.WriteLine($"Bot voice audio playback completed: FileExtension={Path.GetExtension(filePath)}; ElapsedMilliseconds={stopwatch.ElapsedMilliseconds}.");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Bot voice audio playback failed: {exception}");
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
        var waveFormat = new WaveFormat(
            AudioConstants.BotVoicePcmSampleRate,
            AudioConstants.BotVoicePcmBitsPerSample,
            AudioConstants.BotVoicePcmChannels);
        var bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferDuration = TimeSpan.FromSeconds(BackendConstants.BotVoiceStreamOverallTimeoutSeconds),
            DiscardOnBufferOverflow = true
        };
        using var outputDevice = new WaveOutEvent();
        var buffer = new byte[AudioConstants.BotVoiceStreamReadBufferBytes];
        var totalBytes = 0L;
        var playbackStarted = false;
        var streamCompleted = false;

        outputDevice.PlaybackStopped += OnPlaybackStopped;

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

            while (true)
            {
                var bytesRead = await pcmStream.ReadAsync(buffer, linkedCancellationTokenSource.Token);

                if (bytesRead == 0)
                {
                    streamCompleted = true;
                    break;
                }

                totalBytes += bytesRead;

                if (totalBytes == bytesRead)
                {
                    var firstChunkMs = stopwatch.ElapsedMilliseconds;
                    Debug.WriteLine($"Bot voice stream first audio chunk received for message {messageId}: ElapsedMilliseconds={firstChunkMs}; ChunkBytes={bytesRead}.");
                    onFirstAudioChunkReceived?.Invoke(firstChunkMs);
                }

                bufferedWaveProvider.AddSamples(buffer, 0, bytesRead);

                if (!playbackStarted)
                {
                    outputDevice.Play();
                    playbackStarted = true;
                    var playbackStartedMs = stopwatch.ElapsedMilliseconds;
                    Debug.WriteLine($"Bot voice stream playback started for message {messageId}: ElapsedMilliseconds={playbackStartedMs}; BufferedBytes={bufferedWaveProvider.BufferedBytes}.");
                    onPlaybackStarted?.Invoke(playbackStartedMs);
                }
            }

            while (bufferedWaveProvider.BufferedBytes > 0 && !linkedCancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(50, linkedCancellationTokenSource.Token);
            }

            if (streamCompleted)
            {
                outputDevice.Stop();
                playbackCompletion.TrySetResult();
            }

            await playbackCompletion.Task;
            Debug.WriteLine($"Bot voice stream playback completed for message {messageId}: ElapsedMilliseconds={stopwatch.ElapsedMilliseconds}; AudioBytes={totalBytes}.");
        }
        finally
        {
            outputDevice.PlaybackStopped -= OnPlaybackStopped;
            outputDevice.Stop();

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
                playbackCompletion.TrySetException(args.Exception);
                return;
            }

            if (streamCompleted)
            {
                playbackCompletion.TrySetResult();
            }
        }
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

    private async Task PlayTemporaryAudioFileAsync(string filePath, CancellationToken cancellationToken)
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
            Debug.WriteLine($"Bot voice audio playback initialized: ElapsedMilliseconds={initializationStopwatch.ElapsedMilliseconds}.");
            outputDevice.Play();
            Debug.WriteLine("Bot voice audio playback started.");
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
            outputDevice.Stop();

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
