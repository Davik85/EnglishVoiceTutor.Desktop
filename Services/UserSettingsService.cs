using System.IO;
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

            if (settings is null)
            {
                return CreateDefaultSettings();
            }

            Normalize(settings);

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

        Normalize(settings);

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
            NativeLanguageName = AppConstants.NativeLanguageRussian,
            SelectedTutorAvatarId = TutorAvatarOptions.DefaultAvatarId
        };
    }

    private static void Normalize(UserSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.NativeLanguageName))
        {
            settings.NativeLanguageName = AppConstants.NativeLanguageRussian;
        }

        settings.SelectedTutorAvatarId = TutorAvatarOptions.GetById(settings.SelectedTutorAvatarId).Id;
    }
}
