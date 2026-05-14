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
        LessonSummaryInput summaryInput,
        Action navigateToSubtopics,
        Action navigateToHome)
    {
        this.localizedText = localizedText;
        SelectedLevel = selectedLevel;
        SelectedTopic = selectedTopic;
        SelectedSubtopic = selectedSubtopic;
        this.navigateToSubtopics = navigateToSubtopics;
        this.navigateToHome = navigateToHome;

        GoodText = BuildGoodText(summaryInput, localizedText);
        ImproveText = BuildImproveText(summaryInput, localizedText);
        UsefulPhrases = new ObservableCollection<string>(BuildUsefulPhrases(summaryInput, localizedText));
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
