namespace EnglishVoiceTutor.Api.Contracts.Progress;

public sealed record ProgressResponse(
    DateTimeOffset GeneratedAtUtc,
    string CalendarTimezone,
    string CompletionRule,
    ProgressCompletedLessonsResponse CompletedLessons,
    ProgressStreaksResponse Streaks,
    ProgressLastCompletedLessonResponse? LastCompletedLesson,
    IReadOnlyList<ProgressStudyLanguageDistributionItemResponse> CompletedLessonsByStudyLanguage,
    IReadOnlyList<ProgressLevelDistributionItemResponse> CompletedLessonsByLevel,
    IReadOnlyList<ProgressDailyActivityItemResponse> DailyActivity);

public sealed record ProgressCompletedLessonsResponse(int AllTime, int Last7Days, int Last30Days);

public sealed record ProgressStreaksResponse(int CurrentDays, int LongestDays);

public sealed record ProgressLastCompletedLessonResponse(
    DateTimeOffset CompletedAtUtc,
    string? StudyLanguage,
    string? Level,
    string? TopicTitle,
    string? SubtopicTitle);

public sealed record ProgressStudyLanguageDistributionItemResponse(string StudyLanguage, int CompletedLessons);

public sealed record ProgressLevelDistributionItemResponse(string Level, int CompletedLessons);

public sealed record ProgressDailyActivityItemResponse(DateOnly ActivityDate, int CompletedLessons);
