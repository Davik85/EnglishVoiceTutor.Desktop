using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly Action<string, string, string, string> saveSettings;
    private readonly Action navigateBack;

    public string Title => AppConstants.SettingsTitle;

    public string Subtitle => AppConstants.SettingsSubtitle;

    public string NativeLanguageTitle => AppConstants.NativeLanguageTitle;

    public string NativeLanguageSubtitle => AppConstants.NativeLanguageSubtitle;

    public string TutorAvatarTitle => AppConstants.TutorAvatarTitle;

    public string TutorAvatarSubtitle => AppConstants.TutorAvatarSubtitle;

    public string UserProfileTitle => AppConstants.UserProfileTitle;

    public string UserProfileSubtitle => AppConstants.UserProfileSubtitle;

    public string UserDisplayNameLabel => AppConstants.UserDisplayNameLabel;

    public string LearningGoalLabel => AppConstants.LearningGoalLabel;

    public IReadOnlyList<string> SupportedNativeLanguages { get; } = AppConstants.SupportedNativeLanguages;

    public IReadOnlyList<TutorAvatarOption> AvailableTutorAvatars { get; } = TutorAvatarOptions.All;

    public string SelectedTutorAvatarDescription => SelectedTutorAvatarOption?.ShortDescription ?? string.Empty;

    [ObservableProperty]
    private string selectedNativeLanguage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTutorAvatarDescription))]
    private TutorAvatarOption? selectedTutorAvatarOption;

    [ObservableProperty]
    private string userDisplayName = string.Empty;

    [ObservableProperty]
    private string learningGoal = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public SettingsViewModel(
        string currentNativeLanguage,
        string currentTutorAvatarId,
        string currentUserDisplayName,
        string currentLearningGoal,
        Action<string, string, string, string> saveSettings,
        Action navigateBack)
    {
        selectedNativeLanguage = currentNativeLanguage;
        selectedTutorAvatarOption = TutorAvatarOptions.GetById(currentTutorAvatarId);
        userDisplayName = currentUserDisplayName;
        learningGoal = currentLearningGoal;
        this.saveSettings = saveSettings;
        this.navigateBack = navigateBack;
    }

    [RelayCommand]
    private void Save()
    {
        var selectedAvatar = SelectedTutorAvatarOption ?? TutorAvatarOptions.Elena;
        saveSettings(SelectedNativeLanguage, selectedAvatar.Id, UserDisplayName, LearningGoal);
        StatusMessage = AppConstants.SettingsSavedMessage;
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }
}
