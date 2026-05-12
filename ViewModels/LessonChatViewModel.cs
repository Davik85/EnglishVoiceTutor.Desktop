using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
    private int messageCounter;
    private Feedback? latestFeedback;
    private string lastBotMessage = AppConstants.MockBotFirstMessage;
    private ContextVariant? selectedContextVariant;
    private string selectedCustomContextTitle = string.Empty;
    private bool isTranscribingAudio;
    private bool hasFinishedLesson;
    private readonly SemaphoreSlim botVoiceSemaphore = new(1, 1);
    private readonly Dictionary<int, string> botVoiceAudioFilePaths = [];
    private readonly HashSet<string> currentSessionBotVoiceFilePaths = new(StringComparer.OrdinalIgnoreCase);

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

    public bool IsLessonOptionsEnabled => CurrentLessonPhase != LessonPhase.Completed && !IsLessonCompleteAwaitingFinish && !hasFinishedLesson;

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
        this.lessonChatBackendService = lessonChatBackendService;
        this.audioRecordingService = audioRecordingService;
        this.audioPlaybackService = audioPlaybackService;
        this.botVoiceTempFileCleanupService = botVoiceTempFileCleanupService;
        this.audioInputDeviceId = string.IsNullOrWhiteSpace(audioInputDeviceId)
            ? AudioConstants.DefaultAudioInputDeviceId
            : audioInputDeviceId;
        this.navigateBack = navigateBack;
        this.finishLesson = finishLesson;

        CurrentLessonPhase = LessonPhase.SetupContextSelection;
        var setupMessage = string.IsNullOrWhiteSpace(this.lessonScenario.LessonSetup.SetupMessage)
            ? AppConstants.MockBotFirstMessage
            : this.lessonScenario.LessonSetup.SetupMessage.Trim();
        AddMessage(TutorAvatarDisplayName, setupMessage, true);
        lastBotMessage = setupMessage;
        _ = CheckBackendHealthAsync();
        _ = CheckBackendConfigStatusAsync();
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
        return !IsRecording && !IsSending && !hasFinishedLesson;
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

        return !ContainsMostlyCyrillicLetters(transcriptionText);
    }

    private static bool ContainsMostlyCyrillicLetters(string text)
    {
        var letterCount = 0;
        var cyrillicLetterCount = 0;

        foreach (var character in text)
        {
            if (!char.IsLetter(character))
            {
                continue;
            }

            letterCount++;

            if (character is >= '\u0400' and <= '\u04FF')
            {
                cyrillicLetterCount++;
            }
        }

        return letterCount > 0 && cyrillicLetterCount > letterCount / 2;
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

        if (message.Text.Length > AudioConstants.AutoPlayMaxCharacters)
        {
            Debug.WriteLine($"Skipping bot voice auto-play for message {message.Id}: text length exceeds {AudioConstants.AutoPlayMaxCharacters} characters.");
            return Task.CompletedTask;
        }

        // MVP behavior: auto-play never queues. If another bot voice is already loading or playing, skip this one
        // and leave manual Play voice available so the lesson stays responsive.
        if (IsBotVoicePlaying)
        {
            Debug.WriteLine($"Skipping bot voice auto-play for message {message.Id}: another bot voice is busy.");
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

        if (!await botVoiceSemaphore.WaitAsync(0, cancellationToken))
        {
            Debug.WriteLine($"Skipping bot voice {(isAutoPlay ? "auto-play" : "manual play")} for message {message.Id}: another voice request is busy.");
            return;
        }

        try
        {
            IsBotVoicePlaying = true;
            RefreshAvatarState();
            StatusMessage = localizedText.PlayingBotVoiceMessage;

            var audioFilePath = await GetOrCreateBotVoiceAudioFileAsync(message, cancellationToken);

            if (isAutoPlay && !IsNewestBotMessage(message))
            {
                Debug.WriteLine($"Skipping bot voice auto-play for message {message.Id}: a newer bot message arrived before playback.");
                return;
            }

            await audioPlaybackService.PlayAudioFileAsync(audioFilePath, cancellationToken);
            BackendStatusText = BackendConstants.BackendStatusConnected;
        }
        catch (OperationCanceledException exception)
        {
            Debug.WriteLine($"Bot voice {(isAutoPlay ? "auto-play" : "manual play")} canceled for message {message.Id}: {exception}");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Bot voice {(isAutoPlay ? "auto-play" : "manual play")} failed for message {message.Id}: {exception}");
            StatusMessage = localizedText.BotVoiceFailedMessage;
        }
        finally
        {
            IsBotVoicePlaying = false;
            RefreshAvatarState();
            botVoiceSemaphore.Release();
        }
    }

    private async Task<string> GetOrCreateBotVoiceAudioFileAsync(ChatMessageViewModel message, CancellationToken cancellationToken)
    {
        if (botVoiceAudioFilePaths.TryGetValue(message.Id, out var cachedFilePath) && File.Exists(cachedFilePath))
        {
            TrackCurrentSessionBotVoiceFile(cachedFilePath);
            return cachedFilePath;
        }

        try
        {
            var totalStopwatch = Stopwatch.StartNew();
            var backendStopwatch = Stopwatch.StartNew();
            var inputLength = message.Text.Trim().Length;

            Debug.WriteLine($"Bot voice generation starting for message {message.Id}: InputLength={inputLength}.");
            var speechResponse = await lessonChatBackendService.CreateBotSpeechAsync(message.Text, cancellationToken);
            Debug.WriteLine($"Bot voice backend response received for message {message.Id}: InputLength={inputLength}; ElapsedMilliseconds={backendStopwatch.ElapsedMilliseconds}; AudioBytes={speechResponse.AudioBytes.Length}; ContentType={speechResponse.ContentType}.");

            var saveStopwatch = Stopwatch.StartNew();
            var audioFilePath = await audioPlaybackService.SaveBotVoiceAudioAsync(
                speechResponse.AudioBytes,
                speechResponse.FileExtension,
                cancellationToken);
            Debug.WriteLine($"Bot voice file ready for message {message.Id}: SaveElapsedMilliseconds={saveStopwatch.ElapsedMilliseconds}; TotalElapsedMilliseconds={totalStopwatch.ElapsedMilliseconds}; FileExtension={Path.GetExtension(audioFilePath)}.");
            botVoiceAudioFilePaths[message.Id] = audioFilePath;
            TrackCurrentSessionBotVoiceFile(audioFilePath);
            return audioFilePath;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Debug.WriteLine($"Bot voice generation or save failed for message {message.Id}: {exception}");
            throw;
        }
    }

    private bool IsNewestBotMessage(ChatMessageViewModel message)
    {
        return Messages.LastOrDefault(candidate => candidate.IsFromBot)?.Id == message.Id;
    }

    public void CleanupCurrentSessionBotVoiceFiles()
    {
        audioPlaybackService.StopPlayback();
        CleanupTrackedBotVoiceFiles();
    }

    private async Task CleanupCurrentSessionBotVoiceFilesAsync()
    {
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
        var wasSent = await SendLessonMessageAsync(trimmedUserInput);

        if (wasSent)
        {
            UserInput = string.Empty;
        }
    }

    private async Task<bool> SendLessonMessageAsync(string userMessage)
    {
        if (!IsLessonInputEnabled || string.IsNullOrWhiteSpace(userMessage))
        {
            return false;
        }

        CurrentHintText = string.Empty;

        if (CurrentLessonPhase == LessonPhase.SetupContextSelection)
        {
            return await HandleContextSelectionMessageAsync(userMessage);
        }

        if (CurrentLessonPhase == LessonPhase.Completed)
        {
            return false;
        }

        BotStatus = BackendConstants.BotStatusThinking;
        IsSending = true;
        RefreshAvatarState();

        var nextLearnerTurnCount = LearnerTurnCount + 1;
        var softWrapUpTurn = GetSoftWrapUpTurn();
        var finalTurn = GetFinalTurn();
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
                LessonScenarioId = lessonScenario.Id,
                Level = lessonScenario.Metadata.Level,
                Topic = lessonScenario.Metadata.Topic,
                Subtopic = lessonScenario.Metadata.Subtopic,
                LessonGoal = lessonScenario.LearningGoal.Goal,
                SelectedContextVariantId = selectedContextVariant?.Id ?? string.Empty,
                SelectedContextTitle = GetSelectedContextTitle(),
                SelectedContextOpeningLine = selectedContextVariant?.OpeningLine ?? lessonScenario.ConversationFlow.DefaultOpeningExample,
                UserTurnNumber = nextLearnerTurnCount,
                SoftWrapUpAfterUserTurn = softWrapUpTurn,
                FinalMessageAtUserTurn = finalTurn,
                TargetLanguageKeyPhrases = lessonScenario.TargetLanguage.KeyPhrases,
                GrammarFocus = lessonScenario.TargetLanguage.GrammarFocus,
                FeedbackRulesSummary = BuildFeedbackRulesSummary(),
                TutorProfileId = tutorAvatarId
            });

            var mappedFeedback = MapFeedback(response.Feedback);
            latestFeedback = mappedFeedback;

            AddMessage(AppConstants.UserSenderName, userMessage, false, mappedFeedback);
            LearnerTurnCount = nextLearnerTurnCount;
            var botReply = shouldEndLessonNow && !string.IsNullOrWhiteSpace(lessonScenario.ConversationFlow.FinalMessage)
                ? lessonScenario.ConversationFlow.FinalMessage
                : response.BotReply;
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
            : ["meet", "meeting", "introduce", "introduction", "first time", "знаком", "познаком", "встреч", "представ"];

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

        return "That sounds interesting, but this lesson is about introductions. Please choose a situation about meeting someone for the first time.";
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
        return lessonScenario.Metadata.SoftWrapUpAfterUserTurn > 0
            ? lessonScenario.Metadata.SoftWrapUpAfterUserTurn
            : AppConstants.DefaultLessonSoftLearnerTurnLimit;
    }

    private int GetFinalTurn()
    {
        return lessonScenario.Metadata.FinalMessageAtUserTurn > 0
            ? lessonScenario.Metadata.FinalMessageAtUserTurn
            : AppConstants.DefaultLessonHardLearnerTurnLimit;
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
                RecentMessages = GetRecentConversationMessages(),
                LessonScenarioId = lessonScenario.Id,
                Level = lessonScenario.Metadata.Level,
                Topic = lessonScenario.Metadata.Topic,
                Subtopic = lessonScenario.Metadata.Subtopic,
                LessonGoal = lessonScenario.LearningGoal.Goal,
                SelectedContextVariantId = selectedContextVariant?.Id ?? string.Empty,
                SelectedContextTitle = GetSelectedContextTitle(),
                SelectedContextOpeningLine = selectedContextVariant?.OpeningLine ?? lessonScenario.ConversationFlow.DefaultOpeningExample,
                UserTurnNumber = LearnerTurnCount,
                SoftWrapUpAfterUserTurn = softWrapUpTurn,
                FinalMessageAtUserTurn = finalTurn,
                TargetLanguageKeyPhrases = lessonScenario.TargetLanguage.KeyPhrases,
                GrammarFocus = lessonScenario.TargetLanguage.GrammarFocus,
                FeedbackRulesSummary = BuildFeedbackRulesSummary(),
                TutorProfileId = tutorAvatarId
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
        if (IsLessonCompleteAwaitingFinish)
        {
            return;
        }

        CurrentLessonPhase = LessonPhase.Completed;
        IsLessonCompleteAwaitingFinish = true;
        IsConversationModeEnabled = false;
        UserInput = string.Empty;
        StatusMessage = AppConstants.LessonCompleteAwaitingFinishMessage;
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
        return !IsLessonCompleteAwaitingFinish && !hasFinishedLesson && !IsSending && !IsRecording;
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
