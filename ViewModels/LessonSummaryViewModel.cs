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

    public string GoodText => AppConstants.MockSummaryGoodText;

    public string ImproveText => AppConstants.MockSummaryImproveText;

    public string UsefulPhrasesTitle => AppConstants.MockUsefulPhrasesTitle;

    public string ChooseAnotherSituationText => AppConstants.ChooseAnotherSituationText;

    public string BackToTopicsText => AppConstants.BackToTopicsText;

    public ObservableCollection<string> UsefulPhrases { get; } =
    [
        "Could you help me, please?",
        "I would like to...",
        "Could you repeat that, please?",
        "That sounds good to me."
    ];

    public LessonSummaryViewModel(
        string selectedLevel,
        Topic selectedTopic,
        Subtopic selectedSubtopic,
        Action navigateToSubtopics,
        Action navigateToHome)
    {
        SelectedLevel = selectedLevel;
        SelectedTopic = selectedTopic;
        SelectedSubtopic = selectedSubtopic;
        this.navigateToSubtopics = navigateToSubtopics;
        this.navigateToHome = navigateToHome;
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
}
