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
    private int messageCounter;

    public string SelectedLevel { get; }

    public Topic SelectedTopic { get; }

    public Subtopic SelectedSubtopic { get; }

    public string Title => AppConstants.LessonChatTitle;

    public string ContextText => $"Topic: {SelectedTopic.Title} • Situation: {SelectedSubtopic.Title} • Level: {SelectedLevel}";

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    [ObservableProperty]
    private string userInput = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedFeedback))]
    private Feedback? selectedFeedback;

    public bool HasSelectedFeedback => SelectedFeedback is not null;

    public LessonChatViewModel(
        string selectedLevel,
        Topic selectedTopic,
        Subtopic selectedSubtopic,
        Action navigateBack,
        Action finishLesson)
    {
        SelectedLevel = selectedLevel;
        SelectedTopic = selectedTopic;
        SelectedSubtopic = selectedSubtopic;
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
    private void ViewFeedback(ChatMessage? message)
    {
        if (message is null || message.IsFromBot || message.Feedback is null)
        {
            return;
        }

        SelectedFeedback = message.Feedback;
        StatusMessage = message.Feedback.ShortText;
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
        Messages.Add(new ChatMessage(messageCounter, sender, text, isFromBot, isFromBot ? null : CreateMockFeedback()));
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
            AppConstants.MockNaturalVersion);
    }
}
