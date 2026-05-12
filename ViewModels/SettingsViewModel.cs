using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Localization;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private const string AppVersionFallbackText = "local build";
    private const string OpenAiNotConfiguredStatus = "not_configured";

    private readonly Action<string, string, string, string, string, string, string> saveSettings;
    private readonly Action navigateBack;
    private readonly LessonChatBackendService lessonChatBackendService;
    private readonly AudioInputDeviceService audioInputDeviceService;
    private readonly AudioRecordingService audioRecordingService;
    private readonly LessonHistoryItem? latestLesson;
    private readonly string appVersionText;
    private readonly string settingsFilePathText;
    private readonly string lessonHistoryFilePathText;
    private SettingsLocalizedText localizedText;
    private DiagnosticsLocalizedText diagnosticsLocalizedText;
    private DiagnosticBackendStatus backendStatus = DiagnosticBackendStatus.Unknown;
    private DiagnosticAiStatus aiStatus = DiagnosticAiStatus.Unknown;

    public string Title => localizedText.Title;

    public string Subtitle => localizedText.Subtitle;

    public string InterfaceLanguageTitle => localizedText.InterfaceLanguageTitle;

    public string NativeLanguageTitle => localizedText.NativeLanguageTitle;

    public string NativeLanguageSubtitle => localizedText.NativeLanguageSubtitle;

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

    public string DiagnosticsSubtitle => diagnosticsLocalizedText.Subtitle;

    public string DiagnosticsAppVersionLabel => diagnosticsLocalizedText.AppVersionLabel;

    public string DiagnosticsBackendUrlLabel => diagnosticsLocalizedText.BackendUrlLabel;

    public string DiagnosticsBackendStatusLabel => diagnosticsLocalizedText.BackendStatusLabel;

    public string DiagnosticsAiStatusLabel => diagnosticsLocalizedText.AiStatusLabel;

    public string DiagnosticsSettingsFileLabel => diagnosticsLocalizedText.SettingsFileLabel;

    public string DiagnosticsLessonHistoryFileLabel => diagnosticsLocalizedText.LessonHistoryFileLabel;

    public string DiagnosticsInterfaceLanguageLabel => diagnosticsLocalizedText.InterfaceLanguageLabel;

    public string DiagnosticsNativeLanguageLabel => diagnosticsLocalizedText.NativeLanguageLabel;

    public string DiagnosticsTutorAvatarLabel => diagnosticsLocalizedText.TutorAvatarLabel;

    public string RefreshDiagnosticsButtonText => diagnosticsLocalizedText.RefreshButtonText;

    public string AppVersionText => appVersionText;

    public string DiagnosticsBackendUrlText => BackendEndpointBuilder.NormalizeBaseUrl(BackendBaseUrl);

    public string DiagnosticsBackendStatusText => LocalizeBackendStatus(backendStatus);

    public string DiagnosticsAiStatusText => LocalizeAiStatus(aiStatus);

    public string SettingsFilePathText => settingsFilePathText;

    public string LessonHistoryFilePathText => lessonHistoryFilePathText;

    public string DiagnosticsInterfaceLanguageText => SelectedInterfaceLanguageOption.DisplayName;

    public string DiagnosticsNativeLanguageText => SelectedNativeLanguage;

    public string DiagnosticsTutorAvatarText => SelectedTutorAvatarDisplayName;

    public string MicrophoneStatusText => StatusMessage;

    public string SaveButtonText => localizedText.SaveButtonText;

    public string BackButtonText => localizedText.BackButtonText;

    public string TotalCompletedLessonsText { get; }

    public string LessonsTodayText { get; }

    public string CurrentStreakText { get; }

    public string LastCompletedLessonText => BuildLastCompletedLessonText(latestLesson, localizedText.NoCompletedLessonsText);

    public IReadOnlyList<InterfaceLanguageOption> AvailableInterfaceLanguages { get; } = InterfaceLanguageOptions.All;

    public IReadOnlyList<string> SupportedNativeLanguages { get; } = AppConstants.SupportedNativeLanguages;

    public IReadOnlyList<TutorAvatarOption> AvailableTutorAvatars { get; } = TutorAvatarOptions.All;

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
    private string selectedNativeLanguage;

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
    private AudioInputDeviceOption? selectedAudioInputDeviceOption;

    public SettingsViewModel(
        string currentInterfaceLanguageId,
        string currentNativeLanguage,
        string currentTutorAvatarId,
        string currentUserDisplayName,
        string currentLearningGoal,
        string currentBackendBaseUrl,
        string currentAudioInputDeviceId,
        string settingsFilePath,
        string lessonHistoryFilePath,
        IReadOnlyList<LessonHistoryItem> lessonHistory,
        LessonChatBackendService lessonChatBackendService,
        AudioInputDeviceService audioInputDeviceService,
        AudioRecordingService audioRecordingService,
        Action<string, string, string, string, string, string, string> saveSettings,
        Action navigateBack)
    {
        selectedInterfaceLanguageOption = InterfaceLanguageOptions.GetById(currentInterfaceLanguageId);
        localizedText = SettingsLocalization.GetSettingsText(selectedInterfaceLanguageOption.Id);
        diagnosticsLocalizedText = DiagnosticsLocalization.GetText(selectedInterfaceLanguageOption.Id);
        selectedNativeLanguage = currentNativeLanguage;
        selectedTutorAvatarOption = TutorAvatarOptions.GetById(currentTutorAvatarId);
        userDisplayName = currentUserDisplayName;
        learningGoal = currentLearningGoal;
        backendBaseUrl = currentBackendBaseUrl;
        settingsFilePathText = settingsFilePath;
        lessonHistoryFilePathText = lessonHistoryFilePath;
        appVersionText = BuildAppVersionText();
        this.lessonChatBackendService = lessonChatBackendService;
        this.audioInputDeviceService = audioInputDeviceService;
        this.audioRecordingService = audioRecordingService;
        this.saveSettings = saveSettings;
        this.navigateBack = navigateBack;

        latestLesson = lessonHistory
            .OrderByDescending(item => item.CompletedAt)
            .FirstOrDefault();
        TotalCompletedLessonsText = lessonHistory.Count.ToString();
        LessonsTodayText = CountLessonsToday(lessonHistory).ToString();
        CurrentStreakText = CalculateCurrentStreak(lessonHistory).ToString();
        RefreshAudioInputDevices(currentAudioInputDeviceId, showUnavailableStatus: false);
    }

    [RelayCommand]
    private void Save()
    {
        var selectedAvatar = SelectedTutorAvatarOption ?? TutorAvatarOptions.Elena;
        var selectedAudioInputDeviceId = SelectedAudioInputDeviceOption?.Id ?? AudioConstants.DefaultAudioInputDeviceId;
        saveSettings(SelectedInterfaceLanguageId, SelectedNativeLanguage, selectedAvatar.Id, UserDisplayName, LearningGoal, BackendBaseUrl, selectedAudioInputDeviceId);
        BackendBaseUrl = BackendEndpointBuilder.NormalizeBaseUrl(BackendBaseUrl);
        StatusMessage = localizedText.SettingsSavedMessage;
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

    [RelayCommand]
    private async Task RefreshDiagnosticsAsync()
    {
        SetDiagnosticStatuses(DiagnosticBackendStatus.Checking, DiagnosticAiStatus.Checking);

        var isBackendHealthy = await lessonChatBackendService.CheckHealthAsync(BackendBaseUrl);
        if (!isBackendHealthy)
        {
            SetDiagnosticStatuses(DiagnosticBackendStatus.Unavailable, DiagnosticAiStatus.Unavailable);
            return;
        }

        BackendStatus = DiagnosticBackendStatus.Connected;
        var configStatus = await lessonChatBackendService.GetBackendConfigStatusAsync(BackendBaseUrl);
        AiStatus = MapAiStatus(configStatus);
    }

    partial void OnSelectedInterfaceLanguageOptionChanged(InterfaceLanguageOption value)
    {
        localizedText = SettingsLocalization.GetSettingsText(value.Id);
        diagnosticsLocalizedText = DiagnosticsLocalization.GetText(value.Id);
        RefreshLocalizedText();

        RefreshAudioInputDevices(SelectedAudioInputDeviceOption?.Id, showUnavailableStatus: false);

        if (!string.IsNullOrWhiteSpace(StatusMessage))
        {
            StatusMessage = localizedText.SettingsSavedMessage;
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

    private void SetDiagnosticStatuses(DiagnosticBackendStatus nextBackendStatus, DiagnosticAiStatus nextAiStatus)
    {
        BackendStatus = nextBackendStatus;
        AiStatus = nextAiStatus;
    }

    private void RefreshLocalizedText()
    {
        RefreshSelectedTutorAvatarProfileText();
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(InterfaceLanguageTitle));
        OnPropertyChanged(nameof(NativeLanguageTitle));
        OnPropertyChanged(nameof(NativeLanguageSubtitle));
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
        OnPropertyChanged(nameof(DiagnosticsAiStatusLabel));
        OnPropertyChanged(nameof(DiagnosticsSettingsFileLabel));
        OnPropertyChanged(nameof(DiagnosticsLessonHistoryFileLabel));
        OnPropertyChanged(nameof(DiagnosticsInterfaceLanguageLabel));
        OnPropertyChanged(nameof(DiagnosticsNativeLanguageLabel));
        OnPropertyChanged(nameof(DiagnosticsTutorAvatarLabel));
        OnPropertyChanged(nameof(RefreshDiagnosticsButtonText));
        OnPropertyChanged(nameof(DiagnosticsBackendStatusText));
        OnPropertyChanged(nameof(DiagnosticsAiStatusText));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(BackButtonText));
    }

    private void RefreshAudioInputDevices(string? preferredAudioInputDeviceId, bool showUnavailableStatus)
    {
        var devices = audioInputDeviceService.GetAudioInputDevices(localizedText.SystemDefaultMicrophoneText);
        var selectedDevice = devices.FirstOrDefault(device => string.Equals(device.Id, preferredAudioInputDeviceId, StringComparison.OrdinalIgnoreCase))
            ?? devices.First(device => device.IsDefault);

        AudioInputDevices.Clear();
        foreach (var device in devices)
        {
            AudioInputDevices.Add(device);
        }

        SelectedAudioInputDeviceOption = selectedDevice;

        if (showUnavailableStatus && !audioInputDeviceService.IsSystemDefault(preferredAudioInputDeviceId) && selectedDevice.IsDefault)
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

    private static string BuildAppVersionText()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version is null)
        {
            return AppVersionFallbackText;
        }

        var versionText = version.ToString(fieldCount: 3);
        return string.IsNullOrWhiteSpace(versionText) ? AppVersionFallbackText : versionText;
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

    private enum DiagnosticAiStatus
    {
        Unknown,
        Checking,
        Configured,
        NotConfigured,
        Unavailable
    }
}
