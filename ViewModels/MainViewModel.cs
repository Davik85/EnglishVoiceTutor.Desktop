using CommunityToolkit.Mvvm.ComponentModel;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase currentViewModel;

    public MainViewModel()
    {
        currentViewModel = CreateWelcomeViewModel();
    }

    public void NavigateToWelcome()
    {
        CurrentViewModel = CreateWelcomeViewModel();
    }

    public void NavigateToLevelSelection()
    {
        CurrentViewModel = CreateLevelSelectionViewModel();
    }

    public void NavigateToHome(string selectedLevel)
    {
        CurrentViewModel = CreateHomeViewModel(selectedLevel);
    }

    private WelcomeViewModel CreateWelcomeViewModel()
    {
        return new WelcomeViewModel(NavigateToLevelSelection);
    }

    private LevelSelectionViewModel CreateLevelSelectionViewModel()
    {
        return new LevelSelectionViewModel(NavigateToWelcome, NavigateToHome);
    }

    private HomeViewModel CreateHomeViewModel(string selectedLevel)
    {
        return new HomeViewModel(selectedLevel, NavigateToLevelSelection);
    }
}
