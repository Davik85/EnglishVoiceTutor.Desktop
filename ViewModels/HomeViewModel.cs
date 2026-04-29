using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly Action navigateBack;

    public string SelectedLevel { get; }

    public string Title => AppConstants.HomeTitle;

    public string Subtitle => AppConstants.HomeSubtitle;

    public string DailyLimitText => AppConstants.DailyLimitText;

    public IReadOnlyList<Topic> Topics { get; } =
    [
        new(1, "Everyday English", "Small talk, introductions, and daily situations."),
        new(2, "Travel", "Airports, hotels, directions, and transport."),
        new(3, "Work & Business", "Meetings, emails, calls, and workplace conversations."),
        new(4, "Job Interview", "Practice common interview questions and answers."),
        new(5, "Restaurant & Cafe", "Ordering food, booking tables, and polite requests.")
    ];

    [ObservableProperty]
    private Topic? selectedTopic;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public HomeViewModel(string selectedLevel, Action navigateBack)
    {
        SelectedLevel = selectedLevel;
        this.navigateBack = navigateBack;
    }

    [RelayCommand]
    private void SelectTopic(Topic topic)
    {
        SelectedTopic = topic;
        StatusMessage = $"Selected topic: {topic.Title}";
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }

    [RelayCommand]
    private void History()
    {
        StatusMessage = AppConstants.HistoryPlaceholderMessage;
    }

    [RelayCommand]
    private void Settings()
    {
        StatusMessage = AppConstants.SettingsPlaceholderMessage;
    }
}
