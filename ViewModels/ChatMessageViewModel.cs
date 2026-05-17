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

    [ObservableProperty]
    private string text = string.Empty;

    public bool IsFromBot { get; }

    public string Role => IsFromBot ? "assistant" : "user";

    public string Source { get; }

    public int LessonTurnNumber { get; private set; }

    public string LessonPhase { get; }

    public string Topic { get; }

    public string Subtopic { get; }

    public string Level { get; }

    public string SelectedContextTitle { get; }

    public string SelectedContextVariantId { get; }

    public DateTimeOffset Timestamp { get; }

    public bool CountsAsValidLessonTurn { get; private set; }

    public bool IsTechnicalMessage { get; private set; }

    public bool IsFeedbackEligible { get; private set; }

    public bool CanShowFeedbackAction => !IsFromBot && IsFeedbackEligible && !IsTechnicalMessage;

    public bool ShowPlayVoiceButton => IsFromBot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFeedback))]
    private Feedback? feedback;

    public bool HasFeedback => Feedback is not null;

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
        Func<ChatMessageViewModel, Task> translateAsync,
        string source = ChatMessageSource.Technical,
        int lessonTurnNumber = 0,
        string lessonPhase = "",
        string topic = "",
        string subtopic = "",
        string level = "",
        string selectedContextTitle = "",
        string selectedContextVariantId = "",
        DateTimeOffset? timestamp = null,
        bool countsAsValidLessonTurn = false,
        bool isTechnicalMessage = false,
        bool isFeedbackEligible = false)
    {
        Id = id;
        Sender = sender;
        Text = text;
        IsFromBot = isFromBot;
        Feedback = feedback;
        Source = string.IsNullOrWhiteSpace(source) ? ChatMessageSource.Technical : source;
        LessonTurnNumber = lessonTurnNumber;
        LessonPhase = lessonPhase;
        Topic = topic;
        Subtopic = subtopic;
        Level = level;
        SelectedContextTitle = selectedContextTitle;
        SelectedContextVariantId = selectedContextVariantId;
        Timestamp = timestamp ?? DateTimeOffset.Now;
        CountsAsValidLessonTurn = countsAsValidLessonTurn;
        IsTechnicalMessage = isTechnicalMessage;
        IsFeedbackEligible = isFeedbackEligible;
        TranslationText = translationText;
        this.nativeLanguageName = nativeLanguageName;
        this.localizedText = localizedText;
        this.translateAsync = translateAsync;
    }

    public void MarkAsValidLearnerTurn(string normalizedText, int lessonTurnNumber)
    {
        Text = normalizedText;
        LessonTurnNumber = lessonTurnNumber;
        CountsAsValidLessonTurn = true;
        IsTechnicalMessage = false;
        IsFeedbackEligible = !IsFromBot && !string.IsNullOrWhiteSpace(normalizedText);
        OnPropertyChanged(nameof(LessonTurnNumber));
        OnPropertyChanged(nameof(CountsAsValidLessonTurn));
        OnPropertyChanged(nameof(IsTechnicalMessage));
        OnPropertyChanged(nameof(IsFeedbackEligible));
        OnPropertyChanged(nameof(CanShowFeedbackAction));
    }

    public void MarkAsInvalidLearnerTranscript(string retryText)
    {
        Text = retryText;
        CountsAsValidLessonTurn = false;
        IsFeedbackEligible = false;
        OnPropertyChanged(nameof(CountsAsValidLessonTurn));
        OnPropertyChanged(nameof(IsFeedbackEligible));
        OnPropertyChanged(nameof(CanShowFeedbackAction));
    }

    public void SetFeedback(Feedback value)
    {
        Feedback = value;
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
