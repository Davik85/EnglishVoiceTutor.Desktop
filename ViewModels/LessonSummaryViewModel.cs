using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class LessonSummaryViewModel : ViewModelBase
{
    private readonly Action navigateToSubtopics;
    private readonly Action navigateToHome;

    public string SelectedLevel { get; }

    public Topic SelectedTopic { get; }

    public Subtopic SelectedSubtopic { get; }

    public string Title => AppConstants.LessonSummaryTitle;

    public string ContextText => $"Topic: {SelectedTopic.Title} • Situation: {SelectedSubtopic.Title} • Level: {SelectedLevel}";

    public string GoodText { get; }

    public string ImproveText { get; }

    public string UsefulPhrasesTitle => AppConstants.UsefulPhrasesTitle;

    public string ChooseAnotherSituationText => AppConstants.ChooseAnotherSituationText;

    public string BackToTopicsText => AppConstants.BackToTopicsText;

    public ObservableCollection<string> UsefulPhrases { get; }

    public LessonSummaryViewModel(
        string selectedLevel,
        Topic selectedTopic,
        Subtopic selectedSubtopic,
        Feedback? latestFeedback,
        Action navigateToSubtopics,
        Action navigateToHome)
    {
        SelectedLevel = selectedLevel;
        SelectedTopic = selectedTopic;
        SelectedSubtopic = selectedSubtopic;
        this.navigateToSubtopics = navigateToSubtopics;
        this.navigateToHome = navigateToHome;

        GoodText = BuildGoodText(latestFeedback);
        ImproveText = BuildImproveText(latestFeedback);
        UsefulPhrases = BuildUsefulPhrases(latestFeedback);
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

    private static string BuildGoodText(Feedback? latestFeedback)
    {
        if (!string.IsNullOrWhiteSpace(latestFeedback?.ShortText))
        {
            return latestFeedback.ShortText;
        }

        return AppConstants.SummaryFallbackGoodText;
    }

    private static string BuildImproveText(Feedback? latestFeedback)
    {
        if (latestFeedback is null)
        {
            return AppConstants.SummaryFallbackImproveText;
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
            return AppConstants.SummaryFallbackImproveText;
        }

        return string.Join(" ", tips);
    }

    private static ObservableCollection<string> BuildUsefulPhrases(Feedback? latestFeedback)
    {
        if (latestFeedback is null)
        {
            return new ObservableCollection<string>(AppConstants.SummaryFallbackUsefulPhrases);
        }

        var phrases = new List<string>();

        AddPhraseIfValid(phrases, latestFeedback.NaturalVersion);
        AddPhraseIfValid(phrases, latestFeedback.CorrectedVersion);

        if (phrases.Count == 0)
        {
            return new ObservableCollection<string>(AppConstants.SummaryFallbackUsefulPhrases);
        }

        return new ObservableCollection<string>(phrases);
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
