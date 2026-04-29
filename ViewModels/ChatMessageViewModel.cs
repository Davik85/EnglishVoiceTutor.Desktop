using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class ChatMessageViewModel : ViewModelBase
{
    private readonly string nativeLanguageName;

    public int Id { get; }

    public string Sender { get; }

    public string Text { get; }

    public bool IsFromBot { get; }

    public Feedback? Feedback { get; }

    public string TranslationText { get; }

    public string TranslationHeader => $"{AppConstants.TranslationLabel} ({nativeLanguageName})";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TranslateButtonText))]
    private bool isTranslationVisible;

    public string TranslateButtonText => IsTranslationVisible
        ? AppConstants.HideTranslationButtonText
        : AppConstants.TranslateButtonText;

    public ChatMessageViewModel(int id, string sender, string text, bool isFromBot, Feedback? feedback, string translationText, string nativeLanguageName)
    {
        Id = id;
        Sender = sender;
        Text = text;
        IsFromBot = isFromBot;
        Feedback = feedback;
        TranslationText = translationText;
        this.nativeLanguageName = nativeLanguageName;
    }

    [RelayCommand]
    private void ToggleTranslation()
    {
        IsTranslationVisible = !IsTranslationVisible;
    }
}
