using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class LessonChatViewModel : ViewModelBase
{
    private readonly Action navigateBack;
    private readonly Action finishLesson;
    private readonly string nativeLanguageName;
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

    public bool HasSelectedFeedback => SelectedFeedback is not null;

    public string FeedbackTranslateButtonText => IsFeedbackTranslationVisible
        ? AppConstants.FeedbackHideTranslationButtonText
        : AppConstants.FeedbackTranslateButtonText;

    public LessonChatViewModel(
        string selectedLevel,
        Topic selectedTopic,
        Subtopic selectedSubtopic,
        string nativeLanguageName,
        Action navigateBack,
        Action finishLesson)
    {
        SelectedLevel = selectedLevel;
        SelectedTopic = selectedTopic;
        SelectedSubtopic = selectedSubtopic;
        this.nativeLanguageName = nativeLanguageName;
        this.navigateBack = navigateBack;
        this.finishLesson = finishLesson;

        AddMessage(AppConstants.BotSenderName, AppConstants.MockBotFirstMessage, true);
    }

    [RelayCommand]
    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
        {
            StatusMessage = AppConstants.EmptyMessageWarning;
            return;
        }

        AddMessage(AppConstants.UserSenderName, UserInput.Trim(), false);
        UserInput = string.Empty;

        AddMessage(AppConstants.BotSenderName, AppConstants.MockBotReplyText, true);
        StatusMessage = string.Empty;
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

    private void AddMessage(string sender, string text, bool isFromBot)
    {
        messageCounter++;
        Messages.Add(new ChatMessageViewModel(
            messageCounter,
            sender,
            text,
            isFromBot,
            isFromBot ? null : CreateMockFeedback(),
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

    private static Feedback CreateMockFeedback()
    {
        return new Feedback(
            AppConstants.MockFeedbackType,
            AppConstants.MockFeedbackShortText,
            AppConstants.MockCorrectedVersion,
            AppConstants.MockGrammarTip,
            AppConstants.MockVocabularyTip,
            AppConstants.MockCultureTip,
            AppConstants.MockNaturalVersion,
            AppConstants.MockFeedbackShortTextTranslation,
            AppConstants.MockCorrectedVersionTranslation,
            AppConstants.MockGrammarTipTranslation,
            AppConstants.MockVocabularyTipTranslation,
            AppConstants.MockCultureTipTranslation,
            AppConstants.MockNaturalVersionTranslation);
    }
}
