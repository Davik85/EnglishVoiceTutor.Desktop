using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Localization;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Models.LessonContent;
using EnglishVoiceTutor.Desktop.Services;
using EnglishVoiceTutor.Desktop.Services.Voice;
using EnglishVoiceTutor.Shared.LessonPolicies;
using System.Windows;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class LessonChatViewModel : ViewModelBase
{
    private readonly Action navigateBack;
    private readonly Action<LessonSummaryInput> finishLesson;
    private readonly string nativeLanguageName;
    private readonly string tutorAvatarId;
    private readonly LessonChatBackendService lessonChatBackendService;
    private readonly AudioRecordingService audioRecordingService;
    private readonly AudioPlaybackService audioPlaybackService;
    private readonly BotVoiceTempFileCleanupService botVoiceTempFileCleanupService;
    private readonly IVoiceConversationEngine realtimeVoiceEngine;
    private readonly RealtimeAudioPlaybackService realtimeAudioPlaybackService;
    private readonly RealtimeMicrophoneCaptureService realtimeMicrophoneCaptureService;
    private readonly string audioInputDeviceId;
    private readonly AppLocalizedText localizedText;
    private readonly LessonScenario lessonScenario;
    private readonly LevelProfile activeLevelProfile;
    private readonly TutorProfile tutorProfile;
    private int messageCounter;
    private string lastBotMessage = AppConstants.MockBotFirstMessage;
    private ContextVariant? selectedContextVariant;
    private string selectedCustomContextTitle = string.Empty;
    private bool isTranscribingAudio;
    private bool hasFinishedLesson;
    private readonly SemaphoreSlim botVoiceSemaphore = new(1, 1);
    private readonly Dictionary<string, string> botVoiceSegmentAudioFilePaths = [];
    private readonly Dictionary<string, Task<string>> inFlightBotVoiceSegmentTasks = [];
    private readonly object botVoiceSegmentCacheLock = new();
    private readonly HashSet<string> currentSessionBotVoiceFilePaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly object botVoiceCancellationLock = new();
    private CancellationTokenSource? currentBotVoiceCancellationTokenSource;
    private string currentBotVoiceCancellationReason = BotVoiceCancellationReasons.AppDisposalCancel;
    private bool isRealtimeSessionStarted;
    private bool isStartingRealtimeSession;
    private const string RealtimeVoicePendingText = LessonTranscriptValidator.VoiceMessagePlaceholder;
    private const string RealtimeVoiceTranscriptionUnavailableText = LessonTranscriptValidator.InvalidTranscriptUserMessage;
    private ChatMessageViewModel? realtimeAssistantMessage;
    private ChatMessageViewModel? realtimeUserPlaceholderMessage;
    private string realtimeUserPlaceholderItemId = string.Empty;
    private readonly StringBuilder realtimeUserTranscriptBuffer = new();
    private readonly Dictionary<string, int> realtimeItemIdToChatMessageId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> pendingTranscriptByItemId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> pendingTranscriptFailureByItemId = new(StringComparer.Ordinal);
    private string realtimeSessionId = Guid.NewGuid().ToString("N");

    public string SelectedLevel { get; }

    public Topic SelectedTopic { get; }

    public Subtopic SelectedSubtopic { get; }

    public string Title => localizedText.LessonChatTitle;

    public string ContextText => $"{localizedText.TopicContextLabel} {SelectedTopic.DisplayTitle} • {localizedText.SituationContextLabel} {SelectedSubtopic.DisplayTitle} • {localizedText.LevelContextLabel} {SelectedLevel}";

    public string FeedbackTranslationHeader => $"{localizedText.FeedbackTranslationLabel} ({nativeLanguageName})";

    public string TutorAvatarDisplayName { get; }

    public string UserDisplayName { get; }

    public string LearningGoal { get; }

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string userInput = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCurrentHint))]
    private string currentHintText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedFeedback))]
    private Feedback? selectedFeedback;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedbackTranslateButtonText))]
    private bool isFeedbackTranslationVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BotStatusText))]
    [NotifyPropertyChangedFor(nameof(BotStatusDisplayText))]
    [NotifyPropertyChangedFor(nameof(BotStatusIndicatorBrush))]
    private string botStatus = BackendConstants.BotStatusReady;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackendStatusIndicatorBrush))]
    private string backendStatusText = BackendConstants.BackendStatusChecking;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AiStatusIndicatorBrush))]
    private string aiStatusText = BackendConstants.AiStatusChecking;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleVoiceRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(FinishLessonCommand))]
    [NotifyCanExecuteChangedFor(nameof(HintCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleConversationModeCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    private bool isSending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VoiceButtonText))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleVoiceRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(FinishLessonCommand))]
    [NotifyCanExecuteChangedFor(nameof(HintCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleConversationModeCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    private bool isRecording;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayBotVoiceCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleVoiceRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(FinishLessonCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(HintCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleConversationModeCommand))]
    private bool isBotVoicePlaying;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLessonLimitReached))]
    [NotifyPropertyChangedFor(nameof(IsLessonWrappingUp))]
    [NotifyPropertyChangedFor(nameof(IsLessonInputEnabled))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleVoiceRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(HintCommand))]
    [NotifyCanExecuteChangedFor(nameof(FinishLessonCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleConversationModeCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(PlayBotVoiceCommand))]
    private int learnerTurnCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLessonInputEnabled))]
    [NotifyPropertyChangedFor(nameof(IsLessonOptionsEnabled))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleVoiceRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(HintCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleConversationModeCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(PlayBotVoiceCommand))]
    [NotifyCanExecuteChangedFor(nameof(ViewFeedbackCommand))]
    [NotifyCanExecuteChangedFor(nameof(FinishLessonCommand))]
    private bool isLessonCompleteAwaitingFinish;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvatarStateDisplayText))]
    [NotifyPropertyChangedFor(nameof(AvatarAnimationAssetPath))]
    [NotifyPropertyChangedFor(nameof(AvatarAnimationAssetUri))]
    private AvatarState currentAvatarState = AvatarState.Idle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConversationModeButtonText))]
    [NotifyCanExecuteChangedFor(nameof(ToggleConversationModeCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleVoiceRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(HintCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(FinishLessonCommand))]
    [NotifyCanExecuteChangedFor(nameof(PlayBotVoiceCommand))]
    private bool isConversationModeEnabled;

    [ObservableProperty]
    private bool isVoiceAutoSendEnabled;

    [ObservableProperty]
    private bool isBotVoiceAutoPlayEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLessonInputEnabled))]
    [NotifyPropertyChangedFor(nameof(IsLessonOptionsEnabled))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleVoiceRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(HintCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleConversationModeCommand))]
    [NotifyCanExecuteChangedFor(nameof(FinishLessonCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(PlayBotVoiceCommand))]
    private LessonPhase currentLessonPhase = LessonPhase.SetupContextSelection;

    public bool HasSelectedFeedback => SelectedFeedback is not null;

    public bool HasCurrentHint => !string.IsNullOrWhiteSpace(CurrentHintText);

    public bool IsLessonLimitReached => LearnerTurnCount >= GetFinalTurn();

    public bool IsLessonWrappingUp => LearnerTurnCount >= GetSoftWrapUpTurn();

    public bool IsLessonInputEnabled => CanAcceptLessonInput;

    public bool IsLessonOptionsEnabled => !hasFinishedLesson && !IsCompletedAwaitingFinish && !IsLessonLimitReached;

    public string VoiceButtonText => IsRecording
        ? localizedText.StopRecordingButtonText
        : localizedText.StartRecordingButtonText;

    public string FeedbackTranslateButtonText => IsFeedbackTranslationVisible
        ? localizedText.FeedbackHideTranslationButtonText
        : localizedText.FeedbackTranslateButtonText;

    public string BotStatusText => $"{localizedText.BotStatusLabel} {BotStatusDisplayText}";

    public string BotStatusDisplayText => BotStatus == BackendConstants.BotStatusThinking
        ? localizedText.BotStatusThinking
        : localizedText.BotStatusReady;

    public string BotStatusIndicatorBrush => BotStatus == BackendConstants.BotStatusReady
        ? BackendConstants.StatusIndicatorReadyBrush
        : BackendConstants.StatusIndicatorCheckingBrush;

    public string BackendStatusIndicatorBrush => BackendStatusText switch
    {
        BackendConstants.BackendStatusConnected => BackendConstants.StatusIndicatorReadyBrush,
        BackendConstants.BackendStatusChecking => BackendConstants.StatusIndicatorCheckingBrush,
        _ => BackendConstants.StatusIndicatorUnavailableBrush
    };

    public string AiStatusIndicatorBrush => AiStatusText.StartsWith(BackendConstants.AiStatusConfiguredPrefix, StringComparison.OrdinalIgnoreCase)
        ? BackendConstants.StatusIndicatorReadyBrush
        : AiStatusText == BackendConstants.AiStatusChecking
            ? BackendConstants.StatusIndicatorCheckingBrush
            : BackendConstants.StatusIndicatorUnavailableBrush;

    public string LatestBotMessageText => lastBotMessage;

    public string ConversationModeButtonText => IsConversationModeEnabled
        ? localizedText.BackToChatButtonText
        : localizedText.ConversationModeButtonText;

    public string AvatarStateDisplayText => AvatarConstants.GetDisplayText(CurrentAvatarState);

    public string AvatarAnimationAssetPath => AvatarConstants.GetAnimationPath(CurrentAvatarState);

    public Uri AvatarAnimationAssetUri => AvatarConstants.ToPackUri(AvatarAnimationAssetPath);


    public string SendButtonText => localizedText.SendButtonText;

    public string HintButtonText => localizedText.HintButtonText;

    public string AutoSendVoiceLabel => localizedText.AutoSendVoiceLabel;

    public string AutoSendVoiceToolTip => localizedText.AutoSendVoiceToolTip;

    public string AutoPlayBotVoiceLabel => localizedText.AutoPlayBotVoiceLabel;

    public string AutoPlayBotVoiceToolTip => localizedText.AutoPlayBotVoiceToolTip;

    public string FinishLessonButtonText => localizedText.FinishLessonButtonText;

    public string BackButtonText => localizedText.BackButtonText;

    public string BackToChatToolTip => localizedText.BackToChatToolTip;

    public string PlayVoiceButtonText => localizedText.PlayVoiceButtonText;

    public string ViewFeedbackButtonText => localizedText.ViewFeedbackButtonText;

    public string TranslationLabel => localizedText.TranslationLabel;

    public string HintPanelTitle => localizedText.HintPanelTitle;

    public string ClickToCloseText => localizedText.ClickToCloseText;

    public string FeedbackPanelTitle => localizedText.FeedbackPanelTitle;

    public string FeedbackCorrectedVersionTitle => localizedText.FeedbackCorrectedVersionTitle;

    public string FeedbackGrammarTipTitle => localizedText.FeedbackGrammarTipTitle;

    public string FeedbackVocabularyTipTitle => localizedText.FeedbackVocabularyTipTitle;

    public string FeedbackCultureTipTitle => localizedText.FeedbackCultureTipTitle;

    public string FeedbackNaturalVersionTitle => localizedText.FeedbackNaturalVersionTitle;

    private bool ShouldAutoSendTranscribedVoiceResult()
    {
        return CanAcceptTranscriptionResult && (IsConversationModeEnabled || IsVoiceAutoSendEnabled);
    }

    // Lesson chat deterministic state table (Stage 1 stabilization):
    //
    // SetupContextSelection
    // - Guided lessons only. Scenario/context has not been selected yet.
    // - User messages are context selection messages, not lesson turns; LearnerTurnCount must not increment here.
    // - Send enabled when text is present and not busy. Start recording enabled unless busy.
    // - Hint enabled in setup to show context-selection guidance when not busy.
    // - Back enabled when not busy. Finish lesson enabled when not sending/recording.
    // - Conversation Mode enabled as UI/layout mode, but Realtime must not start until ActiveRoleplay.
    //
    // ActiveRoleplay
    // - Guided scenario selected OR Free Conversation started. User messages count as lesson turns.
    // - Send enabled unless busy/final and text is present. Start recording enabled unless busy/final.
    // - Hint enabled unless busy/final. Back enabled unless final/awaiting-finish.
    // - Finish lesson enabled. Conversation Mode can start Realtime.
    //
    // CompletedAwaitingFinish
    // - Final message has been shown. Lesson no longer accepts input. Only Finish lesson enabled.
    // - Send, recording, hint, and back disabled. Conversation Mode disabled/stopped.
    //
    // Finished
    // - Summary navigation has happened or lesson is closed. All lesson commands disabled except navigation controlled outside this VM.
    //
    // Transient busy flags used by command state:
    // - IsSending: backend/realtime assistant turn in progress.
    // - IsRecording: local or realtime microphone capture in progress.
    // - IsRealtimeSessionStarting: realtime WebSocket/session startup in progress.
    // - IsBotVoicePlaying: bot voice playback in progress; blocks local recording but not text input.
    private bool IsLessonBusyForInput => IsSending || IsRecording || IsRealtimeSessionStarting;

    private bool IsCompletedAwaitingFinish => IsLessonCompleteAwaitingFinish || CurrentLessonPhase == LessonPhase.Completed;

    private bool IsRealtimeSessionStarting => isStartingRealtimeSession;

    private bool CanAcceptLessonInput => !hasFinishedLesson && !IsCompletedAwaitingFinish && !IsLessonLimitReached && !IsLessonBusyForInput;

    private bool CanAcceptTranscriptionResult =>
        !hasFinishedLesson
        && !IsCompletedAwaitingFinish
        && !IsLessonLimitReached
        && (CurrentLessonPhase == LessonPhase.SetupContextSelection || CurrentLessonPhase == LessonPhase.ActiveRoleplay);

    private bool IsRealtimeConversationActive => BackendConstants.UseRealtimeConversationMode && IsConversationModeEnabled && CurrentLessonPhase == LessonPhase.ActiveRoleplay;

    private bool ShouldAutoPlayBotVoice => !IsRealtimeConversationActive && !IsLessonCompleteAwaitingFinish && IsBotVoiceAutoPlayEnabled;

    public LessonChatViewModel(
        AppLocalizedText localizedText,
        string selectedLevel,
        Topic selectedTopic,
        Subtopic selectedSubtopic,
        string nativeLanguageName,
        string userDisplayName,
        string learningGoal,
        TutorAvatarOption tutorAvatar,
        TutorProfile? tutorProfile,
        LessonScenario? lessonScenario,
        LessonChatBackendService lessonChatBackendService,
        AudioRecordingService audioRecordingService,
        AudioPlaybackService audioPlaybackService,
        BotVoiceTempFileCleanupService botVoiceTempFileCleanupService,
        string audioInputDeviceId,
        Action navigateBack,
        Action<LessonSummaryInput> finishLesson)
    {
        this.localizedText = localizedText;
        SelectedLevel = selectedLevel;
        SelectedTopic = selectedTopic;
        SelectedSubtopic = selectedSubtopic;
        this.nativeLanguageName = nativeLanguageName;
        UserDisplayName = NormalizeOptionalText(userDisplayName);
        LearningGoal = NormalizeOptionalText(learningGoal);
        tutorAvatarId = tutorAvatar.Id;
        TutorAvatarDisplayName = tutorAvatar.DisplayName;
        this.tutorProfile = tutorProfile ?? new TutorProfile { Id = tutorAvatar.Id, DisplayName = tutorAvatar.DisplayName };
        this.lessonScenario = lessonScenario ?? new LessonScenario();
        activeLevelProfile = ResolveActiveLevelProfile(this.lessonScenario, selectedLevel);
        this.lessonChatBackendService = lessonChatBackendService;
        this.audioRecordingService = audioRecordingService;
        this.audioPlaybackService = audioPlaybackService;
        this.botVoiceTempFileCleanupService = botVoiceTempFileCleanupService;
        realtimeVoiceEngine = new RealtimeVoiceConversationEngine(lessonChatBackendService);
        realtimeAudioPlaybackService = new RealtimeAudioPlaybackService();
        realtimeMicrophoneCaptureService = new RealtimeMicrophoneCaptureService();
        realtimeVoiceEngine.AssistantAudioChunkReceived += OnRealtimeAssistantAudioChunkReceived;
        realtimeVoiceEngine.AssistantTranscriptDeltaReceived += OnRealtimeAssistantTranscriptDeltaReceived;
        realtimeVoiceEngine.AssistantTurnCompleted += OnRealtimeAssistantTurnCompleted;
        realtimeVoiceEngine.UserAudioCommitted += OnRealtimeUserAudioCommitted;
        realtimeVoiceEngine.UserTranscriptDeltaReceived += OnRealtimeUserTranscriptDeltaReceived;
        realtimeVoiceEngine.UserTranscriptCompleted += OnRealtimeUserTranscriptCompleted;
        realtimeVoiceEngine.UserTranscriptFailed += OnRealtimeUserTranscriptFailed;
        realtimeVoiceEngine.ErrorReceived += OnRealtimeErrorReceived;
        realtimeAudioPlaybackService.PlaybackStarted += OnRealtimePlaybackStarted;
        realtimeMicrophoneCaptureService.AudioChunkCaptured += OnRealtimeMicrophoneAudioChunkCaptured;
        this.audioInputDeviceId = string.IsNullOrWhiteSpace(audioInputDeviceId)
            ? AudioConstants.DefaultAudioInputDeviceId
            : audioInputDeviceId;
        this.navigateBack = navigateBack;
        this.finishLesson = finishLesson;

        CurrentLessonPhase = IsFreeConversationLesson()
            ? LessonPhase.ActiveRoleplay
            : LessonPhase.SetupContextSelection;
        var setupMessage = string.IsNullOrWhiteSpace(this.lessonScenario.LessonSetup.SetupMessage)
            ? AppConstants.MockBotFirstMessage
            : RenderLessonTemplate(this.lessonScenario.LessonSetup.SetupMessage.Trim());
        AddMessage(TutorAvatarDisplayName, setupMessage, true);
        lastBotMessage = setupMessage;
        _ = CheckBackendHealthAsync();
        _ = CheckBackendConfigStatusAsync();
        LogLessonStateSnapshot("lesson initialization");
    }


    private static LevelProfile ResolveActiveLevelProfile(LessonScenario lessonScenario, string selectedLevel)
    {
        if (!string.IsNullOrWhiteSpace(selectedLevel)
            && lessonScenario.LevelProfiles.TryGetValue(selectedLevel, out var exactProfile))
        {
            return exactProfile;
        }

        var matchingProfile = lessonScenario.LevelProfiles.Values.FirstOrDefault(profile =>
            string.Equals(profile.Level, selectedLevel, StringComparison.OrdinalIgnoreCase));

        return matchingProfile ?? new LevelProfile { Level = selectedLevel };
    }

    private bool CanSendMessage()
    {
        return CanAcceptLessonInput && !string.IsNullOrWhiteSpace(UserInput);
    }

    private bool CanToggleVoiceRecording()
    {
        if (IsRecording)
        {
            return !hasFinishedLesson;
        }

        return !hasFinishedLesson
            && !IsCompletedAwaitingFinish
            && !IsLessonLimitReached
            && !IsSending
            && !IsRealtimeSessionStarting
            && !IsBotVoicePlaying
            && !isTranscribingAudio;
    }

    private bool CanPlayBotVoice(ChatMessageViewModel? message)
    {
        return IsLessonOptionsEnabled
            && !IsBotVoicePlaying
            && message is not null
            && message.ShowPlayVoiceButton
            && !string.IsNullOrWhiteSpace(message.Text);
    }

    private bool CanFinishLesson()
    {
        return !hasFinishedLesson && !IsSending && !IsRecording;
    }

    [RelayCommand(CanExecute = nameof(CanToggleVoiceRecording))]
    private async Task ToggleVoiceRecordingAsync()
    {
        if (IsRecording)
        {
            await StopVoiceRecordingAsync();
            return;
        }

        StartVoiceRecording();
    }

    private void StartVoiceRecording()
    {
        if (!IsLessonInputEnabled)
        {
            return;
        }

        if (IsRecording || audioRecordingService.IsRecording)
        {
            StatusMessage = AudioConstants.RecordingAlreadyInProgressMessage;
            return;
        }

        if (IsBotVoicePlaying)
        {
            StatusMessage = AudioConstants.BotVoicePlayingRecordingBlockedMessage;
            return;
        }

        if (IsSending || isTranscribingAudio)
        {
            return;
        }

        try
        {
            if (IsRealtimeConversationActive)
            {
                _ = StartRealtimeVoiceRecordingAsync();
                return;
            }

            audioRecordingService.StartRecording(audioInputDeviceId);
            CurrentHintText = string.Empty;
            IsRecording = true;
            RefreshAvatarState();
            StatusMessage = localizedText.RecordingStartedMessage;
        }
        catch
        {
            IsRecording = false;
            RefreshAvatarState();
            StatusMessage = localizedText.RecordingStartErrorMessage;
        }
    }

    private async Task StartRealtimeVoiceRecordingAsync()
    {
        try
        {
            await EnsureRealtimeSessionStartedAsync(CancellationToken.None);
            await realtimeVoiceEngine.StartUserAudioAsync(CancellationToken.None);
            realtimeMicrophoneCaptureService.Start(audioInputDeviceId);
            CurrentHintText = string.Empty;
            IsRecording = true;
            RefreshAvatarState();
            StatusMessage = localizedText.RecordingStartedMessage;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Realtime microphone start failed: SessionId={realtimeSessionId}; {exception}");
            IsRecording = false;
            RefreshAvatarState();
            StatusMessage = BackendConstants.RealtimeUnavailableMessage;
        }
    }

    private async Task StopVoiceRecordingAsync()
    {
        if (isTranscribingAudio)
        {
            return;
        }

        if (IsRealtimeConversationActive)
        {
            await StopRealtimeVoiceRecordingAsync();
            return;
        }

        var savedFilePath = string.Empty;

        try
        {
            savedFilePath = audioRecordingService.StopRecording();
            var recordingDuration = audioRecordingService.LastRecordingDuration;
            IsRecording = false;

            if (string.IsNullOrWhiteSpace(savedFilePath))
            {
                StatusMessage = localizedText.RecordingStopErrorMessage;
                return;
            }

            if (recordingDuration.TotalMilliseconds < AudioConstants.MinimumRecordingDurationMilliseconds)
            {
                StatusMessage = AudioConstants.RecordingTooShortMessage;
                return;
            }

            if (recordingDuration.TotalSeconds > AudioConstants.MaximumRecordingDurationSeconds)
            {
                StatusMessage = AudioConstants.RecordingTooLongMessage;
                return;
            }

            SetIsTranscribingAudio(true);
            Debug.WriteLine(
                $"Voice transcription started: CurrentLessonPhase={CurrentLessonPhase}; " +
                $"IsConversationModeEnabled={IsConversationModeEnabled}; " +
                $"IsVoiceAutoSendEnabled={IsVoiceAutoSendEnabled}; " +
                $"IsSending={IsSending}; " +
                $"IsRecording={IsRecording}; " +
                $"isTranscribingAudio={isTranscribingAudio}.");
            StatusMessage = localizedText.TranscribingAudioMessage;

            var transcriptionText = await lessonChatBackendService.SendAudioForTranscriptionAsync(savedFilePath);
            BackendStatusText = BackendConstants.BackendStatusConnected;
            var transcriptValidation = LessonTranscriptValidator.Validate(transcriptionText);
            var trimmedTranscriptionText = transcriptValidation.NormalizedTranscript;
            var shouldAutoSend = ShouldAutoSendTranscribedVoiceResult();
            Debug.WriteLine(
                $"Voice transcription validation: IsValid={transcriptValidation.IsValid}; " +
                $"Reason={transcriptValidation.Reason}; " +
                $"TranscriptLength={trimmedTranscriptionText.Length}; " +
                $"TurnCounted=False; " +
                $"LearnerTurnCountBefore={LearnerTurnCount}; " +
                $"CanAcceptTranscriptionResult={CanAcceptTranscriptionResult}; " +
                $"ShouldAutoSend={shouldAutoSend}; " +
                $"CurrentLessonPhase={CurrentLessonPhase}; " +
                $"IsConversationModeEnabled={IsConversationModeEnabled}; " +
                $"IsVoiceAutoSendEnabled={IsVoiceAutoSendEnabled}; " +
                $"NormalAssistantResponseCreated=False; " +
                $"RetryPromptShown={!transcriptValidation.IsValid}; " +
                $"Preview={GetLimitedTranscriptPreview(trimmedTranscriptionText)}.");

            if (!transcriptValidation.IsValid)
            {
                StatusMessage = LessonTranscriptValidator.GetRetryMessage(SelectedLevel);
                UserInput = string.Empty;
                Debug.WriteLine($"Voice transcription rejected: Reason={transcriptValidation.Reason}; LearnerTurnCountBefore={LearnerTurnCount}; LearnerTurnCountAfter={LearnerTurnCount}; RetryPromptShown=True.");
                return;
            }

            if (!CanAcceptTranscriptionResult)
            {
                Debug.WriteLine("Voice transcription rejected: Reason=lesson-not-accepting-input.");
                StatusMessage = AppConstants.LessonCompleteAwaitingFinishMessage;
                return;
            }

            if (!shouldAutoSend)
            {
                UserInput = trimmedTranscriptionText;
                Debug.WriteLine("Voice transcription placed into UserInput.");
                StatusMessage = localizedText.TranscriptionCompletedMessage;
                return;
            }

            Debug.WriteLine("Voice transcription auto-send started.");
            SetIsTranscribingAudio(false);
            var wasSent = await SendLessonMessageAsync(trimmedTranscriptionText, ChatMessageSource.LessonChatVoice);

            if (wasSent)
            {
                UserInput = string.Empty;
            }
        }
        catch (AudioTranscriptionBackendException)
        {
            BackendStatusText = BackendConstants.BackendStatusConnected;
            StatusMessage = localizedText.TranscriptionFailedMessage;
        }
        catch
        {
            BackendStatusText = BackendConstants.BackendStatusUnavailable;
            StatusMessage = localizedText.TranscriptionFailedMessage;
        }
        finally
        {
            BotStatus = BackendConstants.BotStatusReady;
            SetIsTranscribingAudio(false);
            IsRecording = false;
            RefreshAvatarState();
            audioRecordingService.SafeDeleteRecording(savedFilePath);
        }
    }


    private void SetIsTranscribingAudio(bool value)
    {
        if (isTranscribingAudio == value)
        {
            return;
        }

        isTranscribingAudio = value;
        RefreshAvatarState();
        RefreshAllCommandStates();
    }

    private static string GetLimitedTranscriptPreview(string transcriptionText)
    {
        if (string.IsNullOrWhiteSpace(transcriptionText))
        {
            return string.Empty;
        }

        var normalizedText = transcriptionText.Replace("\r\n", " ", StringComparison.Ordinal).Replace('\r', ' ').Replace('\n', ' ');
        return normalizedText.Length <= 40
            ? normalizedText
            : normalizedText[..40];
    }

    private async Task StopRealtimeVoiceRecordingAsync()
    {
        try
        {
            var duration = realtimeMicrophoneCaptureService.Stop();
            IsRecording = false;

            if (duration.TotalMilliseconds < AudioConstants.MinimumRecordingDurationMilliseconds)
            {
                StatusMessage = AudioConstants.RecordingTooShortMessage;
                return;
            }

            if (!IsLessonInputEnabled)
            {
                StatusMessage = AppConstants.LessonCompleteAwaitingFinishMessage;
                return;
            }

            realtimeUserTranscriptBuffer.Clear();
            realtimeUserPlaceholderItemId = string.Empty;
            realtimeUserPlaceholderMessage = AddMessage(AppConstants.UserSenderName, RealtimeVoicePendingText, false, source: ChatMessageSource.RealtimeVoice, isTechnicalMessage: true);
            Debug.WriteLine($"Realtime user placeholder message added: SessionId={realtimeSessionId}; UserPlaceholderMessageId={realtimeUserPlaceholderMessage.Id}; Text={RealtimeVoicePendingText}; LearnerTurnCountBefore={LearnerTurnCount}.");

            await realtimeVoiceEngine.CommitUserAudioAsync(CancellationToken.None);
            StatusMessage = string.Empty;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Realtime voice recording stop failed: SessionId={realtimeSessionId}; {exception}");
            StatusMessage = BackendConstants.RealtimeUnavailableMessage;
        }
        finally
        {
            IsRecording = false;
            RefreshAvatarState();
        }
    }

    [RelayCommand(CanExecute = nameof(CanToggleConversationMode))]
    private async Task ToggleConversationModeAsync()
    {
        LogLessonStateSnapshot("Conversation Mode toggle requested");

        if (IsConversationModeEnabled)
        {
            await StopRealtimeConversationAsync("conversation_mode_off");
            IsConversationModeEnabled = false;
            RefreshAllCommandStates();
            LogLessonStateSnapshot("Conversation Mode toggle off");
            return;
        }

        if (IsGuidedRoleplayLesson() && CurrentLessonPhase == LessonPhase.SetupContextSelection)
        {
            IsConversationModeEnabled = true;
            StatusMessage = "Choose a situation to start the conversation.";
            Debug.WriteLine($"Conversation mode enabled before guided context selection: LessonType={lessonScenario.Metadata.LessonType}; CurrentLessonPhase={CurrentLessonPhase}; SelectedTopic={SelectedTopic.Title}; SelectedSubtopic={SelectedSubtopic.Title}; UseRealtimeConversationMode={BackendConstants.UseRealtimeConversationMode}; BackendEndpoint={lessonChatBackendService.CreateRealtimeVoiceWebSocketUri()}.");
            RefreshAllCommandStates();
            LogLessonStateSnapshot("Conversation Mode enabled in setup; realtime deferred");
            return;
        }

        var startStopwatch = Stopwatch.StartNew();
        Debug.WriteLine($"Conversation mode start requested: LessonType={lessonScenario.Metadata.LessonType}; CurrentLessonPhase={CurrentLessonPhase}; IsFreeConversation={IsFreeConversationLesson()}; SelectedTopic={SelectedTopic.Title}; SelectedSubtopic={SelectedSubtopic.Title}; UseRealtimeConversationMode={BackendConstants.UseRealtimeConversationMode}; BackendEndpoint={lessonChatBackendService.CreateRealtimeVoiceWebSocketUri()}.");

        try
        {
            await EnsureRealtimeSessionStartedAsync(CancellationToken.None);
            IsConversationModeEnabled = true;
            StatusMessage = string.Empty;
            Debug.WriteLine($"Conversation mode started: RealtimeSessionId={realtimeSessionId}; ElapsedMs={startStopwatch.ElapsedMilliseconds}.");
            LogLessonStateSnapshot("Realtime start success");
        }
        catch (Exception exception)
        {
            IsConversationModeEnabled = false;
            isRealtimeSessionStarted = false;
            BackendStatusText = BackendConstants.BackendStatusUnavailable;
            StatusMessage = BackendConstants.RealtimeUnavailableMessage;
            Debug.WriteLine($"Conversation mode start failed: RealtimeSessionId={realtimeSessionId}; ExceptionType={exception.GetType().FullName}; Message={exception.Message}; {exception}");
            LogLessonStateSnapshot("Realtime start failure");
        }
        finally
        {
            RefreshAllCommandStates();
        }
    }

    private bool CanToggleConversationMode()
    {
        return !hasFinishedLesson
            && !IsCompletedAwaitingFinish
            && !IsLessonLimitReached
            && !IsSending
            && !IsRecording
            && !IsRealtimeSessionStarting
            && (CurrentLessonPhase == LessonPhase.SetupContextSelection || CurrentLessonPhase == LessonPhase.ActiveRoleplay);
    }

    [RelayCommand(CanExecute = nameof(CanPlayBotVoice))]
    private async Task PlayBotVoiceAsync(ChatMessageViewModel? message)
    {
        if (message is null || !message.ShowPlayVoiceButton)
        {
            return;
        }

        await PlayBotVoiceForMessageAsync(message, isAutoPlay: false);
    }

    private Task TryAutoPlayNewestBotVoiceAsync(ChatMessageViewModel message)
    {
        if (!ShouldAutoPlayBotVoice || !message.IsFromBot || string.IsNullOrWhiteSpace(message.Text))
        {
            return Task.CompletedTask;
        }

        if (!IsNewestBotMessage(message))
        {
            Debug.WriteLine($"Skipping bot voice auto-play for message {message.Id}: it is no longer the newest bot message.");
            return Task.CompletedTask;
        }

        if (IsSetupBotMessage(message))
        {
            Debug.WriteLine($"Skipping bot voice auto-play for setup message {message.Id}: setup auto-play is disabled; TextLength={message.Text.Trim().Length}.");
            return Task.CompletedTask;
        }

        _ = PlayBotVoiceForMessageAsync(message, isAutoPlay: true);
        return Task.CompletedTask;
    }

    private async Task PlayBotVoiceForMessageAsync(
        ChatMessageViewModel message,
        bool isAutoPlay,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.Text))
        {
            return;
        }

        if (isAutoPlay && !IsNewestBotMessage(message))
        {
            Debug.WriteLine($"Skipping bot voice auto-play for message {message.Id}: it is no longer the newest bot message.");
            return;
        }

        CancelCurrentBotVoice(isAutoPlay ? BotVoiceCancellationReasons.NewerMessageCancel : BotVoiceCancellationReasons.ManualReplayCancel);

        if (!await botVoiceSemaphore.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken))
        {
            Debug.WriteLine($"Skipped bot voice {(isAutoPlay ? "auto-play" : "manual play")} for message {message.Id}: previous voice did not stop in time.");
            return;
        }

        using var playbackCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        SetCurrentBotVoiceCancellationTokenSource(playbackCancellationTokenSource);
        var playbackStarted = false;
        var totalStopwatch = Stopwatch.StartNew();
        var selectedBotVoicePath = AudioConstants.BotVoiceDefaultPathName;

        try
        {
            IsBotVoicePlaying = true;
            RefreshAvatarState();
            StatusMessage = localizedText.PlayingBotVoiceMessage;

            var rawBotVoiceText = message.Text.Trim();
            var isSetupVoiceMessage = IsSetupVoiceMessage(message);
            var exactBotVoiceText = GetExactBotVoiceText(message);
            IReadOnlyList<string> allSegments = isAutoPlay
                ? SplitExactBotVoiceTextIntoSegments(exactBotVoiceText)
                : [exactBotVoiceText];
            var segmentsToSpeak = SelectBotVoiceSegments(allSegments, isAutoPlay);

            Debug.WriteLine($"Bot voice exact text: MessageId={message.Id}; RawTextLength={rawBotVoiceText.Length}; VoiceTextLength={exactBotVoiceText.Length}; IsExactText={rawBotVoiceText == exactBotVoiceText}; AutoPlay={isAutoPlay}; IsSetupMessage={isSetupVoiceMessage}; SegmentCount={segmentsToSpeak.Count}; SegmentLengths={string.Join(",", segmentsToSpeak.Select(segment => segment.Length))}.");

            if (segmentsToSpeak.Count == 0)
            {
                Debug.WriteLine("Bot voice skipped because no speakable segments were found.");
                StatusMessage = string.Empty;
                return;
            }

            Debug.WriteLine($"Bot voice request start message id {message.Id}: Path={selectedBotVoicePath}; MessageId={message.Id}; RawInputLength={rawBotVoiceText.Length}; VoiceTextLength={exactBotVoiceText.Length}; AutoPlay={isAutoPlay}; SegmentCount={segmentsToSpeak.Count}; TotalSegmentCount={allSegments.Count}; FirstSegmentLength={segmentsToSpeak[0].Length}; FirstSegmentRequestStartedMs={totalStopwatch.ElapsedMilliseconds}.");

            await PlaySegmentedHighQualityBotVoiceAsync(
                message,
                segmentsToSpeak,
                playbackCancellationTokenSource.Token,
                playbackStartedMs => playbackStarted = true,
                totalStopwatch,
                isAutoPlay);

            Debug.WriteLine($"Bot voice playback completed ms for message {message.Id}: Path={selectedBotVoicePath}; TotalElapsedMilliseconds={totalStopwatch.ElapsedMilliseconds}; SegmentCount={segmentsToSpeak.Count}.");
            BackendStatusText = BackendConstants.BackendStatusConnected;
        }
        catch (OperationCanceledException exception)
        {
            var cancellationReason = GetCurrentBotVoiceCancellationReason();
            Debug.WriteLine($"Bot voice {(isAutoPlay ? "auto-play" : "manual play")} canceled for message {message.Id}: Path={selectedBotVoicePath}; CancellationReason={cancellationReason}; PlaybackStarted={playbackStarted}; TotalMs={totalStopwatch.ElapsedMilliseconds}; {exception}");

            if (string.Equals(cancellationReason, BotVoiceCancellationReasons.HardTimeoutCancel, StringComparison.Ordinal))
            {
                StatusMessage = isAutoPlay || playbackStarted
                    ? string.Empty
                    : "Voice is taking too long. Please try again.";
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Bot voice {(isAutoPlay ? "auto-play" : "manual play")} failed for message {message.Id}: Path={selectedBotVoicePath}; PlaybackStarted={playbackStarted}; TotalMs={totalStopwatch.ElapsedMilliseconds}; {exception}");
            StatusMessage = playbackStarted || isAutoPlay ? string.Empty : localizedText.BotVoiceFailedMessage;
        }
        finally
        {
            ClearCurrentBotVoiceCancellationTokenSource(playbackCancellationTokenSource);
            IsBotVoicePlaying = false;
            RefreshAvatarState();
            botVoiceSemaphore.Release();
        }
    }

    private async Task PlaySegmentedHighQualityBotVoiceAsync(
        ChatMessageViewModel message,
        IReadOnlyList<string> segments,
        CancellationToken cancellationToken,
        Action<long> onFirstPlaybackStarted,
        Stopwatch totalStopwatch,
        bool isAutoPlay)
    {
        var firstSegmentTask = GetOrCreateBotVoiceSegmentAudioFileAsync(
            message,
            segments[0],
            segmentIndex: 0,
            timeout: TimeSpan.FromSeconds(AudioConstants.BotVoiceFirstSegmentHardTimeoutSeconds),
            totalStopwatch,
            cancellationToken,
            isAutoPlay);

        var softTargetTask = Task.Delay(AudioConstants.BotVoiceFirstSegmentSoftTargetMilliseconds, cancellationToken);
        if (await Task.WhenAny(firstSegmentTask, softTargetTask) == softTargetTask && softTargetTask.IsCompletedSuccessfully)
        {
            Debug.WriteLine($"Bot voice first segment soft target reached: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex=0; CancellationReason={BotVoiceCancellationReasons.SoftTargetReachedNoCancel}; SoftTargetMs={AudioConstants.BotVoiceFirstSegmentSoftTargetMilliseconds}; ElapsedMs={totalStopwatch.ElapsedMilliseconds}; RequestContinuedAfterSoftTarget=True.");
            StatusMessage = "Preparing voice...";
        }

        var firstSegmentFilePath = await firstSegmentTask;
        var firstSegmentReadyMs = totalStopwatch.ElapsedMilliseconds;
        Debug.WriteLine($"Bot voice first segment ready: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex=0; FirstSegmentLength={segments[0].Length}; FirstSegmentReadyMs={firstSegmentReadyMs}; SoftTargetReached={firstSegmentReadyMs >= AudioConstants.BotVoiceFirstSegmentSoftTargetMilliseconds}; RequestContinuedAfterSoftTarget={firstSegmentReadyMs >= AudioConstants.BotVoiceFirstSegmentSoftTargetMilliseconds}; AudioFile={Path.GetFileName(firstSegmentFilePath)}.");
        StatusMessage = string.Empty;

        if (isAutoPlay && !IsNewestBotMessage(message))
        {
            Debug.WriteLine($"Discarding bot voice first segment for message {message.Id}: CancellationReason={BotVoiceCancellationReasons.NewerMessageCancel}; it is no longer the newest bot message.");
            return;
        }

        Task<string>? nextSegmentTask = segments.Count > 1
            ? GetOrCreateBotVoiceSegmentAudioFileAsync(message, segments[1], 1, TimeSpan.FromSeconds(AudioConstants.BotVoiceLaterSegmentHardTimeoutSeconds), totalStopwatch, cancellationToken, isAutoPlay)
            : null;
        var currentFilePath = firstSegmentFilePath;

        for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
        {
            var playbackSegmentIndex = segmentIndex;
            var playbackStartedForSegment = false;
            Debug.WriteLine($"Bot voice PlaybackStartRequested: MessageId={message.Id}; VoiceRequestId={CreateBotVoiceRequestId(message.Id, playbackSegmentIndex, segments[playbackSegmentIndex])}; SavedAudioPath={currentFilePath}; SavedAudioFileExists={File.Exists(currentFilePath)}; SavedAudioFileLength={(File.Exists(currentFilePath) ? new FileInfo(currentFilePath).Length : 0)}.");
            await audioPlaybackService.PlayAudioFileAsync(
                currentFilePath,
                cancellationToken,
                _ =>
                {
                    playbackStartedForSegment = true;
                    var playbackStartedMs = totalStopwatch.ElapsedMilliseconds;
                    if (playbackSegmentIndex == 0)
                    {
                        onFirstPlaybackStarted(playbackStartedMs);
                        Debug.WriteLine($"Bot voice PlaybackStarted: MessageId={message.Id}; VoiceRequestId={CreateBotVoiceRequestId(message.Id, playbackSegmentIndex, segments[playbackSegmentIndex])}; FirstSegmentReadyMs={firstSegmentReadyMs}; FirstPlaybackStartedMs={playbackStartedMs}; SoftTargetMet={playbackStartedMs <= AudioConstants.BotVoiceFirstSegmentSoftTargetMilliseconds}.");
                    }

                    Debug.WriteLine($"Bot voice segment playback started: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={playbackSegmentIndex}; SegmentLength={segments[playbackSegmentIndex].Length}; PlaybackStartedMs={playbackStartedMs}; AudioFile={Path.GetFileName(currentFilePath)}.");
                });

            Debug.WriteLine($"Bot voice PlaybackCompleted: MessageId={message.Id}; VoiceRequestId={CreateBotVoiceRequestId(message.Id, segmentIndex, segments[segmentIndex])}; SegmentIndex={segmentIndex}; PlaybackEndMs={totalStopwatch.ElapsedMilliseconds}; PlaybackStarted={playbackStartedForSegment}.");

            if (segmentIndex + 1 >= segments.Count)
            {
                continue;
            }

            try
            {
                currentFilePath = await nextSegmentTask!;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Bot voice later segment canceled: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex + 1}; SegmentLength={segments[segmentIndex + 1].Length}; CancellationReason=later-segment-hard-timeout-or-newer-message; TotalMs={totalStopwatch.ElapsedMilliseconds}.");
                break;
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Bot voice later segment failed: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex + 1}; SegmentLength={segments[segmentIndex + 1].Length}; TotalMs={totalStopwatch.ElapsedMilliseconds}; {exception}");
                break;
            }

            var nextIndex = segmentIndex + 2;
            nextSegmentTask = nextIndex < segments.Count
                ? GetOrCreateBotVoiceSegmentAudioFileAsync(message, segments[nextIndex], nextIndex, TimeSpan.FromSeconds(AudioConstants.BotVoiceLaterSegmentHardTimeoutSeconds), totalStopwatch, cancellationToken, isAutoPlay)
                : null;
        }
    }

    private Task<string> GetOrCreateBotVoiceSegmentAudioFileAsync(
        ChatMessageViewModel message,
        string segmentText,
        int segmentIndex,
        TimeSpan timeout,
        Stopwatch totalStopwatch,
        CancellationToken cancellationToken,
        bool isAutoPlay = false)
    {
        var cacheKey = CreateBotVoiceSegmentCacheKey(message.Id, segmentIndex, segmentText);
        lock (botVoiceSegmentCacheLock)
        {
            if (botVoiceSegmentAudioFilePaths.TryGetValue(cacheKey, out var cachedFilePath) && File.Exists(cachedFilePath))
            {
                TrackCurrentSessionBotVoiceFile(cachedFilePath);
                Debug.WriteLine($"Bot voice segment cache hit: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex}; ReadyMs={totalStopwatch.ElapsedMilliseconds}; AudioFile={Path.GetFileName(cachedFilePath)}.");
                return Task.FromResult(cachedFilePath);
            }

            if (inFlightBotVoiceSegmentTasks.TryGetValue(cacheKey, out var inFlightTask))
            {
                Debug.WriteLine($"Bot voice segment in-flight reuse: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex}; ReadyMs={totalStopwatch.ElapsedMilliseconds}.");
                return inFlightTask.WaitAsync(cancellationToken);
            }

            var createdTask = CreateBotVoiceSegmentAudioFileCoreAsync(
                message,
                segmentText,
                segmentIndex,
                timeout,
                totalStopwatch,
                cancellationToken,
                isAutoPlay);
            inFlightBotVoiceSegmentTasks[cacheKey] = createdTask;
            return createdTask;
        }
    }

    private async Task<string> CreateBotVoiceSegmentAudioFileCoreAsync(
        ChatMessageViewModel message,
        string segmentText,
        int segmentIndex,
        TimeSpan timeout,
        Stopwatch totalStopwatch,
        CancellationToken cancellationToken,
        bool isAutoPlay)
    {
        var cacheKey = CreateBotVoiceSegmentCacheKey(message.Id, segmentIndex, segmentText);
        using var segmentTimeoutCancellationTokenSource = new CancellationTokenSource(timeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            segmentTimeoutCancellationTokenSource.Token);
        var backendStopwatch = Stopwatch.StartNew();
        var normalizedSegmentText = NormalizeVoiceWhitespace(segmentText);
        var inputLength = normalizedSegmentText.Length;

        try
        {
            if (!ValidateBotVoiceSegmentForRequest(normalizedSegmentText, allowShortMeaningfulOnlySegment: true))
            {
                throw new InvalidOperationException("Bot voice segment was rejected before backend request because it is not speakable.");
            }

            var rawTextLength = message.Text.Trim().Length;
            var voiceRequestId = CreateBotVoiceRequestId(message.Id, segmentIndex, normalizedSegmentText);
            var isExactText = string.Equals(normalizedSegmentText, GetExactBotVoiceText(message), StringComparison.Ordinal);
            Debug.WriteLine($"Bot voice exact text: MessageId={message.Id}; VoiceRequestId={voiceRequestId}; RawTextLength={rawTextLength}; VoiceTextLength={inputLength}; IsExactText={isExactText}; AutoPlay={isAutoPlay}; IsSetupMessage={IsSetupVoiceMessage(message)}; SegmentIndex={segmentIndex};");
            Debug.WriteLine($"Bot voice segment request: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex}; SegmentLength={inputLength}; SegmentTextPreview={CreateBotVoiceSegmentPreview(normalizedSegmentText)};");
            Debug.WriteLine($"Bot voice segment request starting: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex}; InputLength={inputLength}; RequestStartedMs={totalStopwatch.ElapsedMilliseconds}; TimeoutMs={timeout.TotalMilliseconds}; HardTimeoutSeconds={timeout.TotalSeconds}.");
            var speechResponse = await lessonChatBackendService.CreateBotSpeechAsync(normalizedSegmentText, linkedCancellationTokenSource.Token);
            Debug.WriteLine($"Bot voice segment backend response received: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; VoiceRequestId={voiceRequestId}; SegmentIndex={segmentIndex}; InputLength={inputLength}; SegmentReadyMs={totalStopwatch.ElapsedMilliseconds}; BackendElapsedMs={backendStopwatch.ElapsedMilliseconds}; BackendAudioBytes={speechResponse.AudioBytes.Length}; ContentType={speechResponse.ContentType}.");

            var saveStopwatch = Stopwatch.StartNew();
            var audioFilePath = await audioPlaybackService.SaveBotVoiceAudioAsync(
                speechResponse.AudioBytes,
                speechResponse.FileExtension,
                linkedCancellationTokenSource.Token);
            var savedAudioFileInfo = new FileInfo(audioFilePath);
            Debug.WriteLine($"Bot voice SavedAudioPath: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; VoiceRequestId={voiceRequestId}; SegmentIndex={segmentIndex}; SaveElapsedMs={saveStopwatch.ElapsedMilliseconds}; ReadyMs={totalStopwatch.ElapsedMilliseconds}; SavedAudioPath={audioFilePath}; SavedAudioFileExists={savedAudioFileInfo.Exists}; SavedAudioFileLength={(savedAudioFileInfo.Exists ? savedAudioFileInfo.Length : 0)}; FileExtension={Path.GetExtension(audioFilePath)}.");

            lock (botVoiceSegmentCacheLock)
            {
                botVoiceSegmentAudioFilePaths[cacheKey] = audioFilePath;
            }

            TrackCurrentSessionBotVoiceFile(audioFilePath);
            return audioFilePath;
        }
        catch (OperationCanceledException) when (segmentTimeoutCancellationTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            SetCurrentBotVoiceCancellationReason(BotVoiceCancellationReasons.HardTimeoutCancel);
            Debug.WriteLine($"Bot voice segment hard timeout: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex}; CancellationReason={BotVoiceCancellationReasons.HardTimeoutCancel}; InputLength={inputLength}; TotalMs={totalStopwatch.ElapsedMilliseconds}; BackendElapsedMs={backendStopwatch.ElapsedMilliseconds}; HardTimeoutSeconds={timeout.TotalSeconds}.");
            throw;
        }
        finally
        {
            lock (botVoiceSegmentCacheLock)
            {
                inFlightBotVoiceSegmentTasks.Remove(cacheKey);
            }
        }
    }

    private static string CreateBotVoiceRequestId(int messageId, int segmentIndex, string text)
    {
        return $"msg-{messageId}-seg-{segmentIndex}-len-{text.Length}";
    }

    private static IReadOnlyList<string> SelectBotVoiceSegments(IReadOnlyList<string> segments, bool isAutoPlay)
    {
        if (!isAutoPlay || segments.Count == 0)
        {
            return segments;
        }

        var totalCharacters = segments.Sum(segment => segment.Length);
        if (segments.Count <= AudioConstants.BotVoiceAutoPlayMaxSegments
            && totalCharacters <= AudioConstants.BotVoiceMaxSpokenCharactersAutoPlay)
        {
            return segments;
        }

        Debug.WriteLine($"Skipping bot voice auto-play because exact visible text is too long for auto-play: SegmentCount={segments.Count}; VoiceTextLength={totalCharacters}; MaxSegments={AudioConstants.BotVoiceAutoPlayMaxSegments}; MaxCharacters={AudioConstants.BotVoiceMaxSpokenCharactersAutoPlay}.");
        return [];
    }

    private static string GetExactBotVoiceText(ChatMessageViewModel message)
    {
        return NormalizeVoiceWhitespace(message.Text);
    }

    private static string NormalizeVoiceWhitespace(string rawText)
    {
        return string.IsNullOrWhiteSpace(rawText)
            ? string.Empty
            : rawText.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Trim();
    }

    private static IReadOnlyList<string> SplitExactBotVoiceTextIntoSegments(string exactText)
    {
        var normalizedText = NormalizeVoiceWhitespace(exactText);
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return [];
        }

        return IsSpeakableSegment(normalizedText, allowMeaningfulShortWholeReply: true)
            ? [normalizedText]
            : [];
    }

    private static IReadOnlyList<string> SplitExactTextIntoSentenceSegments(string text)
    {
        var segments = new List<string>();
        var current = new StringBuilder();

        foreach (var character in text)
        {
            current.Append(character);

            if (character is '.' or '?' or '!' or ';')
            {
                AddTrimmedSegment(segments, current.ToString());
                current.Clear();
            }
        }

        AddTrimmedSegment(segments, current.ToString());
        return segments;
    }

    private static IReadOnlyList<string> MergeBotVoiceSegments(IReadOnlyList<string> segments)
    {
        var mergedSegments = new List<string>();

        foreach (var rawSegment in segments)
        {
            var segment = NormalizeBotVoiceSegmentText(rawSegment);
            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

            if (mergedSegments.Count == 0)
            {
                mergedSegments.Add(segment);
                continue;
            }

            var previousIndex = mergedSegments.Count - 1;
            var mergedWithPrevious = JoinBotVoiceSegments(mergedSegments[previousIndex], segment);
            if (mergedWithPrevious.Length <= AudioConstants.BotVoiceIdealSegmentMaxCharacters)
            {
                mergedSegments[previousIndex] = mergedWithPrevious;
                continue;
            }

            mergedSegments.Add(segment);
        }

        return mergedSegments;
    }

    private static IReadOnlyList<string> ValidateAndMergeBotVoiceSegments(IReadOnlyList<string> segments)
    {
        return segments
            .Select(NormalizeBotVoiceSegmentText)
            .Where(segment => ValidateBotVoiceSegmentForRequest(segment, allowShortMeaningfulOnlySegment: true))
            .ToList();
    }

    private static bool ValidateBotVoiceSegmentForRequest(string segment, bool allowShortMeaningfulOnlySegment)
    {
        var normalizedSegment = NormalizeBotVoiceSegmentText(segment);
        if (string.IsNullOrWhiteSpace(normalizedSegment))
        {
            LogSkippedBotVoiceSegment("empty", segment);
            return false;
        }

        if (!ContainsLetterOrDigit(normalizedSegment))
        {
            LogSkippedBotVoiceSegment("no-letters-or-digits", segment);
            return false;
        }

        return true;
    }

    private static bool IsSpeakableSegment(string segment, bool allowMeaningfulShortWholeReply)
    {
        return ValidateBotVoiceSegmentForRequest(segment, allowMeaningfulShortWholeReply);
    }

    private static bool ContainsLetterOrDigit(string text)
    {
        foreach (var character in text)
        {
            if (char.IsLetterOrDigit(character))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeBotVoiceSegmentText(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var previousWasWhitespace = false;

        foreach (var character in segment.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        return builder.ToString().Trim();
    }

    private static string JoinBotVoiceSegments(string firstSegment, string secondSegment)
    {
        var first = NormalizeBotVoiceSegmentText(firstSegment);
        var second = NormalizeBotVoiceSegmentText(secondSegment);

        if (string.IsNullOrWhiteSpace(first))
        {
            return second;
        }

        if (string.IsNullOrWhiteSpace(second))
        {
            return first;
        }

        return $"{first} {second}";
    }

    private static string CreateBotVoiceSegmentPreview(string segment)
    {
        var preview = NormalizeBotVoiceSegmentText(segment);
        return preview.Length <= 60 ? preview : $"{preview[..60]}…";
    }

    private static void LogSkippedBotVoiceSegment(string reason, string segment)
    {
        Debug.WriteLine($"Bot voice segment skipped: Reason={reason}; SegmentTextPreview={CreateBotVoiceSegmentPreview(segment)};");
    }

    private static void SplitLongBotVoiceSegment(string segment, List<string> output)
    {
        var remaining = NormalizeBotVoiceSegmentText(segment);

        while (remaining.Length > AudioConstants.BotVoiceAbsoluteMaxSegmentCharacters)
        {
            var splitIndex = remaining.LastIndexOf(',', AudioConstants.BotVoiceIdealSegmentMaxCharacters - 1, Math.Min(AudioConstants.BotVoiceIdealSegmentMaxCharacters, remaining.Length));
            if (splitIndex < AudioConstants.BotVoiceIdealSegmentMinCharacters)
            {
                splitIndex = remaining.LastIndexOf(' ', Math.Min(AudioConstants.BotVoiceIdealSegmentMaxCharacters - 1, remaining.Length - 1), Math.Min(AudioConstants.BotVoiceIdealSegmentMaxCharacters, remaining.Length));
            }

            if (splitIndex < AudioConstants.BotVoiceIdealSegmentMinCharacters)
            {
                splitIndex = remaining.LastIndexOf(' ', Math.Min(AudioConstants.BotVoiceAbsoluteMaxSegmentCharacters - 1, remaining.Length - 1), Math.Min(AudioConstants.BotVoiceAbsoluteMaxSegmentCharacters, remaining.Length));
            }

            if (splitIndex < AudioConstants.BotVoiceIdealSegmentMinCharacters)
            {
                splitIndex = Math.Min(AudioConstants.BotVoiceAbsoluteMaxSegmentCharacters - 1, remaining.Length - 1);
            }

            var nextSegment = remaining[..(splitIndex + 1)].Trim();
            AddTrimmedSegment(output, nextSegment);
            remaining = remaining[(splitIndex + 1)..].Trim();
        }

        AddTrimmedSegment(output, remaining);
    }

    private static void AddTrimmedSegment(List<string> segments, string segment)
    {
        var trimmedSegment = NormalizeBotVoiceSegmentText(segment);
        if (!string.IsNullOrWhiteSpace(trimmedSegment))
        {
            segments.Add(trimmedSegment);
        }
    }

    private static string CreateBotVoiceSegmentCacheKey(int messageId, int segmentIndex, string segmentText)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}:{2}",
            messageId,
            segmentIndex,
            NormalizeBotVoiceSegmentText(segmentText));
    }

    private void SetCurrentBotVoiceCancellationTokenSource(CancellationTokenSource cancellationTokenSource)
    {
        lock (botVoiceCancellationLock)
        {
            currentBotVoiceCancellationTokenSource = cancellationTokenSource;
            currentBotVoiceCancellationReason = BotVoiceCancellationReasons.AppDisposalCancel;
        }
    }

    private void SetCurrentBotVoiceCancellationReason(string cancellationReason)
    {
        lock (botVoiceCancellationLock)
        {
            currentBotVoiceCancellationReason = cancellationReason;
        }
    }

    private string GetCurrentBotVoiceCancellationReason()
    {
        lock (botVoiceCancellationLock)
        {
            return currentBotVoiceCancellationReason;
        }
    }

    private void ClearCurrentBotVoiceCancellationTokenSource(CancellationTokenSource cancellationTokenSource)
    {
        lock (botVoiceCancellationLock)
        {
            if (ReferenceEquals(currentBotVoiceCancellationTokenSource, cancellationTokenSource))
            {
                currentBotVoiceCancellationTokenSource = null;
                currentBotVoiceCancellationReason = BotVoiceCancellationReasons.AppDisposalCancel;
            }
        }
    }

    private void CancelCurrentBotVoice(string reason)
    {
        CancellationTokenSource? cancellationTokenSource;
        lock (botVoiceCancellationLock)
        {
            cancellationTokenSource = currentBotVoiceCancellationTokenSource;
            if (cancellationTokenSource is null || cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            currentBotVoiceCancellationReason = reason;
        }

        Debug.WriteLine($"Canceling bot voice: Path={AudioConstants.BotVoiceDefaultPathName}; CancellationReason={reason}.");
        cancellationTokenSource.Cancel();
        audioPlaybackService.StopPlayback();
    }

    private bool IsNewestBotMessage(ChatMessageViewModel message)
    {
        return Messages.LastOrDefault(candidate => candidate.IsFromBot)?.Id == message.Id;
    }

    private bool IsSetupBotMessage(ChatMessageViewModel message)
    {
        return message.IsFromBot && CurrentLessonPhase == LessonPhase.SetupContextSelection;
    }

    private bool IsSetupVoiceMessage(ChatMessageViewModel message)
    {
        return IsSetupBotMessage(message) || LooksLikeSetupContextSelectionMessage(message.Text);
    }

    private static bool LooksLikeSetupContextSelectionMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalizedText = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return normalizedText.Contains("Choose a situation:", StringComparison.OrdinalIgnoreCase)
            || normalizedText.Contains("Choose a context:", StringComparison.OrdinalIgnoreCase);
    }

    public void CleanupCurrentSessionBotVoiceFiles()
    {
        CancelCurrentBotVoice(BotVoiceCancellationReasons.AppDisposalCancel);
        audioPlaybackService.StopPlayback();
        CleanupTrackedBotVoiceFiles();
    }

    private async Task CleanupCurrentSessionBotVoiceFilesAsync()
    {
        CancelCurrentBotVoice(BotVoiceCancellationReasons.AppDisposalCancel);
        audioPlaybackService.StopPlayback();

        if (await botVoiceSemaphore.WaitAsync(TimeSpan.FromSeconds(2)))
        {
            try
            {
                CleanupTrackedBotVoiceFiles();
            }
            finally
            {
                botVoiceSemaphore.Release();
            }

            return;
        }

        CleanupTrackedBotVoiceFiles();
    }

    private void TrackCurrentSessionBotVoiceFile(string filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            currentSessionBotVoiceFilePaths.Add(filePath);
        }
    }

    private void CleanupTrackedBotVoiceFiles()
    {
        botVoiceTempFileCleanupService.CleanupFiles(currentSessionBotVoiceFilePaths);
        currentSessionBotVoiceFilePaths.RemoveWhere(filePath => !File.Exists(filePath));
    }

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync()
    {
        if (!IsLessonInputEnabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(UserInput))
        {
            StatusMessage = localizedText.EmptyMessageWarning;
            return;
        }

        var trimmedUserInput = UserInput.Trim();
        CancelCurrentBotVoice(BotVoiceCancellationReasons.NewerMessageCancel);
        var wasSent = await SendLessonMessageAsync(trimmedUserInput, ChatMessageSource.Typed);

        if (wasSent)
        {
            UserInput = string.Empty;
        }
    }

    private async Task<bool> SendLessonMessageAsync(string userMessage, string messageSource = ChatMessageSource.Typed)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return false;
        }

        if (!IsLessonInputEnabled)
        {
            if (CurrentLessonPhase == LessonPhase.Completed || IsLessonLimitReached)
            {
                MarkLessonCompleteAwaitingFinish();
            }

            return false;
        }

        CurrentHintText = string.Empty;

        if (CurrentLessonPhase == LessonPhase.SetupContextSelection && !IsFreeConversationLesson())
        {
            return await HandleContextSelectionMessageAsync(userMessage);
        }

        if (CurrentLessonPhase == LessonPhase.Completed)
        {
            MarkLessonCompleteAwaitingFinish();
            return false;
        }

        var activeTurnTranscriptValidation = LessonTranscriptValidator.Validate(userMessage);
        var activeTurnPolicyPreview = LessonTurnPolicy.EvaluateUserInput(BuildTurnPolicyContext(), activeTurnTranscriptValidation.IsValid);
        Debug.WriteLine(
            $"Lesson chat transcript validation: IsValid={activeTurnTranscriptValidation.IsValid}; " +
            $"Reason={activeTurnTranscriptValidation.Reason}; " +
            $"TurnCounted={activeTurnPolicyPreview.ShouldCountUserTurn}; " +
            $"LearnerTurnCountBefore={activeTurnPolicyPreview.LearnerTurnCountBefore}; " +
            $"LearnerTurnCountAfter={activeTurnPolicyPreview.LearnerTurnCountAfter}; " +
            $"PhaseBefore={activeTurnPolicyPreview.PhaseBefore}; PhaseAfter={activeTurnPolicyPreview.PhaseAfter}; " +
            $"NormalAssistantResponseCreated={activeTurnTranscriptValidation.IsValid}; RetryPromptShown={!activeTurnTranscriptValidation.IsValid}.");
        if (!activeTurnTranscriptValidation.IsValid)
        {
            StatusMessage = LessonTranscriptValidator.GetRetryMessage(SelectedLevel);
            return false;
        }

        userMessage = activeTurnTranscriptValidation.NormalizedTranscript;
        var nextLearnerTurnCount = activeTurnPolicyPreview.LearnerTurnCountAfter;
        var softWrapUpTurn = GetSoftWrapUpTurn();
        var finalTurn = GetFinalTurn();

        if (LearnerTurnCount >= finalTurn)
        {
            LogFinalLimitReached(finalTurn);
            MarkLessonCompleteAwaitingFinish();
            return false;
        }

        if (IsRealtimeConversationActive)
        {
            return await SendRealtimeLessonMessageAsync(userMessage);
        }

        if (nextLearnerTurnCount >= finalTurn)
        {
            AddLearnerMessage(userMessage, messageSource, nextLearnerTurnCount, feedback: null);
            LearnerTurnCount = nextLearnerTurnCount;
            LogFinalLimitReached(finalTurn);
            var finalMessage = GetFinalLessonMessage();
            var botMessage = AddMessage(TutorAvatarDisplayName, finalMessage, true);
            lastBotMessage = finalMessage;
            OnPropertyChanged(nameof(LatestBotMessageText));
            await TryAutoPlayNewestBotVoiceAsync(botMessage);
            CurrentLessonPhase = LessonPhase.Completed;
            MarkLessonCompleteAwaitingFinish();
            return true;
        }

        BotStatus = BackendConstants.BotStatusThinking;
        IsSending = true;
        RefreshAvatarState();

        var shouldStartWrappingUp = nextLearnerTurnCount >= softWrapUpTurn;
        var shouldEndLessonNow = nextLearnerTurnCount >= finalTurn;

        try
        {
            var response = await lessonChatBackendService.SendLessonMessageAsync(new LessonChatBackendRequest
            {
                SelectedLevel = SelectedLevel,
                TopicTitle = SelectedTopic.Title,
                SubtopicTitle = SelectedSubtopic.Title,
                UserMessage = userMessage,
                LastBotMessage = lastBotMessage,
                NativeLanguageName = nativeLanguageName,
                TutorAvatarId = tutorAvatarId,
                UserDisplayName = this.UserDisplayName,
                LearningGoal = this.LearningGoal,
                LearnerTurnCount = nextLearnerTurnCount,
                SoftLearnerTurnLimit = softWrapUpTurn,
                HardLearnerTurnLimit = finalTurn,
                RemainingLearnerTurns = Math.Max(finalTurn - nextLearnerTurnCount, 0),
                ShouldStartWrappingUp = shouldStartWrappingUp,
                ShouldEndLessonNow = shouldEndLessonNow,
                RecentMessages = GetRecentConversationMessages(),
                LessonPhase = CurrentLessonPhase.ToString(),
                LessonScenarioId = lessonScenario.Id,
                Level = SelectedLevel,
                Topic = lessonScenario.Metadata.Topic,
                Subtopic = lessonScenario.Metadata.Subtopic,
                LessonGoal = lessonScenario.LearningGoal.Goal,
                LessonType = lessonScenario.Metadata.LessonType,
                AiTutorPromptInstructions = lessonScenario.AiTutorPromptInstructions,
                SelectedContextVariantId = selectedContextVariant?.Id ?? string.Empty,
                SelectedContextTitle = GetSelectedContextTitle(),
                SelectedContextOpeningLine = selectedContextVariant?.OpeningLine ?? lessonScenario.ConversationFlow.DefaultOpeningExample,
                UserTurnNumber = nextLearnerTurnCount,
                SoftWrapUpAfterUserTurn = softWrapUpTurn,
                FinalMessageAtUserTurn = finalTurn,
                TargetLanguageKeyPhrases = lessonScenario.TargetLanguage.KeyPhrases,
                GrammarFocus = lessonScenario.TargetLanguage.GrammarFocus,
                ConversationOpening = lessonScenario.ConversationFlow.Opening,
                ConversationFirstUserTask = lessonScenario.ConversationFlow.FirstUserTask,
                ConversationGuidedPracticeFollowUpQuestions = lessonScenario.ConversationFlow.GuidedPracticeFollowUpQuestions,
                ConversationVariationOrComplication = lessonScenario.ConversationFlow.VariationOrComplication,
                ConversationCorrectionMoment = lessonScenario.ConversationFlow.CorrectionMoment,
                ConversationWrapUpMessage = lessonScenario.ConversationFlow.WrapUpMessage,
                ConversationFinalMessage = GetFinalLessonMessage(),
                FeedbackRulesSummary = BuildFeedbackRulesSummary(),
                TutorProfileId = tutorAvatarId,
                ActiveLevelProfileDifficultyNotes = activeLevelProfile.DifficultyNotes,
                ActiveLevelProfileTutorLanguageStyle = activeLevelProfile.TutorLanguageStyle,
                ActiveLevelProfileExpectedUserResponse = activeLevelProfile.ExpectedUserResponse,
                ActiveLevelProfileFeedbackStrictness = activeLevelProfile.FeedbackStrictness,
                ActiveLevelProfileHintStrategy = activeLevelProfile.HintStrategy,
                ActiveLevelProfileCorrectionPriority = activeLevelProfile.CorrectionPriority,
                ActiveLevelProfileConversationDepth = activeLevelProfile.ConversationDepth,
                ActiveLevelProfileExampleGoodAnswer = activeLevelProfile.ExampleGoodAnswer,
                ActiveLevelProfileExampleStretchAnswer = activeLevelProfile.ExampleStretchAnswer,
                ActiveLevelProfileAddedKeyPhrases = activeLevelProfile.AddedKeyPhrases,
                ActiveLevelProfileAddedUsefulConstructions = activeLevelProfile.AddedUsefulConstructions,
                ActiveLevelProfileAddedGrammarFocus = activeLevelProfile.AddedGrammarFocus
            });

            var mappedFeedback = MapFeedback(response.Feedback);
            AddLearnerMessage(userMessage, messageSource, nextLearnerTurnCount, mappedFeedback);
            LearnerTurnCount = nextLearnerTurnCount;
            var botReply = response.BotReply;
            var botMessage = AddMessage(TutorAvatarDisplayName, botReply, true);
            lastBotMessage = botReply;
            OnPropertyChanged(nameof(LatestBotMessageText));

            BackendStatusText = BackendConstants.BackendStatusConnected;
            StatusMessage = string.Empty;
            BotStatus = BackendConstants.BotStatusReady;
            IsSending = false;
            RefreshAvatarState();

            if (response.IsLessonComplete || shouldEndLessonNow)
            {
                await TryAutoPlayNewestBotVoiceAsync(botMessage);
                CurrentLessonPhase = LessonPhase.Completed;
                MarkLessonCompleteAwaitingFinish();
                return true;
            }

            await TryAutoPlayNewestBotVoiceAsync(botMessage);

            return true;
        }
        catch
        {
            BackendStatusText = BackendConstants.BackendStatusUnavailable;
            StatusMessage = localizedText.BackendUnavailableMessage;
            return false;
        }
        finally
        {
            BotStatus = BackendConstants.BotStatusReady;
            IsSending = false;
            RefreshAvatarState();
        }
    }


    private async Task<bool> SendRealtimeLessonMessageAsync(string userMessage)
    {
        var nextLearnerTurnCount = LearnerTurnCount + 1;
        var finalTurn = GetFinalTurn();
        if (LearnerTurnCount >= finalTurn)
        {
            LogFinalLimitReached(finalTurn);
            MarkLessonCompleteAwaitingFinish();
            return false;
        }

        try
        {
            await EnsureRealtimeSessionStartedAsync(CancellationToken.None);
            AddLearnerMessage(userMessage, ChatMessageSource.RealtimeVoice, nextLearnerTurnCount, feedback: null);
            LearnerTurnCount = nextLearnerTurnCount;
            if (LearnerTurnCount >= finalTurn)
            {
                LogFinalLimitReached(finalTurn);
            }

            PrepareRealtimeAssistantPlaceholder();
            await realtimeVoiceEngine.SendUserTextAsync(userMessage, CancellationToken.None);
            StatusMessage = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Realtime text turn failed: SessionId={realtimeSessionId}; {exception}");
            StatusMessage = BackendConstants.RealtimeUnavailableMessage;
            return false;
        }
    }

    private async Task EnsureRealtimeSessionStartedAsync(CancellationToken cancellationToken)
    {
        if (isRealtimeSessionStarted)
        {
            return;
        }

        isStartingRealtimeSession = true;
        RefreshAllCommandStates();

        try
        {
            realtimeSessionId = Guid.NewGuid().ToString("N");
            var stopwatch = Stopwatch.StartNew();
            await realtimeVoiceEngine.StartSessionAsync(BuildVoiceSessionStartRequest(), cancellationToken);
            isRealtimeSessionStarted = true;
            BackendStatusText = BackendConstants.BackendStatusConnected;
            Debug.WriteLine($"Desktop realtime session start ms: SessionId={realtimeSessionId}; RealtimeSessionStartMs={stopwatch.ElapsedMilliseconds}; TutorProfileId={tutorProfile.Id}; TutorDisplayName={tutorProfile.DisplayName}; LessonType={lessonScenario.Metadata.LessonType}; Topic={lessonScenario.Metadata.Topic}; Subtopic={lessonScenario.Metadata.Subtopic}; Level={SelectedLevel}; SelectedContextTitle={GetSelectedContextTitle()}.");
        }
        finally
        {
            isStartingRealtimeSession = false;
            RefreshAllCommandStates();
        }
    }

    private VoiceSessionStartRequest BuildVoiceSessionStartRequest()
    {
        return new VoiceSessionStartRequest
        {
            SessionId = realtimeSessionId,
            TutorProfileId = string.IsNullOrWhiteSpace(tutorProfile.Id) ? tutorAvatarId : tutorProfile.Id,
            TutorDisplayName = string.IsNullOrWhiteSpace(tutorProfile.DisplayName) ? TutorAvatarDisplayName : tutorProfile.DisplayName,
            TutorProfileAge = tutorProfile.Age,
            TutorProfileHomeCity = tutorProfile.HomeCity,
            TutorProfileCountryOrRegion = tutorProfile.CountryOrRegion,
            TutorProfileStudies = tutorProfile.Studies,
            TutorProfileHobbies = tutorProfile.Hobbies,
            TutorProfileCommunicationStyle = tutorProfile.CommunicationStyle,
            TutorProfileSpeakingRules = tutorProfile.SpeakingRules,
            TutorProfileIdentityRules = tutorProfile.IdentityRules,
            SelectedLevel = SelectedLevel,
            Topic = lessonScenario.Metadata.Topic,
            TopicTitle = SelectedTopic.Title,
            Subtopic = lessonScenario.Metadata.Subtopic,
            SubtopicTitle = SelectedSubtopic.Title,
            LessonScenarioId = lessonScenario.Id,
            LessonType = lessonScenario.Metadata.LessonType,
            LessonGoal = lessonScenario.LearningGoal.Goal,
            LessonPhase = CurrentLessonPhase.ToString(),
            CurrentPhase = CurrentLessonPhase.ToString(),
            TutorRole = lessonScenario.Roles.TutorRole,
            UserRole = lessonScenario.Roles.UserRole,
            Situation = lessonScenario.Situation.Description,
            NativeLanguageName = nativeLanguageName,
            UserDisplayName = UserDisplayName,
            LearningGoal = LearningGoal,
            SelectedContextVariantId = selectedContextVariant?.Id ?? string.Empty,
            SelectedContextTitle = GetSelectedContextTitle(),
            SelectedContextOpeningLine = selectedContextVariant?.OpeningLine ?? lessonScenario.ConversationFlow.DefaultOpeningExample,
            LastBotMessage = lastBotMessage,
            LearnerTurnCount = LearnerTurnCount,
            SoftLearnerTurnLimit = GetSoftWrapUpTurn(),
            HardLearnerTurnLimit = GetFinalTurn(),
            TargetLanguageKeyPhrases = lessonScenario.TargetLanguage.KeyPhrases,
            GrammarFocus = lessonScenario.TargetLanguage.GrammarFocus,
            ConversationOpening = lessonScenario.ConversationFlow.Opening,
            ConversationFirstUserTask = lessonScenario.ConversationFlow.FirstUserTask,
            ConversationGuidedPracticeFollowUpQuestions = lessonScenario.ConversationFlow.GuidedPracticeFollowUpQuestions,
            ConversationVariationOrComplication = lessonScenario.ConversationFlow.VariationOrComplication,
            ConversationCorrectionMoment = lessonScenario.ConversationFlow.CorrectionMoment,
            ConversationWrapUpMessage = lessonScenario.ConversationFlow.WrapUpMessage,
            ConversationFinalMessage = GetFinalLessonMessage(),
            FeedbackRulesSummary = BuildFeedbackRulesSummary(),
            AiTutorPromptInstructions = lessonScenario.AiTutorPromptInstructions,
            ActiveLevelProfile = activeLevelProfile,
            RecentMessages = GetRecentConversationMessages()
        };
    }

    private void PrepareRealtimeAssistantPlaceholder()
    {
        BotStatus = BackendConstants.BotStatusThinking;
        IsSending = true;
        RefreshAvatarState();
        realtimeAssistantMessage = AddMessage(TutorAvatarDisplayName, $"{TutorAvatarDisplayName} is speaking...", true);
        realtimeAudioPlaybackService.StartSession(realtimeSessionId, string.Empty);
    }

    private async Task StopRealtimeConversationAsync(string reason)
    {
        realtimeAudioPlaybackService.Stop(reason);
        realtimeMicrophoneCaptureService.Stop();
        await realtimeVoiceEngine.StopSessionAsync(CancellationToken.None);
        isRealtimeSessionStarted = false;
        RefreshAllCommandStates();
    }

    private void OnRealtimeMicrophoneAudioChunkCaptured(object? sender, RealtimeMicrophoneAudioChunkEventArgs args)
    {
        _ = realtimeVoiceEngine.AppendUserAudioAsync(args.AudioChunk, CancellationToken.None);
    }

    private void OnRealtimeAssistantAudioChunkReceived(object? sender, AssistantAudioChunkReceivedEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            realtimeAudioPlaybackService.AddAudioChunk(args.SessionId, args.ResponseId, args.AudioChunk);
            IsBotVoicePlaying = true;
            RefreshAvatarState();
        });
    }

    private void OnRealtimeAssistantTranscriptDeltaReceived(object? sender, AssistantTranscriptDeltaEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (realtimeAssistantMessage is null)
            {
                realtimeAssistantMessage = AddMessage(TutorAvatarDisplayName, string.Empty, true);
            }

            realtimeAssistantMessage.Text = args.TranscriptSoFar;
            lastBotMessage = args.TranscriptSoFar;
            OnPropertyChanged(nameof(LatestBotMessageText));
        });
    }

    private void OnRealtimeAssistantTurnCompleted(object? sender, AssistantTurnCompletedEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var finalTranscript = string.IsNullOrWhiteSpace(args.Transcript) ? lastBotMessage : args.Transcript.Trim();
            if (realtimeAssistantMessage is not null)
            {
                realtimeAssistantMessage.Text = finalTranscript;
            }
            lastBotMessage = finalTranscript;
            OnPropertyChanged(nameof(LatestBotMessageText));
            realtimeAudioPlaybackService.CompleteResponse(args.SessionId, args.ResponseId);
            IsBotVoicePlaying = false;
            IsSending = false;
            BotStatus = BackendConstants.BotStatusReady;
            if (LearnerTurnCount >= GetFinalTurn())
            {
                LogFinalLimitReached(GetFinalTurn());
                MarkLessonCompleteAwaitingFinish();
            }
            RefreshAvatarState();
        });
    }

    private void OnRealtimeUserAudioCommitted(object? sender, UserAudioCommittedEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            realtimeUserPlaceholderItemId = args.ItemId;
            if (realtimeUserPlaceholderMessage is not null && !string.IsNullOrWhiteSpace(args.ItemId))
            {
                realtimeItemIdToChatMessageId[args.ItemId] = realtimeUserPlaceholderMessage.Id;
                if (pendingTranscriptByItemId.Remove(args.ItemId, out var bufferedTranscript))
                {
                    ApplyRealtimeUserTranscript(args.ItemId, bufferedTranscript, args.SessionId);
                }
                else if (pendingTranscriptFailureByItemId.Remove(args.ItemId, out _))
                {
                    ApplyRealtimeUserTranscriptFailure(args.ItemId, args.SessionId);
                }
            }

            Debug.WriteLine($"Realtime user audio committed in UI: SessionId={args.SessionId}; ItemId={args.ItemId}; UserPlaceholderMessageId={realtimeUserPlaceholderMessage?.Id}; UserAudioCommittedMs={args.ElapsedMilliseconds}.");
        });
    }

    private void OnRealtimeUserTranscriptDeltaReceived(object? sender, UserTranscriptDeltaEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            realtimeUserTranscriptBuffer.Append(args.Delta);
            var transcriptSoFar = realtimeUserTranscriptBuffer.ToString().Trim();
            Debug.WriteLine($"Realtime user transcript delta in UI: SessionId={args.SessionId}; ItemId={args.ItemId}; UserPlaceholderMessageId={realtimeUserPlaceholderMessage?.Id}; TranscriptLength={transcriptSoFar.Length}.");
            if (!string.IsNullOrWhiteSpace(transcriptSoFar))
            {
                var target = FindRealtimeUserMessage(args.ItemId);
                if (target is not null)
                {
                    target.Text = transcriptSoFar;
                }
            }
        });
    }

    private void OnRealtimeUserTranscriptCompleted(object? sender, UserTranscriptCompletedEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var transcript = args.Transcript.Trim();
            if (string.IsNullOrWhiteSpace(transcript))
            {
                return;
            }

            var itemId = string.IsNullOrWhiteSpace(args.ItemId) ? realtimeUserPlaceholderItemId : args.ItemId;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                ApplyRealtimeUserTranscript(itemId, transcript, args.SessionId);
                return;
            }

            if (FindRealtimeUserMessage(itemId) is null)
            {
                pendingTranscriptByItemId[itemId] = transcript;
                return;
            }

            ApplyRealtimeUserTranscript(itemId, transcript, args.SessionId);
        });
    }


    private void OnRealtimeUserTranscriptFailed(object? sender, UserTranscriptFailedEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var itemId = string.IsNullOrWhiteSpace(args.ItemId) ? realtimeUserPlaceholderItemId : args.ItemId;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                ApplyRealtimeUserTranscriptFailure(itemId, args.SessionId);
                return;
            }

            if (FindRealtimeUserMessage(itemId) is null)
            {
                pendingTranscriptFailureByItemId[itemId] = args.Message;
                return;
            }

            ApplyRealtimeUserTranscriptFailure(itemId, args.SessionId);
        });
    }

    private ChatMessageViewModel? FindRealtimeUserMessage(string itemId)
    {
        if (!string.IsNullOrWhiteSpace(itemId)
            && realtimeItemIdToChatMessageId.TryGetValue(itemId, out var messageId))
        {
            return Messages.FirstOrDefault(message => message.Id == messageId);
        }

        return realtimeUserPlaceholderMessage;
    }

    private void ApplyRealtimeUserTranscript(string itemId, string transcript, string sessionId)
    {
        var target = FindRealtimeUserMessage(itemId);
        if (target is null)
        {
            return;
        }

        realtimeUserPlaceholderItemId = itemId;
        realtimeUserTranscriptBuffer.Clear();
        var validation = LessonTranscriptValidator.Validate(transcript);
        var turnResult = LessonTurnPolicy.EvaluateUserInput(BuildTurnPolicyContext(), validation.IsValid);
        Debug.WriteLine(
            $"Realtime transcript validation: SessionId={sessionId}; ItemId={itemId}; " +
            $"IsValid={validation.IsValid}; Reason={validation.Reason}; " +
            $"TurnCounted={turnResult.ShouldCountUserTurn}; " +
            $"LearnerTurnCountBefore={turnResult.LearnerTurnCountBefore}; " +
            $"LearnerTurnCountAfter={turnResult.LearnerTurnCountAfter}; " +
            $"PhaseBefore={turnResult.PhaseBefore}; PhaseAfter={turnResult.PhaseAfter}; " +
            $"NormalAssistantResponseCreated={validation.IsValid}; RetryPromptShown={!validation.IsValid}.");

        if (!validation.IsValid)
        {
            target.MarkAsInvalidLearnerTranscript(RealtimeVoiceTranscriptionUnavailableText);
            StatusMessage = LessonTranscriptValidator.GetRetryMessage(SelectedLevel);
            BotStatus = BackendConstants.BotStatusReady;
            IsSending = false;
            RefreshAvatarState();
            RefreshAllCommandStates();
            ViewFeedbackCommand.NotifyCanExecuteChanged();
            return;
        }

        realtimeUserTranscriptBuffer.Append(validation.NormalizedTranscript);
        target.MarkAsValidLearnerTurn(validation.NormalizedTranscript, turnResult.LearnerTurnCountAfter);
        ViewFeedbackCommand.NotifyCanExecuteChanged();
        LearnerTurnCount = turnResult.LearnerTurnCountAfter;
        Debug.WriteLine($"Realtime placeholder replaced with transcript: SessionId={sessionId}; ItemId={itemId}; UserPlaceholderMessageId={target.Id}; TranscriptLength={target.Text.Length}; LearnerTurnCount={LearnerTurnCount}.");

        if (turnResult.ShouldUseFinalMessage)
        {
            LogFinalLimitReached(turnResult.FinalTurn);
        }

        PrepareRealtimeAssistantPlaceholder();
        StatusMessage = string.Empty;
    }

    private void ApplyRealtimeUserTranscriptFailure(string itemId, string sessionId)
    {
        var target = FindRealtimeUserMessage(itemId);
        if (target is null)
        {
            return;
        }

        realtimeUserPlaceholderItemId = itemId;
        target.MarkAsInvalidLearnerTranscript(RealtimeVoiceTranscriptionUnavailableText);
        StatusMessage = LessonTranscriptValidator.GetRetryMessage(SelectedLevel);
        BotStatus = BackendConstants.BotStatusReady;
        IsSending = false;
        RefreshAvatarState();
        RefreshAllCommandStates();
        ViewFeedbackCommand.NotifyCanExecuteChanged();
        Debug.WriteLine($"Realtime placeholder marked transcription unavailable: SessionId={sessionId}; ItemId={itemId}; UserPlaceholderMessageId={target.Id}; LearnerTurnCountBefore={LearnerTurnCount}; LearnerTurnCountAfter={LearnerTurnCount}; RetryPromptShown=True; NormalAssistantResponseCreated=False.");
    }

    private void OnRealtimeErrorReceived(object? sender, VoiceSessionErrorEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Debug.WriteLine($"Realtime session error: SessionId={args.SessionId}; ResponseId={args.ResponseId}; Message={args.Message}; Exception={args.Exception}");
            StatusMessage = BackendConstants.RealtimeUnavailableMessage;
            IsSending = false;
            IsBotVoicePlaying = false;
            isRealtimeSessionStarted = false;
            RefreshAvatarState();
            RefreshAllCommandStates();
            LogLessonStateSnapshot("Realtime start failure");
        });
    }

    private void OnRealtimePlaybackStarted(object? sender, RealtimePlaybackStartedEventArgs args)
    {
        Debug.WriteLine($"Desktop realtime playback started ms: SessionId={args.SessionId}; ResponseId={args.ResponseId}; PlaybackStartedMs={args.ElapsedMilliseconds}; BufferUnderrunCount={realtimeAudioPlaybackService.UnderrunCount}.");
    }

    private async Task<bool> HandleContextSelectionMessageAsync(string userMessage)
    {
        var learnerTurnCountBefore = LearnerTurnCount;
        AddMessage(AppConstants.UserSenderName, userMessage, false, source: ChatMessageSource.Typed, isTechnicalMessage: true);

        var matchedVariant = FindMatchingContextVariant(userMessage);
        if (matchedVariant is not null)
        {
            selectedContextVariant = matchedVariant;
            selectedCustomContextTitle = string.Empty;

            var startMessage = $"Great! Let's imagine {BuildContextConfirmationText(matchedVariant)}.\n\n{matchedVariant.OpeningLine}";
            await StartActiveRoleplayAfterContextSelectionAsync(startMessage, learnerTurnCountBefore);
            return true;
        }

        if (IsValidCustomContext(userMessage))
        {
            selectedContextVariant = null;
            selectedCustomContextTitle = userMessage.Trim();

            var openingLine = string.IsNullOrWhiteSpace(lessonScenario.ConversationFlow.DefaultOpeningExample)
                ? "Hi! Nice to meet you. What's your name?"
                : lessonScenario.ConversationFlow.DefaultOpeningExample.Trim();
            await StartActiveRoleplayAfterContextSelectionAsync($"Good idea. Let's keep it simple: {userMessage.Trim()}.\n\n{openingLine}", learnerTurnCountBefore);
            return true;
        }

        AddMessage(TutorAvatarDisplayName, GetInvalidContextRedirect(), true);
        lastBotMessage = GetInvalidContextRedirect();
        OnPropertyChanged(nameof(LatestBotMessageText));
        StatusMessage = string.Empty;
        LogLessonStateSnapshot("context selection invalid");
        return true;
    }

    private async Task StartActiveRoleplayAfterContextSelectionAsync(string startMessage, int learnerTurnCountBefore)
    {
        CurrentLessonPhase = LessonPhase.ActiveRoleplay;
        var roleplayStartMessage = AddRoleplayStartMessage(startMessage);
        RefreshAllCommandStates();
        Debug.WriteLine($"PhaseTransition SetupContextSelection -> ActiveRoleplay; SelectedContextVariantId={selectedContextVariant?.Id ?? string.Empty}; SelectedContextTitle={GetSelectedContextTitle()}; LearnerTurnCountBefore={learnerTurnCountBefore}; LearnerTurnCountAfter={LearnerTurnCount}; ConversationModeEnabled={IsConversationModeEnabled}; RealtimeStartDeferred={IsConversationModeEnabled && BackendConstants.UseRealtimeConversationMode}.");
        LogLessonStateSnapshot("context selected");
        LogLessonStateSnapshot("ActiveRoleplay start");
        await TryStartRealtimeAfterGuidedContextSelectionAsync();

        if (IsConversationModeEnabled && BackendConstants.UseRealtimeConversationMode)
        {
            // Scripted lesson opening playback is allowed through exact TTS because the text is fixed and visible.
            // Realtime generated assistant turns must not use chained TTS; their text/audio come from Realtime events.
            Debug.WriteLine($"Guided conversation scripted opening playback requested: MessageId={roleplayStartMessage.Id}; VoiceTextLength={roleplayStartMessage.Text.Trim().Length}; RealtimeSessionStarted={isRealtimeSessionStarted}.");
            await PlayBotVoiceForMessageAsync(roleplayStartMessage, isAutoPlay: false);
        }
    }

    private async Task TryStartRealtimeAfterGuidedContextSelectionAsync()
    {
        if (!IsConversationModeEnabled || !BackendConstants.UseRealtimeConversationMode)
        {
            return;
        }

        try
        {
            Debug.WriteLine($"Starting realtime after guided context selection: LessonType={lessonScenario.Metadata.LessonType}; CurrentLessonPhase={CurrentLessonPhase}; SelectedContextTitle={GetSelectedContextTitle()}; BackendEndpoint={lessonChatBackendService.CreateRealtimeVoiceWebSocketUri()}.");
            await EnsureRealtimeSessionStartedAsync(CancellationToken.None);
            StatusMessage = string.Empty;
            LogLessonStateSnapshot("Realtime start success");
        }
        catch (Exception exception)
        {
            IsConversationModeEnabled = false;
            isRealtimeSessionStarted = false;
            BackendStatusText = BackendConstants.BackendStatusUnavailable;
            StatusMessage = BackendConstants.RealtimeUnavailableMessage;
            Debug.WriteLine($"Realtime start after guided context selection failed: RealtimeSessionId={realtimeSessionId}; ExceptionType={exception.GetType().FullName}; Message={exception.Message}; {exception}");
            LogLessonStateSnapshot("Realtime start failure");
        }
        finally
        {
            RefreshAllCommandStates();
        }
    }

    private ChatMessageViewModel AddRoleplayStartMessage(string message)
    {
        var botMessage = AddMessage(TutorAvatarDisplayName, message, true);
        lastBotMessage = message;
        OnPropertyChanged(nameof(LatestBotMessageText));
        StatusMessage = string.Empty;
        _ = TryAutoPlayNewestBotVoiceAsync(botMessage);
        return botMessage;
    }

    private ContextVariant? FindMatchingContextVariant(string userMessage)
    {
        var normalizedInput = NormalizeForContextMatching(userMessage);
        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return null;
        }

        foreach (var variant in lessonScenario.ControlledVariation.ContextVariants)
        {
            var candidates = new[] { variant.Id, variant.Title }
                .Concat(variant.Aliases)
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate));

            foreach (var candidate in candidates)
            {
                var normalizedCandidate = NormalizeForContextMatching(candidate);
                if (normalizedInput.Equals(normalizedCandidate, StringComparison.OrdinalIgnoreCase)
                    || normalizedInput.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase)
                    || normalizedCandidate.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase))
                {
                    return variant;
                }
            }
        }

        return null;
    }

    private bool IsValidCustomContext(string userMessage)
    {
        if (!lessonScenario.LessonSetup.ContextSelection.CustomContextAllowed)
        {
            return false;
        }

        var normalizedInput = NormalizeForContextMatching(userMessage);
        var keywords = lessonScenario.LessonSetup.ContextSelection.ValidCustomContextKeywords.Count > 0
            ? lessonScenario.LessonSetup.ContextSelection.ValidCustomContextKeywords
            : ["meet", "meeting", "introduce", "introduction", "first time"];

        return keywords
            .Select(NormalizeForContextMatching)
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Any(keyword => normalizedInput.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private string GetInvalidContextRedirect()
    {
        if (!string.IsNullOrWhiteSpace(lessonScenario.LessonSetup.ContextSelection.InvalidContextRedirect))
        {
            return lessonScenario.LessonSetup.ContextSelection.InvalidContextRedirect.Trim();
        }

        if (!string.IsNullOrWhiteSpace(lessonScenario.ControlledVariation.InvalidContextRedirect))
        {
            return lessonScenario.ControlledVariation.InvalidContextRedirect.Trim();
        }

        return $"That sounds interesting, but this lesson is about {SelectedSubtopic.Title.ToLowerInvariant()}. Please choose a situation that matches this lesson.";
    }

    private string BuildSetupContextHint()
    {
        var titles = lessonScenario.ControlledVariation.ContextVariants
            .Take(3)
            .Select(variant => $"\"{variant.Title}\"")
            .ToArray();

        return titles.Length == 0
            ? $"Choose a simple situation about {SelectedSubtopic.Title.ToLowerInvariant()}."
            : $"You can choose: {string.Join(", ", titles)}.";
    }

    private static string BuildContextConfirmationText(ContextVariant variant)
    {
        const string meetingPrefix = "Meeting ";
        const string meetingReasonPrefix = "meeting ";

        if (variant.ReasonForMeeting.StartsWith(meetingReasonPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return $"you meet {variant.ReasonForMeeting[meetingReasonPrefix.Length..].ToLowerInvariant()}";
        }

        if (variant.Title.StartsWith(meetingPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return $"you meet {variant.Title[meetingPrefix.Length..].ToLowerInvariant()}";
        }

        return $"this situation: {variant.Title}";
    }

    private static string NormalizeForContextMatching(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace('_', ' ').ToLowerInvariant();
    }

    private string GetSelectedContextTitle()
    {
        if (selectedContextVariant is not null)
        {
            return selectedContextVariant.Title;
        }

        return selectedCustomContextTitle;
    }

    private int GetSoftWrapUpTurn()
    {
        return LessonTurnPolicy.ResolveSoftWrapUpTurn(BuildTurnPolicyContext());
    }

    private int GetFinalTurn()
    {
        return LessonTurnPolicy.ResolveFinalTurn(BuildTurnPolicyContext());
    }

    private LessonTurnPolicyContext BuildTurnPolicyContext()
    {
        return new LessonTurnPolicyContext(
            lessonScenario.Metadata.LessonType,
            SelectedLevel,
            CurrentLessonPhase switch
            {
                LessonPhase.ActiveRoleplay => LessonTurnPhase.ActiveRoleplay,
                LessonPhase.Completed => LessonTurnPhase.Completed,
                _ => LessonTurnPhase.SetupContextSelection
            },
            LearnerTurnCount,
            activeLevelProfile.SoftWrapUpAfterUserTurn > 0 ? activeLevelProfile.SoftWrapUpAfterUserTurn : lessonScenario.Metadata.SoftWrapUpAfterUserTurn,
            activeLevelProfile.FinalMessageAtUserTurn > 0 ? activeLevelProfile.FinalMessageAtUserTurn : lessonScenario.Metadata.FinalMessageAtUserTurn,
            IsFreeConversationLesson() || selectedContextVariant is not null || !string.IsNullOrWhiteSpace(selectedCustomContextTitle));
    }

    private bool IsFreeConversationLesson()
    {
        return string.Equals(lessonScenario.Metadata.LessonType, "free_conversation", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsGuidedRoleplayLesson()
    {
        return string.Equals(lessonScenario.Metadata.LessonType, "guided_roleplay", StringComparison.OrdinalIgnoreCase);
    }

    private string RenderLessonTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        var rendered = template.Trim();

        if (rendered.Contains("{{userDisplayName}}", StringComparison.Ordinal))
        {
            rendered = string.IsNullOrWhiteSpace(UserDisplayName)
                ? rendered.Replace("Hi, {{userDisplayName}}!", "Hi!", StringComparison.Ordinal)
                : rendered.Replace("{{userDisplayName}}", UserDisplayName, StringComparison.Ordinal);
        }

        return rendered.Replace("Hi, !", "Hi!", StringComparison.Ordinal).Trim();
    }

    private string BuildFeedbackRulesSummary()
    {
        var levelRules = lessonScenario.FeedbackRules.LevelRules.Count == 0
            ? string.Empty
            : string.Join(" ", lessonScenario.FeedbackRules.LevelRules.Select(rule => $"{rule.Key}: {rule.Value}"));

        return string.Join(" ", new[]
        {
            levelRules,
            lessonScenario.FeedbackRules.FeedbackLength,
            lessonScenario.FeedbackRules.FeedbackStyle
        }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    public async Task CheckBackendHealthAsync()
    {
        var isBackendHealthy = await lessonChatBackendService.CheckHealthAsync();

        if (isBackendHealthy)
        {
            BackendStatusText = BackendConstants.BackendStatusConnected;
            return;
        }

        BackendStatusText = BackendConstants.BackendStatusUnavailable;
        StatusMessage = localizedText.BackendHealthCheckFailedMessage;
    }


    private async Task CheckBackendConfigStatusAsync()
    {
        var configStatus = await lessonChatBackendService.GetBackendConfigStatusAsync();

        if (configStatus is null)
        {
            AiStatusText = BackendConstants.AiStatusUnavailable;
            return;
        }

        if (string.Equals(configStatus.OpenAiStatus, BackendConstants.OpenAiConfiguredStatus, StringComparison.OrdinalIgnoreCase))
        {
            var modelName = configStatus.OpenAiModel?.Trim();
            AiStatusText = string.IsNullOrWhiteSpace(modelName)
                ? BackendConstants.AiStatusConfiguredPrefix
                : $"{BackendConstants.AiStatusConfiguredPrefix} ({modelName})";
            return;
        }

        AiStatusText = BackendConstants.AiStatusNotConfigured;
    }

    [RelayCommand(CanExecute = nameof(CanViewFeedback))]
    private async Task ViewFeedbackAsync(ChatMessageViewModel? message)
    {
        if (message is null || !CanViewFeedback(message))
        {
            return;
        }

        var feedback = message.Feedback;
        if (feedback is null)
        {
            feedback = await GenerateFeedbackForMessageAsync(message);
            if (feedback is null)
            {
                return;
            }

            message.SetFeedback(feedback);
        }

        SelectedFeedback = feedback;
        IsFeedbackTranslationVisible = false;
        StatusMessage = feedback.ShortText;
    }

    private bool CanViewFeedback(ChatMessageViewModel? message)
    {
        return IsLessonOptionsEnabled
            && message is not null
            && !message.IsFromBot
            && message.IsFeedbackEligible
            && !message.IsTechnicalMessage
            && message.CountsAsValidLessonTurn;
    }

    [RelayCommand]
    private async Task ToggleFeedbackTranslationAsync()
    {
        if (SelectedFeedback is null)
        {
            return;
        }

        if (IsFeedbackTranslationVisible)
        {
            IsFeedbackTranslationVisible = false;
            return;
        }

        if (SelectedFeedback.HasTranslations)
        {
            IsFeedbackTranslationVisible = true;
            return;
        }

        StatusMessage = localizedText.TranslationLoadingText;

        try
        {
            await TranslateSelectedFeedbackAsync(SelectedFeedback);
            IsFeedbackTranslationVisible = true;
            StatusMessage = SelectedFeedback.ShortText;
            OnPropertyChanged(nameof(SelectedFeedback));
        }
        catch
        {
            StatusMessage = localizedText.TranslationFailedText;
        }
    }

    [RelayCommand]
    private void CloseFeedback()
    {
        SelectedFeedback = null;
        StatusMessage = string.Empty;
        IsFeedbackTranslationVisible = false;
    }

    [RelayCommand]
    private void CloseHint()
    {
        CurrentHintText = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanRequestHint))]
    private async Task HintAsync()
    {
        if (!CanRequestHint())
        {
            return;
        }

        if (CurrentLessonPhase == LessonPhase.SetupContextSelection)
        {
            CurrentHintText = BuildSetupContextHint();
            StatusMessage = string.Empty;
            return;
        }

        if (CurrentLessonPhase == LessonPhase.ActiveRoleplay && LearnerTurnCount == 0 && !string.IsNullOrWhiteSpace(lessonScenario.HintRules.ExampleHint))
        {
            CurrentHintText = lessonScenario.HintRules.ExampleHint.Trim();
            StatusMessage = string.Empty;
            return;
        }

        IsSending = true;
        BotStatus = BackendConstants.BotStatusThinking;
        RefreshAvatarState();

        try
        {
            var hintUserMessage = string.IsNullOrWhiteSpace(UserInput)
                ? AppConstants.HintFallbackUserMessage
                : UserInput.Trim();
            var softWrapUpTurn = GetSoftWrapUpTurn();
            var finalTurn = GetFinalTurn();

            var hintText = await lessonChatBackendService.SendLessonHintRequestAsync(new LessonChatBackendRequest
            {
                SelectedLevel = SelectedLevel,
                TopicTitle = SelectedTopic.Title,
                SubtopicTitle = SelectedSubtopic.Title,
                UserMessage = hintUserMessage,
                LastBotMessage = lastBotMessage,
                NativeLanguageName = nativeLanguageName,
                TutorAvatarId = tutorAvatarId,
                UserDisplayName = this.UserDisplayName,
                LearningGoal = this.LearningGoal,
                LearnerTurnCount = LearnerTurnCount,
                SoftLearnerTurnLimit = softWrapUpTurn,
                HardLearnerTurnLimit = finalTurn,
                RemainingLearnerTurns = Math.Max(finalTurn - LearnerTurnCount, 0),
                ShouldStartWrappingUp = LearnerTurnCount >= softWrapUpTurn,
                ShouldEndLessonNow = LearnerTurnCount >= finalTurn,
                RecentMessages = GetRecentConversationMessages(),
                LessonPhase = CurrentLessonPhase.ToString(),
                LessonScenarioId = lessonScenario.Id,
                Level = SelectedLevel,
                Topic = lessonScenario.Metadata.Topic,
                Subtopic = lessonScenario.Metadata.Subtopic,
                LessonGoal = lessonScenario.LearningGoal.Goal,
                LessonType = lessonScenario.Metadata.LessonType,
                AiTutorPromptInstructions = lessonScenario.AiTutorPromptInstructions,
                SelectedContextVariantId = selectedContextVariant?.Id ?? string.Empty,
                SelectedContextTitle = GetSelectedContextTitle(),
                SelectedContextOpeningLine = selectedContextVariant?.OpeningLine ?? lessonScenario.ConversationFlow.DefaultOpeningExample,
                UserTurnNumber = LearnerTurnCount,
                SoftWrapUpAfterUserTurn = softWrapUpTurn,
                FinalMessageAtUserTurn = finalTurn,
                TargetLanguageKeyPhrases = lessonScenario.TargetLanguage.KeyPhrases,
                GrammarFocus = lessonScenario.TargetLanguage.GrammarFocus,
                ConversationOpening = lessonScenario.ConversationFlow.Opening,
                ConversationFirstUserTask = lessonScenario.ConversationFlow.FirstUserTask,
                ConversationGuidedPracticeFollowUpQuestions = lessonScenario.ConversationFlow.GuidedPracticeFollowUpQuestions,
                ConversationVariationOrComplication = lessonScenario.ConversationFlow.VariationOrComplication,
                ConversationCorrectionMoment = lessonScenario.ConversationFlow.CorrectionMoment,
                ConversationWrapUpMessage = lessonScenario.ConversationFlow.WrapUpMessage,
                ConversationFinalMessage = GetFinalLessonMessage(),
                FeedbackRulesSummary = BuildFeedbackRulesSummary(),
                TutorProfileId = tutorAvatarId,
                ActiveLevelProfileDifficultyNotes = activeLevelProfile.DifficultyNotes,
                ActiveLevelProfileTutorLanguageStyle = activeLevelProfile.TutorLanguageStyle,
                ActiveLevelProfileExpectedUserResponse = activeLevelProfile.ExpectedUserResponse,
                ActiveLevelProfileFeedbackStrictness = activeLevelProfile.FeedbackStrictness,
                ActiveLevelProfileHintStrategy = activeLevelProfile.HintStrategy,
                ActiveLevelProfileCorrectionPriority = activeLevelProfile.CorrectionPriority,
                ActiveLevelProfileConversationDepth = activeLevelProfile.ConversationDepth,
                ActiveLevelProfileExampleGoodAnswer = activeLevelProfile.ExampleGoodAnswer,
                ActiveLevelProfileExampleStretchAnswer = activeLevelProfile.ExampleStretchAnswer,
                ActiveLevelProfileAddedKeyPhrases = activeLevelProfile.AddedKeyPhrases,
                ActiveLevelProfileAddedUsefulConstructions = activeLevelProfile.AddedUsefulConstructions,
                ActiveLevelProfileAddedGrammarFocus = activeLevelProfile.AddedGrammarFocus
            });

            BackendStatusText = BackendConstants.BackendStatusConnected;
            CurrentHintText = string.IsNullOrWhiteSpace(hintText)
                ? localizedText.MockHintText
                : hintText.Trim();
            StatusMessage = string.Empty;
        }
        catch
        {
            BackendStatusText = BackendConstants.BackendStatusUnavailable;
            CurrentHintText = localizedText.MockHintText;
            StatusMessage = localizedText.BackendUnavailableMessage;
        }
        finally
        {
            BotStatus = BackendConstants.BotStatusReady;
            IsSending = false;
            RefreshAvatarState();
        }
    }

    private bool CanRequestHint()
    {
        // Hints are intentionally allowed during SetupContextSelection so the learner can see valid context choices.
        return CanAcceptLessonInput;
    }

    private string GetFinalLessonMessage()
    {
        return string.IsNullOrWhiteSpace(lessonScenario.ConversationFlow.FinalMessage)
            ? AppConstants.LessonCompleteAwaitingFinishMessage
            : lessonScenario.ConversationFlow.FinalMessage.Trim();
    }

    private void RefreshAvatarState()
    {
        CurrentAvatarState = GetActiveAvatarState();
    }

    private AvatarState GetActiveAvatarState()
    {
        if (IsBotVoicePlaying)
        {
            return AvatarState.Speaking;
        }

        if (IsRecording)
        {
            return AvatarState.Listening;
        }

        if (isTranscribingAudio)
        {
            return AvatarState.Transcribing;
        }

        if (IsSending)
        {
            return AvatarState.Thinking;
        }

        return AvatarState.Idle;
    }

    [RelayCommand(CanExecute = nameof(CanFinishLesson))]
    private async Task FinishLesson()
    {
        CancelCurrentBotVoice(BotVoiceCancellationReasons.BackOrFinishCancel);
        await CleanupCurrentSessionBotVoiceFilesAsync();
        await StopRealtimeConversationAsync("finish_lesson");
        CompleteLesson();
    }

    private void MarkLessonCompleteAwaitingFinish()
    {
        CurrentLessonPhase = LessonPhase.Completed;
        IsLessonCompleteAwaitingFinish = true;
        IsConversationModeEnabled = false;
        UserInput = string.Empty;
        BotStatus = BackendConstants.BotStatusReady;
        StatusMessage = AppConstants.LessonCompleteAwaitingFinishMessage;
        _ = StopRealtimeConversationAsync("lesson_complete");
        RefreshLessonCompletionState();
        LogLessonStateSnapshot("final limit reached");
    }

    private void RefreshLessonCompletionState()
    {
        OnPropertyChanged(nameof(IsLessonInputEnabled));
        OnPropertyChanged(nameof(IsLessonOptionsEnabled));
        OnPropertyChanged(nameof(IsLessonLimitReached));
        OnPropertyChanged(nameof(IsLessonWrappingUp));
        RefreshAllCommandStates();
    }

    private void LogFinalLimitReached(int finalTurn)
    {
        Debug.WriteLine($"FinalLimitReached; FinalTurn={finalTurn}; LearnerTurnCount={LearnerTurnCount}; CurrentLessonPhase={CurrentLessonPhase}; CommandsInvalidated=True.");
        LogLessonStateSnapshot("final limit reached");
    }

    private void LogLessonStateSnapshot(string reason)
    {
        Debug.WriteLine(
            $"LessonStateSnapshot Reason={reason}; " +
            $"CurrentLessonPhase={CurrentLessonPhase}; " +
            $"LearnerTurnCount={LearnerTurnCount}; " +
            $"FinalTurn={GetFinalTurn()}; " +
            $"IsLessonLimitReached={IsLessonLimitReached}; " +
            $"IsLessonCompleteAwaitingFinish={IsLessonCompleteAwaitingFinish}; " +
            $"HasFinishedLesson={hasFinishedLesson}; " +
            $"IsSending={IsSending}; " +
            $"IsRecording={IsRecording}; " +
            $"IsRealtimeSessionStarting={IsRealtimeSessionStarting}; " +
            $"IsBotVoicePlaying={IsBotVoicePlaying}; " +
            $"IsConversationModeEnabled={IsConversationModeEnabled}; " +
            $"IsRealtimeConversationStarted={isRealtimeSessionStarted}; " +
            $"IsRealtimeConversationActive={IsRealtimeConversationActive}; " +
            $"CanSend={CanSendMessage()}; " +
            $"CanRecord={CanToggleVoiceRecording()}; " +
            $"CanHint={CanRequestHint()}; " +
            $"CanBack={CanGoBack()}; " +
            $"CanFinish={CanFinishLesson()}; " +
            $"CanConversationMode={CanToggleConversationMode()}.");
    }

    private void RefreshAllCommandStates()
    {
        SendMessageCommand.NotifyCanExecuteChanged();
        ToggleVoiceRecordingCommand.NotifyCanExecuteChanged();
        HintCommand.NotifyCanExecuteChanged();
        ToggleConversationModeCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
        FinishLessonCommand.NotifyCanExecuteChanged();
        PlayBotVoiceCommand.NotifyCanExecuteChanged();
    }

    private void CompleteLesson()
    {
        if (hasFinishedLesson)
        {
            return;
        }

        CurrentLessonPhase = LessonPhase.Completed;
        hasFinishedLesson = true;
        RefreshAllCommandStates();
        LogLessonStateSnapshot("Finish lesson clicked");
        ViewFeedbackCommand.NotifyCanExecuteChanged();
        finishLesson(BuildLessonSummaryInput());
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private async Task Back()
    {
        CancelCurrentBotVoice(BotVoiceCancellationReasons.BackOrFinishCancel);
        await CleanupCurrentSessionBotVoiceFilesAsync();
        await StopRealtimeConversationAsync("back");
        navigateBack();
    }

    private bool CanGoBack()
    {
        return !hasFinishedLesson
            && !IsCompletedAwaitingFinish
            && !IsSending
            && !IsRecording
            && !IsRealtimeSessionStarting
            && (CurrentLessonPhase == LessonPhase.SetupContextSelection || CurrentLessonPhase == LessonPhase.ActiveRoleplay);
    }

    private IReadOnlyList<RecentConversationMessage> GetRecentConversationMessages()
    {
        return Messages
            .TakeLast(AppConstants.RecentConversationMessagesLimit)
            .Select(message => new RecentConversationMessage
            {
                Sender = message.IsFromBot ? TutorAvatarDisplayName : AppConstants.UserSenderName,
                Text = message.Text
            })
            .Where(message => IsSummaryEligibleConversationText(message.Text))
            .ToArray();
    }

    private ChatMessageViewModel AddMessage(
        string sender,
        string text,
        bool isFromBot,
        Feedback? feedback = null,
        string source = ChatMessageSource.Technical,
        int lessonTurnNumber = 0,
        bool countsAsValidLessonTurn = false,
        bool isTechnicalMessage = false,
        bool isFeedbackEligible = false)
    {
        messageCounter++;
        var message = new ChatMessageViewModel(
            messageCounter,
            sender,
            text,
            isFromBot,
            isFromBot ? null : feedback,
            string.Empty,
            nativeLanguageName,
            localizedText,
            TranslateMessageAsync,
            source,
            lessonTurnNumber,
            CurrentLessonPhase.ToString(),
            lessonScenario.Metadata.Topic,
            lessonScenario.Metadata.Subtopic,
            SelectedLevel,
            GetSelectedContextTitle(),
            selectedContextVariant?.Id ?? string.Empty,
            DateTimeOffset.Now,
            countsAsValidLessonTurn,
            isTechnicalMessage,
            isFeedbackEligible);
        Messages.Add(message);

        if (ShouldPrefetchBotVoice(message))
        {
            QueueBotVoiceFirstSegmentPrefetch(message);
        }

        return message;
    }

    private ChatMessageViewModel AddLearnerMessage(string text, string source, int lessonTurnNumber, Feedback? feedback)
    {
        return AddMessage(
            AppConstants.UserSenderName,
            text,
            isFromBot: false,
            feedback: feedback,
            source: source,
            lessonTurnNumber: lessonTurnNumber,
            countsAsValidLessonTurn: true,
            isTechnicalMessage: false,
            isFeedbackEligible: true);
    }

    private async Task<Feedback?> GenerateFeedbackForMessageAsync(ChatMessageViewModel message)
    {
        try
        {
            var response = await lessonChatBackendService.SendLessonFeedbackRequestAsync(BuildLessonFeedbackRequest(message));
            return MapFeedback(response);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Feedback request failed: MessageId={message.Id}; Source={message.Source}; TextLength={message.Text.Trim().Length}; {exception}");
            StatusMessage = localizedText.BackendUnavailableMessage;
            return null;
        }
    }

    private LessonChatBackendRequest BuildLessonFeedbackRequest(ChatMessageViewModel message)
    {
        return new LessonChatBackendRequest
        {
            SelectedLevel = SelectedLevel,
            TopicTitle = SelectedTopic.Title,
            SubtopicTitle = SelectedSubtopic.Title,
            UserMessage = message.Text.Trim(),
            LastBotMessage = lastBotMessage,
            NativeLanguageName = nativeLanguageName,
            TutorAvatarId = tutorAvatarId,
            UserDisplayName = UserDisplayName,
            LearningGoal = LearningGoal,
            LearnerTurnCount = LearnerTurnCount,
            RecentMessages = GetRecentConversationMessages(),
            LessonPhase = message.LessonPhase,
            LessonScenarioId = lessonScenario.Id,
            Level = SelectedLevel,
            Topic = lessonScenario.Metadata.Topic,
            Subtopic = lessonScenario.Metadata.Subtopic,
            LessonGoal = lessonScenario.LearningGoal.Goal,
            LessonType = lessonScenario.Metadata.LessonType,
            AiTutorPromptInstructions = lessonScenario.AiTutorPromptInstructions,
            SelectedContextVariantId = selectedContextVariant?.Id ?? string.Empty,
            SelectedContextTitle = GetSelectedContextTitle(),
            UserTurnNumber = message.LessonTurnNumber,
            TargetLanguageKeyPhrases = lessonScenario.TargetLanguage.KeyPhrases,
            GrammarFocus = lessonScenario.TargetLanguage.GrammarFocus,
            FeedbackRulesSummary = BuildFeedbackRulesSummary(),
            TutorProfileId = tutorAvatarId,
            ActiveLevelProfileDifficultyNotes = activeLevelProfile.DifficultyNotes,
            ActiveLevelProfileTutorLanguageStyle = activeLevelProfile.TutorLanguageStyle,
            ActiveLevelProfileExpectedUserResponse = activeLevelProfile.ExpectedUserResponse,
            ActiveLevelProfileFeedbackStrictness = activeLevelProfile.FeedbackStrictness,
            ActiveLevelProfileHintStrategy = activeLevelProfile.HintStrategy,
            ActiveLevelProfileCorrectionPriority = activeLevelProfile.CorrectionPriority,
            ActiveLevelProfileConversationDepth = activeLevelProfile.ConversationDepth,
            ActiveLevelProfileExampleGoodAnswer = activeLevelProfile.ExampleGoodAnswer,
            ActiveLevelProfileExampleStretchAnswer = activeLevelProfile.ExampleStretchAnswer,
            ActiveLevelProfileAddedKeyPhrases = activeLevelProfile.AddedKeyPhrases,
            ActiveLevelProfileAddedUsefulConstructions = activeLevelProfile.AddedUsefulConstructions,
            ActiveLevelProfileAddedGrammarFocus = activeLevelProfile.AddedGrammarFocus
        };
    }

    private LessonSummaryInput BuildLessonSummaryInput()
    {
        var summaryMessages = Messages
            .Where(message => !message.IsTechnicalMessage && IsSummaryEligibleConversationText(message.Text))
            .Where(message => message.IsFromBot || message.CountsAsValidLessonTurn)
            .Select(message => new LessonSummaryMessage
            {
                Id = message.Id,
                Role = message.Role,
                Text = message.Text.Trim(),
                Source = message.Source,
                LessonTurnNumber = message.LessonTurnNumber,
                LessonPhase = message.LessonPhase,
                Feedback = message.Feedback
            })
            .ToArray();

        Debug.WriteLine($"Lesson summary input built: MessageCount={summaryMessages.Length}; UserTurnCount={summaryMessages.Count(message => string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))}; RealtimeUserTurnCount={summaryMessages.Count(message => string.Equals(message.Source, ChatMessageSource.RealtimeVoice, StringComparison.OrdinalIgnoreCase))}; FinalUserTurnCount={LearnerTurnCount}.");

        return new LessonSummaryInput
        {
            SelectedLevel = SelectedLevel,
            TopicTitle = SelectedTopic.Title,
            SubtopicTitle = SelectedSubtopic.Title,
            SelectedContextTitle = GetSelectedContextTitle(),
            SelectedContextVariantId = selectedContextVariant?.Id ?? string.Empty,
            LearningGoal = LearningGoal,
            LessonType = lessonScenario.Metadata.LessonType,
            FinalUserTurnCount = LearnerTurnCount,
            Messages = summaryMessages
        };
    }

    private static bool IsSummaryEligibleConversationText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim();
        return !string.Equals(normalized, LessonTranscriptValidator.VoiceMessagePlaceholder, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalized, LessonTranscriptValidator.InvalidTranscriptUserMessage, StringComparison.OrdinalIgnoreCase)
            && !normalized.EndsWith(" is speaking...", StringComparison.OrdinalIgnoreCase);
    }

    private bool ShouldPrefetchBotVoice(ChatMessageViewModel message)
    {
        if (!message.IsFromBot || string.IsNullOrWhiteSpace(message.Text))
        {
            return false;
        }

        if (CurrentLessonPhase == LessonPhase.SetupContextSelection
            && message.Text.Trim().Length > AudioConstants.BotVoiceSetupAutoPlayMaxCharacters)
        {
            return false;
        }

        return !IsRealtimeConversationActive && message.ShowPlayVoiceButton && CurrentLessonPhase != LessonPhase.SetupContextSelection;
    }

    private void QueueBotVoiceFirstSegmentPrefetch(ChatMessageViewModel message)
    {
        var rawBotVoiceText = message.Text.Trim();
        var isSetupVoiceMessage = IsSetupVoiceMessage(message);
        var exactBotVoiceText = GetExactBotVoiceText(message);
        var allSegments = SplitExactBotVoiceTextIntoSegments(exactBotVoiceText);
        Debug.WriteLine($"Exact bot voice text: RawLength={rawBotVoiceText.Length}; VoiceTextLength={exactBotVoiceText.Length}; MessagePhase={(isSetupVoiceMessage ? "setup" : "active")}; SegmentCount={allSegments.Count}; SegmentLengths={string.Join(",", allSegments.Select(segment => segment.Length))}; Prefetch=True.");
        if (allSegments.Count == 0)
        {
            return;
        }

        var totalStopwatch = Stopwatch.StartNew();
        _ = Task.Run(async () =>
        {
            try
            {
                Debug.WriteLine($"Bot voice first segment prefetch starting: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex=0; InputLength={allSegments[0].Length}.");
                await GetOrCreateBotVoiceSegmentAudioFileAsync(
                    message,
                    allSegments[0],
                    segmentIndex: 0,
                    timeout: TimeSpan.FromSeconds(AudioConstants.BotVoiceFirstSegmentHardTimeoutSeconds),
                    totalStopwatch,
                    CancellationToken.None);
                Debug.WriteLine($"Bot voice first segment prefetch completed: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex=0; ReadyMs={totalStopwatch.ElapsedMilliseconds}.");
            }
            catch (OperationCanceledException exception)
            {
                Debug.WriteLine($"Bot voice first segment prefetch canceled: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex=0; CancellationReason={BotVoiceCancellationReasons.HardTimeoutCancel}; TotalMs={totalStopwatch.ElapsedMilliseconds}; {exception}");
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Bot voice first segment prefetch failed: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex=0; TotalMs={totalStopwatch.ElapsedMilliseconds}; {exception}");
            }
        });
    }

    private async Task TranslateMessageAsync(ChatMessageViewModel message)
    {
        try
        {
            var translatedText = await lessonChatBackendService.TranslateTextAsync(message.Text, nativeLanguageName);
            message.TranslationText = translatedText;
            message.IsTranslationVisible = true;
            BackendStatusText = BackendConstants.BackendStatusConnected;
            StatusMessage = string.Empty;
        }
        catch
        {
            BackendStatusText = BackendConstants.BackendStatusUnavailable;
            StatusMessage = localizedText.TranslationFailedText;
        }
    }

    private async Task TranslateSelectedFeedbackAsync(Feedback feedback)
    {
        var translations = await Task.WhenAll(
            lessonChatBackendService.TranslateTextAsync(feedback.ShortText, nativeLanguageName),
            lessonChatBackendService.TranslateTextAsync(feedback.CorrectedVersion, nativeLanguageName),
            lessonChatBackendService.TranslateTextAsync(feedback.GrammarTip, nativeLanguageName),
            lessonChatBackendService.TranslateTextAsync(feedback.VocabularyTip, nativeLanguageName),
            lessonChatBackendService.TranslateTextAsync(feedback.CultureTip, nativeLanguageName),
            lessonChatBackendService.TranslateTextAsync(feedback.NaturalVersion, nativeLanguageName));

        feedback.ShortTextTranslation = translations[0];
        feedback.CorrectedVersionTranslation = translations[1];
        feedback.GrammarTipTranslation = translations[2];
        feedback.VocabularyTipTranslation = translations[3];
        feedback.CultureTipTranslation = translations[4];
        feedback.NaturalVersionTranslation = translations[5];
        BackendStatusText = BackendConstants.BackendStatusConnected;
    }

    private static string NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    private static Feedback MapFeedback(BackendFeedbackDto backendFeedback)
    {
        return new Feedback(
            AppConstants.MockFeedbackType,
            backendFeedback.ShortText,
            backendFeedback.CorrectedVersion,
            backendFeedback.GrammarTip,
            backendFeedback.VocabularyTip,
            backendFeedback.CultureTip,
            backendFeedback.NaturalVersion,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static class BotVoiceCancellationReasons
    {
        public const string SoftTargetReachedNoCancel = "SoftTargetReachedNoCancel";
        public const string HardTimeoutCancel = "HardTimeoutCancel";
        public const string NewerMessageCancel = "NewerMessageCancel";
        public const string BackOrFinishCancel = "BackOrFinishCancel";
        public const string ManualReplayCancel = "ManualReplayCancel";
        public const string AppDisposalCancel = "AppDisposalCancel";
    }
}
