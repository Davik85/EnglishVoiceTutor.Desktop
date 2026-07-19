using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class ProgressServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EmptyAccountReturnsStableZeroProgressAndThirtyFiveDailyDates()
    {
        await using var db = CreateDbContext();

        var progress = await CreateService(db, Guid.NewGuid()).GetProgressAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, progress.CompletedLessons.AllTime);
        Assert.Equal(0, progress.CompletedLessons.Last7Days);
        Assert.Equal(0, progress.CompletedLessons.Last30Days);
        Assert.Equal(0, progress.Streaks.CurrentDays);
        Assert.Equal(0, progress.Streaks.LongestDays);
        Assert.Null(progress.LastCompletedLesson);
        Assert.Empty(progress.CompletedLessonsByStudyLanguage);
        Assert.Empty(progress.CompletedLessonsByLevel);
        Assert.Equal(35, progress.DailyActivity.Count);
        Assert.All(progress.DailyActivity, item => Assert.Equal(0, item.CompletedLessons));
        Assert.Equal(new DateOnly(2026, 6, 15), progress.DailyActivity.First().ActivityDate);
        Assert.Equal(new DateOnly(2026, 7, 19), progress.DailyActivity.Last().ActivityDate);
    }

    [Fact]
    public async Task OnlyOwnedFinishedSessionsWithFinishedAtQualify()
    {
        await using var db = CreateDbContext();
        var owner = Guid.NewGuid();
        var foreign = Guid.NewGuid();
        await SeedUserAsync(db, owner);
        await SeedUserAsync(db, foreign);
        await SeedSessionAsync(db, owner, LessonSessionConstants.FinishedStatus, Now.AddDays(-1));
        await SeedSessionAsync(db, foreign, LessonSessionConstants.FinishedStatus, Now);
        await SeedSessionAsync(db, owner, LessonSessionConstants.ActiveStatus, Now);
        await SeedSessionAsync(db, owner, LessonSessionConstants.AbandonedStatus, Now);
        await SeedSessionAsync(db, owner, LessonSessionConstants.ReleasedStatus, Now);
        await SeedSessionAsync(db, owner, LessonSessionConstants.CanceledStatus, Now);
        await SeedSessionAsync(db, owner, LessonSessionConstants.FinishedStatus, null);

        var progress = await CreateService(db, owner).GetProgressAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, progress.CompletedLessons.AllTime);
        Assert.Equal(1, progress.CompletedLessons.Last7Days);
        Assert.Equal(1, progress.CompletedLessons.Last30Days);
        Assert.Equal(1, progress.Streaks.CurrentDays);
        Assert.Equal(1, progress.Streaks.LongestDays);
    }

    [Fact]
    public async Task CalendarWindowsIncludeExactEarliestBoundariesAndExcludePreviousDays()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-6));
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-7));
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-29));
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-30));

        var progress = await CreateService(db, userId).GetProgressAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, progress.CompletedLessons.Last7Days);
        Assert.Equal(3, progress.CompletedLessons.Last30Days);
        Assert.Equal(4, progress.CompletedLessons.AllTime);
    }

    [Fact]
    public async Task StreaksUseDistinctUtcDatesAndCurrentStreakMayEndYesterday()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-1));
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-1).AddHours(3));
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-2));
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-3));
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-7));
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-8));
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-9));
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-10));

        var progress = await CreateService(db, userId).GetProgressAsync(TestContext.Current.CancellationToken);

        Assert.Equal(8, progress.CompletedLessons.AllTime);
        Assert.Equal(3, progress.Streaks.CurrentDays);
        Assert.Equal(4, progress.Streaks.LongestDays);
    }

    [Fact]
    public async Task OldActivityDoesNotBecomeCurrentStreakAndLatestLessonUsesFinishedAt()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-3), topicTitle: "Earlier finish", startedAt: Now);
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, AtStartOfDay(-2), topicTitle: "Latest finish", startedAt: Now.AddDays(-10));

        var progress = await CreateService(db, userId).GetProgressAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, progress.Streaks.CurrentDays);
        Assert.Equal(2, progress.Streaks.LongestDays);
        Assert.Equal("Latest finish", progress.LastCompletedLesson!.TopicTitle);
        Assert.Equal(AtStartOfDay(-2), progress.LastCompletedLesson.CompletedAtUtc);
    }

    [Fact]
    public async Task DistributionsTrimAndExcludeBlankLegacyValuesWhileTotalsAndActivityRetainThem()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, Now, studyLanguage: " Spanish ", level: " A1 ");
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, Now, studyLanguage: " ", level: "");
        await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, Now, studyLanguage: "French", level: "B1");

        var progress = await CreateService(db, userId).GetProgressAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, progress.CompletedLessons.AllTime);
        Assert.Equal(["French", "Spanish"], progress.CompletedLessonsByStudyLanguage.Select(item => item.StudyLanguage));
        Assert.Equal(["A1", "B1"], progress.CompletedLessonsByLevel.Select(item => item.Level));
        Assert.Equal(3, progress.DailyActivity.Last().CompletedLessons);
    }

    [Fact]
    public async Task AllTimeCountsMoreThanTheFiftyItemHistoryLimit()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        for (var index = 0; index < 51; index++)
        {
            await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus, Now.AddDays(-index));
        }

        var progress = await CreateService(db, userId).GetProgressAsync(TestContext.Current.CancellationToken);

        Assert.Equal(51, progress.CompletedLessons.AllTime);
    }

    private static ProgressService CreateService(AppDbContext db, Guid userId) =>
        new(db, new FakeRequestUserResolver(userId), new FakeUtcClock(Now));

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private static Task SeedUserAsync(AppDbContext db, Guid userId)
    {
        db.Users.Add(new UserEntity { Id = userId, Email = $"{userId:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = Now });
        return db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Task SeedSessionAsync(
        AppDbContext db,
        Guid userId,
        string status,
        DateTimeOffset? finishedAt,
        string studyLanguage = "English",
        string level = "A1",
        string topicTitle = "Topic",
        DateTimeOffset? startedAt = null)
    {
        db.LessonSessions.Add(new LessonSessionEntity
        {
            Id = Guid.NewGuid(), UserId = userId, LessonContentId = "lesson", StudyLanguage = studyLanguage,
            TopicId = "topic", TopicTitle = topicTitle, SubtopicId = "subtopic", SubtopicTitle = "Subtopic",
            Level = level, ModeUsed = LessonSessionConstants.TextMode, Status = status,
            StartedAt = startedAt ?? finishedAt ?? Now, FinishedAt = finishedAt, LastHeartbeatAtUtc = finishedAt,
            CreatedAt = startedAt ?? finishedAt ?? Now, UpdatedAt = Now
        });
        return db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static DateTimeOffset AtStartOfDay(int dayOffset) =>
        new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero).AddDays(dayOffset);

    private sealed class FakeRequestUserResolver(Guid userId) : IRequestUserResolver
    {
        public ResolvedRequestUser ResolveCurrentUser() => new(userId, RequestUserResolver.AuthenticatedSource);
    }

    private sealed class FakeUtcClock(DateTimeOffset now) : IUtcClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
