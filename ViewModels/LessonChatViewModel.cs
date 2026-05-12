using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Localization;
using EnglishVoiceTutor.Desktop.Models;
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
    private readonly string audioInputDeviceId;
    private readonly AppLocalizedText localizedText;
    private int messageCounter;
    private Feedback? latestFeedback;
    private string lastBotMessage = AppConstants.MockBotFirstMessage;
    private bool isTranscribingAudio;
    private bool hasFinishedLesson;

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

    public bool HasSelectedFeedback => SelectedFeedback is not null;

    public bool HasCurrentHint => !string.IsNullOrWhiteSpace(CurrentHintText);

    public bool IsLessonLimitReached => LearnerTurnCount >= AppConstants.DefaultLessonHardLearnerTurnLimit;

    public bool IsLessonWrappingUp => LearnerTurnCount >= AppConstants.DefaultLessonSoftLearnerTurnLimit;

    public bool IsLessonInputEnabled => !IsLessonCompleteAwaitingFinish && !IsLessonLimitReached && !hasFinishedLesson;

    public bool IsLessonOptionsEnabled => !IsLessonCompleteAwaitingFinish && !hasFinishedLesson;

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
        LessonChatBackendService lessonChatBackendService,
        AudioRecordingService audioRecordingService,
        AudioPlaybackService audioPlaybackService,
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
        this.lessonChatBackendService = lessonChatBackendService;
        this.audioRecordingService = audioRecordingService;
        this.audioPlaybackService = audioPlaybackService;
        this.audioInputDeviceId = string.IsNullOrWhiteSpace(audioInputDeviceId)
            ? AudioConstants.DefaultAudioInputDeviceId
            : audioInputDeviceId;
        this.navigateBack = navigateBack;
        this.finishLesson = finishLesson;

        AddMessage(TutorAvatarDisplayName, AppConstants.MockBotFirstMessage, true);
        lastBotMessage = AppConstants.MockBotFirstMessage;
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

        await PlayBotVoiceTextAsync(message.Text);
    }

    private async Task PlayBotVoiceTextAsync(string messageText)
    {
        if (IsBotVoicePlaying || string.IsNullOrWhiteSpace(messageText))
        {
            return;
        }

        IsBotVoicePlaying = true;
        RefreshAvatarState();
        StatusMessage = localizedText.PlayingBotVoiceMessage;

        try
        {
            var audioBytes = await lessonChatBackendService.CreateBotSpeechAsync(messageText);
            await audioPlaybackService.PlayAudioAsync(audioBytes);
            BackendStatusText = BackendConstants.BackendStatusConnected;
        }
        catch
        {
            BackendStatusText = BackendConstants.BackendStatusUnavailable;
            StatusMessage = localizedText.BotVoiceFailedMessage;
        }
        finally
        {
            IsBotVoicePlaying = false;
            RefreshAvatarState();
        }
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
        BotStatus = BackendConstants.BotStatusThinking;
        IsSending = true;
        RefreshAvatarState();

        var nextLearnerTurnCount = LearnerTurnCount + 1;
        var shouldStartWrappingUp = nextLearnerTurnCount >= AppConstants.DefaultLessonSoftLearnerTurnLimit;
        var shouldEndLessonNow = nextLearnerTurnCount >= AppConstants.DefaultLessonHardLearnerTurnLimit;

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
                SoftLearnerTurnLimit = AppConstants.DefaultLessonSoftLearnerTurnLimit,
                HardLearnerTurnLimit = AppConstants.DefaultLessonHardLearnerTurnLimit,
                RemainingLearnerTurns = Math.Max(AppConstants.DefaultLessonHardLearnerTurnLimit - nextLearnerTurnCount, 0),
                ShouldStartWrappingUp = shouldStartWrappingUp,
                ShouldEndLessonNow = shouldEndLessonNow,
                RecentMessages = GetRecentConversationMessages()
            });

            var mappedFeedback = MapFeedback(response.Feedback);
            latestFeedback = mappedFeedback;

            AddMessage(AppConstants.UserSenderName, userMessage, false, mappedFeedback);
            LearnerTurnCount = nextLearnerTurnCount;
            AddMessage(TutorAvatarDisplayName, response.BotReply, true);
            lastBotMessage = response.BotReply;
            OnPropertyChanged(nameof(LatestBotMessageText));

            BackendStatusText = BackendConstants.BackendStatusConnected;
            StatusMessage = string.Empty;
            BotStatus = BackendConstants.BotStatusReady;
            IsSending = false;
            RefreshAvatarState();

            if (response.IsLessonComplete || shouldEndLessonNow)
            {
                MarkLessonCompleteAwaitingFinish();
                return true;
            }

            if (ShouldAutoPlayBotVoice)
            {
                await PlayBotVoiceTextAsync(response.BotReply);
            }

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

        IsSending = true;
        BotStatus = BackendConstants.BotStatusThinking;
        RefreshAvatarState();

        try
        {
            var hintUserMessage = string.IsNullOrWhiteSpace(UserInput)
                ? AppConstants.HintFallbackUserMessage
                : UserInput.Trim();

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
                RecentMessages = GetRecentConversationMessages()
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
    private void FinishLesson()
    {
        CompleteLesson();
    }

    private void MarkLessonCompleteAwaitingFinish()
    {
        if (IsLessonCompleteAwaitingFinish)
        {
            return;
        }

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
    private void Back()
    {
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

    private void AddMessage(string sender, string text, bool isFromBot, Feedback? feedback = null)
    {
        messageCounter++;
        Messages.Add(new ChatMessageViewModel(
            messageCounter,
            sender,
            text,
            isFromBot,
            isFromBot ? null : feedback,
            string.Empty,
            nativeLanguageName,
            localizedText,
            TranslateMessageAsync));
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
