using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Localization;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class LevelSelectionViewModel : ViewModelBase
{
    private readonly Action navigateBack;
    private readonly Action<string> navigateToHome;
    private readonly AppLocalizedText localizedText;

    public string Title => localizedText.LevelSelectionTitle;

    public string Subtitle => localizedText.LevelSelectionSubtitle;

    public string ContinueButtonText => localizedText.ContinueButtonText;

    public string BackButtonText => localizedText.BackButtonText;

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

    public LevelSelectionViewModel(AppLocalizedText localizedText, Action navigateBack, Action<string> navigateToHome)
    {
        this.localizedText = localizedText;
        this.navigateBack = navigateBack;
        this.navigateToHome = navigateToHome;
    }

    [RelayCommand]
    private void SelectLevel(string level)
    {
        SelectedLevel = level;
        StatusMessage = $"{localizedText.SelectedLevelPrefix} {level}";
    }

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue()
    {
        if (!CanContinue())
        {
            StatusMessage = localizedText.LevelSelectionRequiredMessage;
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
