using System.Text.Json.Serialization;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Shared.NativeLanguages;
using EnglishVoiceTutor.Shared.StudyLanguages;

namespace EnglishVoiceTutor.Desktop.Models;

public class UserSettings
{
    public string InterfaceLanguageId { get; set; } = InterfaceLanguageOptions.DetectFromCurrentCulture().Id;

    public string NativeLanguageName { get; set; } = NativeLanguageCatalog.DefaultLanguageId;

    public string StudyLanguageId { get; set; } = StudyLanguageCatalog.DefaultStudyLanguageId;

    public string SelectedTutorAvatarId { get; set; } = TutorAvatarOptions.DefaultAvatarId;

    public string SpeechVoiceId { get; set; } = SpeechVoiceOptions.GetPreferredVoiceIdForTutor(TutorAvatarOptions.DefaultAvatarId);

    public string UserDisplayName { get; set; } = string.Empty;

    public string LearningGoal { get; set; } = string.Empty;

#if !DEBUG
    [JsonIgnore]
#endif
    public string BackendBaseUrl { get; set; } = BackendConstants.DefaultBackendBaseUrl;

    public string AudioInputDeviceId { get; set; } = AudioConstants.DefaultAudioInputDeviceId;
}
