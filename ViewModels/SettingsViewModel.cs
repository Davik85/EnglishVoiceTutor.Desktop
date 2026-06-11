using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Localization;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Models.Auth;
using EnglishVoiceTutor.Desktop.Models.Updates;
using EnglishVoiceTutor.Desktop.Services;
using EnglishVoiceTutor.Desktop.Services.Updates;
using EnglishVoiceTutor.Desktop.Services.Auth;
using EnglishVoiceTutor.Shared.NativeLanguages;
using EnglishVoiceTutor.Shared.StudyLanguages;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    public event EventHandler? ClearPasswordRequested;
    public event EventHandler? ClearPasswordRecoveryFieldsRequested;
    private const string OpenAiNotConfiguredStatus = "not_configured";
    private const string DiagnosticsReportTitle = "Language Voice Tutor Desktop diagnostics";
    private const string DiagnosticsCurrentDateTimeLabel = "Current date/time";
    private const string DiagnosticsAuthTokenPresentLabel = "Auth token present";
    private const string DiagnosticsAuthenticatedLabel = "Authenticated";
    private const int MinimumPasswordLength = 8;
    private const string DiagnosticsSessionRestoreAttemptedLabel = "Session restore attempted";
    private const string UnavailableAudioInputDeviceId = "unavailable_audio_input_device";
    private const string StudyLanguageTitleText = "Study language";
    private const string StudyLanguageSubtitleText = "Choose the language you want to practice. This does not change the app UI language.";
    private const string DiagnosticsStudyLanguageLabelText = "Study language";
    private const string DefaultAccountSignedOutText = "Not signed in";
    private const string SettingsSourceAuthenticatedText = "Authenticated account";
    private const string SettingsSourceDevelopmentText = "Local development user";
    private const string SubscriptionStatusUnavailableText = "Subscription status: unavailable";
    private const string SignedOutSubscriptionPromptText = "Sign in to view your account status.";
    private const string SignedOutSubscriptionPlaceholderText = "—";
    private const string DiagnosticsMaskedValueText = "[masked]";
    private static readonly Regex DiagnosticsSensitiveAssignmentPattern = new(
        @"(""?'?\b(?:password|pwd|secret|token|api[_-]?key|apikey|authorization|access[_-]?token|refresh[_-]?token|webhook[_-]?secret|client[_-]?secret|payment[_-]?secret)\b""?'?\s*[:=]\s*""?'?)[^""',;\s}]+(""?'?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex DiagnosticsBearerTokenPattern = new(
        @"\bBearer\s+[A-Za-z0-9\-._~+/]+=*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly Action<string, string, string, string, string, string, string, string, string> saveSettings;
    private readonly Action navigateBack;
    private readonly LessonChatBackendService lessonChatBackendService;
    private readonly BackendDiagnosticsService backendDiagnosticsService;
    private readonly BackendUserSettingsClient backendUserSettingsClient;
    private readonly BackendSubscriptionStatusClient backendSubscriptionStatusClient;
    private readonly AudioInputDeviceService audioInputDeviceService;
    private readonly AudioRecordingService audioRecordingService;
    private readonly AuthBackendService authBackendService;
    private readonly LessonHistoryService lessonHistoryService;
    private readonly UpdateManifestClient updateManifestClient = new();
    private readonly UpdateDownloadService updateDownloadService = new();
    private UpdateManifest? latestUpdateManifest;
    private Uri? latestInstallerUri;
    private LessonHistoryItem? latestLesson;
    private readonly string appVersionText;
    private readonly string settingsFilePathText;
    private readonly string lessonHistoryFilePathText;
    private SettingsLocalizedText localizedText;
    private DiagnosticsLocalizedText diagnosticsLocalizedText;
    private DiagnosticBackendStatus backendStatus = DiagnosticBackendStatus.Unknown;
    private DiagnosticDatabaseStatus databaseStatus = DiagnosticDatabaseStatus.Unknown;
    private DiagnosticAiStatus aiStatus = DiagnosticAiStatus.Unknown;
    private BackendSettingsSyncStatus backendSettingsSyncStatus = BackendSettingsSyncStatus.NotChecked;
    private DateTimeOffset? lastBackendSettingsSyncTime;
    private string backendSettingsSpeechVoice = BackendConstants.DefaultBackendSettingsSpeechVoice;
    private decimal backendSettingsSpeechSpeed = BackendConstants.DefaultBackendSettingsSpeechSpeed;
    private bool backendSettingsConversationModeEnabled = BackendConstants.DefaultBackendSettingsConversationModeEnabled;
    private bool isApplyingBackendSettings;
    private string databaseProviderText = string.Empty;
    private string databaseErrorText = string.Empty;
    private bool sessionRestoreAttempted;
    private bool isRefreshingAudioInputDevices;
    private bool isSelectedAudioInputDeviceUnavailable;
    private string lastBackendErrorCategory = "none";
    private HttpStatusCode? lastBackendStatusCode;
    private string lastBackendHealthResult = "not checked";
    private string lastBackendSettingsResult = "not checked";
    private string lastAccountStatusResult = "not checked";

    public string Title => localizedText.Title;

    public string Subtitle => localizedText.Subtitle;

    public string InterfaceLanguageTitle => localizedText.InterfaceLanguageTitle;

    public string NativeLanguageTitle => localizedText.NativeLanguageTitle;

    public string NativeLanguageSubtitle => localizedText.NativeLanguageSubtitle;

    public string StudyLanguageTitle => localizedText.StudyLanguageTitle;

    public string StudyLanguageSubtitle => localizedText.StudyLanguageSubtitle;

    public string TutorAvatarTitle => localizedText.TutorAvatarTitle;

    public string TutorAvatarSubtitle => localizedText.TutorAvatarSubtitle;

    public string AvatarProfileTitle => localizedText.AvatarProfileTitle;

    public string AvatarAgeLabel => localizedText.AvatarAgeLabel;

    public string AvatarLocationLabel => localizedText.AvatarLocationLabel;

    public string AvatarRoleLabel => localizedText.AvatarRoleLabel;

    public string AvatarInterestsLabel => localizedText.AvatarInterestsLabel;

    public string AvatarPersonalityLabel => localizedText.AvatarPersonalityLabel;

    public string AvatarSpeakingStyleLabel => localizedText.AvatarSpeakingStyleLabel;

    public string ConnectionTitle => localizedText.ConnectionTitle;

    public string AudioInputTitle => localizedText.AudioInputTitle;

    public string MicrophoneLabel => localizedText.MicrophoneLabel;

    public string SystemDefaultMicrophoneText => localizedText.SystemDefaultMicrophoneText;

    public string RefreshMicrophonesText => localizedText.RefreshMicrophonesText;

    public string TestMicrophoneText => localizedText.TestMicrophoneText;

    public string BackendUrlLabel => localizedText.BackendUrlLabel;

    public string BackendUrlSubtitle => localizedText.BackendUrlSubtitle;

    public string UserProfileTitle => localizedText.UserProfileTitle;

    public string UserProfileSubtitle => localizedText.UserProfileSubtitle;

    public string UserDisplayNameLabel => localizedText.UserDisplayNameLabel;

    public string LearningGoalLabel => localizedText.LearningGoalLabel;

    public string LearningStatisticsTitle => localizedText.LearningStatisticsTitle;

    public string LearningStatisticsSubtitle => localizedText.LearningStatisticsSubtitle;

    public string TotalCompletedLessonsLabel => localizedText.TotalCompletedLessonsLabel;

    public string LessonsTodayLabel => localizedText.LessonsTodayLabel;

    public string CurrentStreakLabel => localizedText.CurrentStreakLabel;

    public string LastCompletedLessonLabel => localizedText.LastCompletedLessonLabel;

    public string DiagnosticsTitle => diagnosticsLocalizedText.Title;

    private string LocalizeUiText(string englishText) => AppLocalization.GetLearnerUiText(SelectedInterfaceLanguageOption.Id, englishText);

    private BackendUxLocalizedText BackendUxText => BackendUxLocalization.GetText(SelectedInterfaceLanguageOption.Id);

    public string DiagnosticsSubtitle => diagnosticsLocalizedText.Subtitle;

    public string DiagnosticsAppVersionLabel => diagnosticsLocalizedText.AppVersionLabel;

    public string DiagnosticsBackendUrlLabel => diagnosticsLocalizedText.BackendUrlLabel;

    public string DiagnosticsBackendStatusLabel => diagnosticsLocalizedText.BackendStatusLabel;

    public string DiagnosticsDatabaseStatusLabel => diagnosticsLocalizedText.DatabaseStatusLabel;

    public string DiagnosticsAiStatusLabel => diagnosticsLocalizedText.AiStatusLabel;

    public string DiagnosticsSettingsFileLabel => diagnosticsLocalizedText.SettingsFileLabel;

    public string DiagnosticsLessonHistoryFileLabel => diagnosticsLocalizedText.LessonHistoryFileLabel;

    public string DiagnosticsInterfaceLanguageLabel => diagnosticsLocalizedText.InterfaceLanguageLabel;

    public string DiagnosticsNativeLanguageLabel => diagnosticsLocalizedText.NativeLanguageLabel;

    public string DiagnosticsStudyLanguageLabel => localizedText.StudyLanguageTitle;

    public string DiagnosticsTutorAvatarLabel => diagnosticsLocalizedText.TutorAvatarLabel;

    public string DiagnosticsMicrophoneLabel => localizedText.MicrophoneLabel;

    public string RefreshDiagnosticsButtonText => diagnosticsLocalizedText.RefreshButtonText;

    public string CopyDiagnosticsText => diagnosticsLocalizedText.CopyButtonText;

    public string AppVersionText => appVersionText;

    public string InstalledAppVersionText => $"Version: v{appVersionText}";

    public string CheckForUpdatesButtonText => IsCheckingForUpdates || IsDownloadingUpdate
        ? "Checking for updates..."
        : "Check for updates";

    public string DiagnosticsBackendUrlText => SanitizeDiagnosticsValue(BackendEndpointBuilder.NormalizeBaseUrl(BackendBaseUrl));

    public string DiagnosticsBackendStatusText => LocalizeBackendStatus(backendStatus);

    public string DiagnosticsDatabaseStatusText => LocalizeDatabaseStatus(databaseStatus);

    public string DiagnosticsAiStatusText => LocalizeAiStatus(aiStatus);

    public string SettingsFilePathText => settingsFilePathText;

    public string LessonHistoryFilePathText => lessonHistoryFilePathText;

    public string DiagnosticsInterfaceLanguageText => SelectedInterfaceLanguageOption.DisplayName;

    public string DiagnosticsNativeLanguageText => SelectedNativeLanguageOption.DisplayName;

    public string DiagnosticsStudyLanguageText => SelectedStudyLanguage.DisplayName;

    public string DiagnosticsTutorAvatarText => SelectedTutorAvatarDisplayName;

    public string DiagnosticsMicrophoneText => BuildDiagnosticsMicrophoneText();

    public string MicrophoneStatusText => StatusMessage;

    public string SaveButtonText => localizedText.SaveButtonText;

    public string BackButtonText => localizedText.BackButtonText;

    [ObservableProperty]
    private string totalCompletedLessonsText = "0";

    [ObservableProperty]
    private string lessonsTodayText = "0";

    [ObservableProperty]
    private string currentStreakText = "0";

    public string LastCompletedLessonText => BuildLastCompletedLessonText(latestLesson, localizedText.NoCompletedLessonsText);
    public string LearningTabHeader => localizedText.LearningTabHeader;
    public string ProgressTabHeader => localizedText.ProgressTabHeader;
    public string AccountTitle => localizedText.AccountTitle;
    public string AccountSubtitle => localizedText.AccountSubtitle;
    public string AccountEmailLabel => localizedText.AccountEmailLabel;
    public string AccountPasswordLabel => localizedText.AccountPasswordLabel;
    public string AccountDisplayNameLabel => localizedText.AccountDisplayNameLabel;
    public string AccountRegisterButtonText => localizedText.AccountRegisterButtonText;
    public string AccountLoginButtonText => localizedText.AccountLoginButtonText;
    public string AccountLogoutButtonText => localizedText.AccountLogoutButtonText;
    public string ForgotPasswordTitle => localizedText.ForgotPasswordTitle;
    public string ForgotPasswordSubtitle => localizedText.ForgotPasswordSubtitle;
    public string ForgotPasswordButtonText => localizedText.ForgotPasswordButtonText;
    public string ResetTokenLabel => localizedText.ResetTokenLabel;
    public string NewPasswordLabel => localizedText.NewPasswordLabel;
    public string ConfirmNewPasswordLabel => localizedText.ConfirmNewPasswordLabel;
    public string ResetPasswordButtonText => localizedText.ResetPasswordButtonText;
    public string ChangePasswordTitle => localizedText.ChangePasswordTitle;
    public string ChangePasswordSubtitle => localizedText.ChangePasswordSubtitle;
    public string CurrentPasswordLabel => localizedText.CurrentPasswordLabel;
    public string ChangePasswordButtonText => localizedText.ChangePasswordButtonText;
    public string CurrentAccountLabel => localizedText.CurrentAccountLabel;
    public string SettingsSourceLabel => localizedText.SettingsSourceLabel;
    public string SubscriptionStatusTitle => localizedText.SubscriptionStatusTitle;
    public string SubscriptionPlanLabel => localizedText.SubscriptionPlanLabel;
    public string SubscriptionPremiumLabel => localizedText.SubscriptionPremiumLabel;
    public string SubscriptionTrialLabel => localizedText.SubscriptionTrialLabel;
    public string SubscriptionFreeLessonLabel => localizedText.SubscriptionFreeLessonLabel;
    public string SubscriptionEnforcementLabel => localizedText.SubscriptionEnforcementLabel;
    public string SubscriptionSourceLabel => localizedText.SubscriptionSourceLabel;
    public string SubscriptionCheckedAtLabel => localizedText.SubscriptionCheckedAtLabel;

    public IReadOnlyList<InterfaceLanguageOption> AvailableInterfaceLanguages { get; } = InterfaceLanguageOptions.All;

    public IReadOnlyList<NativeLanguageDefinition> AvailableNativeLanguages { get; } = NativeLanguageCatalog.All;

    public IReadOnlyList<StudyLanguageDefinition> AvailableStudyLanguages { get; } = StudyLanguageCatalog.All;

    public IReadOnlyList<TutorAvatarOption> AvailableTutorAvatars { get; } = TutorAvatarOptions.All;

    public IReadOnlyList<SpeechVoiceOption> AvailableSpeechVoices { get; } = SpeechVoiceOptions.All;

    public ObservableCollection<AudioInputDeviceOption> AudioInputDevices { get; } = [];

    private TutorAvatarLocalizedProfileText SelectedTutorAvatarProfileText =>
        TutorAvatarProfileLocalization.GetProfileText(SelectedTutorAvatarOption?.Id, SelectedInterfaceLanguageId);

    public string SelectedTutorAvatarDescription => SelectedTutorAvatarProfileText.ShortDescription;

    public string SelectedTutorAvatarDisplayName => SelectedTutorAvatarOption?.DisplayName ?? string.Empty;

    public string SelectedTutorAvatarAgeText => SelectedTutorAvatarProfileText.AgeText;

    public string SelectedTutorAvatarLocation => SelectedTutorAvatarProfileText.Location;

    public string SelectedTutorAvatarRole => SelectedTutorAvatarProfileText.Role;

    public string SelectedTutorAvatarInterestsText => SelectedTutorAvatarProfileText.InterestsText;

    public string SelectedTutorAvatarPersonalityText => SelectedTutorAvatarProfileText.PersonalityText;

    public string SelectedTutorAvatarSpeakingStyleText => SelectedTutorAvatarProfileText.SpeakingStyleText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiagnosticsNativeLanguageText))]
    private NativeLanguageDefinition selectedNativeLanguageOption = NativeLanguageCatalog.English;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiagnosticsStudyLanguageText))]
    private StudyLanguageDefinition selectedStudyLanguage = StudyLanguageCatalog.English;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedInterfaceLanguageId))]
    [NotifyPropertyChangedFor(nameof(DiagnosticsInterfaceLanguageText))]
    private InterfaceLanguageOption selectedInterfaceLanguageOption;

    public string SelectedInterfaceLanguageId => SelectedInterfaceLanguageOption.Id;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTutorAvatarDescription))]
    [NotifyPropertyChangedFor(nameof(SelectedTutorAvatarDisplayName))]
    [NotifyPropertyChangedFor(nameof(SelectedTutorAvatarAgeText))]
    [NotifyPropertyChangedFor(nameof(SelectedTutorAvatarLocation))]
    [NotifyPropertyChangedFor(nameof(SelectedTutorAvatarRole))]
    [NotifyPropertyChangedFor(nameof(SelectedTutorAvatarInterestsText))]
    [NotifyPropertyChangedFor(nameof(SelectedTutorAvatarPersonalityText))]
    [NotifyPropertyChangedFor(nameof(SelectedTutorAvatarSpeakingStyleText))]
    [NotifyPropertyChangedFor(nameof(DiagnosticsTutorAvatarText))]
    private TutorAvatarOption? selectedTutorAvatarOption;

    [ObservableProperty]
    private SpeechVoiceOption selectedSpeechVoiceOption = SpeechVoiceOptions.Coral;

    public string SpeechVoiceTitle => LocalizeUiText("Tutor voice");

    public string SpeechVoiceSubtitle => LocalizeUiText("Choose the voice used for normal lesson playback and Conversation Mode TTS.");

    [ObservableProperty]
    private string userDisplayName = string.Empty;

    [ObservableProperty]
    private string learningGoal = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiagnosticsBackendUrlText))]
    private string backendBaseUrl = BackendConstants.DefaultBackendBaseUrl;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MicrophoneStatusText))]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string diagnosticsCopyStatusText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckForUpdatesButtonText))]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    private bool isCheckingForUpdates;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckForUpdatesButtonText))]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    private bool isDownloadingUpdate;

    [ObservableProperty]
    private string email = string.Empty;
    [ObservableProperty]
    private string password = string.Empty;
    [ObservableProperty]
    private string displayName = string.Empty;
    [ObservableProperty]
    private string resetToken = string.Empty;
    [ObservableProperty]
    private string resetNewPassword = string.Empty;
    [ObservableProperty]
    private string resetConfirmPassword = string.Empty;
    [ObservableProperty]
    private string currentPassword = string.Empty;
    [ObservableProperty]
    private string changeNewPassword = string.Empty;
    [ObservableProperty]
    private string changeConfirmPassword = string.Empty;
    [ObservableProperty]
    private string passwordRecoveryStatusMessage = string.Empty;
    [ObservableProperty]
    private string changePasswordStatusMessage = string.Empty;
    [ObservableProperty]
    private string currentUserEmail = string.Empty;
    [ObservableProperty]
    private string currentUserDisplayName = string.Empty;
    [ObservableProperty]
    private bool isPasswordResetPanelExpanded;
    [ObservableProperty]
    private bool isChangePasswordPanelExpanded;
    [ObservableProperty]
    private bool isAuthenticated;
    [ObservableProperty]
    private bool isBusy;
    [ObservableProperty]
    private string errorMessage = string.Empty;
    [ObservableProperty]
    private string settingsSource = SettingsSourceDevelopmentText;
    [ObservableProperty]
    private string subscriptionPlanText = SubscriptionStatusUnavailableText;
    [ObservableProperty]
    private string subscriptionPremiumText = SubscriptionStatusUnavailableText;
    [ObservableProperty]
    private string subscriptionTrialText = SubscriptionStatusUnavailableText;
    [ObservableProperty]
    private string subscriptionFreeLessonText = SubscriptionStatusUnavailableText;
    [ObservableProperty]
    private string subscriptionEnforcementText = SubscriptionStatusUnavailableText;
    [ObservableProperty]
    private string subscriptionSourceText = SubscriptionStatusUnavailableText;
    [ObservableProperty]
    private string subscriptionCheckedAtText = SubscriptionStatusUnavailableText;

    [ObservableProperty]
    private bool isLearningSectionSelected = true;

    [ObservableProperty]
    private bool isAccountSectionSelected;

    [ObservableProperty]
    private bool isAudioSectionSelected;

    [ObservableProperty]
    private bool isProgressSectionSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiagnosticsMicrophoneText))]
    private AudioInputDeviceOption? selectedAudioInputDeviceOption;

    public SettingsViewModel(
        string currentInterfaceLanguageId,
        string currentNativeLanguage,
        string currentStudyLanguageId,
        string currentTutorAvatarId,
        string currentSpeechVoiceId,
        string currentUserDisplayName,
        string currentLearningGoal,
        string currentBackendBaseUrl,
        string currentAudioInputDeviceId,
        string settingsFilePath,
        string lessonHistoryFilePath,
        IReadOnlyList<LessonHistoryItem> lessonHistory,
        LessonChatBackendService lessonChatBackendService,
        BackendDiagnosticsService backendDiagnosticsService,
        BackendUserSettingsClient backendUserSettingsClient,
        BackendSubscriptionStatusClient backendSubscriptionStatusClient,
        AuthBackendService authBackendService,
        LessonHistoryService lessonHistoryService,
        AudioInputDeviceService audioInputDeviceService,
        AudioRecordingService audioRecordingService,
        Action<string, string, string, string, string, string, string, string, string> saveSettings,
        Action navigateBack,
        SettingsSection initialSection = SettingsSection.Learning)
    {
        selectedInterfaceLanguageOption = InterfaceLanguageOptions.GetById(currentInterfaceLanguageId);
        localizedText = SettingsLocalization.GetSettingsText(selectedInterfaceLanguageOption.Id);
        diagnosticsLocalizedText = DiagnosticsLocalization.GetText(selectedInterfaceLanguageOption.Id);
        selectedNativeLanguageOption = NativeLanguageCatalog.GetByIdOrName(currentNativeLanguage);
        selectedStudyLanguage = StudyLanguageCatalog.GetById(currentStudyLanguageId);
        selectedTutorAvatarOption = TutorAvatarOptions.GetById(currentTutorAvatarId);
        selectedSpeechVoiceOption = SpeechVoiceOptions.GetById(string.IsNullOrWhiteSpace(currentSpeechVoiceId) ? SpeechVoiceOptions.GetPreferredVoiceIdForTutor(selectedTutorAvatarOption.Id) : currentSpeechVoiceId);
        backendSettingsSpeechVoice = selectedSpeechVoiceOption.Id;
        userDisplayName = currentUserDisplayName;
        learningGoal = currentLearningGoal;
        backendBaseUrl = BackendEndpointBuilder.ResolveSavedBaseUrlForCurrentBuild(currentBackendBaseUrl);
        settingsFilePathText = settingsFilePath;
        lessonHistoryFilePathText = lessonHistoryFilePath;
        appVersionText = DesktopAppVersionProvider.GetCurrentVersionText();
        this.lessonChatBackendService = lessonChatBackendService;
        this.backendDiagnosticsService = backendDiagnosticsService;
        this.backendUserSettingsClient = backendUserSettingsClient;
        this.backendSubscriptionStatusClient = backendSubscriptionStatusClient;
        this.authBackendService = authBackendService;
        this.lessonHistoryService = lessonHistoryService;
        this.audioInputDeviceService = audioInputDeviceService;
        this.audioRecordingService = audioRecordingService;
        this.saveSettings = saveSettings;
        this.navigateBack = navigateBack;

        ApplyInitialSection(initialSection);
        ApplyLearningStatistics(lessonHistory);
        RefreshAudioInputDevices(currentAudioInputDeviceId, showUnavailableStatus: false);
        _ = RestoreSessionAsync();
    }


    partial void OnIsLearningSectionSelectedChanged(bool value)
    {
        if (value)
        {
            ClearOtherSelectedSections(nameof(IsLearningSectionSelected));
        }
    }

    partial void OnIsAccountSectionSelectedChanged(bool value)
    {
        if (value)
        {
            ClearOtherSelectedSections(nameof(IsAccountSectionSelected));
        }
    }

    partial void OnIsAudioSectionSelectedChanged(bool value)
    {
        if (value)
        {
            ClearOtherSelectedSections(nameof(IsAudioSectionSelected));
        }
    }

    partial void OnIsProgressSectionSelectedChanged(bool value)
    {
        if (value)
        {
            ClearOtherSelectedSections(nameof(IsProgressSectionSelected));
        }
    }

    private void ApplyInitialSection(SettingsSection initialSection)
    {
        IsLearningSectionSelected = initialSection == SettingsSection.Learning;
        IsAccountSectionSelected = initialSection == SettingsSection.Account;
        IsAudioSectionSelected = initialSection == SettingsSection.Audio;
        IsProgressSectionSelected = initialSection == SettingsSection.Progress;

        if (!IsLearningSectionSelected && !IsAccountSectionSelected && !IsAudioSectionSelected && !IsProgressSectionSelected)
        {
            IsLearningSectionSelected = true;
        }
    }

    private void ClearOtherSelectedSections(string selectedPropertyName)
    {
        if (selectedPropertyName != nameof(IsLearningSectionSelected))
        {
            IsLearningSectionSelected = false;
        }

        if (selectedPropertyName != nameof(IsAccountSectionSelected))
        {
            IsAccountSectionSelected = false;
        }

        if (selectedPropertyName != nameof(IsAudioSectionSelected))
        {
            IsAudioSectionSelected = false;
        }

        if (selectedPropertyName != nameof(IsProgressSectionSelected))
        {
            IsProgressSectionSelected = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (!TryValidateCredentials(requireDisplayName: true))
        {
            return;
        }

        await AuthenticateAsync(
            () => authBackendService.RegisterAsync(new RegisterRequest
            {
                Email = Email.Trim(),
                Password = Password,
                DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName.Trim()
            }),
            BackendUxText.RegisterFailed);
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (!TryValidateCredentials(requireDisplayName: false))
        {
            return;
        }

        await AuthenticateAsync(
            () => authBackendService.LoginAsync(new LoginRequest
            {
                Email = Email.Trim(),
                Password = Password
            }),
            BackendUxText.LoginFailed);
    }

    [RelayCommand]
    private void ShowPasswordResetPanel()
    {
        IsPasswordResetPanelExpanded = true;
        IsChangePasswordPanelExpanded = false;
        ChangePasswordStatusMessage = string.Empty;
        ClearPasswordRecoveryFields();
    }

    [RelayCommand]
    private void ClosePasswordResetPanel()
    {
        IsPasswordResetPanelExpanded = false;
        PasswordRecoveryStatusMessage = string.Empty;
        ClearPasswordRecoveryFields();
    }

    [RelayCommand]
    private void ShowChangePasswordPanel()
    {
        if (!IsAuthenticated)
        {
            ChangePasswordStatusMessage = BackendUxText.SignInRequired;
            return;
        }

        IsChangePasswordPanelExpanded = true;
        IsPasswordResetPanelExpanded = false;
        PasswordRecoveryStatusMessage = string.Empty;
        ClearPasswordRecoveryFields();
    }

    [RelayCommand]
    private void CloseChangePasswordPanel()
    {
        IsChangePasswordPanelExpanded = false;
        ChangePasswordStatusMessage = string.Empty;
        ClearPasswordRecoveryFields();
    }

    [RelayCommand]
    private async Task RequestPasswordResetAsync()
    {
        ErrorMessage = string.Empty;
        PasswordRecoveryStatusMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Email))
        {
            PasswordRecoveryStatusMessage = BackendUxText.EmailRequired;
            return;
        }

        IsBusy = true;
        try
        {
            var result = await authBackendService.RequestPasswordResetAsync(new PasswordResetRequest
            {
                Email = Email.Trim()
            });

            PasswordRecoveryStatusMessage = result.Status switch
            {
                PasswordOperationResultStatus.Success => string.IsNullOrWhiteSpace(result.Message) ? BackendUxText.PasswordResetRequested : result.Message,
                PasswordOperationResultStatus.BackendUnavailable => BackendUxText.CouldNotConnect,
                _ => string.IsNullOrWhiteSpace(result.Message) ? BackendUxText.PasswordResetRequestFailed : result.Message
            };
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmPasswordResetAsync()
    {
        ErrorMessage = string.Empty;
        PasswordRecoveryStatusMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(ResetToken))
        {
            PasswordRecoveryStatusMessage = BackendUxText.ResetCodeRequired;
            return;
        }

        if (string.IsNullOrWhiteSpace(ResetNewPassword) || string.IsNullOrWhiteSpace(ResetConfirmPassword))
        {
            PasswordRecoveryStatusMessage = BackendUxText.PasswordResetFieldsRequired;
            return;
        }

        if (ResetNewPassword.Length < MinimumPasswordLength)
        {
            PasswordRecoveryStatusMessage = BackendUxText.PasswordTooShort;
            return;
        }

        if (!string.Equals(ResetNewPassword, ResetConfirmPassword, StringComparison.Ordinal))
        {
            PasswordRecoveryStatusMessage = BackendUxText.PasswordsDoNotMatch;
            return;
        }

        IsBusy = true;
        try
        {
            var result = await authBackendService.ConfirmPasswordResetAsync(new PasswordResetConfirmRequest
            {
                Token = ResetToken.Trim(),
                NewPassword = ResetNewPassword
            });

            if (result.IsSuccess)
            {
                ClearPasswordRecoveryFields();
                PasswordRecoveryStatusMessage = string.IsNullOrWhiteSpace(result.Message) ? BackendUxText.PasswordResetConfirmed : result.Message;
                return;
            }

            PasswordRecoveryStatusMessage = result.Status == PasswordOperationResultStatus.BackendUnavailable
                ? BackendUxText.CouldNotConnect
                : string.IsNullOrWhiteSpace(result.Message) ? BackendUxText.PasswordResetConfirmFailed : result.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        ErrorMessage = string.Empty;
        ChangePasswordStatusMessage = string.Empty;
        if (!IsAuthenticated)
        {
            ChangePasswordStatusMessage = BackendUxText.SignInRequired;
            return;
        }

        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            ChangePasswordStatusMessage = BackendUxText.CurrentPasswordRequired;
            return;
        }

        if (string.IsNullOrWhiteSpace(ChangeNewPassword) || string.IsNullOrWhiteSpace(ChangeConfirmPassword))
        {
            ChangePasswordStatusMessage = BackendUxText.PasswordChangeFieldsRequired;
            return;
        }

        if (ChangeNewPassword.Length < MinimumPasswordLength)
        {
            ChangePasswordStatusMessage = BackendUxText.PasswordTooShort;
            return;
        }

        if (!string.Equals(ChangeNewPassword, ChangeConfirmPassword, StringComparison.Ordinal))
        {
            ChangePasswordStatusMessage = BackendUxText.PasswordsDoNotMatch;
            return;
        }

        IsBusy = true;
        try
        {
            var result = await authBackendService.ChangePasswordAsync(new ChangePasswordRequest
            {
                CurrentPassword = CurrentPassword,
                NewPassword = ChangeNewPassword,
                ConfirmNewPassword = ChangeConfirmPassword
            });

            ChangePasswordStatusMessage = result.Status switch
            {
                PasswordOperationResultStatus.Success => string.IsNullOrWhiteSpace(result.Message) ? BackendUxText.PasswordChanged : result.Message,
                PasswordOperationResultStatus.Unauthorized => BackendUxText.SessionExpired,
                PasswordOperationResultStatus.BackendUnavailable => BackendUxText.CouldNotConnect,
                _ => string.IsNullOrWhiteSpace(result.Message) ? BackendUxText.PasswordChangeFailed : result.Message
            };

            if (result.IsSuccess)
            {
                ClearPasswordRecoveryFields();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            await authBackendService.LogoutAsync();
            ClearAccountState();
            await LoadSettingsForCurrentSessionAsync();
            await RefreshLearningStatisticsAsync();
            await RefreshSubscriptionStatusAsync();
            StatusMessage = BackendUxText.SignedOut;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreSessionAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            sessionRestoreAttempted = true;
            await RefreshBackendHealthDiagnosticsAsync();
            var session = await authBackendService.TryRestoreSessionAsync();
            if (session is null)
            {
                ClearAccountState();
                await LoadDevelopmentSettingsAsync();
                await RefreshLearningStatisticsAsync();
                await RefreshSubscriptionStatusAsync();
                return;
            }

            ApplyAuthenticatedUser(session.User);
            var meResult = await authBackendService.GetMeAsync(session.AccessToken);
            if (meResult.Status == AuthMeResultStatus.InvalidSession)
            {
                await authBackendService.LogoutAsync();
                ClearAccountState();
                await LoadDevelopmentSettingsAsync();
                await RefreshLearningStatisticsAsync();
                await RefreshSubscriptionStatusAsync();
                StatusMessage = BackendUxText.SessionExpired;
                return;
            }

            if (meResult.Status == AuthMeResultStatus.BackendUnavailable || meResult.User is null)
            {
                SettingsSource = LocalizeUiText(SettingsSourceAuthenticatedText);
                await RefreshLearningStatisticsAsync();
                RecordAccountStatusResult(null, "unavailable");
                ResetSubscriptionStatus();
                StatusMessage = BackendUxText.CouldNotConnect;
                return;
            }

            ApplyAuthenticatedUser(meResult.User);
            await LoadAuthenticatedSettingsAsync(session.AccessToken);
            await RefreshLearningStatisticsAsync();
            await RefreshSubscriptionStatusAsync();
            StatusMessage = BackendUxText.SessionRestored;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        SaveCurrentSettingsLocally();
        BackendBaseUrl = BackendEndpointBuilder.ResolveSavedBaseUrlForCurrentBuild(BackendBaseUrl);
        StatusMessage = localizedText.SettingsSavedMessage;
        await SaveBackendUserSettingsAsync();
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }

    [RelayCommand]
    private async Task TestMicrophoneAsync()
    {
        var previousAudioInputDeviceId = SelectedAudioInputDeviceOption?.Id;
        RefreshAudioInputDevices(previousAudioInputDeviceId, showUnavailableStatus: true);

        if (AudioInputDevices.Count <= 1)
        {
            StatusMessage = localizedText.NoMicrophoneFoundMessage;
            return;
        }

        if (!audioInputDeviceService.IsSystemDefault(previousAudioInputDeviceId) && SelectedAudioInputDeviceOption?.IsDefault == true)
        {
            StatusMessage = localizedText.SelectedMicrophoneUnavailableMessage;
            return;
        }

        var testFilePath = string.Empty;

        try
        {
            testFilePath = audioRecordingService.StartRecording(SelectedAudioInputDeviceOption?.Id);
            await Task.Delay(TimeSpan.FromSeconds(2));
            testFilePath = audioRecordingService.StopRecording();
            StatusMessage = localizedText.MicrophoneTestCompletedMessage;
        }
        catch
        {
            StatusMessage = localizedText.NoMicrophoneFoundMessage;
        }
        finally
        {
            if (audioRecordingService.IsRecording)
            {
                testFilePath = audioRecordingService.StopRecording();
            }

            audioRecordingService.SafeDeleteRecording(testFilePath);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
    private async Task CheckForUpdatesAsync()
    {
        IsCheckingForUpdates = true;
        latestUpdateManifest = null;
        latestInstallerUri = null;

        try
        {
            var result = await updateManifestClient.LoadLatestAsync();
            if (!result.IsSuccess || result.ValidationResult?.Manifest is null || result.ValidationResult.InstallerUri is null)
            {
                ShowUpdateMessage(
                    string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? "Could not check for updates right now. Please check your internet connection and try again."
                        : result.ErrorMessage,
                    MessageBoxImage.Information);
                return;
            }

            latestUpdateManifest = result.ValidationResult.Manifest;
            latestInstallerUri = result.ValidationResult.InstallerUri;

            var comparison = UpdateVersionComparer.Compare(appVersionText, latestUpdateManifest.Version);
            if (comparison == 0)
            {
                ShowUpdateMessage(
                    $"You are using the latest version. Current: {appVersionText}. Latest: {latestUpdateManifest.Version}.",
                    MessageBoxImage.Information);
                return;
            }

            if (comparison > 0)
            {
                ShowUpdateMessage("This app version is newer than the public update manifest.", MessageBoxImage.Warning);
                return;
            }

            var downloadChoice = MessageBox.Show(
                $"A new version of Language Voice Tutor is available. Do you want to download and install it now?\n\nCurrent version: {appVersionText}\nLatest version: {latestUpdateManifest.Version}",
                "Update available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (downloadChoice != MessageBoxResult.Yes)
            {
                return;
            }

            await DownloadVerifyAndMaybeRunUpdateAsync(latestUpdateManifest, latestInstallerUri);
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    private async Task DownloadVerifyAndMaybeRunUpdateAsync(UpdateManifest manifest, Uri installerUri)
    {
        IsDownloadingUpdate = true;
        try
        {
            var result = await updateDownloadService.DownloadAndVerifyAsync(manifest, installerUri);
            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.FilePath))
            {
                ShowUpdateMessage(
                    "The update could not be downloaded or verified. Please try again later.",
                    MessageBoxImage.Warning);
                return;
            }

            var installChoice = MessageBox.Show(
                "The update was downloaded and verified. Do you want to start the installer now?",
                "Start installer?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (installChoice == MessageBoxResult.Yes)
            {
                UpdateDownloadService.OpenInstaller(result.FilePath);
            }
        }
        finally
        {
            IsDownloadingUpdate = false;
        }
    }

    private static void ShowUpdateMessage(string message, MessageBoxImage icon)
    {
        MessageBox.Show(message, "App updates", MessageBoxButton.OK, icon);
    }

    private bool CanCheckForUpdates() => !IsCheckingForUpdates && !IsDownloadingUpdate;

    [RelayCommand]
    private async Task CopyDiagnosticsAsync()
    {
        try
        {
            Clipboard.SetText(await BuildDiagnosticsReportAsync());
            DiagnosticsCopyStatusText = diagnosticsLocalizedText.CopySuccessMessage;
        }
        catch
        {
            DiagnosticsCopyStatusText = diagnosticsLocalizedText.CopyFailureMessage;
        }
    }

    [RelayCommand]
    private async Task RefreshDiagnosticsAsync()
    {
        DiagnosticsCopyStatusText = string.Empty;
        SetDiagnosticStatuses(DiagnosticBackendStatus.Checking, DiagnosticDatabaseStatus.Checking, DiagnosticAiStatus.Checking);

        var diagnosticsResult = await backendDiagnosticsService.CheckAsync(BackendBaseUrl);
        RecordHealthDiagnostics(diagnosticsResult);
        DatabaseProviderText = diagnosticsResult.DatabaseHealth?.Provider ?? string.Empty;
        DatabaseErrorText = diagnosticsResult.DatabaseError ?? string.Empty;

        if (!diagnosticsResult.IsBackendHealthy)
        {
            SetDiagnosticStatuses(DiagnosticBackendStatus.Unavailable, DiagnosticDatabaseStatus.Unavailable, DiagnosticAiStatus.Unavailable);
            SetBackendSettingsSyncStatus(BackendSettingsSyncStatus.Unavailable);
            return;
        }

        BackendStatus = DiagnosticBackendStatus.Connected;
        DatabaseStatus = diagnosticsResult.IsDatabaseHealthy
            ? DiagnosticDatabaseStatus.Healthy
            : DiagnosticDatabaseStatus.Unavailable;
        var configStatus = await lessonChatBackendService.GetBackendConfigStatusAsync(BackendBaseUrl);
        AiStatus = MapAiStatus(configStatus);
        await LoadSettingsForCurrentSessionAsync();
        await RefreshSubscriptionStatusAsync();
    }

    partial void OnSelectedInterfaceLanguageOptionChanged(InterfaceLanguageOption value)
    {
        localizedText = SettingsLocalization.GetSettingsText(value.Id);
        diagnosticsLocalizedText = DiagnosticsLocalization.GetText(value.Id);
        RefreshLocalizedText();

        var preferredAudioInputDeviceId = isSelectedAudioInputDeviceUnavailable
            ? UnavailableAudioInputDeviceId
            : SelectedAudioInputDeviceOption?.Id;
        RefreshAudioInputDevices(preferredAudioInputDeviceId, showUnavailableStatus: false);

        if (!string.IsNullOrWhiteSpace(StatusMessage))
        {
            StatusMessage = localizedText.SettingsSavedMessage;
        }

        DiagnosticsCopyStatusText = string.Empty;
    }

    partial void OnBackendBaseUrlChanged(string value)
    {
        var effectiveBaseUrl = BackendEndpointBuilder.NormalizeBaseUrl(value);
        authBackendService.SetBackendBaseUrl(effectiveBaseUrl);
        OnPropertyChanged(nameof(DiagnosticsBackendUrlText));
    }

    partial void OnSelectedStudyLanguageChanged(StudyLanguageDefinition value)
    {
        if (isApplyingBackendSettings)
        {
            return;
        }

        DiagnosticsCopyStatusText = string.Empty;
        SaveCurrentSettingsLocally();
        _ = SaveBackendUserSettingsAsync();
    }

    partial void OnSelectedTutorAvatarOptionChanged(TutorAvatarOption? value)
    {
        if (isApplyingBackendSettings)
        {
            return;
        }

        SelectedSpeechVoiceOption = SpeechVoiceOptions.GetById(SpeechVoiceOptions.GetPreferredVoiceIdForTutor(value?.Id));
        DiagnosticsCopyStatusText = string.Empty;
        SaveCurrentSettingsLocally();
        _ = SaveBackendUserSettingsAsync();
    }

    partial void OnSelectedSpeechVoiceOptionChanged(SpeechVoiceOption value)
    {
        backendSettingsSpeechVoice = SpeechVoiceOptions.GetById(value?.Id).Id;

        if (isApplyingBackendSettings)
        {
            return;
        }

        DiagnosticsCopyStatusText = string.Empty;
        SaveCurrentSettingsLocally();
        _ = SaveBackendUserSettingsAsync();
    }

    partial void OnSelectedAudioInputDeviceOptionChanged(AudioInputDeviceOption? value)
    {
        if (!isRefreshingAudioInputDevices)
        {
            isSelectedAudioInputDeviceUnavailable = false;
            OnPropertyChanged(nameof(DiagnosticsMicrophoneText));
        }
    }

    private DiagnosticBackendStatus BackendStatus
    {
        get => backendStatus;
        set
        {
            if (backendStatus == value)
            {
                return;
            }

            backendStatus = value;
            OnPropertyChanged(nameof(DiagnosticsBackendStatusText));
        }
    }

    private DiagnosticDatabaseStatus DatabaseStatus
    {
        get => databaseStatus;
        set
        {
            if (databaseStatus == value)
            {
                return;
            }

            databaseStatus = value;
            OnPropertyChanged(nameof(DiagnosticsDatabaseStatusText));
        }
    }

    private string DatabaseProviderText
    {
        get => databaseProviderText;
        set
        {
            if (databaseProviderText == value)
            {
                return;
            }

            databaseProviderText = value;
        }
    }

    private string DatabaseErrorText
    {
        get => databaseErrorText;
        set
        {
            if (databaseErrorText == value)
            {
                return;
            }

            databaseErrorText = value;
        }
    }

    private DiagnosticAiStatus AiStatus
    {
        get => aiStatus;
        set
        {
            if (aiStatus == value)
            {
                return;
            }

            aiStatus = value;
            OnPropertyChanged(nameof(DiagnosticsAiStatusText));
        }
    }

    private void SetDiagnosticStatuses(DiagnosticBackendStatus nextBackendStatus, DiagnosticDatabaseStatus nextDatabaseStatus, DiagnosticAiStatus nextAiStatus)
    {
        BackendStatus = nextBackendStatus;
        DatabaseStatus = nextDatabaseStatus;
        AiStatus = nextAiStatus;
    }

    private void RefreshLocalizedText()
    {
        RefreshSelectedTutorAvatarProfileText();
        if (!IsAuthenticated)
        {
            CurrentUserEmail = LocalizeUiText(DefaultAccountSignedOutText);
        }
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(InterfaceLanguageTitle));
        OnPropertyChanged(nameof(NativeLanguageTitle));
        OnPropertyChanged(nameof(NativeLanguageSubtitle));
        OnPropertyChanged(nameof(StudyLanguageTitle));
        OnPropertyChanged(nameof(StudyLanguageSubtitle));
        OnPropertyChanged(nameof(TutorAvatarTitle));
        OnPropertyChanged(nameof(ConnectionTitle));
        OnPropertyChanged(nameof(AudioInputTitle));
        OnPropertyChanged(nameof(MicrophoneLabel));
        OnPropertyChanged(nameof(SystemDefaultMicrophoneText));
        OnPropertyChanged(nameof(RefreshMicrophonesText));
        OnPropertyChanged(nameof(TestMicrophoneText));
        OnPropertyChanged(nameof(BackendUrlLabel));
        OnPropertyChanged(nameof(BackendUrlSubtitle));
        OnPropertyChanged(nameof(TutorAvatarSubtitle));
        OnPropertyChanged(nameof(SpeechVoiceTitle));
        OnPropertyChanged(nameof(SpeechVoiceSubtitle));
        OnPropertyChanged(nameof(AvatarProfileTitle));
        OnPropertyChanged(nameof(AvatarAgeLabel));
        OnPropertyChanged(nameof(AvatarLocationLabel));
        OnPropertyChanged(nameof(AvatarRoleLabel));
        OnPropertyChanged(nameof(AvatarInterestsLabel));
        OnPropertyChanged(nameof(AvatarPersonalityLabel));
        OnPropertyChanged(nameof(AvatarSpeakingStyleLabel));
        OnPropertyChanged(nameof(UserProfileTitle));
        OnPropertyChanged(nameof(UserProfileSubtitle));
        OnPropertyChanged(nameof(UserDisplayNameLabel));
        OnPropertyChanged(nameof(LearningGoalLabel));
        OnPropertyChanged(nameof(LearningStatisticsTitle));
        OnPropertyChanged(nameof(LearningStatisticsSubtitle));
        OnPropertyChanged(nameof(TotalCompletedLessonsLabel));
        OnPropertyChanged(nameof(LessonsTodayLabel));
        OnPropertyChanged(nameof(CurrentStreakLabel));
        OnPropertyChanged(nameof(LastCompletedLessonLabel));
        OnPropertyChanged(nameof(LastCompletedLessonText));
        OnPropertyChanged(nameof(DiagnosticsTitle));
        OnPropertyChanged(nameof(DiagnosticsSubtitle));
        OnPropertyChanged(nameof(DiagnosticsAppVersionLabel));
        OnPropertyChanged(nameof(DiagnosticsBackendUrlLabel));
        OnPropertyChanged(nameof(DiagnosticsBackendStatusLabel));
        OnPropertyChanged(nameof(DiagnosticsDatabaseStatusLabel));
        OnPropertyChanged(nameof(DiagnosticsAiStatusLabel));
        OnPropertyChanged(nameof(DiagnosticsSettingsFileLabel));
        OnPropertyChanged(nameof(DiagnosticsLessonHistoryFileLabel));
        OnPropertyChanged(nameof(DiagnosticsInterfaceLanguageLabel));
        OnPropertyChanged(nameof(DiagnosticsNativeLanguageLabel));
        OnPropertyChanged(nameof(DiagnosticsStudyLanguageLabel));
        OnPropertyChanged(nameof(DiagnosticsTutorAvatarLabel));
        OnPropertyChanged(nameof(DiagnosticsMicrophoneLabel));
        OnPropertyChanged(nameof(DiagnosticsMicrophoneText));
        OnPropertyChanged(nameof(RefreshDiagnosticsButtonText));
        OnPropertyChanged(nameof(CopyDiagnosticsText));
        OnPropertyChanged(nameof(DiagnosticsCopyStatusText));
        OnPropertyChanged(nameof(InstalledAppVersionText));
        OnPropertyChanged(nameof(DiagnosticsBackendStatusText));
        OnPropertyChanged(nameof(DiagnosticsDatabaseStatusText));
        OnPropertyChanged(nameof(DiagnosticsAiStatusText));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(BackButtonText));
        OnPropertyChanged(nameof(LearningTabHeader));
        OnPropertyChanged(nameof(ProgressTabHeader));
        OnPropertyChanged(nameof(AccountTitle));
        OnPropertyChanged(nameof(AccountSubtitle));
        OnPropertyChanged(nameof(CurrentUserEmail));
        OnPropertyChanged(nameof(AccountEmailLabel));
        OnPropertyChanged(nameof(AccountPasswordLabel));
        OnPropertyChanged(nameof(AccountDisplayNameLabel));
        OnPropertyChanged(nameof(AccountRegisterButtonText));
        OnPropertyChanged(nameof(AccountLoginButtonText));
        OnPropertyChanged(nameof(AccountLogoutButtonText));
        OnPropertyChanged(nameof(ForgotPasswordTitle));
        OnPropertyChanged(nameof(ForgotPasswordSubtitle));
        OnPropertyChanged(nameof(ForgotPasswordButtonText));
        OnPropertyChanged(nameof(ResetTokenLabel));
        OnPropertyChanged(nameof(NewPasswordLabel));
        OnPropertyChanged(nameof(ConfirmNewPasswordLabel));
        OnPropertyChanged(nameof(ResetPasswordButtonText));
        OnPropertyChanged(nameof(ChangePasswordTitle));
        OnPropertyChanged(nameof(ChangePasswordSubtitle));
        OnPropertyChanged(nameof(CurrentPasswordLabel));
        OnPropertyChanged(nameof(ChangePasswordButtonText));
        OnPropertyChanged(nameof(CurrentAccountLabel));
        OnPropertyChanged(nameof(SettingsSourceLabel));
        OnPropertyChanged(nameof(SubscriptionStatusTitle));
        OnPropertyChanged(nameof(SubscriptionPlanLabel));
        OnPropertyChanged(nameof(SubscriptionPremiumLabel));
        OnPropertyChanged(nameof(SubscriptionTrialLabel));
        OnPropertyChanged(nameof(SubscriptionFreeLessonLabel));
        OnPropertyChanged(nameof(SubscriptionEnforcementLabel));
        OnPropertyChanged(nameof(SubscriptionSourceLabel));
        OnPropertyChanged(nameof(SubscriptionCheckedAtLabel));
        _ = RefreshSubscriptionStatusAsync();
    }

    private void RefreshAudioInputDevices(string? preferredAudioInputDeviceId, bool showUnavailableStatus)
    {
        var devices = audioInputDeviceService.GetAudioInputDevices(localizedText.SystemDefaultMicrophoneText);
        var selectedDevice = devices.FirstOrDefault(device => string.Equals(device.Id, preferredAudioInputDeviceId, StringComparison.OrdinalIgnoreCase))
            ?? devices.First(device => device.IsDefault);
        var isUnavailable = !audioInputDeviceService.IsSystemDefault(preferredAudioInputDeviceId) && selectedDevice.IsDefault;

        AudioInputDevices.Clear();
        foreach (var device in devices)
        {
            AudioInputDevices.Add(device);
        }

        isRefreshingAudioInputDevices = true;
        try
        {
            isSelectedAudioInputDeviceUnavailable = isUnavailable;
            SelectedAudioInputDeviceOption = selectedDevice;
        }
        finally
        {
            isRefreshingAudioInputDevices = false;
        }

        OnPropertyChanged(nameof(DiagnosticsMicrophoneText));

        if (showUnavailableStatus && isUnavailable)
        {
            StatusMessage = localizedText.SelectedMicrophoneUnavailableMessage;
        }
    }

    private void RefreshSelectedTutorAvatarProfileText()
    {
        OnPropertyChanged(nameof(SelectedTutorAvatarDescription));
        OnPropertyChanged(nameof(SelectedTutorAvatarAgeText));
        OnPropertyChanged(nameof(SelectedTutorAvatarLocation));
        OnPropertyChanged(nameof(SelectedTutorAvatarRole));
        OnPropertyChanged(nameof(SelectedTutorAvatarInterestsText));
        OnPropertyChanged(nameof(SelectedTutorAvatarPersonalityText));
        OnPropertyChanged(nameof(SelectedTutorAvatarSpeakingStyleText));
    }

    private string BuildDiagnosticsMicrophoneText()
    {
        if (isSelectedAudioInputDeviceUnavailable)
        {
            return localizedText.SelectedMicrophoneUnavailableMessage;
        }

        if (SelectedAudioInputDeviceOption?.IsDefault != false)
        {
            return localizedText.SystemDefaultMicrophoneText;
        }

        return string.IsNullOrWhiteSpace(SelectedAudioInputDeviceOption.DisplayName)
            ? localizedText.SelectedMicrophoneUnavailableMessage
            : SelectedAudioInputDeviceOption.DisplayName;
    }

    private async Task<string> BuildDiagnosticsReportAsync()
    {
        var authTokenPresent = false;
        try
        {
            authTokenPresent = await authBackendService.HasStoredSessionAsync();
        }
        catch
        {
            authTokenPresent = false;
        }

        var report = new StringBuilder();
        report.AppendLine(DiagnosticsReportTitle);
        AppendDiagnosticsLine(report, DiagnosticsAppVersionLabel, AppVersionText);
        AppendDiagnosticsLine(report, "Update flow", "Manual confirmation");
        AppendDiagnosticsLine(report, DiagnosticsBackendUrlLabel, DiagnosticsBackendUrlText);
        AppendDiagnosticsLine(report, DiagnosticsBackendStatusLabel, DiagnosticsBackendStatusText);
        AppendDiagnosticsLine(report, "Backend health check", lastBackendHealthResult);
        AppendDiagnosticsLine(report, "Last backend error category", lastBackendErrorCategory);
        AppendDiagnosticsLine(report, "Last backend HTTP status", FormatBackendStatusCode(lastBackendStatusCode));
        AppendDiagnosticsLine(report, DiagnosticsDatabaseStatusLabel, DiagnosticsDatabaseStatusText);
        if (!string.IsNullOrWhiteSpace(DatabaseProviderText))
        {
            AppendDiagnosticsLine(report, "Database provider", DatabaseProviderText);
        }

        if (!string.IsNullOrWhiteSpace(DatabaseErrorText))
        {
            AppendDiagnosticsLine(report, "Database error", DatabaseErrorText);
        }

        AppendDiagnosticsLine(report, DiagnosticsAiStatusLabel, DiagnosticsAiStatusText);
        AppendDiagnosticsLine(report, DiagnosticsSettingsFileLabel, SettingsFilePathText);
        AppendDiagnosticsLine(report, "Backend request diagnostics log", BackendRequestDiagnosticsService.LogFilePath);
        AppendDiagnosticsLine(report, DiagnosticsLessonHistoryFileLabel, LessonHistoryFilePathText);
        AppendDiagnosticsLine(report, DiagnosticsInterfaceLanguageLabel, DiagnosticsInterfaceLanguageText);
        AppendDiagnosticsLine(report, DiagnosticsNativeLanguageLabel, DiagnosticsNativeLanguageText);
        AppendDiagnosticsLine(report, DiagnosticsStudyLanguageLabel, DiagnosticsStudyLanguageText);
        AppendDiagnosticsLine(report, DiagnosticsAuthTokenPresentLabel, FormatDiagnosticBoolean(authTokenPresent));
        AppendDiagnosticsLine(report, DiagnosticsAuthenticatedLabel, FormatDiagnosticBoolean(IsAuthenticated));
        AppendDiagnosticsLine(report, DiagnosticsSessionRestoreAttemptedLabel, FormatDiagnosticBoolean(sessionRestoreAttempted));
        AppendDiagnosticsLine(report, "Backend settings sync", GetBackendSettingsSyncStatusText());
        AppendDiagnosticsLine(report, "Backend settings endpoint", lastBackendSettingsResult);
        AppendDiagnosticsLine(report, "Account status endpoint", lastAccountStatusResult);
        AppendDiagnosticsLine(report, "Last backend settings sync time", GetLastBackendSettingsSyncTimeText());
        AppendDiagnosticsLine(report, DiagnosticsTutorAvatarLabel, DiagnosticsTutorAvatarText);
        AppendDiagnosticsLine(report, DiagnosticsMicrophoneLabel, DiagnosticsMicrophoneText);
        AppendDiagnosticsLine(report, DiagnosticsCurrentDateTimeLabel, DateTimeOffset.Now.ToString("u"));
        report.AppendLine();
        report.AppendLine("Backend request diagnostics log contents:");
        report.AppendLine(await BackendRequestDiagnosticsService.ReadReportAsync());

        return report.ToString().TrimEnd();
    }

    private static string FormatDiagnosticBoolean(bool value) => value ? "yes" : "no";

    private static void AppendDiagnosticsLine(StringBuilder report, string label, string value)
    {
        report.Append(label);
        report.Append(": ");
        report.AppendLine(SanitizeDiagnosticsValue(value));
    }

    private static string SanitizeDiagnosticsValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var sanitizedValue = SanitizeDiagnosticsUri(value);
        sanitizedValue = DiagnosticsBearerTokenPattern.Replace(sanitizedValue, $"Bearer {DiagnosticsMaskedValueText}");
        sanitizedValue = DiagnosticsSensitiveAssignmentPattern.Replace(sanitizedValue, match =>
            $"{match.Groups[1].Value}{DiagnosticsMaskedValueText}{match.Groups[2].Value}");

        return sanitizedValue;
    }

    private static string SanitizeDiagnosticsUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return value;
        }

        if (string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment))
        {
            return value;
        }

        var sanitizedUri = new UriBuilder(uri)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty
        };

        return sanitizedUri.Uri.ToString().TrimEnd('/');
    }

    private async Task LoadSettingsForCurrentSessionAsync()
    {
        var session = await authBackendService.TryRestoreSessionAsync();
        if (session is null)
        {
            await LoadDevelopmentSettingsAsync();
            return;
        }

        await LoadAuthenticatedSettingsAsync(session.AccessToken);
    }

    private async Task LoadAuthenticatedSettingsAsync(string accessToken)
    {
        try
        {
            var result = await backendUserSettingsClient.GetAuthenticatedSettingsAsync(BackendBaseUrl, accessToken);
            if (!result.IsSuccess)
            {
                if (result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await authBackendService.LogoutAsync();
                    ClearAccountState();
                    await LoadDevelopmentSettingsAsync();
                    StatusMessage = BackendUxText.SessionExpired;
                    return;
                }

                RecordOptionalSettingsResult(result.StatusCode);
                SetBackendSettingsSyncStatus(BackendSettingsSyncStatus.Unavailable);
                if (!IsOptionalEndpointMissing(result.StatusCode))
                {
                    StatusMessage = BuildSettingsLoadFailureMessage(result.StatusCode);
                }
                return;
            }

            if (result.Value is null)
            {
                RecordBackendFailure("settings_empty_response", result.StatusCode);
                RecordOptionalSettingsResult(result.StatusCode, "empty response");
                SetBackendSettingsSyncStatus(BackendSettingsSyncStatus.Unavailable);
                StatusMessage = BuildSettingsLoadFailureMessage(result.StatusCode);
                return;
            }

            ApplyBackendUserSettings(result.Value);
            SettingsSource = LocalizeUiText(SettingsSourceAuthenticatedText);
            SaveCurrentSettingsLocally();
            RecordOptionalSettingsResult(null, "success");
            SetBackendSettingsSyncStatus(BackendSettingsSyncStatus.Available);
        }
        catch
        {
            RecordBackendFailure("settings_exception", null);
            RecordOptionalSettingsResult(null, "unavailable");
            SetBackendSettingsSyncStatus(BackendSettingsSyncStatus.Unavailable);
            StatusMessage = BuildSettingsLoadFailureMessage(null);
        }
    }

    private async Task LoadDevelopmentSettingsAsync()
    {
        try
        {
            var result = await backendUserSettingsClient.GetDevelopmentSettingsAsync(BackendBaseUrl);
            if (!result.IsSuccess || result.Value is null)
            {
                if (!IsOptionalEndpointMissing(result.StatusCode))
                {
                    RecordBackendFailure(result.Value is null ? "settings_empty_response" : "settings", result.StatusCode);
                }
                RecordOptionalSettingsResult(result.StatusCode);
                SettingsSource = LocalizeUiText(SettingsSourceDevelopmentText);
                SetBackendSettingsSyncStatus(BackendSettingsSyncStatus.Unavailable);
                if (!IsOptionalEndpointMissing(result.StatusCode))
                {
                    StatusMessage = BuildSettingsLoadFailureMessage(result.StatusCode);
                }
                return;
            }

            ApplyBackendUserSettings(result.Value);
            SettingsSource = LocalizeUiText(SettingsSourceDevelopmentText);
            SaveCurrentSettingsLocally();
            RecordOptionalSettingsResult(null, "success");
            SetBackendSettingsSyncStatus(BackendSettingsSyncStatus.Available);
        }
        catch
        {
            RecordBackendFailure("settings_exception", null);
            RecordOptionalSettingsResult(null, "unavailable");
            SettingsSource = LocalizeUiText(SettingsSourceDevelopmentText);
            SetBackendSettingsSyncStatus(BackendSettingsSyncStatus.Unavailable);
            StatusMessage = BuildSettingsLoadFailureMessage(null);
        }
    }

    private bool TryValidateCredentials(bool requireDisplayName)
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = BackendUxText.CredentialsRequired;
            return false;
        }

        if (requireDisplayName && string.IsNullOrWhiteSpace(DisplayName))
        {
            ErrorMessage = BackendUxText.DisplayNameRequired;
            return false;
        }

        return true;
    }

    private async Task AuthenticateAsync(Func<Task<AuthOperationResult>> authenticateAsync, string failureMessage)
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await authenticateAsync();
            if (result.Status != AuthOperationResultStatus.Success)
            {
                RecordBackendFailure(result.ErrorCategory, result.StatusCode);
                ErrorMessage = BuildAuthFailureMessage(result, failureMessage);
                return;
            }

            if (result.Response is null || result.Response.User is null)
            {
                RecordBackendFailure("unexpected_response", result.StatusCode);
                ErrorMessage = string.IsNullOrWhiteSpace(result.Message) ? failureMessage : result.Message;
                return;
            }

            RecordBackendFailure("none", null);
            ApplyAuthenticatedUser(result.Response.User);
            RequestPasswordClear();
            await RunPostAuthRefreshesAsync();
        }
        catch
        {
            RecordBackendFailure("auth_exception", null);
            ErrorMessage = BackendUxText.CouldNotConnect;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunPostAuthRefreshesAsync()
    {
        try
        {
            await LoadSettingsForCurrentSessionAsync();
        }
        catch
        {
            RecordBackendFailure("settings_exception", null);
            RecordOptionalSettingsResult(null, "unavailable");
            SetBackendSettingsSyncStatus(BackendSettingsSyncStatus.Unavailable);
        }

        try
        {
            await RefreshLearningStatisticsAsync();
        }
        catch
        {
            // Local progress/history refresh is non-blocking after successful authentication.
        }

        try
        {
            await RefreshSubscriptionStatusAsync();
        }
        catch
        {
            RecordAccountStatusResult(null, "unavailable");
            ResetSubscriptionStatus();
        }

        StatusMessage = BackendUxText.SignedIn;
    }

    private async Task RefreshBackendHealthDiagnosticsAsync()
    {
        var diagnosticsResult = await backendDiagnosticsService.CheckAsync(BackendBaseUrl);
        RecordHealthDiagnostics(diagnosticsResult);
        if (!diagnosticsResult.IsBackendHealthy)
        {
            StatusMessage = $"{BackendUxText.CouldNotConnect} Server health: {lastBackendHealthResult}.";
        }
    }

    private void RecordHealthDiagnostics(BackendDiagnosticsResult diagnosticsResult)
    {
        lastBackendHealthResult = diagnosticsResult.IsBackendHealthy
            ? $"healthy ({FormatBackendStatusCode(diagnosticsResult.BackendStatusCode)})"
            : $"unavailable ({diagnosticsResult.ErrorCategory}; {FormatBackendStatusCode(diagnosticsResult.BackendStatusCode)})";
        lastBackendErrorCategory = diagnosticsResult.ErrorCategory;
        lastBackendStatusCode = diagnosticsResult.BackendStatusCode;
        BackendStatus = diagnosticsResult.IsBackendHealthy
            ? DiagnosticBackendStatus.Connected
            : DiagnosticBackendStatus.Unavailable;
    }

    private void RecordBackendFailure(string category, HttpStatusCode? statusCode)
    {
        lastBackendErrorCategory = string.IsNullOrWhiteSpace(category) ? "unknown" : category;
        lastBackendStatusCode = statusCode;
    }

    private void RecordOptionalSettingsResult(HttpStatusCode? statusCode, string? category = null)
    {
        var normalizedCategory = string.IsNullOrWhiteSpace(category)
            ? (IsOptionalEndpointMissing(statusCode) ? "missing optional settings endpoint" : "settings")
            : category;
        lastBackendSettingsResult = $"{normalizedCategory} ({FormatBackendStatusCode(statusCode)})";
        if (!string.Equals(normalizedCategory, "success", StringComparison.OrdinalIgnoreCase)
            && !IsOptionalEndpointMissing(statusCode))
        {
            RecordBackendFailure(normalizedCategory, statusCode);
        }
    }

    private void RecordAccountStatusResult(HttpStatusCode? statusCode, string? category = null)
    {
        var normalizedCategory = string.IsNullOrWhiteSpace(category)
            ? (IsOptionalEndpointMissing(statusCode) ? "missing optional account status endpoint" : "account_status")
            : category;
        lastAccountStatusResult = $"{normalizedCategory} ({FormatBackendStatusCode(statusCode)})";
    }

    private static string FormatBackendStatusCode(HttpStatusCode? statusCode)
    {
        return statusCode is null ? "none" : $"HTTP {(int)statusCode.Value}";
    }

    private string BuildSettingsLoadFailureMessage(HttpStatusCode? statusCode)
    {
        if (IsOptionalEndpointMissing(statusCode))
        {
            return "Cloud settings are not available yet. Local settings are still available.";
        }

        if (statusCode is null)
        {
            return $"{BackendUxText.SettingsLoadUnavailable} Server health: {lastBackendHealthResult}.";
        }

        return $"{BackendUxText.SettingsLoadUnavailable} ({FormatBackendStatusCode(statusCode)}).";
    }

    private static bool IsOptionalEndpointMissing(HttpStatusCode? statusCode)
    {
        return statusCode == HttpStatusCode.NotFound;
    }

    private string BuildAuthFailureMessage(AuthOperationResult result, string fallbackMessage)
    {
        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            return result.Message;
        }

        return result.ErrorCategory switch
        {
            "authorization" => fallbackMessage,
            "validation" => fallbackMessage,
            "server_error" => "The server returned an error. Please try again later.",
            "network" or "timeout" => BackendUxText.CouldNotConnect,
            "unexpected_response" => BackendUxText.BackendUnexpectedResponse,
            _ => fallbackMessage
        };
    }

    private void ApplyAuthenticatedUser(AuthUserDto user)
    {
        CurrentUserEmail = user.Email;
        CurrentUserDisplayName = user.DisplayName ?? string.Empty;
        IsAuthenticated = true;
    }

    private void ClearAccountState()
    {
        CurrentUserEmail = LocalizeUiText(DefaultAccountSignedOutText);
        CurrentUserDisplayName = string.Empty;
        IsAuthenticated = false;
        IsPasswordResetPanelExpanded = false;
        IsChangePasswordPanelExpanded = false;
        RequestPasswordClear();
        ClearPasswordRecoveryFields();
    }

    private void RequestPasswordClear()
    {
        Password = string.Empty;
        ClearPasswordRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ClearPasswordRecoveryFields()
    {
        ResetToken = string.Empty;
        ResetNewPassword = string.Empty;
        ResetConfirmPassword = string.Empty;
        CurrentPassword = string.Empty;
        ChangeNewPassword = string.Empty;
        ChangeConfirmPassword = string.Empty;
        ClearPasswordRecoveryFieldsRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task SaveBackendUserSettingsAsync()
    {
        try
        {
            var request = new UpdateBackendUserSettingsRequest
            {
                StudyLanguage = GetSupportedBackendStudyLanguage(SelectedStudyLanguage),
                ExplanationLanguage = SelectedNativeLanguageOption.Id,
                SpeechVoice = SpeechVoiceOptions.GetById(backendSettingsSpeechVoice).Id,
                SpeechSpeed = backendSettingsSpeechSpeed <= 0
                    ? BackendConstants.DefaultBackendSettingsSpeechSpeed
                    : backendSettingsSpeechSpeed,
                ConversationModeEnabled = backendSettingsConversationModeEnabled
            };

            var session = await authBackendService.TryRestoreSessionAsync();
            var result = session is null
                ? await backendUserSettingsClient.UpdateDevelopmentSettingsAsync(BackendBaseUrl, request)
                : await backendUserSettingsClient.UpdateAuthenticatedSettingsAsync(BackendBaseUrl, session.AccessToken, request);

            if (!result.IsSuccess)
            {
                if (result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await authBackendService.LogoutAsync();
                    ClearAccountState();
                    await LoadDevelopmentSettingsAsync();
                    StatusMessage = BackendUxText.SessionExpired;
                    return;
                }

                SetBackendSettingsSyncStatus(BackendSettingsSyncStatus.Unavailable);
                RecordOptionalSettingsResult(result.StatusCode);
                StatusMessage = IsOptionalEndpointMissing(result.StatusCode)
                    ? BuildSettingsLoadFailureMessage(result.StatusCode)
                    : BackendUxText.SettingsSaveUnavailable;
                return;
            }

            if (result.Value is null)
            {
                SetBackendSettingsSyncStatus(BackendSettingsSyncStatus.Unavailable);
                StatusMessage = BackendUxText.SettingsSaveUnavailable;
                return;
            }

            ApplyBackendUserSettings(result.Value);
            SettingsSource = session is null ? LocalizeUiText(SettingsSourceDevelopmentText) : LocalizeUiText(SettingsSourceAuthenticatedText);
            SaveCurrentSettingsLocally();
            SetBackendSettingsSyncStatus(BackendSettingsSyncStatus.Available);
        }
        catch
        {
            SetBackendSettingsSyncStatus(BackendSettingsSyncStatus.Unavailable);
            StatusMessage = BackendUxText.SettingsSaveUnavailable;
        }
    }

    private async Task RefreshSubscriptionStatusAsync()
    {
        try
        {
            var session = await authBackendService.TryRestoreSessionAsync();
            if (session is null)
            {
                ApplySignedOutSubscriptionStatus();
                return;
            }

            var result = await backendSubscriptionStatusClient.GetAsync(BackendBaseUrl);
            if (!result.IsSuccess || result.Value is null)
            {
                RecordAccountStatusResult(result.StatusCode);
                ResetSubscriptionStatus();
                return;
            }

            var status = result.Value;
            SubscriptionPlanText = $"{localizedText.SubscriptionPlanLabel}: {(!string.IsNullOrWhiteSpace(status.PlanName) ? status.PlanName : LocalizeUiText("Free"))}";
            SubscriptionPremiumText = $"{localizedText.SubscriptionPremiumLabel}: {(status.PremiumActive ? LocalizeUiText("Active") : LocalizeUiText("Not active"))}";
            SubscriptionTrialText = status.TrialActive && status.TrialEndsAtUtc is not null
                ? $"{localizedText.SubscriptionTrialLabel}: {LocalizeUiText("Active until")} {status.TrialEndsAtUtc.Value:u}"
                : $"{localizedText.SubscriptionTrialLabel}: {LocalizeUiText("Not active")}";
            SubscriptionFreeLessonText = status.FreeLessonUsedToday
                ? $"{localizedText.SubscriptionFreeLessonLabel}: {LocalizeUiText("Used")}"
                : $"{localizedText.SubscriptionFreeLessonLabel}: {Math.Max(status.FreeLessonRemainingToday, 0)} {LocalizeUiText("remaining")}";
            SubscriptionEnforcementText = $"{localizedText.SubscriptionEnforcementLabel}: {(status.EnforcementEnabled ? LocalizeUiText("On") : LocalizeUiText("Off"))}";
            SubscriptionSourceText = $"{localizedText.SubscriptionSourceLabel}: {LocalizeUiText("authenticated")}";
            SubscriptionCheckedAtText = $"{localizedText.SubscriptionCheckedAtLabel}: {status.CheckedAtUtc:u}";
            RecordAccountStatusResult(null, "success");
        }
        catch
        {
            RecordAccountStatusResult(null, "unavailable");
            ResetSubscriptionStatus();
        }
    }

    private void ApplySignedOutSubscriptionStatus()
    {
        SubscriptionPlanText = LocalizeUiText(SignedOutSubscriptionPromptText);
        SubscriptionPremiumText = SignedOutSubscriptionPlaceholderText;
        SubscriptionTrialText = SignedOutSubscriptionPlaceholderText;
        SubscriptionFreeLessonText = SignedOutSubscriptionPlaceholderText;
        SubscriptionEnforcementText = SignedOutSubscriptionPlaceholderText;
        SubscriptionSourceText = SignedOutSubscriptionPlaceholderText;
        SubscriptionCheckedAtText = SignedOutSubscriptionPlaceholderText;
    }

    private void ResetSubscriptionStatus()
    {
        SubscriptionPlanText = LocalizeUiText(SubscriptionStatusUnavailableText);
        SubscriptionPremiumText = LocalizeUiText(SubscriptionStatusUnavailableText);
        SubscriptionTrialText = LocalizeUiText(SubscriptionStatusUnavailableText);
        SubscriptionFreeLessonText = LocalizeUiText(SubscriptionStatusUnavailableText);
        SubscriptionEnforcementText = LocalizeUiText(SubscriptionStatusUnavailableText);
        SubscriptionSourceText = LocalizeUiText(SubscriptionStatusUnavailableText);
        SubscriptionCheckedAtText = LocalizeUiText(SubscriptionStatusUnavailableText);
    }

    private void ApplyBackendUserSettings(BackendUserSettingsResponse settings)
    {
        backendSettingsSpeechVoice = string.IsNullOrWhiteSpace(settings.SpeechVoice)
            ? SpeechVoiceOptions.GetPreferredVoiceIdForTutor(SelectedTutorAvatarOption?.Id)
            : SpeechVoiceOptions.GetById(settings.SpeechVoice).Id;
        backendSettingsSpeechSpeed = settings.SpeechSpeed <= 0
            ? BackendConstants.DefaultBackendSettingsSpeechSpeed
            : settings.SpeechSpeed;
        backendSettingsConversationModeEnabled = settings.ConversationModeEnabled;

        var backendStudyLanguage = GetStudyLanguageByBackendValue(settings.StudyLanguage);
        isApplyingBackendSettings = true;
        try
        {
            SelectedStudyLanguage = backendStudyLanguage;
            SelectedSpeechVoiceOption = SpeechVoiceOptions.GetById(backendSettingsSpeechVoice);
        }
        finally
        {
            isApplyingBackendSettings = false;
        }
    }

    private void SaveCurrentSettingsLocally()
    {
        var selectedAvatar = SelectedTutorAvatarOption ?? TutorAvatarOptions.Elena;
        var selectedAudioInputDeviceId = SelectedAudioInputDeviceOption?.Id ?? AudioConstants.DefaultAudioInputDeviceId;
        saveSettings(SelectedInterfaceLanguageId, SelectedNativeLanguageOption.Id, SelectedStudyLanguage.Id, selectedAvatar.Id, SelectedSpeechVoiceOption.Id, UserDisplayName, LearningGoal, BackendBaseUrl, selectedAudioInputDeviceId);
    }

    private void SetBackendSettingsSyncStatus(BackendSettingsSyncStatus status)
    {
        backendSettingsSyncStatus = status;
        if (status == BackendSettingsSyncStatus.Available)
        {
            lastBackendSettingsSyncTime = DateTimeOffset.Now;
        }
    }

    private string GetBackendSettingsSyncStatusText()
    {
        return backendSettingsSyncStatus switch
        {
            BackendSettingsSyncStatus.Available => "available",
            BackendSettingsSyncStatus.Unavailable => "unavailable",
            _ => "not checked"
        };
    }

    private string GetLastBackendSettingsSyncTimeText()
    {
        return lastBackendSettingsSyncTime?.ToString("u") ?? "not checked";
    }

    private static string GetSupportedBackendStudyLanguage(StudyLanguageDefinition? studyLanguage)
    {
        var supportedLanguage = StudyLanguageCatalog.GetById(studyLanguage?.Id);
        return supportedLanguage.EnglishName;
    }

    private static StudyLanguageDefinition GetStudyLanguageByBackendValue(string? backendStudyLanguage)
    {
        if (string.IsNullOrWhiteSpace(backendStudyLanguage))
        {
            return StudyLanguageCatalog.English;
        }

        return StudyLanguageCatalog.All.FirstOrDefault(language =>
                string.Equals(language.EnglishName, backendStudyLanguage.Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(language.Id, backendStudyLanguage.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? StudyLanguageCatalog.English;
    }

    private string LocalizeBackendStatus(DiagnosticBackendStatus status)
    {
        return status switch
        {
            DiagnosticBackendStatus.Connected => diagnosticsLocalizedText.ConnectedStatus,
            DiagnosticBackendStatus.Unavailable => diagnosticsLocalizedText.UnavailableStatus,
            DiagnosticBackendStatus.Checking => diagnosticsLocalizedText.CheckingStatus,
            _ => diagnosticsLocalizedText.UnknownStatus
        };
    }

    private string LocalizeDatabaseStatus(DiagnosticDatabaseStatus status)
    {
        return status switch
        {
            DiagnosticDatabaseStatus.Healthy => diagnosticsLocalizedText.ConnectedStatus,
            DiagnosticDatabaseStatus.Unavailable => diagnosticsLocalizedText.UnavailableStatus,
            DiagnosticDatabaseStatus.Checking => diagnosticsLocalizedText.CheckingStatus,
            _ => diagnosticsLocalizedText.UnknownStatus
        };
    }

    private string LocalizeAiStatus(DiagnosticAiStatus status)
    {
        return status switch
        {
            DiagnosticAiStatus.Configured => diagnosticsLocalizedText.ConfiguredStatus,
            DiagnosticAiStatus.NotConfigured => diagnosticsLocalizedText.NotConfiguredStatus,
            DiagnosticAiStatus.Unavailable => diagnosticsLocalizedText.UnavailableStatus,
            DiagnosticAiStatus.Checking => diagnosticsLocalizedText.CheckingStatus,
            _ => diagnosticsLocalizedText.UnknownStatus
        };
    }

    private static DiagnosticAiStatus MapAiStatus(BackendConfigStatusResponse? configStatus)
    {
        if (configStatus is null)
        {
            return DiagnosticAiStatus.Unavailable;
        }

        if (string.Equals(configStatus.OpenAiStatus, BackendConstants.OpenAiConfiguredStatus, StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticAiStatus.Configured;
        }

        if (string.Equals(configStatus.OpenAiStatus, OpenAiNotConfiguredStatus, StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticAiStatus.NotConfigured;
        }

        return string.IsNullOrWhiteSpace(configStatus.OpenAiStatus)
            ? DiagnosticAiStatus.Unknown
            : DiagnosticAiStatus.NotConfigured;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes <= 0)
        {
            return "—";
        }

        var value = bytes;
        string[] units = ["B", "KB", "MB", "GB"];
        var unitIndex = 0;
        var displayValue = (double)value;
        while (displayValue >= 1024 && unitIndex < units.Length - 1)
        {
            displayValue /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{bytes} {units[unitIndex]}"
            : $"{displayValue:0.0} {units[unitIndex]}";
    }

    private async Task RefreshLearningStatisticsAsync()
    {
        var lessonHistory = await lessonHistoryService.LoadVisibleCompletedLessonsForCurrentSessionAsync();
        ApplyLearningStatistics(lessonHistory);
    }

    private void ApplyLearningStatistics(IReadOnlyList<LessonHistoryItem> lessonHistory)
    {
        latestLesson = lessonHistory
            .OrderByDescending(item => item.CompletedAt)
            .FirstOrDefault();
        TotalCompletedLessonsText = lessonHistory.Count.ToString();
        LessonsTodayText = CountLessonsToday(lessonHistory).ToString();
        CurrentStreakText = CalculateCurrentStreak(lessonHistory).ToString();
        OnPropertyChanged(nameof(LastCompletedLessonText));
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

    private static string BuildLastCompletedLessonText(LessonHistoryItem? latestLesson, string noCompletedLessonsText)
    {
        if (latestLesson is null)
        {
            return noCompletedLessonsText;
        }

        var lessonTitle = string.Join(" — ", new[] { latestLesson.TopicTitle, latestLesson.SubtopicTitle }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var completedAtText = latestLesson.CompletedAt.ToString("g");

        return string.IsNullOrWhiteSpace(lessonTitle)
            ? completedAtText
            : $"{completedAtText} · {lessonTitle}";
    }

    private enum DiagnosticBackendStatus
    {
        Unknown,
        Checking,
        Connected,
        Unavailable
    }

    private enum DiagnosticDatabaseStatus
    {
        Unknown,
        Checking,
        Healthy,
        Unavailable
    }

    private enum BackendSettingsSyncStatus
    {
        NotChecked,
        Available,
        Unavailable
    }

    private enum DiagnosticAiStatus
    {
        Unknown,
        Checking,
        Configured,
        NotConfigured,
        Unavailable
    }
}

public enum SettingsSection
{
    Learning,
    Account,
    Audio,
    Progress
}
