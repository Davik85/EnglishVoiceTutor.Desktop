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

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class LessonChatViewModel : ViewModelBase
{
    private readonly Action navigateBack;
    private readonly Action<Feedback?> finishLesson;
    private readonly string nativeLanguageName;
    private readonly string tutorAvatarId;
    private readonly LessonChatBackendService lessonChatBackendService;
    private readonly AudioRecordingService audioRecordingService;
    private readonly AudioPlaybackService audioPlaybackService;
    private readonly BotVoiceTempFileCleanupService botVoiceTempFileCleanupService;
    private readonly string audioInputDeviceId;
    private readonly AppLocalizedText localizedText;
    private readonly LessonScenario lessonScenario;
    private readonly LevelProfile activeLevelProfile;
    private int messageCounter;
    private Feedback? latestFeedback;
    private string lastBotMessage = AppConstants.MockBotFirstMessage;
    private ContextVariant? selectedContextVariant;
    private string selectedCustomContextTitle = string.Empty;
    private bool isTranscribingAudio;
    private bool hasFinishedLesson;
    private readonly SemaphoreSlim botVoiceSemaphore = new(1, 1);
    private readonly Dictionary<string, string> botVoiceSegmentAudioFilePaths = [];
    private readonly HashSet<string> currentSessionBotVoiceFilePaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly object botVoiceCancellationLock = new();
    private CancellationTokenSource? currentBotVoiceCancellationTokenSource;

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
    private bool isBotVoicePlaying;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLessonLimitReached))]
    [NotifyPropertyChangedFor(nameof(IsLessonWrappingUp))]
    [NotifyPropertyChangedFor(nameof(IsLessonInputEnabled))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleVoiceRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(HintCommand))]
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
    private bool isLessonCompleteAwaitingFinish;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvatarStateDisplayText))]
    [NotifyPropertyChangedFor(nameof(AvatarAnimationAssetPath))]
    [NotifyPropertyChangedFor(nameof(AvatarAnimationAssetUri))]
    private AvatarState currentAvatarState = AvatarState.Idle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConversationModeButtonText))]
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
    private LessonPhase currentLessonPhase = LessonPhase.SetupContextSelection;

    public bool HasSelectedFeedback => SelectedFeedback is not null;

    public bool HasCurrentHint => !string.IsNullOrWhiteSpace(CurrentHintText);

    public bool IsLessonLimitReached => LearnerTurnCount >= GetFinalTurn();

    public bool IsLessonWrappingUp => LearnerTurnCount >= GetSoftWrapUpTurn();

    public bool IsLessonInputEnabled => CurrentLessonPhase != LessonPhase.Completed && !IsLessonCompleteAwaitingFinish && !IsLessonLimitReached && !hasFinishedLesson;

    public bool IsLessonOptionsEnabled => CurrentLessonPhase != LessonPhase.Completed && !IsLessonCompleteAwaitingFinish && !IsLessonLimitReached && !hasFinishedLesson;

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

    private bool ShouldAutoSendTranscribedVoice => IsLessonInputEnabled && (IsConversationModeEnabled || IsVoiceAutoSendEnabled);

    private bool ShouldAutoPlayBotVoice => !IsLessonCompleteAwaitingFinish && (IsConversationModeEnabled || IsBotVoiceAutoPlayEnabled);

    public LessonChatViewModel(
        AppLocalizedText localizedText,
        string selectedLevel,
        Topic selectedTopic,
        Subtopic selectedSubtopic,
        string nativeLanguageName,
        string userDisplayName,
        string learningGoal,
        TutorAvatarOption tutorAvatar,
        LessonScenario? lessonScenario,
        LessonChatBackendService lessonChatBackendService,
        AudioRecordingService audioRecordingService,
        AudioPlaybackService audioPlaybackService,
        BotVoiceTempFileCleanupService botVoiceTempFileCleanupService,
        string audioInputDeviceId,
        Action navigateBack,
        Action<Feedback?> finishLesson)
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
        this.lessonScenario = lessonScenario ?? new LessonScenario();
        activeLevelProfile = ResolveActiveLevelProfile(this.lessonScenario, selectedLevel);
        this.lessonChatBackendService = lessonChatBackendService;
        this.audioRecordingService = audioRecordingService;
        this.audioPlaybackService = audioPlaybackService;
        this.botVoiceTempFileCleanupService = botVoiceTempFileCleanupService;
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
        return IsLessonInputEnabled && !IsSending && !IsRecording;
    }

    private bool CanToggleVoiceRecording()
    {
        if (IsRecording)
        {
            return true;
        }

        return IsLessonInputEnabled && !IsSending && !IsBotVoicePlaying && !isTranscribingAudio;
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
        return IsLessonCompleteAwaitingFinish && !IsRecording && !IsSending && !hasFinishedLesson;
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

    private async Task StopVoiceRecordingAsync()
    {
        if (isTranscribingAudio)
        {
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

            IsSending = true;
            isTranscribingAudio = true;
            RefreshAvatarState();
            StatusMessage = localizedText.TranscribingAudioMessage;

            var transcriptionText = await lessonChatBackendService.SendAudioForTranscriptionAsync(savedFilePath);
            BackendStatusText = BackendConstants.BackendStatusConnected;
            var trimmedTranscriptionText = transcriptionText.Trim();

            if (string.IsNullOrWhiteSpace(trimmedTranscriptionText))
            {
                StatusMessage = localizedText.EmptyTranscriptionMessage;
                return;
            }

            if (!IsUsableEnglishPracticeTranscription(trimmedTranscriptionText))
            {
                StatusMessage = AudioConstants.UnclearEnglishTranscriptionMessage;
                return;
            }

            if (!IsLessonInputEnabled)
            {
                StatusMessage = AppConstants.LessonCompleteAwaitingFinishMessage;
                return;
            }

            if (!ShouldAutoSendTranscribedVoice)
            {
                UserInput = trimmedTranscriptionText;
                StatusMessage = localizedText.TranscriptionCompletedMessage;
                return;
            }

            isTranscribingAudio = false;
            var wasSent = await SendLessonMessageAsync(trimmedTranscriptionText);

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
            isTranscribingAudio = false;
            IsRecording = false;
            IsSending = false;
            RefreshAvatarState();
            audioRecordingService.SafeDeleteRecording(savedFilePath);
        }
    }


    private static bool IsUsableEnglishPracticeTranscription(string transcriptionText)
    {
        if (string.IsNullOrWhiteSpace(transcriptionText))
        {
            return false;
        }

        if (transcriptionText.Length == 1 && char.IsPunctuation(transcriptionText[0]))
        {
            return false;
        }

        if (ContainsCyrillicLetter(transcriptionText))
        {
            return false;
        }

        return ContainsLatinLetter(transcriptionText);
    }

    private static bool ContainsCyrillicLetter(string text)
    {
        foreach (var character in text)
        {
            if (character is >= '\u0400' and <= '\u04FF')
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsLatinLetter(string text)
    {
        foreach (var character in text)
        {
            if (character is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
            {
                return true;
            }
        }

        return false;
    }

    [RelayCommand(CanExecute = nameof(CanToggleConversationMode))]
    private void ToggleConversationMode()
    {
        IsConversationModeEnabled = !IsConversationModeEnabled;
    }

    private bool CanToggleConversationMode()
    {
        return IsLessonOptionsEnabled && !IsSending && !IsRecording;
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

        CancelCurrentBotVoice($"new {(isAutoPlay ? "auto-play" : "manual play")} request for message {message.Id}");

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

            var allSegments = SplitBotVoiceTextIntoSegments(message.Text, isAutoPlay);
            var segmentsToSpeak = SelectBotVoiceSegments(allSegments, isAutoPlay);

            if (segmentsToSpeak.Count == 0)
            {
                Debug.WriteLine("Bot voice skipped because no speakable segments were found.");
                StatusMessage = string.Empty;
                return;
            }

            var inputLength = message.Text.Trim().Length;
            Debug.WriteLine($"Bot voice request start message id {message.Id}: Path={selectedBotVoicePath}; MessageId={message.Id}; InputLength={inputLength}; AutoPlay={isAutoPlay}; SegmentCount={segmentsToSpeak.Count}; TotalSegmentCount={allSegments.Count}; FirstSegmentLength={segmentsToSpeak[0].Length}; FirstSegmentRequestStartedMs={totalStopwatch.ElapsedMilliseconds}.");

            await PlaySegmentedHighQualityBotVoiceAsync(
                message,
                segmentsToSpeak,
                playbackCancellationTokenSource.Token,
                playbackStartedMs => playbackStarted = true,
                totalStopwatch);

            Debug.WriteLine($"Bot voice playback completed ms for message {message.Id}: Path={selectedBotVoicePath}; TotalElapsedMilliseconds={totalStopwatch.ElapsedMilliseconds}; SegmentCount={segmentsToSpeak.Count}.");
            BackendStatusText = BackendConstants.BackendStatusConnected;
        }
        catch (OperationCanceledException exception)
        {
            if (!playbackStarted && !cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine($"Bot voice canceled before first playback for message {message.Id}: Path={selectedBotVoicePath}; CancellationReason=first-segment-timeout-or-newer-message; TotalMs={totalStopwatch.ElapsedMilliseconds}; {exception}");
                StatusMessage = "Voice took too long. Continuing without audio.";
            }
            else
            {
                Debug.WriteLine($"Bot voice {(isAutoPlay ? "auto-play" : "manual play")} canceled for message {message.Id}: Path={selectedBotVoicePath}; CancellationReason=request-canceled; TotalMs={totalStopwatch.ElapsedMilliseconds}; {exception}");
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Bot voice {(isAutoPlay ? "auto-play" : "manual play")} failed for message {message.Id}: Path={selectedBotVoicePath}; TotalMs={totalStopwatch.ElapsedMilliseconds}; {exception}");
            StatusMessage = localizedText.BotVoiceFailedMessage;
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
        Stopwatch totalStopwatch)
    {
        var firstSegmentFilePath = await GetOrCreateBotVoiceSegmentAudioFileAsync(
            message,
            segments[0],
            segmentIndex: 0,
            timeout: TimeSpan.FromSeconds(AudioConstants.BotVoiceFirstSegmentTimeoutSeconds),
            totalStopwatch,
            cancellationToken);
        var firstSegmentReadyMs = totalStopwatch.ElapsedMilliseconds;
        Debug.WriteLine($"Bot voice first segment ready: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex=0; FirstSegmentLength={segments[0].Length}; FirstSegmentReadyMs={firstSegmentReadyMs}; AudioFile={Path.GetFileName(firstSegmentFilePath)}.");

        Task<string>? nextSegmentTask = segments.Count > 1
            ? GetOrCreateBotVoiceSegmentAudioFileAsync(message, segments[1], 1, TimeSpan.FromSeconds(AudioConstants.BotVoiceSegmentTimeoutSeconds), totalStopwatch, cancellationToken)
            : null;
        var currentFilePath = firstSegmentFilePath;

        for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
        {
            var playbackSegmentIndex = segmentIndex;
            var playbackStartedForSegment = false;
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
                        Debug.WriteLine($"Bot voice first playback started: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex=0; FirstSegmentReadyMs={firstSegmentReadyMs}; FirstPlaybackStartedMs={playbackStartedMs}; AcceptanceUnder5000={playbackStartedMs <= 5000}.");
                    }

                    Debug.WriteLine($"Bot voice segment playback started: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={playbackSegmentIndex}; SegmentLength={segments[playbackSegmentIndex].Length}; PlaybackStartedMs={playbackStartedMs}; AudioFile={Path.GetFileName(currentFilePath)}.");
                });

            Debug.WriteLine($"Bot voice segment playback ended: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex}; PlaybackEndMs={totalStopwatch.ElapsedMilliseconds}; PlaybackStarted={playbackStartedForSegment}.");

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
                Debug.WriteLine($"Bot voice later segment canceled: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex + 1}; CancellationReason=later-segment-timeout-or-newer-message; TotalMs={totalStopwatch.ElapsedMilliseconds}.");
                break;
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Bot voice later segment failed: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex + 1}; TotalMs={totalStopwatch.ElapsedMilliseconds}; {exception}");
                break;
            }

            var nextIndex = segmentIndex + 2;
            nextSegmentTask = nextIndex < segments.Count
                ? GetOrCreateBotVoiceSegmentAudioFileAsync(message, segments[nextIndex], nextIndex, TimeSpan.FromSeconds(AudioConstants.BotVoiceSegmentTimeoutSeconds), totalStopwatch, cancellationToken)
                : null;
        }
    }

    private async Task<string> GetOrCreateBotVoiceSegmentAudioFileAsync(
        ChatMessageViewModel message,
        string segmentText,
        int segmentIndex,
        TimeSpan timeout,
        Stopwatch totalStopwatch,
        CancellationToken cancellationToken)
    {
        var cacheKey = CreateBotVoiceSegmentCacheKey(message.Id, segmentIndex);
        if (botVoiceSegmentAudioFilePaths.TryGetValue(cacheKey, out var cachedFilePath) && File.Exists(cachedFilePath))
        {
            TrackCurrentSessionBotVoiceFile(cachedFilePath);
            Debug.WriteLine($"Bot voice segment cache hit: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex}; ReadyMs={totalStopwatch.ElapsedMilliseconds}; AudioFile={Path.GetFileName(cachedFilePath)}.");
            return cachedFilePath;
        }

        using var segmentTimeoutCancellationTokenSource = new CancellationTokenSource(timeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            segmentTimeoutCancellationTokenSource.Token);
        var backendStopwatch = Stopwatch.StartNew();
        var inputLength = segmentText.Trim().Length;

        Debug.WriteLine($"Bot voice segment prepared: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex}; SegmentLength={inputLength}; SegmentTextPreview={CreateBotVoiceSegmentPreview(segmentText)};");
        Debug.WriteLine($"Bot voice segment request starting: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex}; InputLength={inputLength}; RequestStartedMs={totalStopwatch.ElapsedMilliseconds}; TimeoutMs={timeout.TotalMilliseconds}.");
        var speechResponse = await lessonChatBackendService.CreateBotSpeechAsync(segmentText, linkedCancellationTokenSource.Token);
        Debug.WriteLine($"Bot voice segment backend response received: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex}; InputLength={inputLength}; SegmentReadyMs={totalStopwatch.ElapsedMilliseconds}; BackendElapsedMs={backendStopwatch.ElapsedMilliseconds}; AudioBytes={speechResponse.AudioBytes.Length}; ContentType={speechResponse.ContentType}.");

        var saveStopwatch = Stopwatch.StartNew();
        var audioFilePath = await audioPlaybackService.SaveBotVoiceAudioAsync(
            speechResponse.AudioBytes,
            speechResponse.FileExtension,
            linkedCancellationTokenSource.Token);
        Debug.WriteLine($"Bot voice segment file ready: Path={AudioConstants.BotVoiceDefaultPathName}; MessageId={message.Id}; SegmentIndex={segmentIndex}; SaveElapsedMs={saveStopwatch.ElapsedMilliseconds}; ReadyMs={totalStopwatch.ElapsedMilliseconds}; FileExtension={Path.GetExtension(audioFilePath)}.");
        botVoiceSegmentAudioFilePaths[cacheKey] = audioFilePath;
        TrackCurrentSessionBotVoiceFile(audioFilePath);
        return audioFilePath;
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

        var selectedSegments = new List<string>();
        var selectedCharacters = 0;

        foreach (var segment in segments)
        {
            if (selectedSegments.Count >= AudioConstants.BotVoiceAutoPlayMaxSegments)
            {
                break;
            }

            if (selectedSegments.Count > 0
                && selectedCharacters + segment.Length > AudioConstants.BotVoiceMaxSpokenCharactersAutoPlay)
            {
                break;
            }

            selectedSegments.Add(segment);
            selectedCharacters += segment.Length;
        }

        return selectedSegments.Count > 0 ? selectedSegments : segments.Take(1).ToList();
    }

    private static IReadOnlyList<string> SplitBotVoiceTextIntoSegments(string text, bool isAutoPlay)
    {
        _ = isAutoPlay;

        var rawSegments = SplitRawBotVoiceSegments(text);
        return NormalizeBotVoiceSegments(rawSegments);
    }

    private static IReadOnlyList<string> SplitRawBotVoiceSegments(string text)
    {
        var normalizedText = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var sentenceSegments = new List<string>();
        var current = new StringBuilder();

        foreach (var character in normalizedText)
        {
            current.Append(character);

            if (character is '.' or '?' or '!' or ';' or ':' or '\n')
            {
                AddTrimmedSegment(sentenceSegments, current.ToString());
                current.Clear();
            }
        }

        AddTrimmedSegment(sentenceSegments, current.ToString());

        var splitSegments = new List<string>();
        foreach (var segment in sentenceSegments)
        {
            SplitLongBotVoiceSegment(segment, splitSegments);
        }

        return splitSegments;
    }

    private static IReadOnlyList<string> NormalizeBotVoiceSegments(IEnumerable<string> rawSegments)
    {
        var speakableSegments = new List<string>();

        foreach (var rawSegment in rawSegments)
        {
            var normalizedSegment = NormalizeBotVoiceSegmentText(rawSegment);
            if (string.IsNullOrWhiteSpace(normalizedSegment))
            {
                LogSkippedBotVoiceSegment("empty-after-normalization", rawSegment);
                continue;
            }

            if (!ContainsLetterOrDigit(normalizedSegment))
            {
                LogSkippedBotVoiceSegment("punctuation-only", rawSegment);
                continue;
            }

            speakableSegments.Add(normalizedSegment);
        }

        if (speakableSegments.Count == 0)
        {
            return [];
        }

        var mergedSegments = MergeShortBotVoiceSegments(speakableSegments);
        var finalSegments = new List<string>();
        var allowMeaningfulShortWholeReply = mergedSegments.Count == 1;

        foreach (var segment in mergedSegments)
        {
            if (IsSpeakableSegment(segment, allowMeaningfulShortWholeReply))
            {
                SplitLongBotVoiceSegment(segment, finalSegments);
            }
            else
            {
                LogSkippedBotVoiceSegment("too-short", segment);
            }
        }

        return finalSegments;
    }

    private static List<string> MergeShortBotVoiceSegments(IReadOnlyList<string> segments)
    {
        var mergedSegments = new List<string>();

        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            if (!ShouldMergeShortSegment(segment))
            {
                mergedSegments.Add(segment);
                continue;
            }

            if (index + 1 < segments.Count)
            {
                var mergedWithNext = JoinBotVoiceSegments(segment, segments[index + 1]);
                if (mergedWithNext.Length <= AudioConstants.BotVoiceMaxSegmentCharacters)
                {
                    mergedSegments.Add(mergedWithNext);
                    index++;
                    continue;
                }
            }

            if (mergedSegments.Count > 0)
            {
                var previousIndex = mergedSegments.Count - 1;
                var mergedWithPrevious = JoinBotVoiceSegments(mergedSegments[previousIndex], segment);
                if (mergedWithPrevious.Length <= AudioConstants.BotVoiceMaxSegmentCharacters)
                {
                    mergedSegments[previousIndex] = mergedWithPrevious;
                    continue;
                }
            }

            mergedSegments.Add(segment);
        }

        return mergedSegments;
    }

    private static bool IsSpeakableSegment(string segment)
    {
        return IsSpeakableSegment(segment, allowMeaningfulShortWholeReply: false);
    }

    private static bool IsSpeakableSegment(string segment, bool allowMeaningfulShortWholeReply)
    {
        var normalizedSegment = NormalizeBotVoiceSegmentText(segment);
        if (string.IsNullOrWhiteSpace(normalizedSegment) || !ContainsLetterOrDigit(normalizedSegment))
        {
            return false;
        }

        if (normalizedSegment.Length >= AudioConstants.BotVoiceMinimumSegmentCharacters)
        {
            return true;
        }

        return allowMeaningfulShortWholeReply && IsMeaningfulShortWholeReply(normalizedSegment);
    }

    private static bool ShouldMergeShortSegment(string segment)
    {
        return NormalizeBotVoiceSegmentText(segment).Length < AudioConstants.BotVoiceShortSegmentMergeThreshold;
    }

    private static bool IsMeaningfulShortWholeReply(string segment)
    {
        var normalizedSegment = NormalizeBotVoiceSegmentText(segment);
        var lowerSegment = normalizedSegment.TrimEnd('.', '?', '!', ',').ToLowerInvariant();
        return lowerSegment is "no" or "yes" or "hi";
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
        return preview.Length <= 40 ? preview : $"{preview[..40]}…";
    }

    private static void LogSkippedBotVoiceSegment(string reason, string segment)
    {
        Debug.WriteLine($"Bot voice segment skipped: Reason={reason}; SegmentTextPreview={CreateBotVoiceSegmentPreview(segment)};");
    }

    private static void SplitLongBotVoiceSegment(string segment, List<string> output)
    {
        var remaining = NormalizeBotVoiceSegmentText(segment);

        while (remaining.Length > AudioConstants.BotVoiceMaxSegmentCharacters)
        {
            var splitIndex = remaining.LastIndexOf(',', AudioConstants.BotVoiceMaxSegmentCharacters - 1, AudioConstants.BotVoiceMaxSegmentCharacters);
            if (splitIndex < AudioConstants.BotVoiceMaxSegmentCharacters / 2)
            {
                splitIndex = remaining.LastIndexOf(' ', AudioConstants.BotVoiceMaxSegmentCharacters - 1, AudioConstants.BotVoiceMaxSegmentCharacters);
            }

            if (splitIndex < AudioConstants.BotVoiceMaxSegmentCharacters / 2)
            {
                splitIndex = AudioConstants.BotVoiceMaxSegmentCharacters - 1;
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

    private static string CreateBotVoiceSegmentCacheKey(int messageId, int segmentIndex)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", messageId, segmentIndex);
    }

    private void SetCurrentBotVoiceCancellationTokenSource(CancellationTokenSource cancellationTokenSource)
    {
        lock (botVoiceCancellationLock)
        {
            currentBotVoiceCancellationTokenSource = cancellationTokenSource;
        }
    }

    private void ClearCurrentBotVoiceCancellationTokenSource(CancellationTokenSource cancellationTokenSource)
    {
        lock (botVoiceCancellationLock)
        {
            if (ReferenceEquals(currentBotVoiceCancellationTokenSource, cancellationTokenSource))
            {
                currentBotVoiceCancellationTokenSource = null;
            }
        }
    }

    private void CancelCurrentBotVoice(string reason)
    {
        CancellationTokenSource? cancellationTokenSource;
        lock (botVoiceCancellationLock)
        {
            cancellationTokenSource = currentBotVoiceCancellationTokenSource;
        }

        if (cancellationTokenSource is null || cancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        Debug.WriteLine($"Canceling bot voice: Path={AudioConstants.BotVoiceDefaultPathName}; CancellationReason={reason}.");
        cancellationTokenSource.Cancel();
        audioPlaybackService.StopPlayback();
    }

    private bool IsNewestBotMessage(ChatMessageViewModel message)
    {
        return Messages.LastOrDefault(candidate => candidate.IsFromBot)?.Id == message.Id;
    }

    public void CleanupCurrentSessionBotVoiceFiles()
    {
        CancelCurrentBotVoice("lesson cleanup");
        audioPlaybackService.StopPlayback();
        CleanupTrackedBotVoiceFiles();
    }

    private async Task CleanupCurrentSessionBotVoiceFilesAsync()
    {
        CancelCurrentBotVoice("lesson cleanup");
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
        CancelCurrentBotVoice("learner sent a new message");
        var wasSent = await SendLessonMessageAsync(trimmedUserInput);

        if (wasSent)
        {
            UserInput = string.Empty;
        }
    }

    private async Task<bool> SendLessonMessageAsync(string userMessage)
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

        var nextLearnerTurnCount = LearnerTurnCount + 1;
        var softWrapUpTurn = GetSoftWrapUpTurn();
        var finalTurn = GetFinalTurn();

        if (LearnerTurnCount >= finalTurn)
        {
            MarkLessonCompleteAwaitingFinish();
            return false;
        }

        if (nextLearnerTurnCount >= finalTurn)
        {
            AddMessage(AppConstants.UserSenderName, userMessage, false);
            LearnerTurnCount = nextLearnerTurnCount;
            var finalMessage = GetFinalLessonMessage();
            AddMessage(TutorAvatarDisplayName, finalMessage, true);
            lastBotMessage = finalMessage;
            OnPropertyChanged(nameof(LatestBotMessageText));
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
            latestFeedback = mappedFeedback;

            AddMessage(AppConstants.UserSenderName, userMessage, false, mappedFeedback);
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


    private Task<bool> HandleContextSelectionMessageAsync(string userMessage)
    {
        AddMessage(AppConstants.UserSenderName, userMessage, false);

        var matchedVariant = FindMatchingContextVariant(userMessage);
        if (matchedVariant is not null)
        {
            selectedContextVariant = matchedVariant;
            selectedCustomContextTitle = string.Empty;
            CurrentLessonPhase = LessonPhase.ActiveRoleplay;

            var startMessage = $"Great! Let's imagine {BuildContextConfirmationText(matchedVariant)}.\n\n{matchedVariant.OpeningLine}";
            AddRoleplayStartMessage(startMessage);
            return Task.FromResult(true);
        }

        if (IsValidCustomContext(userMessage))
        {
            selectedContextVariant = null;
            selectedCustomContextTitle = userMessage.Trim();
            CurrentLessonPhase = LessonPhase.ActiveRoleplay;

            var openingLine = string.IsNullOrWhiteSpace(lessonScenario.ConversationFlow.DefaultOpeningExample)
                ? "Hi! Nice to meet you. What's your name?"
                : lessonScenario.ConversationFlow.DefaultOpeningExample.Trim();
            AddRoleplayStartMessage($"Good idea. Let's keep it simple: {userMessage.Trim()}.\n\n{openingLine}");
            return Task.FromResult(true);
        }

        AddMessage(TutorAvatarDisplayName, GetInvalidContextRedirect(), true);
        lastBotMessage = GetInvalidContextRedirect();
        OnPropertyChanged(nameof(LatestBotMessageText));
        StatusMessage = string.Empty;
        return Task.FromResult(true);
    }

    private void AddRoleplayStartMessage(string message)
    {
        var botMessage = AddMessage(TutorAvatarDisplayName, message, true);
        lastBotMessage = message;
        OnPropertyChanged(nameof(LatestBotMessageText));
        StatusMessage = string.Empty;
        _ = TryAutoPlayNewestBotVoiceAsync(botMessage);
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
        if (activeLevelProfile.SoftWrapUpAfterUserTurn > 0)
        {
            return activeLevelProfile.SoftWrapUpAfterUserTurn;
        }

        return lessonScenario.Metadata.SoftWrapUpAfterUserTurn > 0
            ? lessonScenario.Metadata.SoftWrapUpAfterUserTurn
            : AppConstants.DefaultLessonSoftLearnerTurnLimit;
    }

    private int GetFinalTurn()
    {
        if (activeLevelProfile.FinalMessageAtUserTurn > 0)
        {
            return activeLevelProfile.FinalMessageAtUserTurn;
        }

        return lessonScenario.Metadata.FinalMessageAtUserTurn > 0
            ? lessonScenario.Metadata.FinalMessageAtUserTurn
            : AppConstants.DefaultLessonHardLearnerTurnLimit;
    }

    private bool IsFreeConversationLesson()
    {
        return string.Equals(lessonScenario.Metadata.LessonType, "free_conversation", StringComparison.OrdinalIgnoreCase);
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
    private void ViewFeedback(ChatMessageViewModel? message)
    {
        if (message is null || !CanViewFeedback(message))
        {
            return;
        }

        var feedback = message.Feedback;
        if (feedback is null)
        {
            return;
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
            && message.Feedback is not null;
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
        return IsLessonInputEnabled && !IsSending && !IsRecording;
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
        await CleanupCurrentSessionBotVoiceFilesAsync();
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
        RefreshLessonCompletionState();
    }

    private void RefreshLessonCompletionState()
    {
        OnPropertyChanged(nameof(IsLessonInputEnabled));
        OnPropertyChanged(nameof(IsLessonOptionsEnabled));
        OnPropertyChanged(nameof(IsLessonLimitReached));
        OnPropertyChanged(nameof(IsLessonWrappingUp));
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
        OnPropertyChanged(nameof(IsLessonInputEnabled));
        OnPropertyChanged(nameof(IsLessonOptionsEnabled));
        SendMessageCommand.NotifyCanExecuteChanged();
        ToggleVoiceRecordingCommand.NotifyCanExecuteChanged();
        HintCommand.NotifyCanExecuteChanged();
        ToggleConversationModeCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
        PlayBotVoiceCommand.NotifyCanExecuteChanged();
        ViewFeedbackCommand.NotifyCanExecuteChanged();
        FinishLessonCommand.NotifyCanExecuteChanged();
        finishLesson(latestFeedback);
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private async Task Back()
    {
        await CleanupCurrentSessionBotVoiceFilesAsync();
        navigateBack();
    }

    private bool CanGoBack()
    {
        return IsLessonOptionsEnabled && !IsLessonCompleteAwaitingFinish && !hasFinishedLesson && !IsSending && !IsRecording;
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
            .Where(message => !string.IsNullOrWhiteSpace(message.Text))
            .ToArray();
    }

    private ChatMessageViewModel AddMessage(string sender, string text, bool isFromBot, Feedback? feedback = null)
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
            TranslateMessageAsync);
        Messages.Add(message);
        return message;
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
}
