using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Localization;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class SubtopicsViewModel : ViewModelBase
{
    private readonly Action navigateBack;
    private readonly Action<Subtopic> navigateToLessonChat;
    private readonly AppLocalizedText localizedText;

    public string SelectedLevel { get; }

    public Topic SelectedTopic { get; }

    public string Title => string.Format(localizedText.SubtopicsTitleTemplate, SelectedTopic.DisplayTitle);

    public string Subtitle => localizedText.SubtopicsSubtitle;

    public string CurrentLevelText => $"{localizedText.CurrentLevelLabel} {SelectedLevel}";

    public string TopicText => $"{localizedText.TopicLabel} {SelectedTopic.DisplayTitle}";

    public string BackButtonText => localizedText.BackButtonText;

    public string StartLessonButtonText => localizedText.StartLessonButtonText;

    public IReadOnlyList<Subtopic> Subtopics { get; }

    [ObservableProperty]
    private Subtopic? selectedSubtopic;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public SubtopicsViewModel(
        AppLocalizedText localizedText,
        string selectedLevel,
        Topic selectedTopic,
        Action navigateBack,
        Action<Subtopic> navigateToLessonChat)
    {
        this.localizedText = localizedText;
        SelectedLevel = selectedLevel;
        SelectedTopic = selectedTopic;
        this.navigateBack = navigateBack;
        this.navigateToLessonChat = navigateToLessonChat;
        Subtopics = CreateSubtopicsForTopic(selectedTopic.Id, localizedText.LanguageId);
    }

    [RelayCommand]
    private void SelectSubtopic(Subtopic subtopic)
    {
        SelectedSubtopic = subtopic;
        StatusMessage = $"{localizedText.SelectedSituationPrefix} {subtopic.DisplayTitle}";
    }

    [RelayCommand]
    private void StartLesson()
    {
        if (SelectedSubtopic is null)
        {
            StatusMessage = localizedText.NoSubtopicSelectedMessage;
            return;
        }

        StatusMessage = string.Empty;
        navigateToLessonChat(SelectedSubtopic);
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }

    private static IReadOnlyList<Subtopic> CreateSubtopicsForTopic(int topicId, string interfaceLanguageId)
    {
        IReadOnlyList<Subtopic> canonicalSubtopics = topicId switch
        {
            1 =>
            [
                new Subtopic(101, 1, "Introductions", "Introduce yourself and ask basic personal questions."),
                new Subtopic(102, 1, "Small talk with a neighbor", "Have a friendly short conversation near home."),
                new Subtopic(103, 1, "Asking for help", "Ask someone for help in a simple everyday situation."),
                new Subtopic(104, 1, "Making plans", "Plan an activity and agree on time and place."),
                new Subtopic(105, 1, "Talking about your day", "Describe your day and daily routine.")
            ],
            2 =>
            [
                new Subtopic(201, 2, "Airport check-in", "Check in for a flight and confirm travel details."),
                new Subtopic(202, 2, "Hotel check-in", "Check in at a hotel and ask common questions."),
                new Subtopic(203, 2, "Asking for directions", "Ask for and understand directions in a new city."),
                new Subtopic(204, 2, "Ordering transport", "Arrange a taxi or rideshare to your destination."),
                new Subtopic(205, 2, "Lost luggage", "Report lost baggage and explain your situation.")
            ],
            3 =>
            [
                new Subtopic(301, 3, "First meeting", "Introduce yourself in a new work meeting."),
                new Subtopic(302, 3, "Daily standup", "Give a short update about your tasks."),
                new Subtopic(303, 3, "Phone call with a client", "Handle a polite and clear business call."),
                new Subtopic(304, 3, "Asking for clarification", "Ask follow-up questions to confirm requirements."),
                new Subtopic(305, 3, "Discussing deadlines", "Talk about timelines and delivery expectations.")
            ],
            4 =>
            [
                new Subtopic(401, 4, "Tell me about yourself", "Give a short and clear self-introduction."),
                new Subtopic(402, 4, "Your strengths", "Describe your strongest skills with examples."),
                new Subtopic(403, 4, "Your weaknesses", "Explain a weakness and how you improve it."),
                new Subtopic(404, 4, "Previous experience", "Summarize your past roles and achievements."),
                new Subtopic(405, 4, "Salary expectations", "Discuss salary politely and professionally.")
            ],
            5 =>
            [
                new Subtopic(501, 5, "Booking a table", "Call or speak to reserve a table."),
                new Subtopic(502, 5, "Ordering food", "Order a meal and ask simple menu questions."),
                new Subtopic(503, 5, "Asking about ingredients", "Ask about allergies and dish ingredients."),
                new Subtopic(504, 5, "Handling a wrong order", "Politely explain an issue with your order."),
                new Subtopic(505, 5, "Paying the bill", "Ask for the check and complete payment.")
            ],
            _ => []
        };

        return canonicalSubtopics
            .Select(subtopic =>
            {
                var displayText = AppLocalization.GetSubtopicDisplayText(interfaceLanguageId, subtopic.Title, subtopic.Description);
                return subtopic with { DisplayTitle = displayText.Title, DisplayDescription = displayText.Description };
            })
            .ToArray();
    }
}
