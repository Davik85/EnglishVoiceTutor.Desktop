using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Localization;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly Action navigateBack;
    private readonly Action<Topic> navigateToSubtopics;
    private readonly Action navigateToHistory;
    private readonly Action navigateToSettings;
    private readonly AppLocalizedText localizedText;

    public string SelectedLevel { get; }

    public string Title => localizedText.HomeTitle;

    public string Subtitle => localizedText.HomeSubtitle;

    public string CurrentLevelText => $"{localizedText.CurrentLevelLabel} {SelectedLevel}";

    public string DailyLimitText => localizedText.DailyLimitText;

    public string BackButtonText => localizedText.BackButtonText;

    public string HistoryButtonText => localizedText.LessonHistoryButtonText;

    public string SettingsButtonText => localizedText.SettingsButtonText;

    public IReadOnlyList<Topic> Topics { get; }

    [ObservableProperty]
    private Topic? selectedTopic;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public HomeViewModel(
        AppLocalizedText localizedText,
        string selectedLevel,
        Action navigateBack,
        Action<Topic> navigateToSubtopics,
        Action navigateToHistory,
        Action navigateToSettings)
    {
        this.localizedText = localizedText;
        SelectedLevel = selectedLevel;
        this.navigateBack = navigateBack;
        this.navigateToSubtopics = navigateToSubtopics;
        this.navigateToHistory = navigateToHistory;
        this.navigateToSettings = navigateToSettings;
        Topics = CreateTopics(localizedText.LanguageId);
    }

    [RelayCommand]
    private void SelectTopic(Topic topic)
    {
        SelectedTopic = topic;
        navigateToSubtopics(topic);
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }

    [RelayCommand]
    private void History()
    {
        navigateToHistory();
    }

    [RelayCommand]
    private void Settings()
    {
        navigateToSettings();
    }

    public static IReadOnlyList<Topic> CreateTopics(string interfaceLanguageId)
    {
        var canonicalTopics = new[]
        {
            new Topic(1, "Everyday English", "Small talk, introductions, and daily situations."),
            new Topic(2, "Travel", "Airports, hotels, directions, and transport."),
            new Topic(3, "Work & Business", "Meetings, emails, calls, and workplace conversations."),
            new Topic(4, "Job Interview", "Practice common interview questions and answers."),
            new Topic(5, "Restaurant & Cafe", "Ordering food, booking tables, and polite requests."),
            new Topic(6, "Free Conversation", "Open English conversation with safe, respectful boundaries.")
        };

        return canonicalTopics
            .Select(topic =>
            {
                var displayText = AppLocalization.GetTopicDisplayText(interfaceLanguageId, topic.Title, topic.Description);
                return topic with { DisplayTitle = displayText.Title, DisplayDescription = displayText.Description };
            })
            .ToArray();
    }
}
