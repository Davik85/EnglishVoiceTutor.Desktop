using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.Models;

public class UserSettings
{
    public string NativeLanguageName { get; set; } = AppConstants.NativeLanguageRussian;

    public string SelectedTutorAvatarId { get; set; } = TutorAvatarOptions.DefaultAvatarId;
}
