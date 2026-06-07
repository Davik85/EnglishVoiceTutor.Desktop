using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models.LessonContent;
using EnglishVoiceTutor.Desktop.Localization;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Models.Access;
using EnglishVoiceTutor.Desktop.Services;
using EnglishVoiceTutor.Desktop.Services.Auth;
using EnglishVoiceTutor.Desktop.Services.Access;
using EnglishVoiceTutor.Shared.NativeLanguages;
using EnglishVoiceTutor.Shared.StudyLanguages;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private const string AccessPanelSignedOutTitle = "Sign in required";
    private const string AccessPanelUpgradeOptionsTitle = "Upgrade options";
    private const string AccessPanelAccountStatusTitle = "Account status";
    private const string AccessPanelSubscriptionInactiveTitle = "Subscription inactive";
    private const string AccessPanelAccessCheckUnavailableTitle = "Access check unavailable";
    private const string AccessPanelUpgradeUnavailableTitle = "Upgrade unavailable";
    private const string AccessPanelDefaultTitle = "Lesson access";
    private const string AccessPanelUpgradeText = "Upgrade";
    private const string AccessPanelRefreshText = "Refresh status";
    private const string CheckoutOpenedMessage = "Checkout opened. After payment, return to the app and refresh your status.";
    private const string PremiumActiveAfterRefreshMessage = "Premium is active. You can start your lesson now.";
    private const string PaymentConfirmationPendingMessage = "Payment confirmation has not arrived yet. Please wait a moment and refresh again.";
    private const string RefreshStatusFailedMessage = "We could not refresh your status right now. Please try again.";
    private const string UpgradeUnavailableMessage = "Upgrade is temporarily unavailable. Please try again later.";
    private const string CheckoutStartFailedMessage = "We could not start checkout right now. Please try again.";

    private readonly UserSettingsService userSettingsService = new();
    private readonly LessonChatBackendService lessonChatBackendService = new();
    private readonly BackendDiagnosticsService backendDiagnosticsService = new();
    private readonly BackendUserSettingsClient backendUserSettingsClient = new();
    private readonly BackendSubscriptionStatusClient backendSubscriptionStatusClient = new();
    private readonly BackendLessonSessionClient backendLessonSessionClient = new();
    private readonly BackendLessonMessageClient backendLessonMessageClient = new();
    private readonly BackendLessonSummaryClient backendLessonSummaryClient = new();
    private readonly BackendLessonHistoryClient backendLessonHistoryClient = new();
    private readonly BackendCheckoutSessionClient backendCheckoutSessionClient = new();
    private readonly BackendLessonAccessDecisionClient backendLessonAccessDecisionClient = new();
    private readonly LessonStartGuardService lessonStartGuardService = new();
    private readonly AuthSessionStorageService authSessionStorageService = new();
    private readonly AuthBackendService authBackendService;
    private readonly AudioRecordingService audioRecordingService = new();
    private readonly AudioInputDeviceService audioInputDeviceService = new();
    private readonly AudioPlaybackService audioPlaybackService = new();
    private readonly BotVoiceTempFileCleanupService botVoiceTempFileCleanupService = new();
    private readonly LessonHistoryService lessonHistoryService = new();
    private readonly LessonContentService lessonContentService = new();
    private readonly UserSettings userSettings;
    private AccessDisplayState currentAccessPanelState = AccessDisplayState.UnknownOrError;
    private bool isCheckoutOpenedForCurrentPanel;
    private bool shutdownCleanupCompleted;


    public FlowDirection AppFlowDirection => InterfaceLanguageOptions.GetById(userSettings.InterfaceLanguageId).IsRightToLeft
        ? FlowDirection.RightToLeft
        : FlowDirection.LeftToRight;

    [ObservableProperty]
    private ViewModelBase currentViewModel;

    [ObservableProperty]
    private bool isAccessPanelVisible;

    [ObservableProperty]
    private string accessPanelTitle = string.Empty;

    [ObservableProperty]
    private string accessPanelMessage = string.Empty;

    [ObservableProperty]
    private string accessPanelPrimaryActionText = string.Empty;

    [ObservableProperty]
    private bool isAccessPanelPrimaryActionVisible;

    [ObservableProperty]
    private bool isAccessPanelPrimaryActionEnabled;

    [ObservableProperty]
    private string accessPanelRefreshActionText = AccessPanelRefreshText;

    [ObservableProperty]
    private bool isAccessPanelRefreshActionVisible;

    [ObservableProperty]
    private bool isAccessPanelRefreshActionEnabled;

    public string AccessPanelCloseActionText => AppLocalization.GetText(userSettings.InterfaceLanguageId).Settings.CloseButtonText;

    public MainViewModel()
    {
        audioRecordingService.CleanupOldRecordings();
        botVoiceTempFileCleanupService.CleanupOldBotVoiceFiles();
        authBackendService = new AuthBackendService(authSessionStorageService);
        userSettings = userSettingsService.Load();
        lessonChatBackendService.SetBackendBaseUrl(userSettings.BackendBaseUrl);
        authBackendService.SetBackendBaseUrl(userSettings.BackendBaseUrl);
        currentViewModel = CreateWelcomeViewModel();
        _ = TryRestoreSavedAuthSessionOnStartupAsync();
    }

    private async Task TryRestoreSavedAuthSessionOnStartupAsync()
    {
        try
        {
            var session = await authBackendService.TryRestoreSessionAsync();
            if (session is null)
            {
                return;
            }

            var meResult = await authBackendService.GetMeAsync(session.AccessToken);
            if (meResult.Status == AuthMeResultStatus.InvalidSession)
            {
                await authBackendService.LogoutAsync();
                Debug.WriteLine("Saved auth session restore failed. Reason=invalid_session; StoredSessionCleared=True.");
                return;
            }

            if (meResult.Status == AuthMeResultStatus.BackendUnavailable || meResult.User is null)
            {
                Debug.WriteLine("Saved auth session restore deferred. Reason=backend_unavailable; StoredSessionCleared=False.");
                return;
            }

            Debug.WriteLine("Saved auth session restored on startup.");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Saved auth session restore failed unexpectedly. Error={exception.Message}.");
        }
    }

    public void NavigateToWelcome()
    {
        HideAccessPanel();
        CurrentViewModel = CreateWelcomeViewModel();
    }

    public void NavigateToLevelSelection()
    {
        HideAccessPanel();
        CurrentViewModel = CreateLevelSelectionViewModel();
    }

    public void NavigateToHome(string selectedLevel)
    {
        HideAccessPanel();
        CurrentViewModel = CreateHomeViewModel(selectedLevel);
    }

    public void NavigateToSubtopics(string selectedLevel, Topic selectedTopic)
    {
        HideAccessPanel();
        CurrentViewModel = CreateSubtopicsViewModel(selectedLevel, selectedTopic);
    }

    public void NavigateToLessonChat(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic)
    {
        _ = TryNavigateToLessonChatAsync(selectedLevel, selectedTopic, selectedSubtopic);
    }

    public void NavigateToLessonSummary(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic, LessonSummaryInput summaryInput, Guid? backendLessonSessionId)
    {
        HideAccessPanel();
        SaveLessonHistory(selectedLevel, selectedTopic, selectedSubtopic, summaryInput);
        CurrentViewModel = CreateLessonSummaryViewModel(selectedLevel, selectedTopic, selectedSubtopic, summaryInput);
        _ = TrySaveLessonSummaryAsync(summaryInput, backendLessonSessionId);
    }

    public void NavigateToLessonHistory(string selectedLevel)
    {
        HideAccessPanel();
        CurrentViewModel = CreateLessonHistoryViewModel(selectedLevel);
    }

    private void NavigateToSettings(Action navigateBack)
    {
        HideAccessPanel();
        var lessonHistory = lessonHistoryService.Load();

        CurrentViewModel = new SettingsViewModel(
            userSettings.InterfaceLanguageId,
            userSettings.NativeLanguageName,
            userSettings.StudyLanguageId,
            userSettings.SelectedTutorAvatarId,
            userSettings.SpeechVoiceId,
            userSettings.UserDisplayName,
            userSettings.LearningGoal,
            userSettings.BackendBaseUrl,
            userSettings.AudioInputDeviceId,
            userSettingsService.SettingsFilePath,
            lessonHistoryService.LessonHistoryFilePath,
            lessonHistory,
            lessonChatBackendService,
            backendDiagnosticsService,
            backendUserSettingsClient,
            backendSubscriptionStatusClient,
            authBackendService,
            audioInputDeviceService,
            audioRecordingService,
            SaveSettings,
            navigateBack);
    }

    private void SaveSettings(string interfaceLanguageId, string nativeLanguage, string studyLanguageId, string tutorAvatarId, string speechVoiceId, string userDisplayName, string learningGoal, string backendBaseUrl, string audioInputDeviceId)
    {
        userSettings.InterfaceLanguageId = InterfaceLanguageOptions.GetById(interfaceLanguageId).Id;
        userSettings.NativeLanguageName = NativeLanguageCatalog.GetByIdOrName(nativeLanguage).Id;
        userSettings.StudyLanguageId = StudyLanguageCatalog.GetById(studyLanguageId).Id;
        userSettings.SelectedTutorAvatarId = TutorAvatarOptions.GetById(tutorAvatarId).Id;
        userSettings.SpeechVoiceId = SpeechVoiceOptions.GetById(speechVoiceId).Id;
        userSettings.UserDisplayName = userDisplayName;
        userSettings.LearningGoal = learningGoal;
        userSettings.BackendBaseUrl = backendBaseUrl;
        userSettings.AudioInputDeviceId = audioInputDeviceId;
        userSettingsService.Save(userSettings);
        OnPropertyChanged(nameof(AppFlowDirection));
        OnPropertyChanged(nameof(AccessPanelCloseActionText));
        lessonChatBackendService.SetBackendBaseUrl(userSettings.BackendBaseUrl);
        authBackendService.SetBackendBaseUrl(userSettings.BackendBaseUrl);
        Debug.WriteLine($"Settings saved: StudyLanguageId={userSettings.StudyLanguageId}; InterfaceLanguageId={userSettings.InterfaceLanguageId}; TutorAvatarId={userSettings.SelectedTutorAvatarId}; BackendBaseUrlConfigured={!string.IsNullOrWhiteSpace(userSettings.BackendBaseUrl)}. Start a new lesson to apply changed study language to lesson content.");
    }

    private void RefreshMutableUserSettingsFromPersistedSettings()
    {
        var persistedSettings = userSettingsService.Load();
        userSettings.InterfaceLanguageId = persistedSettings.InterfaceLanguageId;
        userSettings.NativeLanguageName = persistedSettings.NativeLanguageName;
        userSettings.StudyLanguageId = persistedSettings.StudyLanguageId;
        userSettings.SelectedTutorAvatarId = persistedSettings.SelectedTutorAvatarId;
        userSettings.SpeechVoiceId = persistedSettings.SpeechVoiceId;
        userSettings.UserDisplayName = persistedSettings.UserDisplayName;
        userSettings.LearningGoal = persistedSettings.LearningGoal;
        userSettings.BackendBaseUrl = persistedSettings.BackendBaseUrl;
        userSettings.AudioInputDeviceId = persistedSettings.AudioInputDeviceId;
        lessonChatBackendService.SetBackendBaseUrl(userSettings.BackendBaseUrl);
        authBackendService.SetBackendBaseUrl(userSettings.BackendBaseUrl);
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


    private async Task TrySaveLessonSummaryAsync(LessonSummaryInput summaryInput, Guid? backendLessonSessionId)
    {
        if (!backendLessonSessionId.HasValue)
        {
            Debug.WriteLine("Backend lesson summary save skipped. Reason=session_id_unavailable.");
            return;
        }

        var summaryText = LessonSummaryViewModel.BuildGoodText(summaryInput, AppLocalization.GetText(userSettings.InterfaceLanguageId)).Trim();
        if (string.IsNullOrWhiteSpace(summaryText))
        {
            Debug.WriteLine($"Backend lesson summary save skipped. SessionId={backendLessonSessionId}; Reason=empty_summary_text.");
            return;
        }

        var request = new UpsertBackendLessonSummaryRequest
        {
            Summary = summaryText,
            Strengths = null,
            Improvements = null,
            Vocabulary = null,
            Grammar = null,
            NextSteps = null
        };

        var result = await backendLessonSummaryClient.UpsertAsync(userSettings.BackendBaseUrl, backendLessonSessionId.Value, request);
        if (!result.Succeeded)
        {
            Debug.WriteLine($"Backend lesson summary save failed. SessionId={backendLessonSessionId}; Error={result.SafeErrorMessage ?? "unknown"}.");
            return;
        }

        Debug.WriteLine($"Backend lesson summary saved. SessionId={backendLessonSessionId}; SummaryId={result.Summary?.Id}.");
    }

    private async Task TryNavigateToLessonChatAsync(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic)
    {
        try
        {
            var session = await authSessionStorageService.GetValidSessionOrNullAsync();
            if (session is null)
            {
                ShowAccessPanel(AccessDisplayStateMapper.MapSignedOut());
                return;
            }

            var result = await lessonStartGuardService.CheckAsync(userSettings.BackendBaseUrl, isSignedIn: true);

            Debug.WriteLine(
                $"Lesson start guard check completed. ShouldAllowStart={result.ShouldAllowStart}; IsBackendDecisionAvailable={result.IsBackendDecisionAvailable}; Source={result.Source}; CanStartNewLesson={result.CanStartNewLesson}; Decision={result.Decision}; Reason={result.Reason}; EnforcementEnabled={result.EnforcementEnabled}; FreeLessonRemainingToday={result.FreeLessonRemainingToday}; FreeLessonUsedToday={result.FreeLessonUsedToday}.");

            if (!result.ShouldAllowStart)
            {
                ShowAccessPanel(result.AccessDisplay);
                return;
            }

            HideAccessPanel();
            CurrentViewModel = CreateLessonChatViewModel(selectedLevel, selectedTopic, selectedSubtopic);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Lesson start guard check failed unexpectedly. Error={exception.Message}. Blocking lesson start.");
            ShowAccessPanel(AccessDisplayStateMapper.MapUnknownOrError());
        }
    }

    private void ShowAccessPanel(AccessDisplayModel accessDisplay)
    {
        currentAccessPanelState = accessDisplay.State;
        isCheckoutOpenedForCurrentPanel = false;
        AccessPanelTitle = GetAccessPanelTitle(accessDisplay.State);
        AccessPanelMessage = accessDisplay.Message;
        AccessPanelPrimaryActionText = GetAccessPanelPrimaryActionText(accessDisplay.State) ?? string.Empty;
        IsAccessPanelPrimaryActionVisible = !string.IsNullOrWhiteSpace(AccessPanelPrimaryActionText);
        IsAccessPanelPrimaryActionEnabled = IsAccessPanelPrimaryActionEnabledFor(accessDisplay.State);
        HideAccessPanelRefreshAction();
        IsAccessPanelVisible = true;
    }

    [RelayCommand]
    private async Task AccessPanelPrimaryActionAsync()
    {
        if (currentAccessPanelState != AccessDisplayState.FreeAllowanceUsed)
        {
            return;
        }

        IsAccessPanelPrimaryActionEnabled = false;

        var result = await backendCheckoutSessionClient.CreateAsync(userSettings.BackendBaseUrl);
        if (!result.IsSuccess)
        {
            Debug.WriteLine($"Backend checkout-session request failed. Error={result.ErrorMessage ?? "unknown"}; RequiresLogin={result.RequiresLogin}.");
            ShowCheckoutStartFailedPanel();
            return;
        }

        var checkoutUrl = result.Value?.CheckoutUrl;
        if (string.IsNullOrWhiteSpace(checkoutUrl))
        {
            Debug.WriteLine($"Backend checkout-session did not return a checkout URL. Created={result.Value?.Created}; CheckoutEnabled={result.Value?.CheckoutEnabled}; ErrorCode={result.Value?.ErrorCode}.");
            ShowUpgradeUnavailablePanel();
            return;
        }

        if (!TryOpenCheckoutUrl(checkoutUrl))
        {
            Debug.WriteLine("Backend checkout-session returned a checkout URL, but the desktop could not open it.");
            ShowCheckoutStartFailedPanel();
            return;
        }

        AccessPanelTitle = AccessPanelUpgradeOptionsTitle;
        AccessPanelMessage = CheckoutOpenedMessage;
        AccessPanelPrimaryActionText = string.Empty;
        IsAccessPanelPrimaryActionVisible = false;
        IsAccessPanelPrimaryActionEnabled = false;
        isCheckoutOpenedForCurrentPanel = true;
        ShowAccessPanelRefreshAction(isEnabled: true);
    }

    [RelayCommand]
    private async Task RefreshAccessStatusAsync()
    {
        if (!isCheckoutOpenedForCurrentPanel)
        {
            return;
        }

        IsAccessPanelRefreshActionEnabled = false;

        var session = await authSessionStorageService.GetValidSessionOrNullAsync();
        if (session is null)
        {
            ShowAccessPanel(AccessDisplayStateMapper.MapSignedOut());
            return;
        }

        var lessonAccessResult = await backendLessonAccessDecisionClient.GetAsync(userSettings.BackendBaseUrl);
        var subscriptionStatusResult = await backendSubscriptionStatusClient.GetAsync(userSettings.BackendBaseUrl);
        var lessonAccess = lessonAccessResult.Value;
        var subscriptionStatus = subscriptionStatusResult.Value;

        if (lessonAccess is null && subscriptionStatus is null)
        {
            Debug.WriteLine($"Access status refresh failed. LessonAccessError={lessonAccessResult.ErrorMessage ?? "unknown"}; SubscriptionStatusError={subscriptionStatusResult.ErrorMessage ?? "unknown"}.");
            ShowRefreshStatusResult(AccessPanelAccessCheckUnavailableTitle, RefreshStatusFailedMessage, keepRefreshAvailable: true);
            return;
        }

        var accessDisplay = AccessDisplayStateMapper.Map(isSignedIn: true, lessonAccess, subscriptionStatus);
        if (accessDisplay.State == AccessDisplayState.PremiumActive)
        {
            ShowRefreshStatusResult(AccessPanelAccountStatusTitle, PremiumActiveAfterRefreshMessage, keepRefreshAvailable: false);
            return;
        }

        ShowRefreshStatusResult(AccessPanelUpgradeOptionsTitle, PaymentConfirmationPendingMessage, keepRefreshAvailable: true);
    }

    [RelayCommand]
    private void CloseAccessPanel()
    {
        HideAccessPanel();
    }

    private void HideAccessPanel()
    {
        currentAccessPanelState = AccessDisplayState.UnknownOrError;
        isCheckoutOpenedForCurrentPanel = false;
        HideAccessPanelRefreshAction();
        IsAccessPanelVisible = false;
    }

    private void ShowUpgradeUnavailablePanel()
    {
        currentAccessPanelState = AccessDisplayState.CheckoutUnavailable;
        AccessPanelTitle = AccessPanelUpgradeUnavailableTitle;
        AccessPanelMessage = UpgradeUnavailableMessage;
        AccessPanelPrimaryActionText = string.Empty;
        IsAccessPanelPrimaryActionVisible = false;
        IsAccessPanelPrimaryActionEnabled = false;
        isCheckoutOpenedForCurrentPanel = false;
        HideAccessPanelRefreshAction();
        IsAccessPanelVisible = true;
    }

    private void ShowCheckoutStartFailedPanel()
    {
        currentAccessPanelState = AccessDisplayState.CheckoutUnavailable;
        AccessPanelTitle = AccessPanelUpgradeUnavailableTitle;
        AccessPanelMessage = CheckoutStartFailedMessage;
        AccessPanelPrimaryActionText = string.Empty;
        IsAccessPanelPrimaryActionVisible = false;
        IsAccessPanelPrimaryActionEnabled = false;
        isCheckoutOpenedForCurrentPanel = false;
        HideAccessPanelRefreshAction();
        IsAccessPanelVisible = true;
    }

    private void ShowRefreshStatusResult(string title, string message, bool keepRefreshAvailable)
    {
        AccessPanelTitle = title;
        AccessPanelMessage = message;
        AccessPanelPrimaryActionText = string.Empty;
        IsAccessPanelPrimaryActionVisible = false;
        IsAccessPanelPrimaryActionEnabled = false;
        isCheckoutOpenedForCurrentPanel = keepRefreshAvailable;

        if (keepRefreshAvailable)
        {
            ShowAccessPanelRefreshAction(isEnabled: true);
            return;
        }

        HideAccessPanelRefreshAction();
    }

    private void ShowAccessPanelRefreshAction(bool isEnabled)
    {
        AccessPanelRefreshActionText = AppLocalization.GetText(userSettings.InterfaceLanguageId).Settings.RefreshStatusButtonText;
        IsAccessPanelRefreshActionVisible = true;
        IsAccessPanelRefreshActionEnabled = isEnabled;
    }

    private void HideAccessPanelRefreshAction()
    {
        AccessPanelRefreshActionText = AppLocalization.GetText(userSettings.InterfaceLanguageId).Settings.RefreshStatusButtonText;
        IsAccessPanelRefreshActionVisible = false;
        IsAccessPanelRefreshActionEnabled = false;
    }

    private static bool TryOpenCheckoutUrl(string checkoutUrl)
    {
        if (!Uri.TryCreate(checkoutUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private string GetAccessPanelTitle(AccessDisplayState state)
    {
        var text = AppLocalization.GetText(userSettings.InterfaceLanguageId);
        return state switch
        {
            AccessDisplayState.SignedOut => text.Settings.AccountTitle,
            AccessDisplayState.FreeAllowanceUsed => text.Settings.SubscriptionPremiumLabel,
            AccessDisplayState.PastDue => text.Settings.SubscriptionStatusTitle,
            AccessDisplayState.CanceledOrPaused => AccessPanelSubscriptionInactiveTitle,
            AccessDisplayState.CheckoutUnavailable => AccessPanelUpgradeUnavailableTitle,
            AccessDisplayState.UnknownOrError => AccessPanelAccessCheckUnavailableTitle,
            _ => AccessPanelDefaultTitle
        };
    }

    private string? GetAccessPanelPrimaryActionText(AccessDisplayState state)
    {
        return state == AccessDisplayState.FreeAllowanceUsed
            ? AppLocalization.GetText(userSettings.InterfaceLanguageId).Settings.UpgradeButtonText
            : null;
    }

    private static bool IsAccessPanelPrimaryActionEnabledFor(AccessDisplayState state)
    {
        return state switch
        {
            AccessDisplayState.FreeAllowanceUsed => true,
            _ => false
        };
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
        RefreshMutableUserSettingsFromPersistedSettings();
        Debug.WriteLine($"Starting lesson with StudyLanguageId={userSettings.StudyLanguageId}; Topic={selectedTopic.Title}; Subtopic={selectedSubtopic.Title}; Level={selectedLevel}.");
        return new LessonChatViewModel(
            AppLocalization.GetText(userSettings.InterfaceLanguageId),
            selectedLevel,
            selectedTopic,
            selectedSubtopic,
            userSettings.NativeLanguageName,
            StudyLanguageCatalog.GetById(userSettings.StudyLanguageId),
            userSettings.UserDisplayName,
            userSettings.LearningGoal,
            TutorAvatarOptions.GetById(userSettings.SelectedTutorAvatarId),
            SpeechVoiceOptions.GetById(userSettings.SpeechVoiceId).Id,
            LoadTutorProfile(userSettings.SelectedTutorAvatarId),
            LoadLessonScenarioForSubtopic(selectedTopic, selectedSubtopic),
            lessonChatBackendService,
            backendLessonSessionClient,
            backendLessonMessageClient,
            userSettings.BackendBaseUrl,
            audioRecordingService,
            audioPlaybackService,
            botVoiceTempFileCleanupService,
            userSettings.AudioInputDeviceId,
            () => NavigateToSubtopics(selectedLevel, selectedTopic),
            (summaryInput, backendLessonSessionId) => NavigateToLessonSummary(selectedLevel, selectedTopic, selectedSubtopic, summaryInput, backendLessonSessionId));
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
            backendLessonHistoryClient,
            userSettings.BackendBaseUrl,
            selectedLevel,
            () => NavigateToHome(selectedLevel));
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        if (shutdownCleanupCompleted)
        {
            return;
        }

        shutdownCleanupCompleted = true;
        Debug.WriteLine("Desktop shutdown cleanup started.");

        if (CurrentViewModel is LessonChatViewModel lessonChatViewModel)
        {
            await lessonChatViewModel.StopLessonActivityForShutdownAsync(cancellationToken);
        }

        if (audioRecordingService.IsRecording)
        {
            try
            {
                var recordingPath = audioRecordingService.StopRecording();
                audioRecordingService.SafeDeleteRecording(recordingPath);
                Debug.WriteLine("Desktop shutdown cleanup stopped active audio recording.");
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Desktop shutdown cleanup could not stop audio recording: {exception.Message}");
            }
        }

        audioPlaybackService.StopPlayback();
        Debug.WriteLine("Desktop shutdown cleanup completed.");
    }

    public void Dispose()
    {
        audioPlaybackService.StopPlayback();
        if (CurrentViewModel is IDisposable disposableCurrentViewModel)
        {
            disposableCurrentViewModel.Dispose();
        }

        audioRecordingService.Dispose();
    }
}
