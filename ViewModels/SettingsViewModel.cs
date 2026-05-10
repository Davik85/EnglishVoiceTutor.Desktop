using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly Action<string, string, string, string> saveSettings;
    private readonly Action navigateBack;

    public string Title => AppConstants.SettingsTitle;

    public string Subtitle => AppConstants.SettingsSubtitle;

    public string NativeLanguageTitle => AppConstants.NativeLanguageTitle;

    public string NativeLanguageSubtitle => AppConstants.NativeLanguageSubtitle;

    public string TutorAvatarTitle => AppConstants.TutorAvatarTitle;

    public string TutorAvatarSubtitle => AppConstants.TutorAvatarSubtitle;

    public string UserProfileTitle => AppConstants.UserProfileTitle;

    public string UserProfileSubtitle => AppConstants.UserProfileSubtitle;

    public string UserDisplayNameLabel => AppConstants.UserDisplayNameLabel;

    public string LearningGoalLabel => AppConstants.LearningGoalLabel;

    public string LearningStatisticsTitle => AppConstants.LearningStatisticsTitle;

    public string LearningStatisticsSubtitle => AppConstants.LearningStatisticsSubtitle;

    public string TotalCompletedLessonsLabel => AppConstants.TotalCompletedLessonsLabel;

    public string LessonsTodayLabel => AppConstants.LessonsTodayLabel;

    public string CurrentStreakLabel => AppConstants.CurrentStreakLabel;

    public string LastCompletedLessonLabel => AppConstants.LastCompletedLessonLabel;

    public string TotalCompletedLessonsText { get; }

    public string LessonsTodayText { get; }

    public string CurrentStreakText { get; }

    public string LastCompletedLessonText { get; }

    public IReadOnlyList<string> SupportedNativeLanguages { get; } = AppConstants.SupportedNativeLanguages;

    public IReadOnlyList<TutorAvatarOption> AvailableTutorAvatars { get; } = TutorAvatarOptions.All;

    public string SelectedTutorAvatarDescription => SelectedTutorAvatarOption?.ShortDescription ?? string.Empty;

    [ObservableProperty]
    private string selectedNativeLanguage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTutorAvatarDescription))]
    private TutorAvatarOption? selectedTutorAvatarOption;

    [ObservableProperty]
    private string userDisplayName = string.Empty;

    [ObservableProperty]
    private string learningGoal = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public SettingsViewModel(
        string currentNativeLanguage,
        string currentTutorAvatarId,
        string currentUserDisplayName,
        string currentLearningGoal,
        IReadOnlyList<LessonHistoryItem> lessonHistory,
        Action<string, string, string, string> saveSettings,
        Action navigateBack)
    {
        selectedNativeLanguage = currentNativeLanguage;
        selectedTutorAvatarOption = TutorAvatarOptions.GetById(currentTutorAvatarId);
        userDisplayName = currentUserDisplayName;
        learningGoal = currentLearningGoal;
        this.saveSettings = saveSettings;
        this.navigateBack = navigateBack;

        TotalCompletedLessonsText = lessonHistory.Count.ToString();
        LessonsTodayText = CountLessonsToday(lessonHistory).ToString();
        CurrentStreakText = CalculateCurrentStreak(lessonHistory).ToString();
        LastCompletedLessonText = BuildLastCompletedLessonText(lessonHistory);
    }

    [RelayCommand]
    private void Save()
    {
        var selectedAvatar = SelectedTutorAvatarOption ?? TutorAvatarOptions.Elena;
        saveSettings(SelectedNativeLanguage, selectedAvatar.Id, UserDisplayName, LearningGoal);
        StatusMessage = AppConstants.SettingsSavedMessage;
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }

    private static int CountLessonsToday(IReadOnlyList<LessonHistoryItem> lessonHistory)
    {
        var today = DateTime.Today;
        return lessonHistory.Count(item => item.CompletedAt.Date == today);
    }

    private static int CalculateCurrentStreak(IReadOnlyList<LessonHistoryItem> lessonHistory)
    {
        var completedDates = lessonHistory
            .Select(item => item.CompletedAt.Date)
            .Distinct()
            .ToHashSet();
        var today = DateTime.Today;

        if (!completedDates.Contains(today))
        {
            return 0;
        }

        var streak = 0;
        for (var date = today; completedDates.Contains(date); date = date.AddDays(-1))
        {
            streak++;
        }

        return streak;
    }

    private static string BuildLastCompletedLessonText(IReadOnlyList<LessonHistoryItem> lessonHistory)
    {
        var latestLesson = lessonHistory
            .OrderByDescending(item => item.CompletedAt)
            .FirstOrDefault();

        if (latestLesson is null)
        {
            return AppConstants.NoCompletedLessonsStatisticsText;
        }

        var lessonTitle = string.Join(" — ", new[] { latestLesson.TopicTitle, latestLesson.SubtopicTitle }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var completedAtText = latestLesson.CompletedAt.ToString("g");

        return string.IsNullOrWhiteSpace(lessonTitle)
            ? completedAtText
            : $"{completedAtText} · {lessonTitle}";
    }
}
