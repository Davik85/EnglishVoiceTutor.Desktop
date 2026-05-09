using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class LessonChatViewModel : ViewModelBase
{
    private const string BotStatusPrefix = "Bot status:";
    private const string ConversationModeEnabledButtonText = "Conversation mode";
    private const string BackToChatButtonText = "Back to chat";

    private readonly Action navigateBack;
    private readonly Action<Feedback?> finishLesson;
    private readonly string nativeLanguageName;
    private readonly LessonChatBackendService lessonChatBackendService;
    private readonly AudioRecordingService audioRecordingService;
    private readonly AudioPlaybackService audioPlaybackService;
    private int messageCounter;
    private Feedback? latestFeedback;
    private string lastBotMessage = AppConstants.MockBotFirstMessage;
    private bool isTranscribingAudio;

    public string SelectedLevel { get; }

    public Topic SelectedTopic { get; }

    public Subtopic SelectedSubtopic { get; }

    public string Title => AppConstants.LessonChatTitle;

    public string ContextText => $"Topic: {SelectedTopic.Title} • Situation: {SelectedSubtopic.Title} • Level: {SelectedLevel}";

    public string FeedbackTranslationHeader => $"{AppConstants.FeedbackTranslationLabel} ({nativeLanguageName})";

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = [];

    [ObservableProperty]
    private string userInput = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedFeedback))]
    private Feedback? selectedFeedback;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedbackTranslateButtonText))]
    private bool isFeedbackTranslationVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BotStatusText))]
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
    private bool isSending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VoiceButtonText))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleVoiceRecordingCommand))]
    private bool isRecording;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayBotVoiceCommand))]
    private bool isBotVoicePlaying;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvatarStateDisplayText))]
    [NotifyPropertyChangedFor(nameof(AvatarAnimationAssetPath))]
    [NotifyPropertyChangedFor(nameof(AvatarAnimationAssetUri))]
    private AvatarState currentAvatarState = AvatarState.Idle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConversationModeButtonText))]
    private bool isConversationModeEnabled;

    public bool HasSelectedFeedback => SelectedFeedback is not null;

    public string VoiceButtonText => IsRecording
        ? AppConstants.StopRecordingButtonText
        : AppConstants.StartRecordingButtonText;

    public string FeedbackTranslateButtonText => IsFeedbackTranslationVisible
        ? AppConstants.FeedbackHideTranslationButtonText
        : AppConstants.FeedbackTranslateButtonText;

    public string BotStatusText => $"{BotStatusPrefix} {BotStatus}";

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
        ? BackToChatButtonText
        : ConversationModeEnabledButtonText;

    public string AvatarStateDisplayText => AvatarConstants.GetDisplayText(CurrentAvatarState);

    public string AvatarAnimationAssetPath => AvatarConstants.GetAnimationPath(CurrentAvatarState);

    public Uri AvatarAnimationAssetUri => AvatarConstants.ToPackUri(AvatarAnimationAssetPath);

    public LessonChatViewModel(
        string selectedLevel,
        Topic selectedTopic,
        Subtopic selectedSubtopic,
        string nativeLanguageName,
        LessonChatBackendService lessonChatBackendService,
        AudioRecordingService audioRecordingService,
        AudioPlaybackService audioPlaybackService,
        Action navigateBack,
        Action<Feedback?> finishLesson)
    {
        SelectedLevel = selectedLevel;
        SelectedTopic = selectedTopic;
        SelectedSubtopic = selectedSubtopic;
        this.nativeLanguageName = nativeLanguageName;
        this.lessonChatBackendService = lessonChatBackendService;
        this.audioRecordingService = audioRecordingService;
        this.audioPlaybackService = audioPlaybackService;
        this.navigateBack = navigateBack;
        this.finishLesson = finishLesson;

        AddMessage(AppConstants.BotSenderName, AppConstants.MockBotFirstMessage, true);
        lastBotMessage = AppConstants.MockBotFirstMessage;
        _ = CheckBackendHealthAsync();
        _ = CheckBackendConfigStatusAsync();
    }

    private bool CanSendMessage()
    {
        return !IsSending && !IsRecording;
    }

    private bool CanToggleVoiceRecording()
    {
        return !IsSending || IsRecording;
    }

    private bool CanPlayBotVoice(ChatMessageViewModel? message)
    {
        return !IsBotVoicePlaying
            && message is not null
            && message.ShowPlayVoiceButton
            && !string.IsNullOrWhiteSpace(message.Text);
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
        if (IsSending)
        {
            return;
        }

        try
        {
            audioRecordingService.StartRecording();
            IsRecording = true;
            RefreshAvatarState();
            StatusMessage = AppConstants.RecordingStartedMessage;
        }
        catch
        {
            IsRecording = false;
            RefreshAvatarState();
            StatusMessage = AppConstants.RecordingStartErrorMessage;
        }
    }

    private async Task StopVoiceRecordingAsync()
    {
        var savedFilePath = string.Empty;

        try
        {
            savedFilePath = audioRecordingService.StopRecording();
            IsRecording = false;

            if (string.IsNullOrWhiteSpace(savedFilePath))
            {
                StatusMessage = AppConstants.RecordingStopErrorMessage;
                return;
            }

            IsSending = true;
            isTranscribingAudio = true;
            RefreshAvatarState();
            StatusMessage = AppConstants.TranscribingAudioMessage;

            var transcriptionText = await lessonChatBackendService.SendAudioForTranscriptionAsync(savedFilePath);
            BackendStatusText = BackendConstants.BackendStatusConnected;

            if (string.IsNullOrWhiteSpace(transcriptionText))
            {
                StatusMessage = AppConstants.EmptyTranscriptionMessage;
                return;
            }

            UserInput = transcriptionText;
            StatusMessage = AppConstants.TranscriptionCompletedMessage;
        }
        catch
        {
            BackendStatusText = BackendConstants.BackendStatusUnavailable;
            StatusMessage = AppConstants.TranscriptionFailedMessage;
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

    [RelayCommand]
    private void ToggleConversationMode()
    {
        IsConversationModeEnabled = !IsConversationModeEnabled;
    }

    [RelayCommand(CanExecute = nameof(CanPlayBotVoice))]
    private async Task PlayBotVoiceAsync(ChatMessageViewModel? message)
    {
        if (message is null || IsBotVoicePlaying || !message.ShowPlayVoiceButton)
        {
            return;
        }

        var messageText = message.Text;

        if (string.IsNullOrWhiteSpace(messageText))
        {
            return;
        }

        IsBotVoicePlaying = true;
        RefreshAvatarState();
        StatusMessage = AppConstants.PlayingBotVoiceMessage;

        try
        {
            var audioBytes = await lessonChatBackendService.CreateBotSpeechAsync(messageText);
            await audioPlaybackService.PlayAudioAsync(audioBytes);
            BackendStatusText = BackendConstants.BackendStatusConnected;
        }
        catch
        {
            BackendStatusText = BackendConstants.BackendStatusUnavailable;
            StatusMessage = AppConstants.BotVoiceFailedMessage;
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
        if (string.IsNullOrWhiteSpace(UserInput))
        {
            StatusMessage = AppConstants.EmptyMessageWarning;
            return;
        }

        var trimmedUserInput = UserInput.Trim();
        BotStatus = BackendConstants.BotStatusThinking;
        IsSending = true;
        RefreshAvatarState();

        try
        {
            var response = await lessonChatBackendService.SendLessonMessageAsync(new LessonChatBackendRequest
            {
                SelectedLevel = SelectedLevel,
                TopicTitle = SelectedTopic.Title,
                SubtopicTitle = SelectedSubtopic.Title,
                UserMessage = trimmedUserInput,
                LastBotMessage = lastBotMessage,
                NativeLanguageName = nativeLanguageName
            });

            var mappedFeedback = MapFeedback(response.Feedback);
            latestFeedback = mappedFeedback;

            AddMessage(AppConstants.UserSenderName, trimmedUserInput, false, mappedFeedback);
            AddMessage(AppConstants.BotSenderName, response.BotReply, true);
            lastBotMessage = response.BotReply;
            OnPropertyChanged(nameof(LatestBotMessageText));

            BackendStatusText = BackendConstants.BackendStatusConnected;
            UserInput = string.Empty;
            StatusMessage = string.Empty;
        }
        catch
        {
            BackendStatusText = BackendConstants.BackendStatusUnavailable;
            StatusMessage = BackendConstants.BackendUnavailableMessage;
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
        StatusMessage = BackendConstants.BackendHealthCheckFailedMessage;
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

    [RelayCommand]
    private void ViewFeedback(ChatMessageViewModel? message)
    {
        if (message is null || message.IsFromBot || message.Feedback is null)
        {
            return;
        }

        SelectedFeedback = message.Feedback;
        IsFeedbackTranslationVisible = false;
        StatusMessage = message.Feedback.ShortText;
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

        StatusMessage = AppConstants.TranslationLoadingText;

        try
        {
            await TranslateSelectedFeedbackAsync(SelectedFeedback);
            IsFeedbackTranslationVisible = true;
            StatusMessage = SelectedFeedback.ShortText;
            OnPropertyChanged(nameof(SelectedFeedback));
        }
        catch
        {
            StatusMessage = AppConstants.TranslationFailedText;
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
    private async Task HintAsync()
    {
        if (IsSending || IsRecording)
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
                NativeLanguageName = nativeLanguageName
            });

            BackendStatusText = BackendConstants.BackendStatusConnected;
            StatusMessage = hintText;
        }
        catch
        {
            BackendStatusText = BackendConstants.BackendStatusUnavailable;
            StatusMessage = BackendConstants.BackendUnavailableMessage;
        }
        finally
        {
            BotStatus = BackendConstants.BotStatusReady;
            IsSending = false;
            RefreshAvatarState();
        }
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

    [RelayCommand]
    private void FinishLesson()
    {
        finishLesson(latestFeedback);
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
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
            StatusMessage = AppConstants.TranslationFailedText;
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
