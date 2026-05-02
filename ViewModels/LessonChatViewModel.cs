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

    private readonly Action navigateBack;
    private readonly Action finishLesson;
    private readonly string nativeLanguageName;
    private readonly LessonChatBackendService lessonChatBackendService;
    private int messageCounter;

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
    private string botStatus = BackendConstants.BotStatusReady;

    [ObservableProperty]
    private string backendStatusText = BackendConstants.BackendStatusChecking;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private bool isSending;

    public bool HasSelectedFeedback => SelectedFeedback is not null;

    public string FeedbackTranslateButtonText => IsFeedbackTranslationVisible
        ? AppConstants.FeedbackHideTranslationButtonText
        : AppConstants.FeedbackTranslateButtonText;

    public string BotStatusText => $"{BotStatusPrefix} {BotStatus}";

    public LessonChatViewModel(
        string selectedLevel,
        Topic selectedTopic,
        Subtopic selectedSubtopic,
        string nativeLanguageName,
        LessonChatBackendService lessonChatBackendService,
        Action navigateBack,
        Action finishLesson)
    {
        SelectedLevel = selectedLevel;
        SelectedTopic = selectedTopic;
        SelectedSubtopic = selectedSubtopic;
        this.nativeLanguageName = nativeLanguageName;
        this.lessonChatBackendService = lessonChatBackendService;
        this.navigateBack = navigateBack;
        this.finishLesson = finishLesson;

        AddMessage(AppConstants.BotSenderName, AppConstants.MockBotFirstMessage, true);
        _ = CheckBackendHealthAsync();
    }

    private bool CanSendMessage()
    {
        return !IsSending;
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

        try
        {
            var response = await lessonChatBackendService.SendLessonMessageAsync(new LessonChatBackendRequest
            {
                SelectedLevel = SelectedLevel,
                TopicTitle = SelectedTopic.Title,
                SubtopicTitle = SelectedSubtopic.Title,
                UserMessage = trimmedUserInput,
                NativeLanguageName = nativeLanguageName
            });

            AddMessage(AppConstants.UserSenderName, trimmedUserInput, false, MapFeedback(response.Feedback));
            AddMessage(AppConstants.BotSenderName, response.BotReply, true);

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
    private void ToggleFeedbackTranslation()
    {
        if (SelectedFeedback is null)
        {
            return;
        }

        IsFeedbackTranslationVisible = !IsFeedbackTranslationVisible;
    }

    [RelayCommand]
    private void CloseFeedback()
    {
        SelectedFeedback = null;
        StatusMessage = string.Empty;
        IsFeedbackTranslationVisible = false;
    }

    [RelayCommand]
    private void Hint()
    {
        StatusMessage = AppConstants.MockHintText;
    }

    [RelayCommand]
    private void FinishLesson()
    {
        finishLesson();
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
            GetMockTranslation(sender, text, isFromBot),
            nativeLanguageName));
    }

    private static string GetMockTranslation(string sender, string text, bool isFromBot)
    {
        if (!isFromBot)
        {
            return AppConstants.MockUserMessageTranslationText;
        }

        if (sender == AppConstants.BotSenderName && text == AppConstants.MockBotFirstMessage)
        {
            return AppConstants.MockBotFirstMessageTranslation;
        }

        return AppConstants.MockBotReplyTextTranslation;
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
            AppConstants.MockFeedbackShortTextTranslation,
            AppConstants.MockCorrectedVersionTranslation,
            AppConstants.MockGrammarTipTranslation,
            AppConstants.MockVocabularyTipTranslation,
            AppConstants.MockCultureTipTranslation,
            AppConstants.MockNaturalVersionTranslation);
    }
}
