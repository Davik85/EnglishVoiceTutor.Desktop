using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Progress;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services;

public sealed class ProgressService(
    AppDbContext dbContext,
    IRequestUserResolver requestUserResolver,
    IUtcClock utcClock) : IProgressService
{
    public const string CalendarTimezone = "UTC";
    public const string CompletionRule = "finished_session_with_finished_at";
    private const int DailyActivityDays = 35;
    private sealed record DistributionGroup(string? Value, int CompletedLessons);

    public async Task<ProgressResponse> GetProgressAsync(CancellationToken cancellationToken)
    {
        var generatedAtUtc = utcClock.UtcNow.ToUniversalTime();
        var today = DateOnly.FromDateTime(generatedAtUtc.UtcDateTime);
        var todayStartUtc = StartOfUtcDay(today);
        var tomorrowStartUtc = todayStartUtc.AddDays(1);
        var last7StartUtc = todayStartUtc.AddDays(-6);
        var last30StartUtc = todayStartUtc.AddDays(-29);
        var dailyActivityStartUtc = todayStartUtc.AddDays(-(DailyActivityDays - 1));
        var userId = requestUserResolver.ResolveCurrentUser().UserId;

        var qualifyingSessions = dbContext.LessonSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId)
            .Where(session => session.Status == LessonSessionConstants.FinishedStatus)
            .Where(session => session.FinishedAt != null);

        var allTime = await qualifyingSessions.CountAsync(cancellationToken);
        var last7Days = await qualifyingSessions
            .Where(session => session.FinishedAt >= last7StartUtc && session.FinishedAt < tomorrowStartUtc)
            .CountAsync(cancellationToken);
        var last30Days = await qualifyingSessions
            .Where(session => session.FinishedAt >= last30StartUtc && session.FinishedAt < tomorrowStartUtc)
            .CountAsync(cancellationToken);

        var lastCompletedLesson = await qualifyingSessions
            .OrderByDescending(session => session.FinishedAt)
            .Select(session => new ProgressLastCompletedLessonResponse(
                session.FinishedAt!.Value,
                ToNullableLearnerText(session.StudyLanguage),
                ToNullableLearnerText(session.Level),
                ToNullableLearnerText(session.TopicTitle),
                ToNullableLearnerText(session.SubtopicTitle)))
            .FirstOrDefaultAsync(cancellationToken);

        var languageGroups = await qualifyingSessions
            .GroupBy(session => session.StudyLanguage)
            .Select(group => new DistributionGroup(group.Key, group.Count()))
            .ToListAsync(cancellationToken);
        var levelGroups = await qualifyingSessions
            .GroupBy(session => session.Level)
            .Select(group => new DistributionGroup(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        var completionDates = (await qualifyingSessions
                .Select(session => session.FinishedAt!.Value)
                .ToListAsync(cancellationToken))
            .Select(finishedAt => DateOnly.FromDateTime(finishedAt.UtcDateTime))
            .Distinct()
            .OrderBy(date => date)
            .ToArray();

        var recentCompletionDates = (await qualifyingSessions
                .Where(session => session.FinishedAt >= dailyActivityStartUtc && session.FinishedAt < tomorrowStartUtc)
                .Select(session => session.FinishedAt!.Value)
                .ToListAsync(cancellationToken))
            .GroupBy(finishedAt => DateOnly.FromDateTime(finishedAt.UtcDateTime))
            .ToDictionary(group => group.Key, group => group.Count());

        return new ProgressResponse(
            generatedAtUtc,
            CalendarTimezone,
            CompletionRule,
            new ProgressCompletedLessonsResponse(allTime, last7Days, last30Days),
            new ProgressStreaksResponse(GetCurrentStreak(completionDates, today), GetLongestStreak(completionDates)),
            lastCompletedLesson,
            ToStudyLanguageDistribution(languageGroups),
            ToLevelDistribution(levelGroups),
            BuildDailyActivity(today, recentCompletionDates));
    }

    private static DateTimeOffset StartOfUtcDay(DateOnly date) =>
        new(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);

    private static string? ToNullableLearnerText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyList<ProgressStudyLanguageDistributionItemResponse> ToStudyLanguageDistribution(
        IEnumerable<DistributionGroup> groups) =>
        groups
            .Select(group => new { Value = ToNullableLearnerText(group.Value), group.CompletedLessons })
            .Where(group => group.Value is not null)
            .GroupBy(group => group.Value!, StringComparer.Ordinal)
            .Select(group => new ProgressStudyLanguageDistributionItemResponse(group.Key, group.Sum(item => item.CompletedLessons)))
            .OrderBy(item => item.StudyLanguage, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<ProgressLevelDistributionItemResponse> ToLevelDistribution(
        IEnumerable<DistributionGroup> groups) =>
        groups
            .Select(group => new { Value = ToNullableLearnerText(group.Value), group.CompletedLessons })
            .Where(group => group.Value is not null)
            .GroupBy(group => group.Value!, StringComparer.Ordinal)
            .Select(group => new ProgressLevelDistributionItemResponse(group.Key, group.Sum(item => item.CompletedLessons)))
            .OrderBy(item => item.Level, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<ProgressDailyActivityItemResponse> BuildDailyActivity(
        DateOnly today,
        IReadOnlyDictionary<DateOnly, int> countsByDate)
    {
        return Enumerable.Range(0, DailyActivityDays)
            .Select(index => today.AddDays(-(DailyActivityDays - 1) + index))
            .Select(date => new ProgressDailyActivityItemResponse(
                date,
                countsByDate.GetValueOrDefault(date)))
            .ToArray();
    }

    private static int GetCurrentStreak(IReadOnlyList<DateOnly> completionDates, DateOnly today)
    {
        if (completionDates.Count == 0)
        {
            return 0;
        }

        var dates = completionDates.ToHashSet();
        var streakEnd = dates.Contains(today)
            ? today
            : dates.Contains(today.AddDays(-1)) ? today.AddDays(-1) : (DateOnly?)null;

        if (streakEnd is null)
        {
            return 0;
        }

        var streak = 0;
        for (var date = streakEnd.Value; dates.Contains(date); date = date.AddDays(-1))
        {
            streak++;
        }

        return streak;
    }

    private static int GetLongestStreak(IReadOnlyList<DateOnly> completionDates)
    {
        if (completionDates.Count == 0)
        {
            return 0;
        }

        var longest = 1;
        var current = 1;
        for (var index = 1; index < completionDates.Count; index++)
        {
            if (completionDates[index] == completionDates[index - 1].AddDays(1))
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 1;
            }
        }

        return longest;
    }
}
