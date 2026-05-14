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

    public void NavigateToLessonSummary(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic, LessonSummaryInput summaryInput)
    {
        SaveLessonHistory(selectedLevel, selectedTopic, selectedSubtopic, summaryInput);
        CurrentViewModel = CreateLessonSummaryViewModel(selectedLevel, selectedTopic, selectedSubtopic, summaryInput);
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

    private void SaveLessonHistory(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic, LessonSummaryInput summaryInput)
    {
        var item = new LessonHistoryItem
        {
            Id = Guid.NewGuid(),
            CompletedAt = DateTime.Now,
            SelectedLevel = selectedLevel,
            TopicTitle = selectedTopic.Title,
            SubtopicTitle = selectedSubtopic.Title,
            GoodText = LessonSummaryViewModel.BuildGoodText(summaryInput, AppLocalization.GetText(userSettings.InterfaceLanguageId)),
            ImproveText = LessonSummaryViewModel.BuildImproveText(summaryInput, AppLocalization.GetText(userSettings.InterfaceLanguageId)),
            UsefulPhrases = LessonSummaryViewModel.BuildUsefulPhrases(summaryInput, AppLocalization.GetText(userSettings.InterfaceLanguageId))
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
            LoadTutorProfile(userSettings.SelectedTutorAvatarId),
            LoadLessonScenarioForSubtopic(selectedTopic, selectedSubtopic),
            lessonChatBackendService,
            audioRecordingService,
            audioPlaybackService,
            botVoiceTempFileCleanupService,
            userSettings.AudioInputDeviceId,
            () => NavigateToSubtopics(selectedLevel, selectedTopic),
            summaryInput => NavigateToLessonSummary(selectedLevel, selectedTopic, selectedSubtopic, summaryInput));
    }

    private TutorProfile LoadTutorProfile(string tutorAvatarId)
    {
        try
        {
            return lessonContentService.LoadTutorProfile(TutorAvatarOptions.GetById(tutorAvatarId).Id);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Tutor profile load failed. TutorProfileId={tutorAvatarId}; {exception.Message}");
            return new TutorProfile
            {
                Id = TutorAvatarOptions.DefaultAvatarId,
                DisplayName = TutorAvatarOptions.Elena.DisplayName
            };
        }
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
        // Route by stable canonical IDs so localized display titles cannot break content loading.
        return (selectedTopic.Id, selectedSubtopic.Id) switch
        {
            (1, 101) => ContentConstants.IntroductionsFileName,
            (1, 102) => ContentConstants.SmallTalkWithANeighborFileName,
            (1, 103) => ContentConstants.AskingForHelpFileName,
            (1, 104) => ContentConstants.MakingPlansFileName,
            (1, 105) => ContentConstants.TalkingAboutYourDayFileName,
            (2, 201) => ContentConstants.AirportCheckInFileName,
            (2, 202) => ContentConstants.HotelCheckInFileName,
            (2, 203) => ContentConstants.AskingForDirectionsFileName,
            (2, 204) => ContentConstants.OrderingTransportFileName,
            (2, 205) => ContentConstants.LostLuggageFileName,
            (3, 301) => ContentConstants.FirstMeetingFileName,
            (3, 302) => ContentConstants.DailyStandupFileName,
            (3, 303) => ContentConstants.PhoneCallWithAClientFileName,
            (3, 304) => ContentConstants.WorkAskingForClarificationFileName,
            (3, 305) => ContentConstants.DiscussingDeadlinesFileName,
            (4, 401) => ContentConstants.TellMeAboutYourselfFileName,
            (4, 402) => ContentConstants.WorkExperienceFileName,
            (4, 403) => ContentConstants.StrengthsAndWeaknessesFileName,
            (4, 404) => ContentConstants.WhyDoYouWantThisJobFileName,
            (4, 405) => ContentConstants.AskingQuestionsAtTheEndFileName,
            (5, 501) => ContentConstants.BookingATableFileName,
            (5, 502) => ContentConstants.OrderingFoodFileName,
            (5, 503) => ContentConstants.AskingAboutIngredientsFileName,
            (5, 504) => ContentConstants.HandlingAWrongOrderFileName,
            (5, 505) => ContentConstants.PayingTheBillFileName,
            (6, 601) => ContentConstants.OpenConversationFileName,
            _ => throw new InvalidOperationException(
                $"No lesson scenario file is mapped for topic '{selectedTopic.Title}' and subtopic '{selectedSubtopic.Title}'.")
        };
    }

    private static string GetTopicFolderName(Topic selectedTopic)
    {
        // Route by stable canonical IDs so localized display titles cannot break content loading.
        return selectedTopic.Id switch
        {
            1 => ContentConstants.EverydayEnglishFolderName,
            2 => ContentConstants.TravelFolderName,
            3 => ContentConstants.WorkAndBusinessFolderName,
            4 => ContentConstants.JobInterviewFolderName,
            5 => ContentConstants.RestaurantAndCafeFolderName,
            6 => ContentConstants.FreeConversationFolderName,
            _ => throw new InvalidOperationException($"No lesson content folder is mapped for topic '{selectedTopic.Title}'.")
        };
    }

    private LessonSummaryViewModel CreateLessonSummaryViewModel(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic, LessonSummaryInput summaryInput)
    {
        return new LessonSummaryViewModel(
            AppLocalization.GetText(userSettings.InterfaceLanguageId),
            selectedLevel,
            selectedTopic,
            selectedSubtopic,
            summaryInput,
            lessonChatBackendService,
            userSettings.NativeLanguageName,
            userSettings.InterfaceLanguageId,
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
