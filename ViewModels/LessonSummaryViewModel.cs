using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Localization;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class LessonSummaryViewModel : ViewModelBase
{
    private readonly Action navigateToSubtopics;
    private readonly Action navigateToHome;
    private readonly AppLocalizedText localizedText;

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

    public LessonSummaryViewModel(
        AppLocalizedText localizedText,
        string selectedLevel,
        Topic selectedTopic,
        Subtopic selectedSubtopic,
        Feedback? latestFeedback,
        Action navigateToSubtopics,
        Action navigateToHome)
    {
        this.localizedText = localizedText;
        SelectedLevel = selectedLevel;
        SelectedTopic = selectedTopic;
        SelectedSubtopic = selectedSubtopic;
        this.navigateToSubtopics = navigateToSubtopics;
        this.navigateToHome = navigateToHome;

        GoodText = BuildGoodText(latestFeedback, localizedText);
        ImproveText = BuildImproveText(latestFeedback, localizedText);
        UsefulPhrases = new ObservableCollection<string>(BuildUsefulPhrases(latestFeedback, localizedText));
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

    public static string BuildGoodText(Feedback? latestFeedback, AppLocalizedText? localizedText = null)
    {
        if (!string.IsNullOrWhiteSpace(latestFeedback?.ShortText))
        {
            return latestFeedback.ShortText;
        }

        return (localizedText ?? AppLocalization.GetText(null)).SummaryFallbackGoodText;
    }

    public static string BuildImproveText(Feedback? latestFeedback, AppLocalizedText? localizedText = null)
    {
        if (latestFeedback is null)
        {
            return (localizedText ?? AppLocalization.GetText(null)).SummaryFallbackImproveText;
        }

        var tips = new List<string>();

        if (!string.IsNullOrWhiteSpace(latestFeedback.GrammarTip))
        {
            tips.Add(latestFeedback.GrammarTip.Trim());
        }

        if (!string.IsNullOrWhiteSpace(latestFeedback.VocabularyTip))
        {
            tips.Add(latestFeedback.VocabularyTip.Trim());
        }

        if (tips.Count == 0)
        {
            return (localizedText ?? AppLocalization.GetText(null)).SummaryFallbackImproveText;
        }

        return string.Join(" ", tips);
    }

    public static IReadOnlyList<string> BuildUsefulPhrases(Feedback? latestFeedback, AppLocalizedText? localizedText = null)
    {
        if (latestFeedback is null)
        {
            return (localizedText ?? AppLocalization.GetText(null)).SummaryFallbackUsefulPhrases;
        }

        var phrases = new List<string>();

        AddPhraseIfValid(phrases, latestFeedback.NaturalVersion);
        AddPhraseIfValid(phrases, latestFeedback.CorrectedVersion);

        if (phrases.Count == 0)
        {
            return (localizedText ?? AppLocalization.GetText(null)).SummaryFallbackUsefulPhrases;
        }

        return phrases;
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
