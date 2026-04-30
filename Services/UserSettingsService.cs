using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Services;

public class UserSettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string settingsFilePath;

    public UserSettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appSettingsDirectory = Path.Combine(appDataPath, StorageConstants.AppDataFolderName);
        settingsFilePath = Path.Combine(appSettingsDirectory, StorageConstants.SettingsFileName);
    }

    public string SettingsFilePath => settingsFilePath;

    public UserSettings Load()
    {
        try
        {
            if (!File.Exists(settingsFilePath))
            {
                return CreateDefaultSettings();
            }

            var json = File.ReadAllText(settingsFilePath);
            var settings = JsonSerializer.Deserialize<UserSettings>(json);

            if (settings is null || string.IsNullOrWhiteSpace(settings.NativeLanguageName))
            {
                return CreateDefaultSettings();
            }

            return settings;
        }
        catch
        {
            return CreateDefaultSettings();
        }
    }

    public void Save(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.NativeLanguageName))
        {
            settings.NativeLanguageName = AppConstants.NativeLanguageRussian;
        }

        var directoryPath = Path.GetDirectoryName(settingsFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(settingsFilePath, json);
    }

    private static UserSettings CreateDefaultSettings()
    {
        return new UserSettings
        {
            NativeLanguageName = AppConstants.NativeLanguageRussian
        };
    }
}
