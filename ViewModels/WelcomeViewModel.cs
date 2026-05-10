using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Localization;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    private readonly Action navigateToLevelSelection;
    private readonly Action navigateToSettings;
    private readonly AppLocalizedText localizedText;

    public string AppTitle => AppConstants.ShortAppName;

    public string Subtitle => localizedText.WelcomeSubtitle;

    public string HowItWorksTitle => localizedText.WelcomeMvpHowItWorksTitle;

    public string ChooseTopicStep => localizedText.WelcomeMvpChooseTopicStep;

    public string PracticeStep => localizedText.WelcomeMvpPracticeStep;

    public string CorrectionsStep => localizedText.WelcomeMvpCorrectionsStep;

    public string StartLessonButtonText => localizedText.WelcomeStartLessonButton;

    public string SettingsButtonText => localizedText.WelcomeSettingsButton;

    public string FooterNote => localizedText.WelcomeFooterNote;

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
