using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class LevelSelectionViewModel : ViewModelBase
{
    private readonly Action navigateBack;

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
    private string? selectedLevel;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public LevelSelectionViewModel(Action navigateBack)
    {
        this.navigateBack = navigateBack;
    }

    [RelayCommand]
    private void SelectLevel(string level)
    {
        SelectedLevel = level;
        StatusMessage = $"{AppConstants.SelectedLevelPrefix} {level}";
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }
}
