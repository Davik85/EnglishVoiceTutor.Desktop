using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class LessonChatViewModel : ViewModelBase
{
    private readonly Action navigateBack;
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

    public LessonChatViewModel(
        string selectedLevel,
        Topic selectedTopic,
        Subtopic selectedSubtopic,
        Action navigateBack)
    {
        SelectedLevel = selectedLevel;
        SelectedTopic = selectedTopic;
        SelectedSubtopic = selectedSubtopic;
        this.navigateBack = navigateBack;

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
    private void Hint()
    {
        StatusMessage = AppConstants.MockHintText;
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }

    private void AddMessage(string sender, string text, bool isFromBot)
    {
        messageCounter++;
        Messages.Add(new ChatMessage(messageCounter, sender, text, isFromBot));
    }
}
