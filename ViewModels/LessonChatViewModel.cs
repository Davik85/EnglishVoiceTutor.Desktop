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
using NAudio.Wave;

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
    private bool usedManualPlayVoice;
    private bool usedAutoPlayVoice;
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
    private readonly SemaphoreSlim realtimeLifecycleSemaphore = new(1, 1);
    private readonly SemaphoreSlim realtimeRecordingSemaphore = new(1, 1);
    private const string RealtimeVoicePendingText = LessonTranscriptValidator.VoiceMessagePlaceholder;
    private const string RealtimeVoiceTranscriptionUnavailableText = LessonTranscriptValidator.InvalidTranscriptUserMessage;
    private ChatMessageViewModel? realtimeAssistantMessage;
    private ChatMessageViewModel? realtimeUserPlaceholderMessage;
    private string realtimeUserPlaceholderItemId = string.Empty;
    private readonly StringBuilder realtimeUserTranscriptBuffer = new();
    private readonly Dictionary<string, int> realtimeItemIdToChatMessageId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> pendingTranscriptByItemId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> pendingTranscriptFailureByItemId = new(StringComparer.Ordinal);
    private readonly HashSet<int> spokenRealtimeOpeningMessageIds = [];
    private readonly Dictionary<int, Feedback> feedbackByMessageId = [];
    private readonly List<byte> realtimeCommittedAudioBuffer = [];
    private int realtimeCommittedAudioChunkCount;
    private int realtimeCommittedAudioBytes;
    private bool realtimeFallbackTranscriptionAttempted;
    private bool realtimeFallbackTranscriptionInProgress;
    private string realtimeSessionId = Guid.NewGuid().ToString("N");
    private const int UiOperationWarningThresholdMilliseconds = 3000;
    private const int RealtimeStartupWarningThresholdMilliseconds = 8000;
    private const int TtsPlaybackPreparationWarningThresholdMilliseconds = 5000;
    private const int RecordingStopCommitWarningThresholdMilliseconds = 5000;

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
    private int selectedFeedbackMessageId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedbackTranslateButtonText))]
    private bool isFeedbackTranslationVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BotStatusText))]
    [NotifyPropertyChangedFor(nameof(BotStatusDisplayText))]
    [NotifyPropertyChangedFor(nameof(IsBotTyping))]
    [NotifyPropertyChangedFor(nameof(BotStatusIndicatorBrush))]
    private string botStatus = BackendConstants.BotStatusReady;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackendStatusIndicatorBrush))]
    private string backendStatusText = BackendConstants.BackendStatusChecking;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AiStatusIndicatorBrush))]
    private string aiStatusText = BackendConstants.AiStatusChecking;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanTypeText))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleVoiceRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(FinishLessonCommand))]
    [NotifyCanExecuteChangedFor(nameof(HintCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleConversationModeCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    private bool isSending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VoiceButtonText))]
    [NotifyPropertyChangedFor(nameof(CanTypeText))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleVoiceRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(FinishLessonCommand))]
    [NotifyCanExecuteChangedFor(nameof(HintCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleConversationModeCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    private bool isRecording;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanTypeText))]
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
    [NotifyPropertyChangedFor(nameof(CanTypeText))]
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
    [NotifyPropertyChangedFor(nameof(CanTypeText))]
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
    [NotifyPropertyChangedFor(nameof(IsLessonInputEnabled))]
    [NotifyPropertyChangedFor(nameof(CanTypeText))]
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
    [NotifyPropertyChangedFor(nameof(CanTypeText))]
    [NotifyCanExecuteChangedFor(nameof(ToggleVoiceRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleConversationModeCommand))]
    private ConversationModeState currentConversationModeState = ConversationModeState.NotStarted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLessonInputEnabled))]
    [NotifyPropertyChangedFor(nameof(IsLessonOptionsEnabled))]
    [NotifyPropertyChangedFor(nameof(CanTypeText))]
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

    public bool CanTypeText => CanAcceptLessonInput;

    public bool IsConversationRecordButtonEnabled => CanToggleVoiceRecording();

    public bool IsLessonOptionsEnabled => !hasFinishedLesson && !IsCompletedAwaitingFinish && !IsLessonLimitReached;

    public string VoiceButtonText => IsRecording
        ? localizedText.StopRecordingButtonText
        : localizedText.StartRecordingButtonText;

    public string FeedbackTranslateButtonText => IsFeedbackTranslationVisible
        ? localizedText.FeedbackHideTranslationButtonText
        : localizedText.FeedbackTranslateButtonText;

    public string BotStatusText => $"{localizedText.BotStatusLabel} {BotStatusDisplayText}";

    public bool IsBotTyping => string.Equals(BotStatus, BackendConstants.BotStatusThinking, StringComparison.OrdinalIgnoreCase);

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
    // - Final message has been shown. Lesson no longer accepts input. Finish lesson remains enabled.
    // - Send, recording, hint, and back disabled. Conversation Mode disabled/stopped.
    // - Existing message review actions remain enabled when each message is eligible.
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

    private bool CanReviewExistingMessages => !hasFinishedLesson;

    private bool CanAcceptLessonInput => !hasFinishedLesson && !IsCompletedAwaitingFinish && !IsLessonLimitReached && !IsLessonBusyForInput && !IsRealtimeConversationActive;

    private bool CanAcceptTranscriptionResult =>
        !hasFinishedLesson
        && !IsCompletedAwaitingFinish
        && !IsLessonLimitReached
        && (CurrentLessonPhase == LessonPhase.SetupContextSelection || CurrentLessonPhase == LessonPhase.ActiveRoleplay);

    private bool IsRealtimeConversationActive => BackendConstants.UseRealtimeConversationMode && IsConversationModeEnabled && CurrentLessonPhase == LessonPhase.ActiveRoleplay;

    private bool CanStartRealtimeRecording => IsRealtimeConversationActive
        && CurrentConversationModeState == ConversationModeState.Ready
        && isRealtimeSessionStarted
        && !IsRecording
        && !IsSending
        && !IsBotVoicePlaying
        && !isTranscribingAudio
        && !IsCompletedAwaitingFinish
        && !IsLessonLimitReached;

    private bool ShouldAutoPlayBotVoice => !IsRealtimeConversationActive && !IsLessonCompleteAwaitingFinish && IsBotVoiceAutoPlayEnabled;

    private bool CanStartNormalRecording()
    {
        return !hasFinishedLesson
            && !IsCompletedAwaitingFinish
            && !IsLessonLimitReached
            && !IsSending
            && !IsRealtimeSessionStarting
            && !IsBotVoicePlaying
            && !isTranscribingAudio;
    }

    private string GetRealtimeRecordBlockReason()
    {
        if (!IsRealtimeConversationActive)
        {
            return "conversation_mode_not_active";
        }

        if (CurrentConversationModeState != ConversationModeState.Ready)
        {
            return $"state_{CurrentConversationModeState}";
        }

        if (!isRealtimeSessionStarted)
        {
            return "session_not_started";
        }

        if (isStartingRealtimeSession)
        {
            return "session_starting";
        }

        if (IsRecording)
        {
            return "already_recording";
        }

        if (IsSending)
        {
            return "assistant_turn_in_progress";
        }

        if (IsBotVoicePlaying)
        {
            return IsRealtimePlaybackActive() ? "realtime_playback_active" : "normal_tts_playback_flag_active";
        }

        if (isTranscribingAudio)
        {
            return "normal_transcription_in_progress";
        }

        if (IsCompletedAwaitingFinish)
        {
            return "lesson_completed_awaiting_finish";
        }

        if (IsLessonLimitReached)
        {
            return "lesson_limit_reached";
        }

        return "none";
    }

    private bool IsRealtimePlaybackActive()
    {
        return IsRealtimeConversationActive && CurrentConversationModeState == ConversationModeState.PlayingAssistantAudio;
    }

    private void LogRealtimeRecordState(string reason)
    {
        Debug.WriteLine(
            $"Realtime record state: Reason={reason}; " +
            $"CurrentConversationModeState={CurrentConversationModeState}; " +
            $"IsConversationModeActive={IsRealtimeConversationActive}; " +
            $"isRealtimeSessionStarted={isRealtimeSessionStarted}; " +
            $"isStartingRealtimeSession={isStartingRealtimeSession}; " +
            $"IsRecording={IsRecording}; " +
            $"IsSending={IsSending}; " +
            $"IsTranscribing={isTranscribingAudio}; " +
            $"IsBotVoicePlaying={IsBotVoicePlaying}; " +
            $"IsBotTyping={IsBotTyping}; " +
            $"IsLessonCompleteAwaitingFinish={IsLessonCompleteAwaitingFinish}; " +
            $"IsRealtimePlaybackActive={IsRealtimePlaybackActive()}; " +
            $"CanStartRealtimeRecording={CanStartRealtimeRecording}; " +
            $"CanStartNormalRecording={CanStartNormalRecording()}; " +
            $"MainRecordCommandCanExecute={CanToggleVoiceRecording()}; " +
            $"RecordBlockReason={GetRealtimeRecordBlockReason()}.");
    }

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
        realtimeVoiceEngine.SessionReady += OnRealtimeSessionReady;
        realtimeVoiceEngine.ErrorReceived += OnRealtimeErrorReceived;
        realtimeVoiceEngine.Disconnected += OnRealtimeDisconnected;
        realtimeAudioPlaybackService.PlaybackStarted += OnRealtimePlaybackStarted;
        realtimeAudioPlaybackService.PlaybackCompleted += OnRealtimePlaybackCompleted;
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
        var canSend = CanAcceptLessonInput && !string.IsNullOrWhiteSpace(UserInput);
        if (!canSend)
        {
            LogTextInputState("send_can_execute_blocked", canSend);
        }

        return canSend;
    }

    private string GetTextSendBlockReason()
    {
        if (hasFinishedLesson)
        {
            return "lesson_finished";
        }

        if (IsCompletedAwaitingFinish)
        {
            return "lesson_completed_awaiting_finish";
        }

        if (IsLessonLimitReached)
        {
            return "lesson_limit_reached";
        }

        if (IsRealtimeConversationActive)
        {
            return "realtime_conversation_active";
        }

        if (IsSending)
        {
            return "assistant_turn_or_text_send_in_progress";
        }

        if (IsRecording)
        {
            return "recording_in_progress";
        }

        if (IsRealtimeSessionStarting)
        {
            return "realtime_session_starting";
        }

        if (string.IsNullOrWhiteSpace(UserInput))
        {
            return "empty_text";
        }

        return "none";
    }

    private void LogTextInputState(string reason, bool canSend)
    {
        Debug.WriteLine(
            $"Text input state: Reason={reason}; " +
            $"CurrentLessonPhase={CurrentLessonPhase}; " +
            $"IsLessonCompleteAwaitingFinish={IsLessonCompleteAwaitingFinish}; " +
            $"ConversationModeState={CurrentConversationModeState}; " +
            $"IsConversationModeActive={IsRealtimeConversationActive}; " +
            $"IsSending={IsSending}; " +
            $"IsBotTyping={IsBotTyping}; " +
            $"IsBotVoicePlaying={IsBotVoicePlaying}; " +
            $"IsRecording={IsRecording}; " +
            $"IsTranscribing={isTranscribingAudio}; " +
            $"LearnerTurnCount={LearnerTurnCount}; " +
            $"FinalTurn={GetFinalTurn()}; " +
            $"HasSelectedContext={selectedContextVariant is not null || !string.IsNullOrWhiteSpace(selectedCustomContextTitle)}; " +
            $"TextLength={UserInput?.Length ?? 0}; " +
            $"CanTypeText={CanTypeText}; " +
            $"CanSend={canSend}; " +
            $"BlockReason={GetTextSendBlockReason()}.");
    }

    private bool CanToggleVoiceRecording()
    {
        if (IsRecording)
        {
            return !hasFinishedLesson && CurrentConversationModeState != ConversationModeState.Stopping;
        }

        if (IsRealtimeConversationActive)
        {
            var canStartRealtime = CanStartRealtimeRecording;
            Debug.WriteLine($"Realtime record command CanExecute evaluated: SessionId={realtimeSessionId}; Result={canStartRealtime}; Reason={GetRealtimeRecordBlockReason()}; State={CurrentConversationModeState}.");
            return canStartRealtime;
        }

        // CanStartNormalRecording includes !IsCompletedAwaitingFinish.
        return CanStartNormalRecording();
    }

    private bool CanPlayBotVoice(ChatMessageViewModel? message)
    {
        return CanReviewExistingMessages
            && !IsRealtimeConversationActive
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
        LogRealtimeRecordState("record_command_invoked");

        if (IsRecording)
        {
            await StopVoiceRecordingAsync();
            return;
        }

        await StartVoiceRecordingAsync();
    }

    private async Task StartVoiceRecordingAsync()
    {
        LogRealtimeRecordState("record_command_execute_start");

        if (IsRealtimeConversationActive)
        {
            await StartRealtimeVoiceRecordingAsync();
            return;
        }

        if (!IsLessonInputEnabled)
        {
            Debug.WriteLine($"Normal voice recording start blocked. Reason=lesson_input_disabled; CanStartNormalRecording={CanStartNormalRecording()}; CanStartRealtimeRecording={CanStartRealtimeRecording}; RealtimeRecordBlockReason={GetRealtimeRecordBlockReason()}.");
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

        var operationStopwatch = StartUiOperationDiagnostics(
            "normal_recording_start",
            UiOperationWarningThresholdMilliseconds,
            $"CurrentLessonPhase={CurrentLessonPhase}; ConversationModeState={CurrentConversationModeState}; IsConversationModeEnabled={IsConversationModeEnabled}");

        try
        {
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
        finally
        {
            CompleteUiOperationDiagnostics(
                operationStopwatch,
                "normal_recording_start",
                UiOperationWarningThresholdMilliseconds,
                $"IsRecording={IsRecording}; CurrentLessonPhase={CurrentLessonPhase}; ConversationModeState={CurrentConversationModeState}");
            RefreshAllCommandStates();
        }
    }

    private async Task StartRealtimeVoiceRecordingAsync()
    {
        if (!await realtimeRecordingSemaphore.WaitAsync(0))
        {
            Debug.WriteLine($"Realtime microphone start ignored because another recording operation is active. SessionId={realtimeSessionId}; State={CurrentConversationModeState}.");
            return;
        }

        var operationStopwatch = StartUiOperationDiagnostics(
            "realtime_recording_start",
            UiOperationWarningThresholdMilliseconds,
            $"SessionId={realtimeSessionId}; State={CurrentConversationModeState}; RecordBlockReason={GetRealtimeRecordBlockReason()}");

        try
        {
            if (!CanStartRealtimeRecording)
            {
                Debug.WriteLine($"Realtime microphone start blocked by state. SessionId={realtimeSessionId}; State={CurrentConversationModeState}; Reason={GetRealtimeRecordBlockReason()}; IsStarted={isRealtimeSessionStarted}; IsStarting={isStartingRealtimeSession}; IsSending={IsSending}; IsTranscribing={isTranscribingAudio}; IsBotVoicePlaying={IsBotVoicePlaying}; CanRecord={CanToggleVoiceRecording()}.");
                RefreshAllCommandStates();
                return;
            }

            SetConversationModeState(ConversationModeState.Recording, "record_start_requested");
            ResetRealtimeCommittedAudioBuffer();
            Debug.WriteLine($"Realtime microphone capture starting: SessionId={realtimeSessionId}; AudioInputDeviceId={audioInputDeviceId}; State={CurrentConversationModeState}.");
            await realtimeVoiceEngine.StartUserAudioAsync(CancellationToken.None);
            realtimeMicrophoneCaptureService.Start(audioInputDeviceId);
            Debug.WriteLine($"Realtime microphone capture started: SessionId={realtimeSessionId}; DeviceId={audioInputDeviceId}; IsMicrophoneRecording={realtimeMicrophoneCaptureService.IsRecording}.");
            CurrentHintText = string.Empty;
            IsRecording = true;
            RefreshAvatarState();
            StatusMessage = localizedText.RecordingStartedMessage;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Realtime microphone start failed: SessionId={realtimeSessionId}; State={CurrentConversationModeState}; {exception}");
            SafeStopRealtimeMicrophone("record_start_failed");
            IsRecording = false;
            SetConversationModeState(isRealtimeSessionStarted ? ConversationModeState.Ready : ConversationModeState.Faulted, "record_start_failed");
            RefreshAvatarState();
            StatusMessage = "Microphone is not available. Please check your input device.";
        }
        finally
        {
            CompleteUiOperationDiagnostics(
                operationStopwatch,
                "realtime_recording_start",
                UiOperationWarningThresholdMilliseconds,
                $"SessionId={realtimeSessionId}; State={CurrentConversationModeState}; IsRecording={IsRecording}; RecordBlockReason={GetRealtimeRecordBlockReason()}");
            RefreshAllCommandStates();
            realtimeRecordingSemaphore.Release();
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
        var operationStopwatch = StartUiOperationDiagnostics(
            "normal_recording_stop",
            RecordingStopCommitWarningThresholdMilliseconds,
            $"CurrentLessonPhase={CurrentLessonPhase}; ConversationModeState={CurrentConversationModeState}; IsConversationModeEnabled={IsConversationModeEnabled}");

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
                $"ConversationModeState={CurrentConversationModeState}; " +
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
        catch (AudioTranscriptionBackendException exception)
        {
            BackendStatusText = BackendConstants.BackendStatusConnected;
            StatusMessage = localizedText.TranscriptionFailedMessage;
            Debug.WriteLine($"Voice transcription backend failure; recording state will be reset. StatusCode={exception.StatusCode}.");
        }
        catch (OperationCanceledException exception)
        {
            Debug.WriteLine($"Voice transcription canceled; recording state will be reset. {exception}");
            BackendStatusText = BackendConstants.BackendStatusConnected;
            StatusMessage = localizedText.TranscriptionFailedMessage;
        }
        catch
        {
            BackendStatusText = BackendConstants.BackendStatusUnavailable;
            StatusMessage = localizedText.TranscriptionFailedMessage;
            Debug.WriteLine("Voice transcription failed unexpectedly; recording state will be reset.");
        }
        finally
        {
            CompleteUiOperationDiagnostics(
                operationStopwatch,
                "normal_recording_stop",
                RecordingStopCommitWarningThresholdMilliseconds,
                $"CurrentLessonPhase={CurrentLessonPhase}; ConversationModeState={CurrentConversationModeState}; IsRecording={IsRecording}; IsTranscribing={isTranscribingAudio}");
            BotStatus = BackendConstants.BotStatusReady;
            SetIsTranscribingAudio(false);
            IsRecording = false;
            RefreshAvatarState();
            audioRecordingService.SafeDeleteRecording(savedFilePath);
            RefreshAllCommandStates();
        }
    }


    private static Stopwatch StartUiOperationDiagnostics(string operationName, int warningThresholdMilliseconds, string context)
    {
        Debug.WriteLine($"UI operation start: Operation={operationName}; ThresholdMs={warningThresholdMilliseconds}; {context}");
        return Stopwatch.StartNew();
    }

    private static void CompleteUiOperationDiagnostics(Stopwatch stopwatch, string operationName, int warningThresholdMilliseconds, string context)
    {
        stopwatch.Stop();
        Debug.WriteLine($"UI operation end: Operation={operationName}; DurationMs={stopwatch.ElapsedMilliseconds}; ThresholdMs={warningThresholdMilliseconds}; {context}");

        if (stopwatch.ElapsedMilliseconds > warningThresholdMilliseconds)
        {
            Debug.WriteLine($"UI operation warning: Operation={operationName}; DurationMs={stopwatch.ElapsedMilliseconds}; ThresholdMs={warningThresholdMilliseconds}; {context}");
        }
    }

    private void SetIsTranscribingAudio(bool value)
    {
        if (isTranscribingAudio == value)
        {
            return;
        }

        isTranscribingAudio = value;
        OnPropertyChanged(nameof(IsLessonInputEnabled));
        OnPropertyChanged(nameof(CanTypeText));
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
        await realtimeRecordingSemaphore.WaitAsync();
        var operationStopwatch = StartUiOperationDiagnostics(
            "realtime_recording_stop",
            RecordingStopCommitWarningThresholdMilliseconds,
            $"SessionId={realtimeSessionId}; State={CurrentConversationModeState}; IsRecording={IsRecording}");

        try
        {
            var duration = SafeStopRealtimeMicrophone("record_stop_requested");
            IsRecording = false;

            if (duration.TotalMilliseconds < AudioConstants.MinimumRecordingDurationMilliseconds)
            {
                ResolveRealtimePlaceholderAsStatus(RealtimeVoiceTranscriptionUnavailableText, "record_too_short");
                StatusMessage = AudioConstants.RecordingTooShortMessage;
                SetConversationModeState(isRealtimeSessionStarted ? ConversationModeState.Ready : ConversationModeState.NotStarted, "record_too_short");
                return;
            }

            if (!CanAcceptTranscriptionResult)
            {
                StatusMessage = AppConstants.LessonCompleteAwaitingFinishMessage;
                SetConversationModeState(ConversationModeState.CompletedAwaitingFinish, "record_stop_lesson_complete");
                return;
            }

            SetConversationModeState(ConversationModeState.WaitingForTranscript, "record_committing_audio");
            realtimeUserTranscriptBuffer.Clear();
            realtimeUserPlaceholderItemId = string.Empty;
            realtimeUserPlaceholderMessage = AddMessage(AppConstants.UserSenderName, RealtimeVoicePendingText, false, source: ChatMessageSource.RealtimeVoice, isTechnicalMessage: true);
            Debug.WriteLine($"Realtime user placeholder message added: SessionId={realtimeSessionId}; UserPlaceholderMessageId={realtimeUserPlaceholderMessage.Id}; Text={RealtimeVoicePendingText}; LearnerTurnCountBefore={LearnerTurnCount}.");

            Debug.WriteLine($"Realtime audio commit starting: SessionId={realtimeSessionId}; State={CurrentConversationModeState}; DurationMs={duration.TotalMilliseconds:F0}.");
            await realtimeVoiceEngine.CommitUserAudioAsync(CancellationToken.None);
            Debug.WriteLine($"Realtime audio commit sent: SessionId={realtimeSessionId}; DurationMs={duration.TotalMilliseconds:F0}.");
            StatusMessage = string.Empty;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Realtime voice recording stop failed: SessionId={realtimeSessionId}; State={CurrentConversationModeState}; {exception}");
            ResolveRealtimePlaceholderAsStatus("[Voice input failed. Please try again.]", "record_stop_failed");
            IsRecording = false;
            SetConversationModeState(isRealtimeSessionStarted ? ConversationModeState.Ready : ConversationModeState.Faulted, "record_stop_failed");
            StatusMessage = BackendConstants.RealtimeUnavailableMessage;
        }
        finally
        {
            CompleteUiOperationDiagnostics(
                operationStopwatch,
                "realtime_recording_stop",
                RecordingStopCommitWarningThresholdMilliseconds,
                $"SessionId={realtimeSessionId}; State={CurrentConversationModeState}; IsRecording={IsRecording}; RecordBlockReason={GetRealtimeRecordBlockReason()}");
            IsRecording = false;
            RefreshAvatarState();
            RefreshAllCommandStates();
            realtimeRecordingSemaphore.Release();
        }
    }

    [RelayCommand(CanExecute = nameof(CanToggleConversationMode))]
    private async Task ToggleConversationModeAsync()
    {
        if (!await realtimeLifecycleSemaphore.WaitAsync(0))
        {
            Debug.WriteLine($"Conversation Mode toggle ignored because lifecycle operation is active. State={CurrentConversationModeState}; SessionId={realtimeSessionId}.");
            return;
        }

        var isDisablingConversationMode = IsConversationModeEnabled;
        var operationName = isDisablingConversationMode ? "conversation_mode_exit" : "conversation_mode_enter";
        var operationThresholdMilliseconds = isDisablingConversationMode
            ? UiOperationWarningThresholdMilliseconds
            : RealtimeStartupWarningThresholdMilliseconds;
        var operationStopwatch = StartUiOperationDiagnostics(
            operationName,
            operationThresholdMilliseconds,
            $"SessionId={realtimeSessionId}; State={CurrentConversationModeState}; CurrentLessonPhase={CurrentLessonPhase}; IsConversationModeEnabled={IsConversationModeEnabled}");

        try
        {
            LogLessonStateSnapshot("Conversation Mode toggle requested");

            if (IsConversationModeEnabled)
            {
                await StopRealtimeConversationAsync("conversation_mode_off");
                IsConversationModeEnabled = false;
                SetConversationModeState(ConversationModeState.NotStarted, "conversation_mode_off");
                LogLessonStateSnapshot("Conversation Mode toggle off");
                return;
            }

            if (IsGuidedRoleplayLesson() && CurrentLessonPhase == LessonPhase.SetupContextSelection)
            {
                IsConversationModeEnabled = true;
                SetConversationModeState(ConversationModeState.NotStarted, "setup_realtime_deferred");
                StatusMessage = "Choose a situation to start the conversation.";
                Debug.WriteLine($"Conversation mode enabled before guided context selection: LessonType={lessonScenario.Metadata.LessonType}; CurrentLessonPhase={CurrentLessonPhase}; SelectedTopic={SelectedTopic.Title}; SelectedSubtopic={SelectedSubtopic.Title}; UseRealtimeConversationMode={BackendConstants.UseRealtimeConversationMode}; BackendEndpoint={lessonChatBackendService.CreateRealtimeVoiceWebSocketUri()}.");
                LogLessonStateSnapshot("Conversation Mode enabled in setup; realtime deferred");
                return;
            }

            var startStopwatch = Stopwatch.StartNew();
            Debug.WriteLine($"Conversation mode start requested: LessonType={lessonScenario.Metadata.LessonType}; CurrentLessonPhase={CurrentLessonPhase}; IsFreeConversation={IsFreeConversationLesson()}; SelectedTopic={SelectedTopic.Title}; SelectedSubtopic={SelectedSubtopic.Title}; UseRealtimeConversationMode={BackendConstants.UseRealtimeConversationMode}; BackendEndpoint={lessonChatBackendService.CreateRealtimeVoiceWebSocketUri()}.");

            try
            {
                PrepareForRealtimeConversationStartup("conversation_mode_start_requested");
                IsConversationModeEnabled = true;
                SetConversationModeState(ConversationModeState.Starting, "conversation_mode_start_requested");
                await EnsureRealtimeSessionStartedAsync(CancellationToken.None);
                await PlayRealtimePreStartOpeningAsync(CancellationToken.None);
                SetConversationModeState(ConversationModeState.Ready, "conversation_mode_start_succeeded");
                StatusMessage = string.Empty;
                Debug.WriteLine($"Conversation mode started: RealtimeSessionId={realtimeSessionId}; ElapsedMs={startStopwatch.ElapsedMilliseconds}.");
                LogLessonStateSnapshot("Realtime start success");
            }
            catch (Exception exception)
            {
                await CleanupRealtimeAfterFaultAsync("conversation_mode_start_failed");
                BackendStatusText = BackendConstants.BackendStatusUnavailable;
                StatusMessage = "Conversation Mode could not start. Please try again.";
                Debug.WriteLine($"Conversation mode start failed: RealtimeSessionId={realtimeSessionId}; ExceptionType={exception.GetType().FullName}; Message={exception.Message}; {exception}");
                LogLessonStateSnapshot("Realtime start failure");
            }
        }
        finally
        {
            CompleteUiOperationDiagnostics(
                operationStopwatch,
                operationName,
                operationThresholdMilliseconds,
                $"SessionId={realtimeSessionId}; State={CurrentConversationModeState}; IsConversationModeEnabled={IsConversationModeEnabled}; IsStarted={isRealtimeSessionStarted}");
            RefreshAllCommandStates();
            realtimeLifecycleSemaphore.Release();
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

    private async Task PlayRealtimePreStartOpeningAsync(CancellationToken cancellationToken)
    {
        var openingMessages = SelectRealtimeOpeningMessagesToSpeak();
        if (openingMessages.Count == 0)
        {
            Debug.WriteLine($"Realtime pre-start opening playback skipped: SessionId={realtimeSessionId}; Reason=no_current_unspoken_bot_prompt; LearnerTurnCount={LearnerTurnCount}.");
            return;
        }

        var operationStopwatch = StartUiOperationDiagnostics(
            "realtime_opening_playback",
            TtsPlaybackPreparationWarningThresholdMilliseconds,
            $"SessionId={realtimeSessionId}; MessageCount={openingMessages.Count}; Purpose={BackendConstants.RealtimePreStartOpeningSpeechPurpose}; Model={BackendConstants.TtsModelName}");

        SetConversationModeState(ConversationModeState.OpeningPlayback, "realtime_pre_start_opening_playback_start");
        IsBotVoicePlaying = true;
        RefreshAvatarState();
        StatusMessage = $"{TutorAvatarDisplayName} is speaking...";
        RefreshAllCommandStates();

        try
        {
            foreach (var openingMessage in openingMessages)
            {
                var exactText = GetExactBotVoiceText(openingMessage);
                Debug.WriteLine($"Realtime pre-start opening playback request: SessionId={realtimeSessionId}; MessageId={openingMessage.Id}; Purpose={BackendConstants.RealtimePreStartOpeningSpeechPurpose}; Model={BackendConstants.TtsModelName}; TextLength={exactText.Length}; ExactVisibleText=True.");
                await PlayBotVoiceForMessageCoreAsync(
                    openingMessage,
                    isAutoPlay: false,
                    allowDuringRealtimeOpeningPlayback: true,
                    speechPurpose: BackendConstants.RealtimePreStartOpeningSpeechPurpose,
                    cancellationToken: cancellationToken);
                spokenRealtimeOpeningMessageIds.Add(openingMessage.Id);
            }
        }
        catch (OperationCanceledException exception)
        {
            Debug.WriteLine($"Realtime pre-start opening playback canceled: SessionId={realtimeSessionId}; MessageCount={openingMessages.Count}; {exception}");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Realtime pre-start opening playback failed: SessionId={realtimeSessionId}; MessageCount={openingMessages.Count}; {exception}");
            StatusMessage = "Conversation Mode ready. Opening voice could not play.";
        }
        finally
        {
            CompleteUiOperationDiagnostics(
                operationStopwatch,
                "realtime_opening_playback",
                TtsPlaybackPreparationWarningThresholdMilliseconds,
                $"SessionId={realtimeSessionId}; MessageCount={openingMessages.Count}; State={CurrentConversationModeState}; IsStarted={isRealtimeSessionStarted}");
            IsBotVoicePlaying = false;
            RefreshAvatarState();
            if (isRealtimeSessionStarted && !IsCompletedAwaitingFinish && IsConversationModeEnabled)
            {
                SetConversationModeState(ConversationModeState.Ready, "realtime_pre_start_opening_playback_finished");
            }
            RefreshAllCommandStates();
        }
    }

    private IReadOnlyList<ChatMessageViewModel> SelectRealtimeOpeningMessagesToSpeak()
    {
        var trailingBotMessages = Messages
            .Reverse()
            .TakeWhile(message => message.IsFromBot && !message.IsTechnicalMessage && !string.IsNullOrWhiteSpace(message.Text))
            .Reverse()
            .Where(message => !spokenRealtimeOpeningMessageIds.Contains(message.Id))
            .ToList();

        if (trailingBotMessages.Count > 0)
        {
            return trailingBotMessages;
        }

        var latestBotPrompt = Messages.LastOrDefault(message => message.IsFromBot && !message.IsTechnicalMessage && !string.IsNullOrWhiteSpace(message.Text));
        if (latestBotPrompt is null || spokenRealtimeOpeningMessageIds.Contains(latestBotPrompt.Id))
        {
            return [];
        }

        var latestValidLearnerTurnIndex = Messages
            .Select((message, index) => new { Message = message, Index = index })
            .LastOrDefault(item => !item.Message.IsFromBot && item.Message.CountsAsValidLessonTurn)?.Index ?? -1;
        var latestBotPromptIndex = Messages.IndexOf(latestBotPrompt);
        return latestBotPromptIndex > latestValidLearnerTurnIndex ? [latestBotPrompt] : [];
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

    private Task PlayBotVoiceForMessageAsync(
        ChatMessageViewModel message,
        bool isAutoPlay,
        CancellationToken cancellationToken = default)
    {
        return PlayBotVoiceForMessageCoreAsync(message, isAutoPlay, allowDuringRealtimeOpeningPlayback: false, speechPurpose: BackendConstants.LessonChatTtsPurpose, cancellationToken: cancellationToken);
    }

    private async Task PlayBotVoiceForMessageCoreAsync(
        ChatMessageViewModel message,
        bool isAutoPlay,
        bool allowDuringRealtimeOpeningPlayback,
        string speechPurpose,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.Text))
        {
            return;
        }

        if (IsRealtimeConversationActive && !allowDuringRealtimeOpeningPlayback)
        {
            Debug.WriteLine($"Skipping bot voice {(isAutoPlay ? "auto-play" : "manual play")} during active Conversation Mode: MessageId={message.Id}; SessionId={realtimeSessionId}.");
            StatusMessage = string.Empty;
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

        if (IsRealtimeConversationActive && !allowDuringRealtimeOpeningPlayback)
        {
            Debug.WriteLine($"Skipped bot voice {(isAutoPlay ? "auto-play" : "manual play")} after waiting because Conversation Mode became active: MessageId={message.Id}; SessionId={realtimeSessionId}.");
            botVoiceSemaphore.Release();
            StatusMessage = string.Empty;
            return;
        }

        using var playbackCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        SetCurrentBotVoiceCancellationTokenSource(playbackCancellationTokenSource);
        var playbackStarted = false;
        var totalStopwatch = Stopwatch.StartNew();
        var selectedBotVoicePath = AudioConstants.BotVoiceDefaultPathName;
        var operationName = allowDuringRealtimeOpeningPlayback
            ? "realtime_pre_start_opening_playback_voice"
            : isAutoPlay ? "auto_play_bot_voice" : "play_voice";
        var operationStopwatch = StartUiOperationDiagnostics(
            operationName,
            TtsPlaybackPreparationWarningThresholdMilliseconds,
            $"MessageId={message.Id}; AutoPlay={isAutoPlay}; AllowDuringRealtimeOpeningPlayback={allowDuringRealtimeOpeningPlayback}; Purpose={speechPurpose}; TextLength={message.Text.Trim().Length}");

        try
        {
            IsBotVoicePlaying = true;
            if (isAutoPlay)
            {
                usedAutoPlayVoice = true;
            }
            else if (!allowDuringRealtimeOpeningPlayback)
            {
                usedManualPlayVoice = true;
            }
            RefreshAvatarState();
            StatusMessage = allowDuringRealtimeOpeningPlayback ? $"{TutorAvatarDisplayName} is speaking..." : localizedText.PlayingBotVoiceMessage;

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
                isAutoPlay,
                speechPurpose);

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
            CompleteUiOperationDiagnostics(
                operationStopwatch,
                operationName,
                TtsPlaybackPreparationWarningThresholdMilliseconds,
                $"MessageId={message.Id}; AutoPlay={isAutoPlay}; PlaybackStarted={playbackStarted}; TotalMs={totalStopwatch.ElapsedMilliseconds}");
            ClearCurrentBotVoiceCancellationTokenSource(playbackCancellationTokenSource);
            IsBotVoicePlaying = false;
            RefreshAvatarState();
            RefreshAllCommandStates();
            botVoiceSemaphore.Release();
        }
    }

    private async Task PlaySegmentedHighQualityBotVoiceAsync(
        ChatMessageViewModel message,
        IReadOnlyList<string> segments,
        CancellationToken cancellationToken,
        Action<long> onFirstPlaybackStarted,
        Stopwatch totalStopwatch,
        bool isAutoPlay,
        string speechPurpose)
    {
        var firstSegmentTask = GetOrCreateBotVoiceSegmentAudioFileAsync(
            message,
            segments[0],
            segmentIndex: 0,
            timeout: TimeSpan.FromSeconds(AudioConstants.BotVoiceFirstSegmentHardTimeoutSeconds),
            totalStopwatch,
            cancellationToken,
            isAutoPlay,
            speechPurpose);

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
            ? GetOrCreateBotVoiceSegmentAudioFileAsync(message, segments[1], 1, TimeSpan.FromSeconds(AudioConstants.BotVoiceLaterSegmentHardTimeoutSeconds), totalStopwatch, cancellationToken, isAutoPlay, speechPurpose)
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
                ? GetOrCreateBotVoiceSegmentAudioFileAsync(message, segments[nextIndex], nextIndex, TimeSpan.FromSeconds(AudioConstants.BotVoiceLaterSegmentHardTimeoutSeconds), totalStopwatch, cancellationToken, isAutoPlay, speechPurpose)
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
        bool isAutoPlay = false,
        string speechPurpose = BackendConstants.LessonChatTtsPurpose)
    {
        var cacheKey = CreateBotVoiceSegmentCacheKey(message.Id, segmentIndex, segmentText, speechPurpose);
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
                isAutoPlay,
                speechPurpose);
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
        bool isAutoPlay,
        string speechPurpose)
    {
        var cacheKey = CreateBotVoiceSegmentCacheKey(message.Id, segmentIndex, segmentText, speechPurpose);
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
            var speechResponse = await lessonChatBackendService.CreateBotSpeechAsync(normalizedSegmentText, linkedCancellationTokenSource.Token, speechPurpose);
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

    private static string CreateBotVoiceSegmentCacheKey(int messageId, int segmentIndex, string segmentText, string speechPurpose = BackendConstants.LessonChatTtsPurpose)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}:{2}:{3}",
            messageId,
            segmentIndex,
            NormalizeBotVoiceSegmentText(segmentText),
            string.IsNullOrWhiteSpace(speechPurpose) ? BackendConstants.LessonChatTtsPurpose : speechPurpose);
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
            LogTextInputState("send_execute_blocked", canSend: false);
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
            LogTextInputState("send_lesson_message_blocked", canSend: false);
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
                SelectedContextOpeningLine = GetSelectedContextOpeningLine(),
                SelectedContextConfirmationLine = selectedContextVariant is null ? string.Empty : GetSelectedContextConfirmationLine(selectedContextVariant),
                SelectedContextOpeningIntent = selectedContextVariant?.OpeningIntent ?? string.Empty,
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
                ConversationWrapUpIntent = lessonScenario.ConversationFlow.WrapUpIntent,
                ConversationFinalMessageIntent = lessonScenario.ConversationFlow.FinalMessageIntent,
                RoleplayBeats = lessonScenario.RoleplayBeats.Select(beat => new ScenarioRoleplayBeat { Id = beat.Id, Intent = beat.Intent }).ToArray(),
                ReciprocalQuestionIfUserAsksTutorName = lessonScenario.ReciprocalQuestionHandling.IfUserAsksTutorName,
                ReciprocalQuestionIfUserAsksSimplePersonalQuestion = lessonScenario.ReciprocalQuestionHandling.IfUserAsksSimplePersonalQuestion,
                ReciprocalQuestionMustNotIgnoreUserQuestion = lessonScenario.ReciprocalQuestionHandling.MustNotIgnoreUserQuestion,
                ReciprocalQuestionMustNotRefuseScenarioCompatibleQuestions = lessonScenario.ReciprocalQuestionHandling.MustNotRefuseScenarioCompatibleQuestions,
                ExpectedScenarioProgression = lessonScenario.ExpectedScenarioProgression,
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

            if (response.IsLessonComplete && !shouldEndLessonNow)
            {
                Debug.WriteLine($"Ignoring early backend lesson completion: LearnerTurnCount={LearnerTurnCount}; NextLearnerTurnCount={nextLearnerTurnCount}; FinalTurn={finalTurn}; CurrentLessonPhase={CurrentLessonPhase}.");
            }

            if (shouldEndLessonNow)
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

    private void PrepareForRealtimeConversationStartup(string reason)
    {
        CancelCurrentBotVoice(BotVoiceCancellationReasons.RealtimeStartupCancel);
        audioPlaybackService.StopPlayback();
        IsBotVoicePlaying = false;
        StatusMessage = string.Empty;
        RefreshAvatarState();
        Debug.WriteLine($"Normal bot voice playback stopped before Conversation Mode startup: Reason={reason}; SessionId={realtimeSessionId}; AutoPlaySuppressed={IsConversationModeEnabled || BackendConstants.UseRealtimeConversationMode}.");
    }

    private async Task EnsureRealtimeSessionStartedAsync(CancellationToken cancellationToken)
    {
        if (isRealtimeSessionStarted)
        {
            return;
        }

        PrepareForRealtimeConversationStartup("ensure_realtime_session_starting");
        isStartingRealtimeSession = true;
        SetConversationModeState(ConversationModeState.Starting, "ensure_realtime_session_starting");

        try
        {
            realtimeSessionId = Guid.NewGuid().ToString("N");
            var stopwatch = Stopwatch.StartNew();
            await realtimeVoiceEngine.StartSessionAsync(BuildVoiceSessionStartRequest(), cancellationToken);
            isRealtimeSessionStarted = true;
            isStartingRealtimeSession = false;
            BackendStatusText = BackendConstants.BackendStatusConnected;
            SetConversationModeState(ConversationModeState.Ready, "ensure_realtime_session_started");
            LogRealtimeRecordState("after_session_ready_start_task_completed");
            Debug.WriteLine($"Desktop realtime session start ms: SessionId={realtimeSessionId}; RealtimeSessionStartMs={stopwatch.ElapsedMilliseconds}; TutorProfileId={tutorProfile.Id}; TutorDisplayName={tutorProfile.DisplayName}; LessonType={lessonScenario.Metadata.LessonType}; Topic={lessonScenario.Metadata.Topic}; Subtopic={lessonScenario.Metadata.Subtopic}; Level={SelectedLevel}; SelectedContextTitle={GetSelectedContextTitle()}.");
        }
        catch
        {
            isRealtimeSessionStarted = false;
            SetConversationModeState(ConversationModeState.Faulted, "ensure_realtime_session_failed");
            throw;
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
            SelectedContextOpeningLine = GetSelectedContextOpeningLine(),
            SelectedContextConfirmationLine = selectedContextVariant is null ? string.Empty : GetSelectedContextConfirmationLine(selectedContextVariant),
            SelectedContextOpeningIntent = selectedContextVariant?.OpeningIntent ?? string.Empty,
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
            ConversationWrapUpIntent = lessonScenario.ConversationFlow.WrapUpIntent,
            ConversationFinalMessageIntent = lessonScenario.ConversationFlow.FinalMessageIntent,
            RoleplayBeats = lessonScenario.RoleplayBeats,
            ReciprocalQuestionIfUserAsksTutorName = lessonScenario.ReciprocalQuestionHandling.IfUserAsksTutorName,
            ReciprocalQuestionIfUserAsksSimplePersonalQuestion = lessonScenario.ReciprocalQuestionHandling.IfUserAsksSimplePersonalQuestion,
            ReciprocalQuestionMustNotIgnoreUserQuestion = lessonScenario.ReciprocalQuestionHandling.MustNotIgnoreUserQuestion,
            ReciprocalQuestionMustNotRefuseScenarioCompatibleQuestions = lessonScenario.ReciprocalQuestionHandling.MustNotRefuseScenarioCompatibleQuestions,
            ExpectedScenarioProgression = lessonScenario.ExpectedScenarioProgression,
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
        var previousState = CurrentConversationModeState;
        var mappedReason = MapRealtimeLifecycleReason(reason);
        Debug.WriteLine($"StopRealtimeConversationAsync requested: SessionId={realtimeSessionId}; Reason={mappedReason}; RawReason={reason}; PreviousState={previousState}; IsStarted={isRealtimeSessionStarted}; IsRecording={IsRecording}; IsSending={IsSending}; IsBotVoicePlaying={IsBotVoicePlaying}; IsRealtimePlaybackActive={IsRealtimePlaybackActive()}.");
        var stopwatch = StartUiOperationDiagnostics(
            "realtime_conversation_stop",
            UiOperationWarningThresholdMilliseconds,
            $"SessionId={realtimeSessionId}; Reason={mappedReason}; RawReason={reason}; PreviousState={previousState}; IsStarted={isRealtimeSessionStarted}; IsRecording={IsRecording}; IsSending={IsSending}");
        SetConversationModeState(ConversationModeState.Stopping, mappedReason);
        try
        {
            CancelCurrentBotVoice(BotVoiceCancellationReasons.RealtimeStartupCancel);
            audioPlaybackService.StopPlayback();
            Debug.WriteLine($"Realtime audio playback stop requested by conversation stop: SessionId={realtimeSessionId}; Reason={mappedReason}; RawReason={reason}.");
            realtimeAudioPlaybackService.Stop(mappedReason);
            SafeStopRealtimeMicrophone(mappedReason);
            Debug.WriteLine($"Realtime engine StopSessionAsync requested: SessionId={realtimeSessionId}; Reason={mappedReason}; RawReason={reason}.");
            await realtimeVoiceEngine.StopSessionAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Realtime conversation stop warning: SessionId={realtimeSessionId}; Reason={reason}; PreviousState={previousState}; {exception}");
        }
        finally
        {
            IsRecording = false;
            IsSending = false;
            IsBotVoicePlaying = false;
            isRealtimeSessionStarted = false;
            realtimeUserTranscriptBuffer.Clear();
            realtimeUserPlaceholderItemId = string.Empty;
            pendingTranscriptByItemId.Clear();
            pendingTranscriptFailureByItemId.Clear();
            SetConversationModeState(IsCompletedAwaitingFinish ? ConversationModeState.CompletedAwaitingFinish : ConversationModeState.NotStarted, mappedReason);
            RefreshAvatarState();
            RefreshAllCommandStates();
            CompleteUiOperationDiagnostics(
                stopwatch,
                "realtime_conversation_stop",
                UiOperationWarningThresholdMilliseconds,
                $"SessionId={realtimeSessionId}; Reason={mappedReason}; RawReason={reason}; PreviousState={previousState}; StopMs={stopwatch.ElapsedMilliseconds}; RecordCanExecute={CanToggleVoiceRecording()}");
            Debug.WriteLine($"Realtime conversation stopped: SessionId={realtimeSessionId}; Reason={mappedReason}; RawReason={reason}; PreviousState={previousState}; StopMs={stopwatch.ElapsedMilliseconds}; RecordCanExecute={CanToggleVoiceRecording()}.");
        }
    }


    private static string MapRealtimeLifecycleReason(string reason)
    {
        return reason switch
        {
            "back" => "user_clicked_back",
            "conversation_mode_off" => "user_clicked_conversation_mode_exit",
            "finish_lesson" => "user_clicked_finish_lesson",
            "lesson_complete" => "final_cleanup",
            "conversation_mode_start_failed" or "guided_context_realtime_start_failed" or "session_error_recoverable" or "receive_loop_failed" or "fallback_text_send_failed" => "runtime_failure",
            "assistant_audio_delta" => "assistant_playback_started",
            "assistant_turn_completed" or "assistant_playback_completed" or "assistant_turn_completed_waiting_for_playback" or "assistant_turn_completed_no_playback" => "assistant_playback_completed",
            "realtime_pre_start_opening_playback_finished" => "opening_playback_completed",
            "cleanup_finally" => "cleanup_finally",
            _ when string.IsNullOrWhiteSpace(reason) => "unknown",
            _ => reason
        };
    }

    private void SetConversationModeState(ConversationModeState newState, string reason)
    {
        if (IsCompletedAwaitingFinish && newState != ConversationModeState.Stopping)
        {
            newState = ConversationModeState.CompletedAwaitingFinish;
        }

        var oldState = CurrentConversationModeState;
        if (oldState != newState)
        {
            CurrentConversationModeState = newState;
        }

        var recordEnabled = CanToggleVoiceRecording();
        Debug.WriteLine($"ConversationModeStateTransition OldState={oldState}; NewState={newState}; Reason={reason}; SessionId={realtimeSessionId}; IsConversationModeActive={IsRealtimeConversationActive}; MicrophoneCapturing={realtimeMicrophoneCaptureService.IsRecording}; WebSocketStarted={isRealtimeSessionStarted}; IsStarting={isStartingRealtimeSession}; IsRecording={IsRecording}; IsSending={IsSending}; IsTranscribing={isTranscribingAudio}; IsBotVoicePlaying={IsBotVoicePlaying}; IsBotTyping={IsBotTyping}; IsLessonCompleteAwaitingFinish={IsLessonCompleteAwaitingFinish}; IsRealtimePlaybackActive={IsRealtimePlaybackActive()}; CanStartRealtimeRecording={CanStartRealtimeRecording}; CanStartNormalRecording={CanStartNormalRecording()}; RecordButtonEnabled={recordEnabled}; MainRecordCommandCanExecute={recordEnabled}; RecordBlockReason={GetRealtimeRecordBlockReason()}; CommandsRefreshed=True.");
        RefreshAllCommandStates();
    }

    private TimeSpan SafeStopRealtimeMicrophone(string reason)
    {
        try
        {
            var duration = realtimeMicrophoneCaptureService.Stop();
            Debug.WriteLine($"Realtime microphone stopped: SessionId={realtimeSessionId}; Reason={MapRealtimeLifecycleReason(reason)}; RawReason={reason}; DurationMs={duration.TotalMilliseconds:F0}.");
            return duration;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Realtime microphone stop warning: SessionId={realtimeSessionId}; Reason={reason}; {exception}");
            return TimeSpan.Zero;
        }
    }

    private void ResolveRealtimePlaceholderAsStatus(string statusText, string reason)
    {
        var target = realtimeUserPlaceholderMessage;
        if (target is null || !string.Equals(target.Text, RealtimeVoicePendingText, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        target.MarkAsInvalidLearnerTranscript(statusText);
        Debug.WriteLine($"Realtime voice placeholder resolved as status: SessionId={realtimeSessionId}; UserPlaceholderMessageId={target.Id}; Reason={reason}; CountsAsValidLessonTurn={target.CountsAsValidLessonTurn}; FeedbackEligible={target.IsFeedbackEligible}.");
        ViewFeedbackCommand.NotifyCanExecuteChanged();
    }

    private async Task CleanupRealtimeAfterFaultAsync(string reason)
    {
        var mappedReason = MapRealtimeLifecycleReason(reason);
        Debug.WriteLine($"CleanupRealtimeAfterFaultAsync requested: SessionId={realtimeSessionId}; Reason={mappedReason}; RawReason={reason}; State={CurrentConversationModeState}; IsStarted={isRealtimeSessionStarted}; IsRealtimePlaybackActive={IsRealtimePlaybackActive()}.");
        ResolveRealtimePlaceholderAsStatus(RealtimeVoiceTranscriptionUnavailableText, mappedReason);
        Debug.WriteLine($"Realtime audio playback stop requested by fault cleanup: SessionId={realtimeSessionId}; Reason={mappedReason}; RawReason={reason}.");
        realtimeAudioPlaybackService.Stop(mappedReason);
        SafeStopRealtimeMicrophone(mappedReason);
        try
        {
            Debug.WriteLine($"Realtime engine StopSessionAsync requested by fault cleanup: SessionId={realtimeSessionId}; Reason={mappedReason}; RawReason={reason}.");
            await realtimeVoiceEngine.StopSessionAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Realtime fault cleanup stop warning: SessionId={realtimeSessionId}; Reason={reason}; {exception}");
        }

        IsConversationModeEnabled = false;
        IsRecording = false;
        IsSending = false;
        IsBotVoicePlaying = false;
        isRealtimeSessionStarted = false;
        isStartingRealtimeSession = false;
        realtimeUserTranscriptBuffer.Clear();
        realtimeUserPlaceholderItemId = string.Empty;
        pendingTranscriptByItemId.Clear();
        pendingTranscriptFailureByItemId.Clear();
        SetConversationModeState(IsCompletedAwaitingFinish ? ConversationModeState.CompletedAwaitingFinish : ConversationModeState.NotStarted, mappedReason);
        RefreshAvatarState();
    }

    private void OnRealtimeMicrophoneAudioChunkCaptured(object? sender, RealtimeMicrophoneAudioChunkEventArgs args)
    {
        BufferRealtimeAudioChunkForFallback(args.AudioChunk);
        Debug.WriteLine($"Realtime microphone audio chunk captured: SessionId={realtimeSessionId}; Bytes={args.AudioChunk.Length}; State={CurrentConversationModeState}; FallbackBufferedBytes={realtimeCommittedAudioBytes}; FallbackAudioChunkCount={realtimeCommittedAudioChunkCount}.");
        _ = SendRealtimeAudioChunkAsync(args.AudioChunk);
    }

    private async Task SendRealtimeAudioChunkAsync(byte[] audioChunk)
    {
        try
        {
            Debug.WriteLine($"Realtime audio append starting: SessionId={realtimeSessionId}; Bytes={audioChunk.Length}.");
            await realtimeVoiceEngine.AppendUserAudioAsync(audioChunk, CancellationToken.None);
            Debug.WriteLine($"Realtime audio append sent: SessionId={realtimeSessionId}; Bytes={audioChunk.Length}.");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Realtime audio append failed: SessionId={realtimeSessionId}; Bytes={audioChunk.Length}; {exception}");
        }
    }

    private void ResetRealtimeCommittedAudioBuffer()
    {
        realtimeCommittedAudioBuffer.Clear();
        realtimeCommittedAudioChunkCount = 0;
        realtimeCommittedAudioBytes = 0;
        realtimeFallbackTranscriptionAttempted = false;
        realtimeFallbackTranscriptionInProgress = false;
    }

    private void BufferRealtimeAudioChunkForFallback(byte[] audioChunk)
    {
        if (audioChunk.Length == 0)
        {
            return;
        }

        realtimeCommittedAudioBuffer.AddRange(audioChunk);
        realtimeCommittedAudioChunkCount++;
        realtimeCommittedAudioBytes += audioChunk.Length;
    }

    private double EstimateRealtimeBufferedAudioDurationSeconds()
    {
        var bytesPerSecond = AudioConstants.RealtimeInputPcmSampleRate
            * AudioConstants.RealtimeInputPcmChannels
            * AudioConstants.RealtimeInputPcmBitsPerSample
            / 8.0;
        return bytesPerSecond <= 0 ? 0 : realtimeCommittedAudioBytes / bytesPerSecond;
    }

    private void LogInvalidRealtimeTranscriptDecision(
        string sessionId,
        string itemId,
        LessonTranscriptValidationReason validationReason,
        int transcriptLength,
        bool retryPromptShown)
    {
        Debug.WriteLine(
            $"Realtime invalid transcript decision: SessionId={sessionId}; " +
            $"RealtimeUserTurnId={itemId}; LearnerTurnNumber={LearnerTurnCount + 1}; " +
            $"AudioChunkCount={realtimeCommittedAudioChunkCount}; BufferedBytes={realtimeCommittedAudioBytes}; " +
            $"EstimatedBufferedAudioDurationSeconds={EstimateRealtimeBufferedAudioDurationSeconds():F2}; " +
            $"TranscriptLength={transcriptLength}; ValidationReason={validationReason}; " +
            $"WasEmpty={validationReason == LessonTranscriptValidationReason.Empty}; " +
            $"WasNonEnglish={validationReason is LessonTranscriptValidationReason.NonLatinScript or LessonTranscriptValidationReason.MostlyNonLatinScript or LessonTranscriptValidationReason.NoEnglishContent}; " +
            $"WasTooShort={validationReason == LessonTranscriptValidationReason.TooShort}; " +
            $"WasPlaceholder={validationReason == LessonTranscriptValidationReason.Placeholder}; " +
            $"RetryPromptShown={retryPromptShown}.");
    }

    private void TryStartRealtimeFallbackTranscription(ChatMessageViewModel target, string itemId, string sessionId, string reason)
    {
        if (realtimeFallbackTranscriptionAttempted || realtimeFallbackTranscriptionInProgress || realtimeCommittedAudioBytes == 0)
        {
            Debug.WriteLine($"Realtime fallback transcription skipped: SessionId={sessionId}; ItemId={itemId}; Reason={reason}; Attempted={realtimeFallbackTranscriptionAttempted}; InProgress={realtimeFallbackTranscriptionInProgress}; BufferedBytes={realtimeCommittedAudioBytes}.");
            return;
        }

        realtimeFallbackTranscriptionAttempted = true;
        realtimeFallbackTranscriptionInProgress = true;
        var audioBytes = realtimeCommittedAudioBuffer.ToArray();
        var audioChunkCount = realtimeCommittedAudioChunkCount;
        var estimatedDurationSeconds = EstimateRealtimeBufferedAudioDurationSeconds();
        Debug.WriteLine($"Realtime fallback transcription starting: SessionId={sessionId}; ItemId={itemId}; Reason={reason}; AudioChunkCount={audioChunkCount}; BufferedBytes={audioBytes.Length}; EstimatedBufferedAudioDurationSeconds={estimatedDurationSeconds:F2}; Model={BackendConstants.TranscriptionModelName}; Language=en.");
        _ = RunRealtimeFallbackTranscriptionAsync(target, itemId, sessionId, reason, audioBytes, audioChunkCount, estimatedDurationSeconds);
    }

    private async Task RunRealtimeFallbackTranscriptionAsync(
        ChatMessageViewModel target,
        string itemId,
        string sessionId,
        string reason,
        byte[] audioBytes,
        int audioChunkCount,
        double estimatedDurationSeconds)
    {
        var fallbackFilePath = string.Empty;
        try
        {
            fallbackFilePath = await SaveRealtimeFallbackAudioFileAsync(audioBytes);
            var fallbackTranscript = await lessonChatBackendService.SendAudioForTranscriptionAsync(fallbackFilePath, CancellationToken.None);
            var validation = LessonTranscriptValidator.Validate(fallbackTranscript);
            Task? applyValidTranscriptTask = null;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (!ReferenceEquals(target, FindRealtimeUserMessage(itemId)) || target.CountsAsValidLessonTurn)
                {
                    Debug.WriteLine($"Realtime fallback transcription ignored stale target: SessionId={sessionId}; ItemId={itemId}; Reason={reason}; TargetMessageId={target.Id}; CountsAsValidLessonTurn={target.CountsAsValidLessonTurn}.");
                    return;
                }

                Debug.WriteLine($"Realtime fallback transcription completed: SessionId={sessionId}; ItemId={itemId}; Reason={reason}; AudioChunkCount={audioChunkCount}; BufferedBytes={audioBytes.Length}; EstimatedBufferedAudioDurationSeconds={estimatedDurationSeconds:F2}; TranscriptLength={validation.NormalizedTranscript.Length}; IsValid={validation.IsValid}; ValidationReason={validation.Reason}; UsageMetricsLogged=True.");
                if (!validation.IsValid)
                {
                    LogInvalidRealtimeTranscriptDecision(sessionId, itemId, validation.Reason, validation.NormalizedTranscript.Length, retryPromptShown: true);
                    target.MarkAsInvalidLearnerTranscript(RealtimeVoiceTranscriptionUnavailableText);
                    StatusMessage = LessonTranscriptValidator.GetRetryMessage(SelectedLevel);
                    SetConversationModeState(isRealtimeSessionStarted ? ConversationModeState.Ready : ConversationModeState.NotStarted, "fallback_invalid_user_transcript");
                    RefreshAllCommandStates();
                    return;
                }

                applyValidTranscriptTask = ApplyValidRealtimeTranscriptAsync(target, itemId, sessionId, validation.NormalizedTranscript, "fallback_valid_user_transcript");
            });

            if (applyValidTranscriptTask is not null)
            {
                await applyValidTranscriptTask;
            }
        }
        catch (Exception exception)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Debug.WriteLine($"Realtime fallback transcription failed: SessionId={sessionId}; ItemId={itemId}; Reason={reason}; AudioChunkCount={audioChunkCount}; BufferedBytes={audioBytes.Length}; EstimatedBufferedAudioDurationSeconds={estimatedDurationSeconds:F2}; {exception}");
                target.MarkAsInvalidLearnerTranscript(RealtimeVoiceTranscriptionUnavailableText);
                StatusMessage = LessonTranscriptValidator.GetRetryMessage(SelectedLevel);
                SetConversationModeState(isRealtimeSessionStarted ? ConversationModeState.Ready : ConversationModeState.NotStarted, "fallback_transcription_failed");
                RefreshAllCommandStates();
            });
        }
        finally
        {
            realtimeFallbackTranscriptionInProgress = false;
            if (!string.IsNullOrWhiteSpace(fallbackFilePath))
            {
                audioRecordingService.SafeDeleteRecording(fallbackFilePath);
            }
        }
    }

    private static Task<string> SaveRealtimeFallbackAudioFileAsync(byte[] pcmAudioBytes)
    {
        var recordingDirectory = Path.Combine(Path.GetTempPath(), AudioConstants.AppTempFolderName, AudioConstants.RecordingFolderName);
        Directory.CreateDirectory(recordingDirectory);
        var filePath = Path.Combine(recordingDirectory, $"realtime-fallback-{DateTime.Now.ToString(AudioConstants.RecordingTimestampFormat, CultureInfo.InvariantCulture)}{AudioConstants.WavFileExtension}");
        using var writer = new WaveFileWriter(filePath, new WaveFormat(AudioConstants.RealtimeInputPcmSampleRate, AudioConstants.RealtimeInputPcmBitsPerSample, AudioConstants.RealtimeInputPcmChannels));
        writer.Write(pcmAudioBytes, 0, pcmAudioBytes.Length);
        return Task.FromResult(filePath);
    }

    private bool IsActiveRealtimeSessionEvent(string eventSessionId, string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventSessionId) || string.Equals(eventSessionId, realtimeSessionId, StringComparison.Ordinal))
        {
            return true;
        }

        Debug.WriteLine($"Ignoring stale realtime UI event: ActiveSessionId={realtimeSessionId}; EventSessionId={eventSessionId}; EventName={eventName}.");
        return false;
    }

    private void OnRealtimeAssistantAudioChunkReceived(object? sender, AssistantAudioChunkReceivedEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!IsActiveRealtimeSessionEvent(args.SessionId, nameof(OnRealtimeAssistantAudioChunkReceived)))
            {
                return;
            }

            realtimeAudioPlaybackService.AddAudioChunk(args.SessionId, args.ResponseId, args.AudioChunk);
            IsBotVoicePlaying = true;
            SetConversationModeState(ConversationModeState.PlayingAssistantAudio, "assistant_audio_delta");
            RefreshAvatarState();
        });
    }

    private void OnRealtimeAssistantTranscriptDeltaReceived(object? sender, AssistantTranscriptDeltaEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!IsActiveRealtimeSessionEvent(args.SessionId, nameof(OnRealtimeAssistantTranscriptDeltaReceived)))
            {
                return;
            }

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
            if (!IsActiveRealtimeSessionEvent(args.SessionId, nameof(OnRealtimeAssistantTurnCompleted)))
            {
                return;
            }

            var finalTranscript = string.IsNullOrWhiteSpace(args.Transcript) ? lastBotMessage : args.Transcript.Trim();
            if (realtimeAssistantMessage is not null)
            {
                realtimeAssistantMessage.Text = finalTranscript;
            }
            lastBotMessage = finalTranscript;
            OnPropertyChanged(nameof(LatestBotMessageText));
            realtimeAudioPlaybackService.CompleteResponse(args.SessionId, args.ResponseId);
            IsSending = false;
            BotStatus = BackendConstants.BotStatusReady;
            if (realtimeAudioPlaybackService.IsPlaybackActive)
            {
                IsBotVoicePlaying = true;
                SetConversationModeState(ConversationModeState.PlayingAssistantAudio, "assistant_turn_completed_waiting_for_playback");
            }
            else
            {
                IsBotVoicePlaying = false;
                SetConversationModeState(ConversationModeState.Ready, "assistant_turn_completed_no_playback");
            }
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
            if (!IsActiveRealtimeSessionEvent(args.SessionId, nameof(OnRealtimeUserAudioCommitted)))
            {
                return;
            }

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
            if (!IsActiveRealtimeSessionEvent(args.SessionId, nameof(OnRealtimeUserTranscriptDeltaReceived)))
            {
                return;
            }

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
            if (!IsActiveRealtimeSessionEvent(args.SessionId, nameof(OnRealtimeUserTranscriptCompleted)))
            {
                return;
            }

            var transcript = args.Transcript.Trim();
            var itemId = string.IsNullOrWhiteSpace(args.ItemId) ? realtimeUserPlaceholderItemId : args.ItemId;
            if (string.IsNullOrWhiteSpace(transcript))
            {
                ApplyRealtimeUserTranscriptFailure(itemId, args.SessionId);
                return;
            }

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
            if (!IsActiveRealtimeSessionEvent(args.SessionId, nameof(OnRealtimeUserTranscriptFailed)))
            {
                return;
            }

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
            LogInvalidRealtimeTranscriptDecision(sessionId, itemId, validation.Reason, validation.NormalizedTranscript.Length, retryPromptShown: true);
            target.MarkAsInvalidLearnerTranscript(RealtimeVoiceTranscriptionUnavailableText);
            StatusMessage = LessonTranscriptValidator.GetRetryMessage(SelectedLevel);
            BotStatus = BackendConstants.BotStatusReady;
            IsSending = false;
            SetConversationModeState(isRealtimeSessionStarted ? ConversationModeState.Ready : ConversationModeState.NotStarted, "invalid_user_transcript");
            RefreshAvatarState();
            RefreshAllCommandStates();
            ViewFeedbackCommand.NotifyCanExecuteChanged();
            TryStartRealtimeFallbackTranscription(target, itemId, sessionId, validation.Reason.ToString());
            return;
        }

        // Keep valid Realtime transcripts normalized before they become feedback-eligible learner turns: MarkAsValidLearnerTurn(validation.NormalizedTranscript).
        _ = ApplyValidRealtimeTranscriptAsync(target, itemId, sessionId, validation.NormalizedTranscript, "valid_user_transcript");
    }

    private async Task ApplyValidRealtimeTranscriptAsync(ChatMessageViewModel target, string itemId, string sessionId, string normalizedTranscript, string stateReason)
    {
        realtimeUserTranscriptBuffer.Clear();
        realtimeUserTranscriptBuffer.Append(normalizedTranscript);
        var turnResult = LessonTurnPolicy.EvaluateUserInput(BuildTurnPolicyContext(), isValidEnglishTranscript: true);
        target.MarkAsValidLearnerTurn(normalizedTranscript, turnResult.LearnerTurnCountAfter);
        ViewFeedbackCommand.NotifyCanExecuteChanged();
        LearnerTurnCount = turnResult.LearnerTurnCountAfter;
        Debug.WriteLine($"Realtime placeholder replaced with transcript: SessionId={sessionId}; ItemId={itemId}; UserPlaceholderMessageId={target.Id}; TranscriptLength={target.Text.Length}; LearnerTurnCount={LearnerTurnCount}; Source={stateReason}.");

        if (turnResult.ShouldUseFinalMessage)
        {
            LogFinalLimitReached(turnResult.FinalTurn);
        }

        PrepareRealtimeAssistantPlaceholder();
        SetConversationModeState(ConversationModeState.WaitingForAssistant, stateReason);
        StatusMessage = string.Empty;

        if (string.Equals(stateReason, "fallback_valid_user_transcript", StringComparison.Ordinal))
        {
            try
            {
                await realtimeVoiceEngine.SendUserTextAsync(normalizedTranscript, CancellationToken.None);
                Debug.WriteLine($"Realtime fallback transcript sent as realtime text turn: SessionId={sessionId}; ItemId={itemId}; TranscriptLength={normalizedTranscript.Length}; DuplicateLearnerTurnCreated=False; DuplicateAssistantResponseCreated=False.");
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Realtime fallback transcript text send failed: SessionId={sessionId}; ItemId={itemId}; TranscriptLength={normalizedTranscript.Length}; {exception}");
                StatusMessage = BackendConstants.RealtimeUnavailableMessage;
                BotStatus = BackendConstants.BotStatusReady;
                IsSending = false;
                SetConversationModeState(isRealtimeSessionStarted ? ConversationModeState.Ready : ConversationModeState.Faulted, "fallback_text_send_failed");
                RefreshAvatarState();
                RefreshAllCommandStates();
            }
        }
    }

    private void ApplyRealtimeUserTranscriptFailure(string itemId, string sessionId)
    {
        var target = FindRealtimeUserMessage(itemId);
        if (target is null)
        {
            return;
        }

        realtimeUserPlaceholderItemId = itemId;
        LogInvalidRealtimeTranscriptDecision(sessionId, itemId, LessonTranscriptValidationReason.Empty, 0, retryPromptShown: true);
        target.MarkAsInvalidLearnerTranscript(RealtimeVoiceTranscriptionUnavailableText);
        StatusMessage = LessonTranscriptValidator.GetRetryMessage(SelectedLevel);
        BotStatus = BackendConstants.BotStatusReady;
        IsSending = false;
        SetConversationModeState(isRealtimeSessionStarted ? ConversationModeState.Ready : ConversationModeState.NotStarted, "transcript_failure");
        RefreshAvatarState();
        RefreshAllCommandStates();
        ViewFeedbackCommand.NotifyCanExecuteChanged();
        Debug.WriteLine($"Realtime placeholder marked transcription unavailable: SessionId={sessionId}; ItemId={itemId}; UserPlaceholderMessageId={target.Id}; LearnerTurnCountBefore={LearnerTurnCount}; LearnerTurnCountAfter={LearnerTurnCount}; RetryPromptShown=True; NormalAssistantResponseCreated=False.");
        TryStartRealtimeFallbackTranscription(target, itemId, sessionId, "transcript_failure_or_timeout");
    }

    private void OnRealtimeSessionReady(object? sender, VoiceSessionReadyEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!IsActiveRealtimeSessionEvent(args.SessionId, nameof(OnRealtimeSessionReady)))
            {
                return;
            }

            isRealtimeSessionStarted = true;
            isStartingRealtimeSession = false;
            BackendStatusText = BackendConstants.BackendStatusConnected;
            StatusMessage = "Conversation Mode ready";
            SetConversationModeState(ConversationModeState.Ready, "session_ready_event");
            Debug.WriteLine($"Realtime session.ready handled by view model: SessionId={args.SessionId}; Model={args.Model}; Voice={args.Voice}; ReadyMs={args.ElapsedMilliseconds}; CanStartRealtimeRecording={CanStartRealtimeRecording}; RecordBlockReason={GetRealtimeRecordBlockReason()}.");
            LogRealtimeRecordState("after_backend_session_ready");
            RefreshAllCommandStates();
        });
    }

    private void OnRealtimeErrorReceived(object? sender, VoiceSessionErrorEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _ = RecoverRealtimeErrorAsync(args);
        });
    }

    private async Task RecoverRealtimeErrorAsync(VoiceSessionErrorEventArgs args)
    {
        if (!IsActiveRealtimeSessionEvent(args.SessionId, nameof(OnRealtimeErrorReceived)))
        {
            return;
        }

        Debug.WriteLine($"Realtime session error: SessionId={args.SessionId}; ResponseId={args.ResponseId}; Message={args.Message}; Exception={args.Exception}");
        StatusMessage = BackendConstants.RealtimeUnavailableMessage;
        isStartingRealtimeSession = false;
        await CleanupRealtimeAfterFaultAsync("session_error_recoverable");
        RefreshAllCommandStates();
        LogLessonStateSnapshot("Realtime session error recovery");
    }

    private void OnRealtimeDisconnected(object? sender, VoiceSessionDisconnectedEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _ = RecoverRealtimeDisconnectAsync(args);
        });
    }

    private async Task RecoverRealtimeDisconnectAsync(VoiceSessionDisconnectedEventArgs args)
    {
        if (!IsActiveRealtimeSessionEvent(args.SessionId, nameof(OnRealtimeDisconnected)))
        {
            return;
        }

        Debug.WriteLine($"Realtime disconnected: SessionId={args.SessionId}; ResponseId={args.ResponseId}; Reason={args.Reason}; Expected={args.IsExpected}; SocketState={args.SocketState}; State={CurrentConversationModeState}.");
        if (!args.IsExpected && IsConversationModeEnabled)
        {
            StatusMessage = "Conversation Mode disconnected. Please try again.";
        }

        await CleanupRealtimeAfterFaultAsync(args.Reason);
        if (!IsCompletedAwaitingFinish)
        {
            SetConversationModeState(ConversationModeState.NotStarted, "unexpected_disconnect_recovered");
        }
        LogLessonStateSnapshot("Realtime disconnected recovery");
    }

    private void OnRealtimePlaybackStarted(object? sender, RealtimePlaybackStartedEventArgs args)
    {
        Debug.WriteLine($"Desktop realtime playback started ms: SessionId={args.SessionId}; ResponseId={args.ResponseId}; Reason=assistant_playback_started; PlaybackStartedMs={args.ElapsedMilliseconds}; BufferUnderrunCount={realtimeAudioPlaybackService.UnderrunCount}; StopConversationModeRequested=False.");
    }

    private void OnRealtimePlaybackCompleted(object? sender, RealtimePlaybackCompletedEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!IsActiveRealtimeSessionEvent(args.SessionId, nameof(OnRealtimePlaybackCompleted)))
            {
                return;
            }

            Debug.WriteLine($"Desktop realtime playback completed: SessionId={args.SessionId}; ResponseId={args.ResponseId}; Reason=assistant_playback_completed; PlaybackCompletedMs={args.ElapsedMilliseconds}; StopConversationModeRequested=False.");
            IsBotVoicePlaying = false;
            IsSending = false;
            BotStatus = BackendConstants.BotStatusReady;
            if (isRealtimeSessionStarted && IsConversationModeEnabled && !IsCompletedAwaitingFinish)
            {
                SetConversationModeState(ConversationModeState.Ready, "assistant_playback_completed");
            }

            RefreshAvatarState();
            RefreshAllCommandStates();
        });
    }

    private async Task<bool> HandleContextSelectionMessageAsync(string userMessage)
    {
        var learnerTurnCountBefore = LearnerTurnCount;
        AddSetupContextLearnerMessage(userMessage, ChatMessageSource.Typed);

        var matchedVariant = FindMatchingContextVariant(userMessage);
        if (matchedVariant is not null)
        {
            selectedContextVariant = matchedVariant;
            selectedCustomContextTitle = string.Empty;

            var startMessage = $"{GetSelectedContextConfirmationLine(matchedVariant)}\n\n{GetSelectedContextOpeningLine()}";
            await StartActiveRoleplayAfterContextSelectionAsync(startMessage, learnerTurnCountBefore);
            return true;
        }

        if (IsValidCustomContext(userMessage))
        {
            selectedContextVariant = null;
            selectedCustomContextTitle = userMessage.Trim();

            var openingLine = string.IsNullOrWhiteSpace(lessonScenario.ConversationFlow.DefaultOpeningExample)
                ? "Hi! Nice to meet you. What's your name?"
                : GetSelectedContextOpeningLine();
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
            await PlayRealtimePreStartOpeningAsync(CancellationToken.None);
            StatusMessage = string.Empty;
            LogLessonStateSnapshot("Realtime start success");
        }
        catch (Exception exception)
        {
            await CleanupRealtimeAfterFaultAsync("guided_context_realtime_start_failed");
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

    private string ResolveScenarioPlaceholders(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("{tutorName}", TutorAvatarDisplayName, StringComparison.OrdinalIgnoreCase).Trim();
    }

    private string GetSelectedContextOpeningLine()
    {
        return ResolveScenarioPlaceholders(selectedContextVariant?.OpeningLine ?? lessonScenario.ConversationFlow.DefaultOpeningExample);
    }

    private string GetSelectedContextConfirmationLine(ContextVariant variant)
    {
        if (!string.IsNullOrWhiteSpace(variant.ContextConfirmationLine))
        {
            return ResolveScenarioPlaceholders(variant.ContextConfirmationLine);
        }

        return $"Great! Let's imagine {BuildContextConfirmationText(variant)}.";
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

        var requestedMessage = message;
        var requestedMessageId = requestedMessage.MessageId;
        var requestedTextLength = requestedMessage.Text.Trim().Length;
        SelectedFeedbackMessageId = requestedMessageId;
        IsFeedbackTranslationVisible = false;

        Debug.WriteLine($"Feedback view requested: MessageId={requestedMessageId}; Source={requestedMessage.Source}; SourceMessageKind={GetFeedbackSourceMessageKind(requestedMessage)}; TextLength={requestedTextLength}; CurrentSelectedFeedbackMessageId={SelectedFeedbackMessageId}.");

        if (feedbackByMessageId.TryGetValue(requestedMessageId, out var cachedFeedback))
        {
            requestedMessage.SetFeedback(cachedFeedback);
            DisplayFeedbackForRequestedMessage(requestedMessageId, cachedFeedback, requestedTextLength, fromCache: true);
            return;
        }

        var feedback = requestedMessage.Feedback;
        if (feedback is not null)
        {
            feedbackByMessageId[requestedMessageId] = feedback;
            DisplayFeedbackForRequestedMessage(requestedMessageId, feedback, requestedTextLength, fromCache: true);
            return;
        }

        feedback = await GenerateFeedbackForMessageAsync(requestedMessage);
        if (feedback is null)
        {
            return;
        }

        requestedMessage.SetFeedback(feedback);
        feedbackByMessageId[requestedMessageId] = feedback;

        if (SelectedFeedbackMessageId != requestedMessageId)
        {
            Debug.WriteLine($"Feedback result ignored as stale: RequestedMessageId={requestedMessageId}; CurrentSelectedFeedbackMessageId={SelectedFeedbackMessageId}; TextLength={requestedTextLength}; Displayed=False.");
            return;
        }

        DisplayFeedbackForRequestedMessage(requestedMessageId, feedback, requestedTextLength, fromCache: false);
    }

    private void DisplayFeedbackForRequestedMessage(int requestedMessageId, Feedback feedback, int requestedTextLength, bool fromCache)
    {
        if (SelectedFeedbackMessageId != requestedMessageId)
        {
            Debug.WriteLine($"Feedback display skipped as stale: RequestedMessageId={requestedMessageId}; CurrentSelectedFeedbackMessageId={SelectedFeedbackMessageId}; TextLength={requestedTextLength}; FromCache={fromCache}; Displayed=False.");
            return;
        }

        SelectedFeedback = feedback;
        IsFeedbackTranslationVisible = false;
        StatusMessage = feedback.ShortText;
        Debug.WriteLine($"Feedback displayed: MessageId={requestedMessageId}; CurrentSelectedFeedbackMessageId={SelectedFeedbackMessageId}; TextLength={requestedTextLength}; FromCache={fromCache}; Displayed=True.");
    }

    private bool CanViewFeedback(ChatMessageViewModel? message)
    {
        return CanReviewExistingMessages
            && message is not null
            && !message.IsFromBot
            && message.IsFeedbackEligible
            && !message.IsTechnicalMessage;
    }

    [RelayCommand]
    private async Task ToggleFeedbackTranslationAsync()
    {
        var feedback = SelectedFeedback;
        if (feedback is null)
        {
            IsFeedbackTranslationVisible = false;
            return;
        }

        if (IsFeedbackTranslationVisible)
        {
            IsFeedbackTranslationVisible = false;
            return;
        }

        if (feedback.HasTranslations)
        {
            IsFeedbackTranslationVisible = true;
            StatusMessage = feedback.ShortText;
            return;
        }

        StatusMessage = localizedText.TranslationLoadingText;
        try
        {
            await TranslateSelectedFeedbackAsync(feedback);

            if (!ReferenceEquals(SelectedFeedback, feedback))
            {
                if (string.Equals(StatusMessage, localizedText.TranslationLoadingText, StringComparison.Ordinal))
                {
                    StatusMessage = SelectedFeedback?.ShortText ?? string.Empty;
                }

                return;
            }

            IsFeedbackTranslationVisible = true;
            StatusMessage = feedback.ShortText;
            OnPropertyChanged(nameof(SelectedFeedback));
        }
        catch (OperationCanceledException)
        {
            if (ReferenceEquals(SelectedFeedback, feedback))
            {
                StatusMessage = string.Empty;
            }
        }
        catch
        {
            if (ReferenceEquals(SelectedFeedback, feedback))
            {
                StatusMessage = localizedText.TranslationFailedText;
            }
        }
        finally
        {
            RefreshAllCommandStates();
        }
    }

    [RelayCommand]
    private void CloseFeedback()
    {
        SelectedFeedback = null;
        SelectedFeedbackMessageId = 0;
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
                SelectedContextOpeningLine = GetSelectedContextOpeningLine(),
                SelectedContextConfirmationLine = selectedContextVariant is null ? string.Empty : GetSelectedContextConfirmationLine(selectedContextVariant),
                SelectedContextOpeningIntent = selectedContextVariant?.OpeningIntent ?? string.Empty,
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
                ConversationWrapUpIntent = lessonScenario.ConversationFlow.WrapUpIntent,
                ConversationFinalMessageIntent = lessonScenario.ConversationFlow.FinalMessageIntent,
                RoleplayBeats = lessonScenario.RoleplayBeats.Select(beat => new ScenarioRoleplayBeat { Id = beat.Id, Intent = beat.Intent }).ToArray(),
                ReciprocalQuestionIfUserAsksTutorName = lessonScenario.ReciprocalQuestionHandling.IfUserAsksTutorName,
                ReciprocalQuestionIfUserAsksSimplePersonalQuestion = lessonScenario.ReciprocalQuestionHandling.IfUserAsksSimplePersonalQuestion,
                ReciprocalQuestionMustNotIgnoreUserQuestion = lessonScenario.ReciprocalQuestionHandling.MustNotIgnoreUserQuestion,
                ReciprocalQuestionMustNotRefuseScenarioCompatibleQuestions = lessonScenario.ReciprocalQuestionHandling.MustNotRefuseScenarioCompatibleQuestions,
                ExpectedScenarioProgression = lessonScenario.ExpectedScenarioProgression,
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
        var operationStopwatch = StartUiOperationDiagnostics(
            "finish_lesson",
            UiOperationWarningThresholdMilliseconds,
            $"SessionId={realtimeSessionId}; CurrentLessonPhase={CurrentLessonPhase}; ConversationModeState={CurrentConversationModeState}; IsConversationModeEnabled={IsConversationModeEnabled}");

        try
        {
            CancelCurrentBotVoice(BotVoiceCancellationReasons.BackOrFinishCancel);
            await CleanupCurrentSessionBotVoiceFilesAsync();
            await StopRealtimeConversationAsync("finish_lesson");
            CompleteLesson();
        }
        finally
        {
            CompleteUiOperationDiagnostics(
                operationStopwatch,
                "finish_lesson",
                UiOperationWarningThresholdMilliseconds,
                $"CurrentLessonPhase={CurrentLessonPhase}; ConversationModeState={CurrentConversationModeState}; HasFinishedLesson={hasFinishedLesson}");
            RefreshAllCommandStates();
        }
    }

    private void MarkLessonCompleteAwaitingFinish()
    {
        CurrentLessonPhase = LessonPhase.Completed;
        IsLessonCompleteAwaitingFinish = true;
        IsConversationModeEnabled = false;
        SetConversationModeState(ConversationModeState.CompletedAwaitingFinish, "lesson_complete_awaiting_finish");
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
        OnPropertyChanged(nameof(CanTypeText));
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
            $"ConversationModeState={CurrentConversationModeState}; " +
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
            $"CanTypeText={CanTypeText}; " +
            $"CanSend={CanSendMessage()}; " +
            $"TextSendBlockReason={GetTextSendBlockReason()}; " +
            $"CanRecord={CanToggleVoiceRecording()}; " +
            $"CanHint={CanRequestHint()}; " +
            $"CanBack={CanGoBack()}; " +
            $"CanFinish={CanFinishLesson()}; " +
            $"CanConversationMode={CanToggleConversationMode()}.");
    }

    private void RefreshAllCommandStates()
    {
        LogRealtimeRecordState("before_refresh_all_command_states");
        SendMessageCommand.NotifyCanExecuteChanged();
        ToggleVoiceRecordingCommand.NotifyCanExecuteChanged();
        HintCommand.NotifyCanExecuteChanged();
        ToggleConversationModeCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
        FinishLessonCommand.NotifyCanExecuteChanged();
        PlayBotVoiceCommand.NotifyCanExecuteChanged();
        ViewFeedbackCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsLessonInputEnabled));
        OnPropertyChanged(nameof(CanTypeText));
        OnPropertyChanged(nameof(IsConversationRecordButtonEnabled));
        LogTextInputState("after_refresh_all_command_states", CanSendMessage());
        LogRealtimeRecordState("after_refresh_all_command_states");
    }

    private void LogDeveloperLessonUsageSummary(string reason)
    {
        var typedTurns = Messages.Count(message => !message.IsFromBot && string.Equals(message.Source, ChatMessageSource.Typed, StringComparison.OrdinalIgnoreCase) && !message.IsTechnicalMessage);
        var chainedVoiceTurns = Messages.Count(message => !message.IsFromBot && string.Equals(message.Source, ChatMessageSource.LessonChatVoice, StringComparison.OrdinalIgnoreCase) && !message.IsTechnicalMessage);
        var realtimeTurns = Messages.Count(message => !message.IsFromBot && string.Equals(message.Source, ChatMessageSource.RealtimeVoice, StringComparison.OrdinalIgnoreCase) && !message.IsTechnicalMessage);
        var invalidTranscriptRetries = Messages.Count(message => !message.IsFromBot && !message.CountsAsValidLessonTurn && !message.IsTechnicalMessage && (string.Equals(message.Source, ChatMessageSource.LessonChatVoice, StringComparison.OrdinalIgnoreCase) || string.Equals(message.Source, ChatMessageSource.RealtimeVoice, StringComparison.OrdinalIgnoreCase)));
        var assistantTurns = Messages.Count(message => message.IsFromBot && !message.IsTechnicalMessage);
        var userCharacters = Messages.Where(message => !message.IsFromBot && !message.IsTechnicalMessage).Sum(message => message.Text?.Length ?? 0);
        var assistantCharacters = Messages.Where(message => message.IsFromBot && !message.IsTechnicalMessage).Sum(message => message.Text?.Length ?? 0);

        Debug.WriteLine(
            $"Developer usage summary: Operation=lesson_completion; Reason={reason}; " +
            $"LessonId={lessonScenario.Id}; Topic={SelectedTopic.Title}; Subtopic={SelectedSubtopic.Title}; Level={SelectedLevel}; LessonType={lessonScenario.Metadata.LessonType}; SelectedContext={GetSelectedContextTitle()}; TutorProfileId={tutorProfile.Id}; " +
            $"UsedTypedChat={typedTurns > 0}; UsedChainedVoice={chainedVoiceTurns > 0}; UsedRealtime={realtimeTurns > 0}; UsedManualPlayVoice={usedManualPlayVoice}; UsedAutoPlayVoice={usedAutoPlayVoice}; " +
            $"TypedUserTurns={typedTurns}; ChainedVoiceUserTurns={chainedVoiceTurns}; RealtimeUserTurns={realtimeTurns}; ValidLearnerTurns={LearnerTurnCount}; InvalidTranscriptRetries={invalidTranscriptRetries}; AssistantTurns={assistantTurns}; " +
            $"TotalUserTranscriptCharacters={userCharacters}; TotalAssistantTranscriptCharacters={assistantCharacters}; " +
            $"LessonChatModel={BackendConstants.LessonChatModelName}; FeedbackModel={BackendConstants.FeedbackModelName}; SummaryModel={BackendConstants.SummaryModelName}; TranscriptionModel={BackendConstants.TranscriptionModelName}; TtsModel={BackendConstants.TtsModelName}; RealtimeModel={BackendConstants.RealtimeModelName}; " +
            "CostEstimateApproximate=True; MissingCostFields=backend_raw_usage_and_pricing_constants.");
    }

    private void CompleteLesson()
    {
        if (hasFinishedLesson)
        {
            return;
        }

        LogDeveloperLessonUsageSummary("finish_lesson");
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
        var operationStopwatch = StartUiOperationDiagnostics(
            "lesson_back_navigation",
            UiOperationWarningThresholdMilliseconds,
            $"SessionId={realtimeSessionId}; CurrentLessonPhase={CurrentLessonPhase}; ConversationModeState={CurrentConversationModeState}; IsConversationModeEnabled={IsConversationModeEnabled}");

        try
        {
            CancelCurrentBotVoice(BotVoiceCancellationReasons.BackOrFinishCancel);
            await CleanupCurrentSessionBotVoiceFilesAsync();
            await StopRealtimeConversationAsync("back");
            navigateBack();
        }
        finally
        {
            CompleteUiOperationDiagnostics(
                operationStopwatch,
                "lesson_back_navigation",
                UiOperationWarningThresholdMilliseconds,
                $"CurrentLessonPhase={CurrentLessonPhase}; ConversationModeState={CurrentConversationModeState}; IsConversationModeEnabled={IsConversationModeEnabled}");
            RefreshAllCommandStates();
        }
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

    private ChatMessageViewModel AddSetupContextLearnerMessage(string text, string source)
    {
        var transcriptValidation = LessonTranscriptValidator.Validate(text);
        var feedbackEligible = transcriptValidation.IsValid && !string.IsNullOrWhiteSpace(text);
        var message = AddMessage(
            AppConstants.UserSenderName,
            text,
            isFromBot: false,
            feedback: null,
            source: source,
            lessonTurnNumber: 0,
            countsAsValidLessonTurn: false,
            isTechnicalMessage: false,
            isFeedbackEligible: feedbackEligible);
        Debug.WriteLine($"Setup context learner message added: MessageId={message.Id}; TextLength={text.Trim().Length}; FeedbackEligible={feedbackEligible}; CountsAsValidLessonTurn={message.CountsAsValidLessonTurn}; ValidationReason={transcriptValidation.Reason}; LearnerTurnCount={LearnerTurnCount}.");
        ViewFeedbackCommand.NotifyCanExecuteChanged();
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
            Debug.WriteLine($"Feedback request starting: MessageId={message.MessageId}; Source={message.Source}; SourceMessageKind={GetFeedbackSourceMessageKind(message)}; TextLength={message.Text.Trim().Length}; CurrentSelectedFeedbackMessageId={SelectedFeedbackMessageId}.");
            var response = await lessonChatBackendService.SendLessonFeedbackRequestAsync(BuildLessonFeedbackRequest(message));
            return MapFeedback(response);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Feedback request failed: MessageId={message.Id}; Source={message.Source}; TextLength={message.Text.Trim().Length}; {exception}");
            if (SelectedFeedbackMessageId == message.MessageId)
            {
                StatusMessage = localizedText.BackendUnavailableMessage;
            }

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
            SourceMessageId = message.MessageId,
            SourceMessageKind = GetFeedbackSourceMessageKind(message),
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

    private static string GetFeedbackSourceMessageKind(ChatMessageViewModel message)
    {
        if (!message.CountsAsValidLessonTurn && string.Equals(message.LessonPhase, LessonPhase.SetupContextSelection.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return "ContextSelection";
        }

        if (string.Equals(message.Source, ChatMessageSource.RealtimeVoice, StringComparison.OrdinalIgnoreCase))
        {
            return "RealtimeTranscript";
        }

        if (string.Equals(message.Source, ChatMessageSource.LessonChatVoice, StringComparison.OrdinalIgnoreCase))
        {
            return "NormalVoiceTranscript";
        }

        return "ActiveRoleplay";
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
        public const string RealtimeStartupCancel = "RealtimeStartupCancel";
        public const string AppDisposalCancel = "AppDisposalCancel";
    }
}
