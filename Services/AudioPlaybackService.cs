using System.Globalization;
using System.IO;
using EnglishVoiceTutor.Desktop.Constants;
using NAudio.Wave;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class AudioPlaybackService
{
    public async Task PlayAudioAsync(byte[] audioBytes, CancellationToken cancellationToken = default)
    {
        if (audioBytes.Length == 0)
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidSpeechResponseMessage);
        }

        var filePath = await SaveTemporaryAudioFileAsync(audioBytes, cancellationToken);

        try
        {
            await PlayTemporaryAudioFileAsync(filePath, cancellationToken);
        }
        finally
        {
            SafeDeleteFile(filePath);
        }
    }

    public void CleanupOldBotVoiceFiles()
    {
        var botVoiceFolderPath = GetBotVoiceFolderPath();
        var oldestAllowedWriteTimeUtc = DateTime.UtcNow.AddHours(-AudioConstants.TemporaryRecordingMaxAgeHours);

        try
        {
            if (!Directory.Exists(botVoiceFolderPath))
            {
                return;
            }

            foreach (var filePath in Directory.EnumerateFiles(botVoiceFolderPath, AudioConstants.Mp3SearchPattern))
            {
                if (IsTemporaryAudioFileOld(filePath, oldestAllowedWriteTimeUtc))
                {
                    SafeDeleteFile(filePath);
                }
            }
        }
        catch
        {
            // Ignore cleanup errors because temporary voice cleanup must not block app startup.
        }
    }

    private static async Task<string> SaveTemporaryAudioFileAsync(byte[] audioBytes, CancellationToken cancellationToken)
    {
        var botVoiceFolderPath = GetBotVoiceFolderPath();
        Directory.CreateDirectory(botVoiceFolderPath);

        var timestamp = DateTime.Now.ToString(AudioConstants.RecordingTimestampFormat, CultureInfo.InvariantCulture);
        var fileName = $"{AudioConstants.BotVoiceFilePrefix}{timestamp}-{Guid.NewGuid():N}{AudioConstants.Mp3FileExtension}";
        var filePath = Path.Combine(botVoiceFolderPath, fileName);

        await File.WriteAllBytesAsync(filePath, audioBytes, cancellationToken);

        return filePath;
    }

    private static async Task PlayTemporaryAudioFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var playbackCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var audioReader = new AudioFileReader(filePath);
        using var outputDevice = new WaveOutEvent();

        outputDevice.PlaybackStopped += OnPlaybackStopped;

        try
        {
            outputDevice.Init(audioReader);
            outputDevice.Play();
            using var cancellationRegistration = cancellationToken.Register(() => playbackCompletion.TrySetCanceled(cancellationToken));
            await playbackCompletion.Task;
        }
        finally
        {
            outputDevice.PlaybackStopped -= OnPlaybackStopped;
            outputDevice.Stop();
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
        return Path.Combine(Path.GetTempPath(), AudioConstants.AppTempFolderName, AudioConstants.BotVoiceFolderName);
    }

    private static bool IsTemporaryAudioFileOld(string filePath, DateTime oldestAllowedWriteTimeUtc)
    {
        try
        {
            return File.GetLastWriteTimeUtc(filePath) < oldestAllowedWriteTimeUtc;
        }
        catch
        {
            return false;
        }
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
            // Ignore cleanup errors because generated speech files are temporary.
        }
    }
}
