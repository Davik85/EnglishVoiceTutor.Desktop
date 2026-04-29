using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    private readonly Action navigateToLevelSelection;
    private readonly Action navigateToSettings;

    public string AppTitle => AppConstants.ShortAppName;

    public string Subtitle => AppConstants.WelcomeSubtitle;

    public string FooterNote => AppConstants.MvpFooterNote;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public WelcomeViewModel(Action navigateToLevelSelection, Action navigateToSettings)
    {
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
