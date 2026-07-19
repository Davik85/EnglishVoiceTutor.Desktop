namespace EnglishVoiceTutor.Api.Services;

public sealed record AchievementDefinition(
    string Id,
    string Category,
    string Scope,
    string Title,
    string Description,
    string IconKey,
    int TargetProgress,
    string? TopicId = null,
    string? LessonContentId = null,
    IReadOnlyList<string>? RequiredLessonContentIds = null);

public static class AchievementDefinitionCatalog
{
    public static readonly IReadOnlyList<AchievementDefinition> All = Create();

    private static IReadOnlyList<AchievementDefinition> Create()
    {
        var definitions = new List<AchievementDefinition>
        {
            Streak(7), Streak(30), Streak(60), Streak(100), Streak(365),
            Lesson(1, "First Step"), Lesson(5, "Getting Started"), Lesson(10, "10 Lessons Strong"),
            Lesson(25, "Steady Learner"), Lesson(50, "50 Lessons Strong"), Lesson(100, "Century Club")
        };

        AddTopic(definitions, "daily-life", "Everyday Hero", "Finish every Daily Life lesson.", "topic-daily-life",
            [
                ("everyday_english_introductions", "First Hello", "Complete the Introductions lesson.", "daily-life-introductions"),
                ("everyday_english_small_talk_with_a_neighbor", "Neighbor Chat", "Complete the Small talk with a neighbor lesson.", "daily-life-neighbor"),
                ("everyday_english_asking_for_help", "Helpful Hand", "Complete the Asking for help lesson.", "daily-life-help"),
                ("everyday_english_making_plans", "Plan Maker", "Complete the Making plans lesson.", "daily-life-plans"),
                ("everyday_english_talking_about_your_day", "Day Teller", "Complete the Talking about your day lesson.", "daily-life-day")
            ]);
        AddTopic(definitions, "travel", "Traveler", "Finish every Travel lesson.", "topic-travel",
            [
                ("travel_airport_check_in", "Airport Expert", "Complete the Airport check-in lesson.", "travel-airport"),
                ("travel_hotel_check_in", "Honored Guest", "Complete the Hotel check-in lesson.", "travel-hotel"),
                ("travel_asking_for_directions", "City Navigator", "Complete the Asking for directions lesson.", "travel-directions"),
                ("travel_ordering_transport", "Ride Ready", "Complete the Ordering transport lesson.", "travel-transport"),
                ("travel_lost_luggage", "Baggage Finder", "Complete the Lost luggage lesson.", "travel-luggage")
            ]);
        AddTopic(definitions, "work-business", "Business Ready", "Finish every Work & Business lesson.", "topic-work-business",
            [
                ("work_business_first_meeting", "Meeting Ready", "Complete the First meeting lesson.", "work-business-meeting"),
                ("work_business_daily_standup", "Standup Star", "Complete the Daily standup lesson.", "work-business-standup"),
                ("work_business_phone_call_with_a_client", "Client Caller", "Complete the Phone call with a client lesson.", "work-business-client-call"),
                ("work_business_asking_for_clarification", "Clear Communicator", "Complete the Asking for clarification lesson.", "work-business-clarification"),
                ("work_business_discussing_deadlines", "Deadline Driver", "Complete the Discussing deadlines lesson.", "work-business-deadlines")
            ]);
        AddTopic(definitions, "job-interview", "Interview Ready", "Finish every Job Interview lesson.", "topic-job-interview",
            [
                ("job_interview_tell_me_about_yourself", "Strong Introduction", "Complete the Tell me about yourself lesson.", "job-interview-introduction"),
                ("job_interview_work_experience", "Career Story", "Complete the Work experience lesson.", "job-interview-experience"),
                ("job_interview_strengths_and_weaknesses", "Self-Aware Candidate", "Complete the Strengths and weaknesses lesson.", "job-interview-strengths"),
                ("job_interview_why_do_you_want_this_job", "Right Fit", "Complete the Why do you want this job? lesson.", "job-interview-fit"),
                ("job_interview_asking_questions_at_the_end", "Curious Candidate", "Complete the Asking questions at the end lesson.", "job-interview-questions")
            ]);
        AddTopic(definitions, "restaurant-cafe", "Dining Pro", "Finish every Restaurant & Cafe lesson.", "topic-restaurant-cafe",
            [
                ("restaurant_and_cafe_booking_a_table", "Table Booker", "Complete the Booking a table lesson.", "restaurant-cafe-booking"),
                ("restaurant_and_cafe_ordering_food", "Menu Expert", "Complete the Ordering food lesson.", "restaurant-cafe-ordering"),
                ("restaurant_and_cafe_asking_about_ingredients", "Ingredient Guide", "Complete the Asking about ingredients lesson.", "restaurant-cafe-ingredients"),
                ("restaurant_and_cafe_handling_a_wrong_order", "Order Fixer", "Complete the Handling a wrong order lesson.", "restaurant-cafe-wrong-order"),
                ("restaurant_and_cafe_paying_the_bill", "Bill Settled", "Complete the Paying the bill lesson.", "restaurant-cafe-bill")
            ]);
        return definitions;
    }

    private static AchievementDefinition Streak(int days) => new($"streak-{days}-v1", "streak", "account", $"{days}-Day Streak", $"Practice for {days} days in a row.", "streak", days);
    private static AchievementDefinition Lesson(int count, string title) => new($"lessons-{count}-v1", "lesson", "account", title, $"Complete {count} lessons.", "lesson-milestone", count);

    private static void AddTopic(List<AchievementDefinition> definitions, string topicId, string title, string description, string iconKey, (string Id, string Title, string Description, string IconKey)[] scenarios)
    {
        foreach (var scenario in scenarios)
        {
            definitions.Add(new AchievementDefinition($"subtopic-{topicId}-{scenario.Id}-v1", "subtopic", "studyLanguage", scenario.Title, scenario.Description, scenario.IconKey, 1, topicId, scenario.Id));
        }
        definitions.Add(new AchievementDefinition($"topic-{topicId}-complete-v1", "topic", "studyLanguage", title, description, iconKey, scenarios.Length, topicId, null, scenarios.Select(scenario => scenario.Id).ToArray()));
    }
}
