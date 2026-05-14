using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Localization;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class LessonSummaryViewModel : ViewModelBase
{
    private readonly Action navigateToSubtopics;
    private readonly Action navigateToHome;
    private readonly AppLocalizedText localizedText;
    private readonly LessonChatBackendService lessonChatBackendService;
    private readonly string targetTranslationLanguage;

    public string SelectedLevel { get; }

    public Topic SelectedTopic { get; }

    public Subtopic SelectedSubtopic { get; }

    public string Title => localizedText.LessonSummaryTitle;

    public string ContextText => $"{localizedText.TopicContextLabel} {SelectedTopic.DisplayTitle} • {localizedText.SituationContextLabel} {SelectedSubtopic.DisplayTitle} • {localizedText.LevelContextLabel} {SelectedLevel}";

    public string GoodTitle => localizedText.WhatWentWellTitle;

    public string ImproveTitle => localizedText.WhatToImproveTitle;

    public string GoodText { get; }

    public string ImproveText { get; }

    public string UsefulPhrasesTitle => localizedText.UsefulPhrasesTitle;

    public string ChooseAnotherSituationText => localizedText.ChooseAnotherSituationText;

    public string BackToTopicsText => localizedText.BackToTopicsText;

    public ObservableCollection<string> UsefulPhrases { get; }

    private string translatedSummaryText = string.Empty;

    public string TranslatedSummaryText
    {
        get => translatedSummaryText;
        private set => SetProperty(ref translatedSummaryText, value);
    }

    private bool isTranslationVisible;

    public bool IsTranslationVisible
    {
        get => isTranslationVisible;
        private set
        {
            if (SetProperty(ref isTranslationVisible, value))
            {
                OnPropertyChanged(nameof(TranslationButtonText));
                OnPropertyChanged(nameof(HasTranslatedSummary));
            }
        }
    }

    private bool isTranslating;

    public bool IsTranslating
    {
        get => isTranslating;
        private set
        {
            if (SetProperty(ref isTranslating, value))
            {
                OnPropertyChanged(nameof(TranslationStatusText));
                ToggleSummaryTranslationCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private string translationErrorText = string.Empty;

    public string TranslationErrorText
    {
        get => translationErrorText;
        private set
        {
            if (SetProperty(ref translationErrorText, value))
            {
                OnPropertyChanged(nameof(HasTranslationError));
            }
        }
    }

    public bool HasTranslatedSummary => IsTranslationVisible && !string.IsNullOrWhiteSpace(TranslatedSummaryText);

    public bool HasTranslationError => !string.IsNullOrWhiteSpace(TranslationErrorText);

    public string TranslationButtonText => IsTranslationVisible
        ? localizedText.HideTranslationButtonText
        : localizedText.TranslateButtonText;

    public string TranslationLabel => localizedText.TranslationLabel;

    public string TranslationStatusText => IsTranslating ? localizedText.TranslationLoadingText : string.Empty;

    public LessonSummaryViewModel(
        AppLocalizedText localizedText,
        string selectedLevel,
        Topic selectedTopic,
        Subtopic selectedSubtopic,
        LessonSummaryInput summaryInput,
        LessonChatBackendService lessonChatBackendService,
        string nativeLanguageName,
        string interfaceLanguageId,
        Action navigateToSubtopics,
        Action navigateToHome)
    {
        this.localizedText = localizedText;
        this.lessonChatBackendService = lessonChatBackendService;
        targetTranslationLanguage = ResolveTargetTranslationLanguage(nativeLanguageName, interfaceLanguageId);
        SelectedLevel = selectedLevel;
        SelectedTopic = selectedTopic;
        SelectedSubtopic = selectedSubtopic;
        this.navigateToSubtopics = navigateToSubtopics;
        this.navigateToHome = navigateToHome;

        GoodText = BuildGoodText(summaryInput, localizedText);
        ImproveText = BuildImproveText(summaryInput, localizedText);
        UsefulPhrases = new ObservableCollection<string>(BuildUsefulPhrases(summaryInput, localizedText));
    }


    [RelayCommand(CanExecute = nameof(CanToggleSummaryTranslation))]
    private async Task ToggleSummaryTranslationAsync()
    {
        if (IsTranslationVisible)
        {
            IsTranslationVisible = false;
            TranslationErrorText = string.Empty;
            return;
        }

        if (!string.IsNullOrWhiteSpace(TranslatedSummaryText))
        {
            IsTranslationVisible = true;
            TranslationErrorText = string.Empty;
            return;
        }

        var sourceText = BuildVisibleSummaryText();
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            TranslatedSummaryText = string.Empty;
            IsTranslationVisible = false;
            TranslationErrorText = string.Empty;
            return;
        }

        IsTranslating = true;
        TranslationErrorText = string.Empty;

        try
        {
            TranslatedSummaryText = await lessonChatBackendService.TranslateTextAsync(sourceText, targetTranslationLanguage);
            IsTranslationVisible = true;
        }
        catch
        {
            TranslationErrorText = "Could not translate summary. Please try again.";
            IsTranslationVisible = false;
        }
        finally
        {
            IsTranslating = false;
        }
    }

    private bool CanToggleSummaryTranslation()
    {
        return !IsTranslating;
    }

    private string BuildVisibleSummaryText()
    {
        var sections = new List<string>();

        AddSection(sections, Title, ContextText);
        AddSection(sections, GoodTitle, GoodText);
        AddSection(sections, ImproveTitle, ImproveText);

        var phrases = UsefulPhrases.Where(phrase => !string.IsNullOrWhiteSpace(phrase)).Select(phrase => $"- {phrase.Trim()}").ToArray();
        if (phrases.Length > 0)
        {
            AddSection(sections, UsefulPhrasesTitle, string.Join(Environment.NewLine, phrases));
        }

        return string.Join(Environment.NewLine + Environment.NewLine, sections);
    }

    private static void AddSection(ICollection<string> sections, string title, string body)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            sections.Add($"{title.Trim()}:\n{body.Trim()}");
        }
    }

    private static string ResolveTargetTranslationLanguage(string nativeLanguageName, string interfaceLanguageId)
    {
        if (!string.IsNullOrWhiteSpace(nativeLanguageName))
        {
            return nativeLanguageName.Trim();
        }

        var interfaceLanguage = InterfaceLanguageOptions.GetById(interfaceLanguageId);
        return string.IsNullOrWhiteSpace(interfaceLanguage.DisplayName) ? InterfaceLanguageOptions.English.DisplayName : interfaceLanguage.DisplayName;
    }

    [RelayCommand]
    private void ChooseAnotherSituation()
    {
        navigateToSubtopics();
    }

    [RelayCommand]
    private void BackToTopics()
    {
        navigateToHome();
    }

    public static string BuildGoodText(LessonSummaryInput summaryInput, AppLocalizedText? localizedText = null)
    {
        var userTurns = GetValidUserTurns(summaryInput).ToArray();
        if (userTurns.Length > 0)
        {
            var examples = string.Join("; ", userTurns.Take(4).Select(message => $"\"{message.Text}\""));
            return $"You practiced {summaryInput.SubtopicTitle} across {userTurns.Length} learner turn(s). Useful learner phrases included: {examples}.";
        }

        return (localizedText ?? AppLocalization.GetText(null)).SummaryFallbackGoodText;
    }

    public static string BuildImproveText(LessonSummaryInput summaryInput, AppLocalizedText? localizedText = null)
    {
        var tips = GetValidUserTurns(summaryInput)
            .SelectMany(message => new[] { message.Feedback?.GrammarTip, message.Feedback?.VocabularyTip })
            .Where(tip => !string.IsNullOrWhiteSpace(tip))
            .Select(tip => tip!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToArray();

        if (tips.Length > 0)
        {
            return string.Join(" ", tips);
        }

        var userTurns = GetValidUserTurns(summaryInput).Select(message => message.Text).Take(3).ToArray();
        if (userTurns.Length > 0)
        {
            return $"Next focus: keep using complete English sentences in the same situation. Review: {string.Join("; ", userTurns)}.";
        }

        return (localizedText ?? AppLocalization.GetText(null)).SummaryFallbackImproveText;
    }

    public static IReadOnlyList<string> BuildUsefulPhrases(LessonSummaryInput summaryInput, AppLocalizedText? localizedText = null)
    {
        var phrases = new List<string>();

        foreach (var message in GetValidUserTurns(summaryInput))
        {
            AddPhraseIfValid(phrases, message.Feedback?.NaturalVersion ?? string.Empty);
            AddPhraseIfValid(phrases, message.Feedback?.CorrectedVersion ?? string.Empty);
            AddPhraseIfValid(phrases, message.Text);
        }

        if (phrases.Count == 0)
        {
            return (localizedText ?? AppLocalization.GetText(null)).SummaryFallbackUsefulPhrases;
        }

        return phrases.Take(6).ToArray();
    }

    private static IEnumerable<LessonSummaryMessage> GetValidUserTurns(LessonSummaryInput summaryInput)
    {
        return summaryInput.Messages
            .Where(message => string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))
            .Where(message => !string.IsNullOrWhiteSpace(message.Text));
    }

    private static void AddPhraseIfValid(ICollection<string> phrases, string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return;
        }

        var normalizedPhrase = phrase.Trim();

        if (phrases.Any(existingPhrase => string.Equals(existingPhrase, normalizedPhrase, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        phrases.Add(normalizedPhrase);
    }
}
