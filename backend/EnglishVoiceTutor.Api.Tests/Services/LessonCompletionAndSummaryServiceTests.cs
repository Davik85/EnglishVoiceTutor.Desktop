using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.LessonSessions;
using EnglishVoiceTutor.Api.Contracts.Subscription;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class LessonCompletionAndSummaryServiceTests
{
    [Fact]
    public async Task FinishOwnedActiveSessionCompletesOnceAndInvokesBackendGeneration()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var session = await SeedSessionAsync(db, userId, LessonSessionConstants.ActiveStatus);
        var generator = new RecordingGenerator();
        var service = CreateSessionService(db, userId, generator);

        var first = await service.FinishLessonSessionAsync(session.Id, new FinishLessonSessionRequest(1), CancellationToken.None);
        var second = await service.FinishLessonSessionAsync(session.Id, new FinishLessonSessionRequest(9), CancellationToken.None);

        Assert.Equal(LessonSessionConstants.FinishedStatus, first.Status);
        Assert.Equal(1, first.ValidTurnCount);
        Assert.Equal(first.FinishedAt, second.FinishedAt);
        Assert.Equal(1, second.ValidTurnCount);
        Assert.Equal(2, generator.SessionIds.Count);
        Assert.All(generator.SessionIds, id => Assert.Equal(session.Id, id));
    }

    [Fact]
    public async Task FinishDoesNotOperateOnAnotherUsersSession()
    {
        await using var db = CreateDbContext();
        var session = await SeedSessionAsync(db, Guid.NewGuid(), LessonSessionConstants.ActiveStatus);
        var service = CreateSessionService(db, Guid.NewGuid(), new RecordingGenerator());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.FinishLessonSessionAsync(session.Id, new FinishLessonSessionRequest(1), CancellationToken.None));
    }

    [Fact]
    public async Task AuthenticatedSummaryIsOwnerScopedAndLearnerSafe()
    {
        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var session = await SeedSessionAsync(db, ownerId, LessonSessionConstants.FinishedStatus);
        db.LessonSummaries.Add(new LessonSummaryEntity { Id = Guid.NewGuid(), SessionId = session.Id, Summary = "Good progress.", Strengths = "Clear greeting\nGood question", Improvements = "Use articles", Vocabulary = "appointment", Grammar = "a/an", NextSteps = "Practice another greeting", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var ownerService = new LessonSummaryService(db, new FakeRequestUserResolver(ownerId));
        var otherService = new LessonSummaryService(db, new FakeRequestUserResolver(Guid.NewGuid()));
        var ready = await ownerService.GetAuthenticatedLessonSummaryAsync(session.Id, CancellationToken.None);

        Assert.NotNull(ready);
        Assert.Equal(LessonSummaryConstants.ReadyStatus, ready!.Status);
        Assert.Equal("Good progress.", ready.Summary);
        Assert.Equal(["Clear greeting", "Good question"], ready.Strengths);
        Assert.Null(await otherService.GetAuthenticatedLessonSummaryAsync(session.Id, CancellationToken.None));
        Assert.DoesNotContain("provider", string.Join(' ', ready.GetType().GetProperties().Select(property => property.Name)), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prompt", string.Join(' ', ready.GetType().GetProperties().Select(property => property.Name)), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthenticatedSummaryIsSafelyUnavailableWhenNotGenerated()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var session = await SeedSessionAsync(db, userId, LessonSessionConstants.FinishedStatus);
        var service = new LessonSummaryService(db, new FakeRequestUserResolver(userId));

        var result = await service.GetAuthenticatedLessonSummaryAsync(session.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(LessonSummaryConstants.UnavailableStatus, result!.Status);
        Assert.Null(result.Summary);
        Assert.Empty(result.Strengths);
    }

    private static LessonSessionService CreateSessionService(AppDbContext db, Guid userId, ILessonSummaryGenerationService generator) =>
        new(db, new FakeRequestUserResolver(userId), new FakeAccessDecisionService(), generator, NullLogger<LessonSessionService>.Instance);

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    private static async Task<LessonSessionEntity> SeedSessionAsync(AppDbContext db, Guid userId, string status)
    {
        db.Users.Add(new UserEntity { Id = userId, Email = $"{userId:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow });
        var session = new LessonSessionEntity { Id = Guid.NewGuid(), UserId = userId, LessonContentId = "lesson-id", StudyLanguage = "English", TopicId = "topic", TopicTitle = "Topic", SubtopicId = "subtopic", SubtopicTitle = "Subtopic", Level = "A1", ModeUsed = LessonSessionConstants.TextMode, Status = status, StartedAt = DateTimeOffset.UtcNow.AddMinutes(-2), LastHeartbeatAtUtc = DateTimeOffset.UtcNow, EstimatedCost = 0m, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2), UpdatedAt = DateTimeOffset.UtcNow };
        if (status == LessonSessionConstants.FinishedStatus) session.FinishedAt = DateTimeOffset.UtcNow;
        db.LessonSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    private sealed class RecordingGenerator : ILessonSummaryGenerationService { public List<Guid> SessionIds { get; } = []; public Task TryGenerateForFinishedSessionAsync(Guid sessionId, CancellationToken cancellationToken) { SessionIds.Add(sessionId); return Task.CompletedTask; } }
    private sealed class FakeRequestUserResolver(Guid userId) : IRequestUserResolver { public ResolvedRequestUser ResolveCurrentUser() => new(userId, RequestUserResolver.AuthenticatedSource); }
    private sealed class FakeAccessDecisionService : ILessonAccessDecisionService { public Task<LessonAccessDecisionResponse> GetDecisionAsync(Guid userId, string source, CancellationToken cancellationToken) => Task.FromResult(new LessonAccessDecisionResponse()); }
}
