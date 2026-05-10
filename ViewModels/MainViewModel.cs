using CommunityToolkit.Mvvm.ComponentModel;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly UserSettingsService userSettingsService = new();
    private readonly LessonChatBackendService lessonChatBackendService = new();
    private readonly AudioRecordingService audioRecordingService = new();
    private readonly AudioPlaybackService audioPlaybackService = new();
    private readonly LessonHistoryService lessonHistoryService = new();
    private readonly UserSettings userSettings;

    [ObservableProperty]
    private ViewModelBase currentViewModel;

    public MainViewModel()
    {
        audioRecordingService.CleanupOldRecordings();
        audioPlaybackService.CleanupOldBotVoiceFiles();
        userSettings = userSettingsService.Load();
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

    public void NavigateToSubtopics(string selectedLevel, Topic selectedTopic)
    {
        CurrentViewModel = CreateSubtopicsViewModel(selectedLevel, selectedTopic);
    }

    public void NavigateToLessonChat(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic)
    {
        CurrentViewModel = CreateLessonChatViewModel(selectedLevel, selectedTopic, selectedSubtopic);
    }

    public void NavigateToLessonSummary(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic, Feedback? latestFeedback)
    {
        SaveLessonHistory(selectedLevel, selectedTopic, selectedSubtopic, latestFeedback);
        CurrentViewModel = CreateLessonSummaryViewModel(selectedLevel, selectedTopic, selectedSubtopic, latestFeedback);
    }

    public void NavigateToLessonHistory(string selectedLevel)
    {
        CurrentViewModel = CreateLessonHistoryViewModel(selectedLevel);
    }

    private void NavigateToSettings(Action navigateBack)
    {
        var lessonHistory = lessonHistoryService.Load();

        CurrentViewModel = new SettingsViewModel(
            userSettings.NativeLanguageName,
            userSettings.SelectedTutorAvatarId,
            userSettings.UserDisplayName,
            userSettings.LearningGoal,
            lessonHistory,
            SaveSettings,
            navigateBack);
    }

    private void SaveSettings(string nativeLanguage, string tutorAvatarId, string userDisplayName, string learningGoal)
    {
        userSettings.NativeLanguageName = nativeLanguage;
        userSettings.SelectedTutorAvatarId = TutorAvatarOptions.GetById(tutorAvatarId).Id;
        userSettings.UserDisplayName = userDisplayName;
        userSettings.LearningGoal = learningGoal;
        userSettingsService.Save(userSettings);
    }

    private void SaveLessonHistory(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic, Feedback? latestFeedback)
    {
        var item = new LessonHistoryItem
        {
            Id = Guid.NewGuid(),
            CompletedAt = DateTime.Now,
            SelectedLevel = selectedLevel,
            TopicTitle = selectedTopic.Title,
            SubtopicTitle = selectedSubtopic.Title,
            GoodText = LessonSummaryViewModel.BuildGoodText(latestFeedback),
            ImproveText = LessonSummaryViewModel.BuildImproveText(latestFeedback),
            UsefulPhrases = LessonSummaryViewModel.BuildUsefulPhrases(latestFeedback)
        };

        lessonHistoryService.Add(item);
    }

    private WelcomeViewModel CreateWelcomeViewModel()
    {
        return new WelcomeViewModel(NavigateToLevelSelection, () => NavigateToSettings(NavigateToWelcome));
    }

    private LevelSelectionViewModel CreateLevelSelectionViewModel()
    {
        return new LevelSelectionViewModel(NavigateToWelcome, NavigateToHome);
    }

    private HomeViewModel CreateHomeViewModel(string selectedLevel)
    {
        return new HomeViewModel(
            selectedLevel,
            NavigateToLevelSelection,
            topic => NavigateToSubtopics(selectedLevel, topic),
            () => NavigateToLessonHistory(selectedLevel),
            () => NavigateToSettings(() => NavigateToHome(selectedLevel)));
    }

    private SubtopicsViewModel CreateSubtopicsViewModel(string selectedLevel, Topic selectedTopic)
    {
        return new SubtopicsViewModel(
            selectedLevel,
            selectedTopic,
            () => NavigateToHome(selectedLevel),
            subtopic => NavigateToLessonChat(selectedLevel, selectedTopic, subtopic));
    }

    private LessonChatViewModel CreateLessonChatViewModel(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic)
    {
        return new LessonChatViewModel(
            selectedLevel,
            selectedTopic,
            selectedSubtopic,
            userSettings.NativeLanguageName,
            userSettings.UserDisplayName,
            userSettings.LearningGoal,
            TutorAvatarOptions.GetById(userSettings.SelectedTutorAvatarId),
            lessonChatBackendService,
            audioRecordingService,
            audioPlaybackService,
            () => NavigateToSubtopics(selectedLevel, selectedTopic),
            latestFeedback => NavigateToLessonSummary(selectedLevel, selectedTopic, selectedSubtopic, latestFeedback));
    }

    private LessonSummaryViewModel CreateLessonSummaryViewModel(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic, Feedback? latestFeedback)
    {
        return new LessonSummaryViewModel(
            selectedLevel,
            selectedTopic,
            selectedSubtopic,
            latestFeedback,
            () => NavigateToSubtopics(selectedLevel, selectedTopic),
            () => NavigateToHome(selectedLevel));
    }

    private LessonHistoryViewModel CreateLessonHistoryViewModel(string selectedLevel)
    {
        return new LessonHistoryViewModel(
            lessonHistoryService,
            () => NavigateToHome(selectedLevel));
    }
}
