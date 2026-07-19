using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Usage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class AchievementsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EmptyAccountReturnsLockedDefinitionsAndThreeHomeItems()
    {
        await using var db = CreateDbContext();
        var user = Guid.NewGuid();
        await SeedUserAsync(db, user, "English");

        var result = await CreateService(db, user).GetAchievementsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(41, result.Summary.Total);
        Assert.Equal(0, result.Summary.Unlocked);
        Assert.Equal(41, result.Achievements.Count);
        Assert.Equal(3, result.HomeItems.Count);
        Assert.All(result.Achievements, achievement => Assert.False(achievement.Unlocked));
    }

    [Fact]
    public async Task UsesOnlyOwnedFinishedSessionsAndAllHistoryForAccountMilestones()
    {
        await using var db = CreateDbContext();
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        await SeedUserAsync(db, owner, "English");
        await SeedUserAsync(db, other, "English");
        for (var index = 0; index < 51; index++) await SeedSessionAsync(db, owner, Now.AddDays(-index), "other", "French");
        await SeedSessionAsync(db, other, Now, "travel_airport_check_in", "English");
        await SeedSessionAsync(db, owner, null, "travel_airport_check_in", "English", LessonSessionConstants.FinishedStatus);
        await SeedSessionAsync(db, owner, Now, "travel_airport_check_in", "English", LessonSessionConstants.ActiveStatus);

        var result = await CreateService(db, owner).GetAchievementsAsync(TestContext.Current.CancellationToken);

        var lessons50 = result.Achievements.Single(item => item.Id == "lessons-50-v1");
        Assert.True(lessons50.Unlocked);
        Assert.Equal(50, lessons50.CurrentProgress);
        Assert.Equal(Now.AddDays(-1), lessons50.UnlockedAtUtc);
        Assert.False(result.Achievements.Single(item => item.Id == "subtopic-travel-travel_airport_check_in-v1").Unlocked);
    }

    [Fact]
    public async Task SubtopicsUseLessonContentIdAndTopicsRequireEachFrozenScenario()
    {
        await using var db = CreateDbContext();
        var user = Guid.NewGuid();
        await SeedUserAsync(db, user, "en");
        await SeedSessionAsync(db, user, Now.AddDays(-4), "travel_airport_check_in", "English", topicId: "wrong", subtopicId: "wrong", title: "wrong");
        await SeedSessionAsync(db, user, Now.AddDays(-3), "travel_airport_check_in", "English");
        await SeedSessionAsync(db, user, Now.AddDays(-2), "travel_hotel_check_in", "English");
        await SeedSessionAsync(db, user, Now.AddDays(-1), "travel_asking_for_directions", "English");
        await SeedSessionAsync(db, user, Now, "travel_ordering_transport", "English");

        var incomplete = await CreateService(db, user).GetAchievementsAsync(TestContext.Current.CancellationToken);
        var airport = incomplete.Achievements.Single(item => item.Id == "subtopic-travel-travel_airport_check_in-v1");
        Assert.True(airport.Unlocked);
        Assert.Equal(Now.AddDays(-4), airport.UnlockedAtUtc);
        Assert.False(incomplete.Achievements.Single(item => item.Id == "topic-travel-complete-v1").Unlocked);

        await SeedSessionAsync(db, user, Now.AddHours(1), "travel_lost_luggage", "English");
        var complete = await CreateService(db, user).GetAchievementsAsync(TestContext.Current.CancellationToken);
        var topic = complete.Achievements.Single(item => item.Id == "topic-travel-complete-v1");
        Assert.True(topic.Unlocked);
        Assert.Equal(Now.AddHours(1), topic.UnlockedAtUtc);
        Assert.DoesNotContain(complete.Achievements, item => item.LessonContentId == "free_conversation_open_conversation");
    }

    [Fact]
    public async Task HistoricalUtcStreakRemainsUnlockedAndRecentUnlocksLeadHome()
    {
        await using var db = CreateDbContext();
        var user = Guid.NewGuid();
        await SeedUserAsync(db, user, "English");
        for (var index = 20; index >= 14; index--) await SeedSessionAsync(db, user, Now.Date.AddDays(-index).AddHours(23), "other", "French");
        await SeedSessionAsync(db, user, Now, "travel_airport_check_in", "English");

        var result = await CreateService(db, user).GetAchievementsAsync(TestContext.Current.CancellationToken);

        var streak = result.Achievements.Single(item => item.Id == "streak-7-v1");
        Assert.True(streak.Unlocked);
        Assert.Equal(7, streak.CurrentProgress);
        Assert.Equal(Now.Date.AddDays(-14).AddHours(23), streak.UnlockedAtUtc);
        Assert.Equal("subtopic-travel-travel_airport_check_in-v1", result.HomeItems.First().Id);
    }

    [Fact]
    public async Task BlankActiveLanguageReturnsOnlyAccountAchievements()
    {
        await using var db = CreateDbContext();
        var user = Guid.NewGuid();
        await SeedUserAsync(db, user, " ");

        var result = await CreateService(db, user).GetAchievementsAsync(TestContext.Current.CancellationToken);

        Assert.Null(result.ActiveStudyLanguage);
        Assert.Equal(11, result.Achievements.Count);
        Assert.All(result.Achievements, item => Assert.Equal("account", item.Scope));
    }

    private static AchievementsService CreateService(AppDbContext db, Guid userId) => new(db, new FakeRequestUserResolver(userId), new UsageStudyLanguageNormalizer(), new FakeUtcClock(Now));

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    private static async Task SeedUserAsync(AppDbContext db, Guid userId, string language)
    {
        db.Users.Add(new UserEntity { Id = userId, Email = $"{userId:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = Now });
        db.UserSettings.Add(new UserSettingsEntity { Id = Guid.NewGuid(), UserId = userId, StudyLanguage = language, ExplanationLanguage = "English", SpeechVoice = "lana", SpeechSpeed = 1, CreatedAt = Now, UpdatedAt = Now });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Task SeedSessionAsync(AppDbContext db, Guid userId, DateTimeOffset? finishedAt, string lessonContentId, string language, string status = LessonSessionConstants.FinishedStatus, string topicId = "topic", string subtopicId = "subtopic", string title = "Title")
    {
        db.LessonSessions.Add(new LessonSessionEntity { Id = Guid.NewGuid(), UserId = userId, LessonContentId = lessonContentId, StudyLanguage = language, TopicId = topicId, TopicTitle = title, SubtopicId = subtopicId, SubtopicTitle = title, Level = "A1", ModeUsed = LessonSessionConstants.TextMode, Status = status, StartedAt = finishedAt ?? Now, FinishedAt = finishedAt, LastHeartbeatAtUtc = finishedAt, CreatedAt = finishedAt ?? Now, UpdatedAt = Now });
        return db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed class FakeRequestUserResolver(Guid userId) : IRequestUserResolver { public ResolvedRequestUser ResolveCurrentUser() => new(userId, RequestUserResolver.AuthenticatedSource); }
    private sealed class FakeUtcClock(DateTimeOffset now) : IUtcClock { public DateTimeOffset UtcNow => now; }
}
