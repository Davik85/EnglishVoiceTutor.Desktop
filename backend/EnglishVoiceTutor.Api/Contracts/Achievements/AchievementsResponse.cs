namespace EnglishVoiceTutor.Api.Contracts.Achievements;

public sealed record AchievementsResponse(
    DateTimeOffset GeneratedAtUtc,
    string CalendarTimezone,
    string? ActiveStudyLanguage,
    AchievementSummaryResponse Summary,
    IReadOnlyList<AchievementResponse> Achievements,
    IReadOnlyList<AchievementResponse> HomeItems);

public sealed record AchievementSummaryResponse(int Unlocked, int Total);

public sealed record AchievementResponse(
    string Id,
    string Category,
    string Scope,
    string? StudyLanguage,
    string? TopicId,
    string? LessonContentId,
    string Title,
    string Description,
    string IconKey,
    bool Unlocked,
    DateTimeOffset? UnlockedAtUtc,
    int CurrentProgress,
    int TargetProgress);
