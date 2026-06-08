using System.IO;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Shared.NativeLanguages;
using EnglishVoiceTutor.Shared.StudyLanguages;

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
            InterfaceLanguageId = InterfaceLanguageOptions.DetectFromCurrentCulture().Id,
            NativeLanguageName = NativeLanguageCatalog.DefaultLanguageId,
            StudyLanguageId = StudyLanguageCatalog.DefaultStudyLanguageId,
            SelectedTutorAvatarId = TutorAvatarOptions.DefaultAvatarId,
            SpeechVoiceId = SpeechVoiceOptions.GetPreferredVoiceIdForTutor(TutorAvatarOptions.DefaultAvatarId),
            UserDisplayName = string.Empty,
            LearningGoal = string.Empty,
            BackendBaseUrl = BackendConstants.DefaultBackendBaseUrl,
            AudioInputDeviceId = AudioConstants.DefaultAudioInputDeviceId
        };
    }

    private static void Normalize(UserSettings settings)
    {
        settings.InterfaceLanguageId = InterfaceLanguageOptions.GetById(settings.InterfaceLanguageId).Id;

        settings.NativeLanguageName = NativeLanguageCatalog.GetByIdOrName(settings.NativeLanguageName).Id;

        settings.StudyLanguageId = StudyLanguageCatalog.GetById(settings.StudyLanguageId).Id;
        settings.SelectedTutorAvatarId = TutorAvatarOptions.GetById(settings.SelectedTutorAvatarId).Id;
        settings.SpeechVoiceId = string.IsNullOrWhiteSpace(settings.SpeechVoiceId)
            ? SpeechVoiceOptions.GetPreferredVoiceIdForTutor(settings.SelectedTutorAvatarId)
            : SpeechVoiceOptions.GetById(settings.SpeechVoiceId).Id;
        settings.UserDisplayName = NormalizeOptionalText(settings.UserDisplayName);
        settings.LearningGoal = NormalizeOptionalText(settings.LearningGoal);
        settings.BackendBaseUrl = BackendEndpointBuilder.ResolveSavedBaseUrlForCurrentBuild(settings.BackendBaseUrl);
        settings.AudioInputDeviceId = string.IsNullOrWhiteSpace(settings.AudioInputDeviceId)
            ? AudioConstants.DefaultAudioInputDeviceId
            : settings.AudioInputDeviceId.Trim();
    }

    private static string NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }
}
