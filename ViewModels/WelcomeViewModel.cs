using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Localization;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    private readonly Action navigateToLevelSelection;
    private readonly Action navigateToSettings;
    private readonly AppLocalizedText localizedText;

    public string AppTitle => localizedText.WelcomeTitle;

    public string Subtitle => localizedText.WelcomeSubtitle;

    public string StartLessonButtonText => localizedText.WelcomeStartLessonButton;

    public string SettingsButtonText => localizedText.WelcomeSettingsButton;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public WelcomeViewModel(AppLocalizedText localizedText, Action navigateToLevelSelection, Action navigateToSettings)
    {
        this.localizedText = localizedText;
        this.navigateToLevelSelection = navigateToLevelSelection;
        this.navigateToSettings = navigateToSettings;
    }

    [RelayCommand]
    private void StartLesson()
    {
        navigateToLevelSelection();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        navigateToSettings();
    }
}
