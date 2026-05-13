using CommunityToolkit.Mvvm.ComponentModel;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models.LessonContent;
using EnglishVoiceTutor.Desktop.Localization;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly UserSettingsService userSettingsService = new();
    private readonly LessonChatBackendService lessonChatBackendService = new();
    private readonly AudioRecordingService audioRecordingService = new();
    private readonly AudioInputDeviceService audioInputDeviceService = new();
    private readonly AudioPlaybackService audioPlaybackService = new();
    private readonly BotVoiceTempFileCleanupService botVoiceTempFileCleanupService = new();
    private readonly LessonHistoryService lessonHistoryService = new();
    private readonly LessonContentService lessonContentService = new();
    private readonly UserSettings userSettings;

    [ObservableProperty]
    private ViewModelBase currentViewModel;

    public MainViewModel()
    {
        audioRecordingService.CleanupOldRecordings();
        botVoiceTempFileCleanupService.CleanupOldBotVoiceFiles();
        userSettings = userSettingsService.Load();
        lessonChatBackendService.SetBackendBaseUrl(userSettings.BackendBaseUrl);
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
            userSettings.InterfaceLanguageId,
            userSettings.NativeLanguageName,
            userSettings.SelectedTutorAvatarId,
            userSettings.UserDisplayName,
            userSettings.LearningGoal,
            userSettings.BackendBaseUrl,
            userSettings.AudioInputDeviceId,
            userSettingsService.SettingsFilePath,
            lessonHistoryService.LessonHistoryFilePath,
            lessonHistory,
            lessonChatBackendService,
            audioInputDeviceService,
            audioRecordingService,
            SaveSettings,
            navigateBack);
    }

    private void SaveSettings(string interfaceLanguageId, string nativeLanguage, string tutorAvatarId, string userDisplayName, string learningGoal, string backendBaseUrl, string audioInputDeviceId)
    {
        userSettings.InterfaceLanguageId = InterfaceLanguageOptions.GetById(interfaceLanguageId).Id;
        userSettings.NativeLanguageName = nativeLanguage;
        userSettings.SelectedTutorAvatarId = TutorAvatarOptions.GetById(tutorAvatarId).Id;
        userSettings.UserDisplayName = userDisplayName;
        userSettings.LearningGoal = learningGoal;
        userSettings.BackendBaseUrl = backendBaseUrl;
        userSettings.AudioInputDeviceId = audioInputDeviceId;
        userSettingsService.Save(userSettings);
        lessonChatBackendService.SetBackendBaseUrl(userSettings.BackendBaseUrl);
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
            GoodText = LessonSummaryViewModel.BuildGoodText(latestFeedback, AppLocalization.GetText(userSettings.InterfaceLanguageId)),
            ImproveText = LessonSummaryViewModel.BuildImproveText(latestFeedback, AppLocalization.GetText(userSettings.InterfaceLanguageId)),
            UsefulPhrases = LessonSummaryViewModel.BuildUsefulPhrases(latestFeedback, AppLocalization.GetText(userSettings.InterfaceLanguageId))
        };

        lessonHistoryService.Add(item);
    }

    private WelcomeViewModel CreateWelcomeViewModel()
    {
        return new WelcomeViewModel(AppLocalization.GetText(userSettings.InterfaceLanguageId), NavigateToLevelSelection, () => NavigateToSettings(NavigateToWelcome));
    }

    private LevelSelectionViewModel CreateLevelSelectionViewModel()
    {
        return new LevelSelectionViewModel(AppLocalization.GetText(userSettings.InterfaceLanguageId), NavigateToWelcome, NavigateToHome);
    }

    private HomeViewModel CreateHomeViewModel(string selectedLevel)
    {
        return new HomeViewModel(
            AppLocalization.GetText(userSettings.InterfaceLanguageId),
            selectedLevel,
            NavigateToLevelSelection,
            topic => NavigateToSubtopics(selectedLevel, topic),
            () => NavigateToLessonHistory(selectedLevel),
            () => NavigateToSettings(() => NavigateToHome(selectedLevel)));
    }

    private SubtopicsViewModel CreateSubtopicsViewModel(string selectedLevel, Topic selectedTopic)
    {
        return new SubtopicsViewModel(
            AppLocalization.GetText(userSettings.InterfaceLanguageId),
            selectedLevel,
            selectedTopic,
            () => NavigateToHome(selectedLevel),
            subtopic => NavigateToLessonChat(selectedLevel, selectedTopic, subtopic));
    }

    private LessonChatViewModel CreateLessonChatViewModel(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic)
    {
        return new LessonChatViewModel(
            AppLocalization.GetText(userSettings.InterfaceLanguageId),
            selectedLevel,
            selectedTopic,
            selectedSubtopic,
            userSettings.NativeLanguageName,
            userSettings.UserDisplayName,
            userSettings.LearningGoal,
            TutorAvatarOptions.GetById(userSettings.SelectedTutorAvatarId),
            LoadLessonScenarioForSubtopic(selectedTopic, selectedSubtopic),
            lessonChatBackendService,
            audioRecordingService,
            audioPlaybackService,
            botVoiceTempFileCleanupService,
            userSettings.AudioInputDeviceId,
            () => NavigateToSubtopics(selectedLevel, selectedTopic),
            latestFeedback => NavigateToLessonSummary(selectedLevel, selectedTopic, selectedSubtopic, latestFeedback));
    }


    private LessonScenario LoadLessonScenarioForSubtopic(Topic selectedTopic, Subtopic selectedSubtopic)
    {
        // These JSON files are shared scenario templates with levelProfiles.
        // The selected level chooses the active level profile during lesson chat.
        return lessonContentService.LoadSharedLessonScenario(
            GetTopicFolderName(selectedTopic),
            GetLessonFileName(selectedTopic, selectedSubtopic));
    }

    private static string GetLessonFileName(Topic selectedTopic, Subtopic selectedSubtopic)
    {
        return (selectedTopic.Title, selectedSubtopic.Title) switch
        {
            ("Everyday English", "Introductions") => ContentConstants.IntroductionsFileName,
            ("Everyday English", "Small talk with a neighbor") => ContentConstants.SmallTalkWithANeighborFileName,
            ("Everyday English", "Asking for help") => ContentConstants.AskingForHelpFileName,
            ("Everyday English", "Making plans") => ContentConstants.MakingPlansFileName,
            ("Everyday English", "Talking about your day") => ContentConstants.TalkingAboutYourDayFileName,
            ("Travel", "Airport check-in") => ContentConstants.AirportCheckInFileName,
            ("Travel", "Hotel check-in") => ContentConstants.HotelCheckInFileName,
            ("Travel", "Asking for directions") => ContentConstants.AskingForDirectionsFileName,
            ("Travel", "Ordering transport") => ContentConstants.OrderingTransportFileName,
            ("Travel", "Lost luggage") => ContentConstants.LostLuggageFileName,
            _ => throw new InvalidOperationException(
                $"No lesson scenario file is mapped for topic '{selectedTopic.Title}' and subtopic '{selectedSubtopic.Title}'.")
        };
    }

    private static string GetTopicFolderName(Topic selectedTopic)
    {
        return selectedTopic.Title switch
        {
            "Everyday English" => ContentConstants.EverydayEnglishFolderName,
            "Travel" => ContentConstants.TravelFolderName,
            _ => throw new InvalidOperationException($"No lesson content folder is mapped for topic '{selectedTopic.Title}'.")
        };
    }

    private LessonSummaryViewModel CreateLessonSummaryViewModel(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic, Feedback? latestFeedback)
    {
        return new LessonSummaryViewModel(
            AppLocalization.GetText(userSettings.InterfaceLanguageId),
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
            AppLocalization.GetText(userSettings.InterfaceLanguageId),
            lessonHistoryService,
            () => NavigateToHome(selectedLevel));
    }

    public void CleanupOnShutdown()
    {
        if (CurrentViewModel is LessonChatViewModel lessonChatViewModel)
        {
            lessonChatViewModel.CleanupCurrentSessionBotVoiceFiles();
        }
    }

    public void Dispose()
    {
        CleanupOnShutdown();
        audioRecordingService.Dispose();
    }
}
