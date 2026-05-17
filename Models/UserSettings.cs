using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Shared.StudyLanguages;

namespace EnglishVoiceTutor.Desktop.Models;

public class UserSettings
{
    public string InterfaceLanguageId { get; set; } = InterfaceLanguageOptions.DetectFromCurrentCulture().Id;

    public string NativeLanguageName { get; set; } = AppConstants.NativeLanguageRussian;

    public string StudyLanguageId { get; set; } = StudyLanguageCatalog.DefaultStudyLanguageId;

    public string SelectedTutorAvatarId { get; set; } = TutorAvatarOptions.DefaultAvatarId;

    public string UserDisplayName { get; set; } = string.Empty;

    public string LearningGoal { get; set; } = string.Empty;

    public string BackendBaseUrl { get; set; } = BackendConstants.DefaultBackendBaseUrl;

    public string AudioInputDeviceId { get; set; } = AudioConstants.DefaultAudioInputDeviceId;
}
