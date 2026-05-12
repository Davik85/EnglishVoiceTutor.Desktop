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

        try
        {
            return await SaveTemporaryAudioFileAsync(audioBytes, NormalizeAudioFileExtension(fileExtension), cancellationToken);
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

        try
        {
            await PlayTemporaryAudioFileAsync(filePath, cancellationToken);
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
            outputDevice.Init(audioReader);
            outputDevice.Play();
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
