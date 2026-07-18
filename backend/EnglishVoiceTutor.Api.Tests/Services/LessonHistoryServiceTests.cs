using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.LessonHistory;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class LessonHistoryServiceTests
{
    [Fact]
    public async Task GetRecentLessonHistoryAsyncReturnsOnlyCurrentUsersSessionsNewestFirst()
    {
        await using var db = CreateDbContext();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await SeedUserAsync(db, userA);
        await SeedUserAsync(db, userB);

        var olderSession = await SeedSessionAsync(db, userA, "older", new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero));
        var newerSession = await SeedSessionAsync(db, userA, "newer", new DateTimeOffset(2026, 7, 2, 9, 0, 0, TimeSpan.Zero));
        var otherUserSession = await SeedSessionAsync(db, userB, "other-user", new DateTimeOffset(2026, 7, 3, 9, 0, 0, TimeSpan.Zero));

        var history = await CreateService(db, userA).GetRecentLessonHistoryAsync(TestContext.Current.CancellationToken);

        Assert.IsType<LessonHistoryListResponse>(history);
        Assert.Equal([newerSession.Id, olderSession.Id], history.Items.Select(item => item.SessionId));
        Assert.DoesNotContain(history.Items, item => item.SessionId == otherUserSession.Id);
        Assert.All(history.Items, item => Assert.Contains(item.SessionId, new[] { olderSession.Id, newerSession.Id }));
        Assert.All(history.Items, item => Assert.IsType<LessonHistoryItemResponse>(item));
    }

    [Fact]
    public async Task GetLessonHistoryDetailAsyncReturnsOwnedSessionWithSummaryMessagesAndFeedback()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        var session = await SeedSessionAsync(db, userId, "owned", new DateTimeOffset(2026, 7, 2, 9, 0, 0, TimeSpan.Zero));
        var message = new LessonMessageEntity
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = LessonMessageConstants.User,
            Text = "Hello",
            Source = "text",
            TurnNumber = 1,
            IsValidLessonTurn = true,
            StudyLanguage = session.StudyLanguage,
            CreatedAt = session.StartedAt.AddMinutes(1)
        };
        db.LessonMessages.Add(message);
        db.LessonSummaries.Add(new LessonSummaryEntity
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Summary = "Good start.",
            Strengths = "Clear greeting",
            CreatedAt = session.StartedAt.AddMinutes(2),
            UpdatedAt = session.StartedAt.AddMinutes(2)
        });
        db.FeedbackResults.Add(new FeedbackResultEntity
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            MessageId = message.Id,
            FeedbackType = "grammar",
            Explanation = "Use a complete sentence.",
            CreatedAt = session.StartedAt.AddMinutes(3)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var detail = await CreateService(db, userId).GetLessonHistoryDetailAsync(session.Id, TestContext.Current.CancellationToken);

        Assert.IsType<LessonHistoryDetailResponse>(detail);
        Assert.NotNull(detail);
        Assert.Equal(session.Id, detail!.SessionId);
        Assert.Equal(userId, detail.UserId);
        Assert.Equal("Good start.", detail.Summary?.Summary);
        Assert.Single(detail.Messages);
        Assert.Equal(message.Id, detail.Messages[0].Id);
        Assert.Single(detail.FeedbackResults);
        Assert.Equal(message.Id, detail.FeedbackResults[0].MessageId);
    }

    [Fact]
    public async Task GetLessonHistoryDetailAsyncReturnsNullForForeignAndUnknownSessions()
    {
        await using var db = CreateDbContext();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await SeedUserAsync(db, userA);
        await SeedUserAsync(db, userB);
        var foreignSession = await SeedSessionAsync(db, userB, "foreign", new DateTimeOffset(2026, 7, 2, 9, 0, 0, TimeSpan.Zero));
        var service = CreateService(db, userA);

        Assert.Null(await service.GetLessonHistoryDetailAsync(foreignSession.Id, TestContext.Current.CancellationToken));
        Assert.Null(await service.GetLessonHistoryDetailAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
    }

    private static LessonHistoryService CreateService(AppDbContext db, Guid userId) => new(db, new FakeRequestUserResolver(userId));

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private static async Task SeedUserAsync(AppDbContext db, Guid userId)
    {
        db.Users.Add(new UserEntity
        {
            Id = userId,
            Email = $"{userId:N}@example.test",
            PasswordHash = "hash",
            Status = "active",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<LessonSessionEntity> SeedSessionAsync(AppDbContext db, Guid userId, string lessonContentId, DateTimeOffset startedAt)
    {
        var session = new LessonSessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LessonContentId = lessonContentId,
            StudyLanguage = "English",
            TopicId = "topic",
            TopicTitle = "Topic",
            SubtopicId = "subtopic",
            SubtopicTitle = "Subtopic",
            Level = "A1",
            ModeUsed = LessonSessionConstants.TextMode,
            Status = LessonSessionConstants.FinishedStatus,
            StartedAt = startedAt,
            FinishedAt = startedAt.AddMinutes(5),
            LastHeartbeatAtUtc = startedAt.AddMinutes(5),
            ValidTurnCount = 1,
            EstimatedCost = 0m,
            CreatedAt = startedAt,
            UpdatedAt = startedAt.AddMinutes(5)
        };
        db.LessonSessions.Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return session;
    }

    private sealed class FakeRequestUserResolver(Guid userId) : IRequestUserResolver
    {
        public ResolvedRequestUser ResolveCurrentUser() => new(userId, RequestUserResolver.AuthenticatedSource);
    }
}
