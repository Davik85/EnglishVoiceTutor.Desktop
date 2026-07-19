using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Achievements;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Usage;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services;

public sealed class AchievementsService(
    AppDbContext dbContext,
    IRequestUserResolver requestUserResolver,
    UsageStudyLanguageNormalizer studyLanguageNormalizer,
    IUtcClock utcClock) : IAchievementsService
{
    private const string CalendarTimezone = "UTC";

    public async Task<AchievementsResponse> GetAchievementsAsync(CancellationToken cancellationToken)
    {
        var userId = requestUserResolver.ResolveCurrentUser().UserId;
        var activeStudyLanguage = await ResolveActiveStudyLanguageAsync(userId, cancellationToken);
        var sessions = await dbContext.LessonSessions.AsNoTracking()
            .Where(session => session.UserId == userId)
            .Where(session => session.Status == LessonSessionConstants.FinishedStatus && session.FinishedAt != null)
            .Select(session => new CompletedSession(session.Id, session.FinishedAt!.Value, session.StudyLanguage, session.LessonContentId))
            .ToListAsync(cancellationToken);

        var accountSessions = sessions.OrderBy(session => session.FinishedAt).ThenBy(session => session.Id).ToArray();
        var languageSessions = activeStudyLanguage is null
            ? []
            : accountSessions.Where(session => IsInLanguage(session.StudyLanguage, activeStudyLanguage)).ToArray();
        var streaks = AnalyzeStreaks(accountSessions);
        var achievements = new List<AchievementResponse>();

        foreach (var definition in AchievementDefinitionCatalog.All)
        {
            if (definition.Scope == "account")
            {
                achievements.Add(CreateAccountAchievement(definition, accountSessions, streaks));
            }
            else if (activeStudyLanguage is not null)
            {
                achievements.Add(CreateLanguageAchievement(definition, activeStudyLanguage, languageSessions));
            }
        }

        var ordered = achievements.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
        var homeItems = SelectHomeItems(ordered);
        return new AchievementsResponse(utcClock.UtcNow.ToUniversalTime(), CalendarTimezone, activeStudyLanguage,
            new AchievementSummaryResponse(ordered.Count(item => item.Unlocked), ordered.Length), ordered, homeItems);
    }

    private async Task<string?> ResolveActiveStudyLanguageAsync(Guid userId, CancellationToken cancellationToken)
    {
        var value = await dbContext.UserSettings.AsNoTracking().Where(settings => settings.UserId == userId)
            .Select(settings => settings.StudyLanguage).SingleOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(value) ? null : studyLanguageNormalizer.NormalizeOrDefault(value, value.Trim());
    }

    private bool IsInLanguage(string value, string activeLanguage) =>
        string.Equals(studyLanguageNormalizer.NormalizeOrDefault(value, value?.Trim() ?? string.Empty), activeLanguage, StringComparison.OrdinalIgnoreCase);

    private static AchievementResponse CreateAccountAchievement(AchievementDefinition definition, IReadOnlyList<CompletedSession> sessions, StreakAnalysis streaks)
    {
        if (definition.Category == "lesson")
        {
            var unlocked = sessions.Count >= definition.TargetProgress;
            return ToResponse(definition, null, unlocked, unlocked ? sessions[definition.TargetProgress - 1].FinishedAt : null, Math.Min(sessions.Count, definition.TargetProgress));
        }
        DateTimeOffset? unlockedAt = streaks.FirstReachedAt.TryGetValue(definition.TargetProgress, out var reachedAt) ? reachedAt : null;
        return ToResponse(definition, null, unlockedAt is not null, unlockedAt, Math.Min(streaks.LongestDays, definition.TargetProgress));
    }

    private static AchievementResponse CreateLanguageAchievement(AchievementDefinition definition, string language, IReadOnlyList<CompletedSession> sessions)
    {
        if (definition.Category == "subtopic")
        {
            var match = sessions.Where(session => string.Equals(session.LessonContentId?.Trim(), definition.LessonContentId, StringComparison.Ordinal))
                .OrderBy(session => session.FinishedAt).ThenBy(session => session.Id).FirstOrDefault();
            return ToResponse(definition, language, match is not null, match?.FinishedAt, match is null ? 0 : 1);
        }
        var firstByContent = sessions.Where(session => !string.IsNullOrWhiteSpace(session.LessonContentId))
            .GroupBy(session => session.LessonContentId.Trim(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(session => session.FinishedAt).ThenBy(session => session.Id).First(), StringComparer.Ordinal);
        var required = definition.RequiredLessonContentIds!;
        var completed = required.Where(firstByContent.ContainsKey).ToArray();
        var unlocked = completed.Length == required.Count;
        DateTimeOffset? unlockedAt = unlocked ? completed.Select(id => firstByContent[id].FinishedAt).Max() : null;
        return ToResponse(definition, language, unlocked, unlockedAt, completed.Length);
    }

    private static AchievementResponse ToResponse(AchievementDefinition definition, string? language, bool unlocked, DateTimeOffset? unlockedAt, int currentProgress) =>
        new(definition.Id, definition.Category, definition.Scope, language, definition.TopicId, definition.LessonContentId, definition.Title, definition.Description, definition.IconKey,
            unlocked, unlocked ? unlockedAt : null, Math.Min(Math.Max(currentProgress, 0), definition.TargetProgress), definition.TargetProgress);

    private static IReadOnlyList<AchievementResponse> SelectHomeItems(IReadOnlyList<AchievementResponse> achievements)
    {
        var selected = achievements.Where(item => item.Unlocked).OrderByDescending(item => item.UnlockedAtUtc).ThenBy(item => item.Id, StringComparer.Ordinal).Take(3).ToList();
        if (selected.Count < 3)
        {
            selected.AddRange(achievements.Where(item => !item.Unlocked)
                .OrderBy(item => (double)(item.TargetProgress - item.CurrentProgress) / item.TargetProgress)
                .ThenBy(item => CategoryOrder(item.Category)).ThenBy(item => item.Id, StringComparer.Ordinal).Take(3 - selected.Count));
        }
        return selected;
    }

    private static int CategoryOrder(string category) => category switch { "streak" => 0, "lesson" => 1, "subtopic" => 2, "topic" => 3, _ => 4 };

    private static StreakAnalysis AnalyzeStreaks(IReadOnlyList<CompletedSession> sessions)
    {
        var grouped = sessions.GroupBy(session => DateOnly.FromDateTime(session.FinishedAt.UtcDateTime)).OrderBy(group => group.Key).ToArray();
        var longest = 0;
        var current = 0;
        DateOnly? previous = null;
        var reached = new Dictionary<int, DateTimeOffset>();
        var thresholds = new[] { 7, 30, 60, 100, 365 };
        foreach (var group in grouped)
        {
            current = previous is not null && group.Key == previous.Value.AddDays(1) ? current + 1 : 1;
            longest = Math.Max(longest, current);
            var firstAt = group.Min(session => session.FinishedAt);
            foreach (var threshold in thresholds.Where(threshold => current >= threshold && !reached.ContainsKey(threshold))) reached[threshold] = firstAt;
            previous = group.Key;
        }
        return new StreakAnalysis(longest, reached);
    }

    private sealed record CompletedSession(Guid Id, DateTimeOffset FinishedAt, string StudyLanguage, string LessonContentId);
    private sealed record StreakAnalysis(int LongestDays, IReadOnlyDictionary<int, DateTimeOffset> FirstReachedAt);
}
