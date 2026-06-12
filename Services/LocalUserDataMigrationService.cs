using System.Diagnostics;
using System.IO;
using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.Services;

public static class LocalUserDataMigrationService
{
    public static string GetCurrentRoamingFilePath(string fileName)
    {
        var roamingAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(roamingAppDataPath, StorageConstants.AppDataFolderName, fileName);
    }

    public static string[] BuildFilePathCandidates(string fileName, bool includeLocalCurrentPath = true)
    {
        var roamingAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var candidates = new List<string>
        {
            Path.Combine(roamingAppDataPath, StorageConstants.AppDataFolderName, fileName)
        };

        foreach (var appDataRoot in new[] { roamingAppDataPath, localAppDataPath })
        {
            foreach (var legacyFolderName in StorageConstants.LegacyAppDataFolderNames)
            {
                candidates.Add(Path.Combine(appDataRoot, legacyFolderName, fileName));
            }
        }

        if (includeLocalCurrentPath)
        {
            candidates.Add(Path.Combine(localAppDataPath, StorageConstants.AppDataFolderName, fileName));
        }

        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string? CopyFirstLegacyFileToCurrentWhenMissing(string currentFilePath, IEnumerable<string> filePathCandidates, string dataKind)
    {
        if (File.Exists(currentFilePath))
        {
            Debug.WriteLine($"Local user-data migration skipped for {dataKind}: current file already exists.");
            return null;
        }

        foreach (var legacyFilePath in filePathCandidates.Where(path => !StringComparer.OrdinalIgnoreCase.Equals(path, currentFilePath)))
        {
            try
            {
                if (!File.Exists(legacyFilePath))
                {
                    continue;
                }

                var directoryPath = Path.GetDirectoryName(currentFilePath);
                if (!string.IsNullOrWhiteSpace(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.Copy(legacyFilePath, currentFilePath, overwrite: false);
                Debug.WriteLine($"Local user-data migration copied {dataKind} from legacy path to current path.");
                return legacyFilePath;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"Local user-data migration could not copy {dataKind} from a legacy path: {exception.GetType().Name}.");
            }
        }

        return null;
    }
}
