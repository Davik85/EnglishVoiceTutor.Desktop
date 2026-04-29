using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class SubtopicsViewModel : ViewModelBase
{
    private readonly Action navigateBack;
    private const string NoSubtopicSelectedMessage = "Please choose a situation before starting the lesson.";
    private const string SelectedSituationPrefix = "Selected situation:";

    public string SelectedLevel { get; }

    public Topic SelectedTopic { get; }

    public string Title => $"{AppConstants.SubtopicsTitlePrefix} {SelectedTopic.Title}";

    public string Subtitle => AppConstants.SubtopicsSubtitle;

    public IReadOnlyList<Subtopic> Subtopics { get; }

    [ObservableProperty]
    private Subtopic? selectedSubtopic;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public SubtopicsViewModel(string selectedLevel, Topic selectedTopic, Action navigateBack)
    {
        SelectedLevel = selectedLevel;
        SelectedTopic = selectedTopic;
        this.navigateBack = navigateBack;
        Subtopics = CreateSubtopicsForTopic(selectedTopic.Id);
    }

    [RelayCommand]
    private void SelectSubtopic(Subtopic subtopic)
    {
        SelectedSubtopic = subtopic;
        StatusMessage = $"{SelectedSituationPrefix} {subtopic.Title}";
    }

    [RelayCommand]
    private void StartLesson()
    {
        StatusMessage = SelectedSubtopic is null
            ? NoSubtopicSelectedMessage
            : AppConstants.StartLessonPlaceholderMessage;
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }

    private static IReadOnlyList<Subtopic> CreateSubtopicsForTopic(int topicId)
    {
        return topicId switch
        {
            1 =>
            [
                new(101, 1, "Introductions", "Introduce yourself and ask basic personal questions."),
                new(102, 1, "Small talk with a neighbor", "Have a friendly short conversation near home."),
                new(103, 1, "Asking for help", "Ask someone for help in a simple everyday situation."),
                new(104, 1, "Making plans", "Plan an activity and agree on time and place."),
                new(105, 1, "Talking about your day", "Describe your day and daily routine.")
            ],
            2 =>
            [
                new(201, 2, "Airport check-in", "Check in for a flight and confirm travel details."),
                new(202, 2, "Hotel check-in", "Check in at a hotel and ask common questions."),
                new(203, 2, "Asking for directions", "Ask for and understand directions in a new city."),
                new(204, 2, "Ordering transport", "Arrange a taxi or rideshare to your destination."),
                new(205, 2, "Lost luggage", "Report lost baggage and explain your situation.")
            ],
            3 =>
            [
                new(301, 3, "First meeting", "Introduce yourself in a new work meeting."),
                new(302, 3, "Daily standup", "Give a short update about your tasks."),
                new(303, 3, "Phone call with a client", "Handle a polite and clear business call."),
                new(304, 3, "Asking for clarification", "Ask follow-up questions to confirm requirements."),
                new(305, 3, "Discussing deadlines", "Talk about timelines and delivery expectations.")
            ],
            4 =>
            [
                new(401, 4, "Tell me about yourself", "Give a short and clear self-introduction."),
                new(402, 4, "Your strengths", "Describe your strongest skills with examples."),
                new(403, 4, "Your weaknesses", "Explain a weakness and how you improve it."),
                new(404, 4, "Previous experience", "Summarize your past roles and achievements."),
                new(405, 4, "Salary expectations", "Discuss salary politely and professionally.")
            ],
            5 =>
            [
                new(501, 5, "Booking a table", "Call or speak to reserve a table."),
                new(502, 5, "Ordering food", "Order a meal and ask simple menu questions."),
                new(503, 5, "Asking about ingredients", "Ask about allergies and dish ingredients."),
                new(504, 5, "Handling a wrong order", "Politely explain an issue with your order."),
                new(505, 5, "Paying the bill", "Ask for the check and complete payment.")
            ],
            _ => []
        };
    }
}
