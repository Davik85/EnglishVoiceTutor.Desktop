using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    private readonly Action navigateToLevelSelection;

    public string AppTitle => AppConstants.ShortAppName;

    public string Subtitle => AppConstants.WelcomeSubtitle;

    public string FooterNote => AppConstants.MvpFooterNote;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public WelcomeViewModel(Action navigateToLevelSelection)
    {
        this.navigateToLevelSelection = navigateToLevelSelection;
    }

    [RelayCommand]
    private void StartLesson()
    {
        navigateToLevelSelection();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        StatusMessage = AppConstants.SettingsPlaceholderMessage;
    }
}
