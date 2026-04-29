using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class LevelSelectionViewModel : ViewModelBase
{
    private readonly Action navigateBack;
    private readonly Action<string> navigateToHome;

    public string Title => AppConstants.LevelSelectionTitle;

    public string Subtitle => AppConstants.LevelSelectionSubtitle;

    public IReadOnlyList<string> Levels { get; } =
    [
        "A1 Beginner",
        "A2 Elementary",
        "B1 Intermediate",
        "B2 Upper-Intermediate"
    ];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private string? selectedLevel;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public LevelSelectionViewModel(Action navigateBack, Action<string> navigateToHome)
    {
        this.navigateBack = navigateBack;
        this.navigateToHome = navigateToHome;
    }

    [RelayCommand]
    private void SelectLevel(string level)
    {
        SelectedLevel = level;
        StatusMessage = $"{AppConstants.SelectedLevelPrefix} {level}";
    }

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue()
    {
        if (!CanContinue())
        {
            StatusMessage = "Please select a level before continuing.";
            return;
        }

        navigateToHome(SelectedLevel!);
    }

    private bool CanContinue()
    {
        return !string.IsNullOrWhiteSpace(SelectedLevel);
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }
}
