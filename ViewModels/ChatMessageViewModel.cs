using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Localization;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class ChatMessageViewModel : ViewModelBase
{
    private readonly string nativeLanguageName;
    private readonly Func<ChatMessageViewModel, Task> translateAsync;
    private readonly AppLocalizedText localizedText;

    public int Id { get; }

    public string Sender { get; }

    public string Text { get; }

    public bool IsFromBot { get; }

    public bool ShowPlayVoiceButton => IsFromBot;

    public Feedback? Feedback { get; }

    public string TranslationHeader => $"{localizedText.TranslationLabel} ({nativeLanguageName})";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTranslation))]
    private string translationText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TranslateButtonText))]
    private bool isTranslationVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TranslateButtonText))]
    private bool isTranslationLoading;

    public string TranslateButtonText
    {
        get
        {
            if (IsTranslationLoading)
            {
                return localizedText.TranslationLoadingText;
            }

            return IsTranslationVisible
                ? localizedText.HideTranslationButtonText
                : localizedText.TranslateButtonText;
        }
    }

    public bool HasTranslation => !string.IsNullOrWhiteSpace(TranslationText);

    public ChatMessageViewModel(
        int id,
        string sender,
        string text,
        bool isFromBot,
        Feedback? feedback,
        string translationText,
        string nativeLanguageName,
        AppLocalizedText localizedText,
        Func<ChatMessageViewModel, Task> translateAsync)
    {
        Id = id;
        Sender = sender;
        Text = text;
        IsFromBot = isFromBot;
        Feedback = feedback;
        TranslationText = translationText;
        this.nativeLanguageName = nativeLanguageName;
        this.localizedText = localizedText;
        this.translateAsync = translateAsync;
    }

    [RelayCommand]
    private async Task ToggleTranslationAsync()
    {
        if (IsTranslationLoading)
        {
            return;
        }

        if (IsTranslationVisible)
        {
            IsTranslationVisible = false;
            return;
        }

        if (HasTranslation)
        {
            IsTranslationVisible = true;
            return;
        }

        IsTranslationLoading = true;

        try
        {
            await translateAsync(this);
        }
        finally
        {
            IsTranslationLoading = false;
        }
    }
}
