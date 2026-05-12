using System.Diagnostics;
using System.IO;
using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class BotVoiceTempFileCleanupService
{
    public void CleanupOldBotVoiceFiles()
    {
        var botVoiceFolderPath = GetBotVoiceTempFolderPath();
        var oldestAllowedWriteTimeUtc = DateTime.UtcNow.AddHours(-AudioConstants.BotVoiceCleanupRetentionHours);

        try
        {
            if (!Directory.Exists(botVoiceFolderPath))
            {
                return;
            }

            foreach (var filePath in Directory.EnumerateFiles(botVoiceFolderPath, AudioConstants.BotVoiceFileSearchPattern))
            {
                if (IsBotVoiceFileOld(filePath, oldestAllowedWriteTimeUtc))
                {
                    TryDeleteFile(filePath);
                }
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"{AudioConstants.BotVoiceCleanupErrorMessage} {exception}");
        }
    }

    public void CleanupFiles(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            TryDeleteFile(filePath);
        }
    }

    public bool TryDeleteFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !IsSafeBotVoiceFilePath(filePath))
        {
            return false;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                return true;
            }

            File.Delete(filePath);
            return true;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"{AudioConstants.BotVoiceCleanupErrorMessage} {filePath}: {exception}");
            return false;
        }
    }

    public string GetBotVoiceTempFolderPath()
    {
        return Path.Combine(Path.GetTempPath(), AudioConstants.AppTempFolderName, AudioConstants.BotVoiceTempFolderName);
    }

    private static bool IsBotVoiceFileOld(string filePath, DateTime oldestAllowedWriteTimeUtc)
    {
        try
        {
            return File.GetLastWriteTimeUtc(filePath) < oldestAllowedWriteTimeUtc;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"{AudioConstants.BotVoiceCleanupErrorMessage} {filePath}: {exception}");
            return false;
        }
    }

    private bool IsSafeBotVoiceFilePath(string filePath)
    {
        try
        {
            var fullFilePath = Path.GetFullPath(filePath);
            var botVoiceFolderPath = Path.GetFullPath(GetBotVoiceTempFolderPath());
            var botVoiceFolderPrefix = botVoiceFolderPath.EndsWith(Path.DirectorySeparatorChar)
                ? botVoiceFolderPath
                : botVoiceFolderPath + Path.DirectorySeparatorChar;
            var fileName = Path.GetFileName(fullFilePath);

            return fullFilePath.StartsWith(botVoiceFolderPrefix, StringComparison.OrdinalIgnoreCase)
                && fileName.StartsWith(AudioConstants.BotVoiceTempFilePrefix, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"{AudioConstants.BotVoiceCleanupErrorMessage} {filePath}: {exception}");
            return false;
        }
    }
}
