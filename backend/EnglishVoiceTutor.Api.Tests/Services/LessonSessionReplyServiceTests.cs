using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.LessonSessions;
using EnglishVoiceTutor.Api.Contracts.Usage;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Usage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class LessonSessionReplyServiceTests
{
    [Fact]
    public async Task BlankMessageTextThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<LessonSessionReplyValidationException>(() =>
            service.CreateReplyAsync(Guid.NewGuid(), new LessonSessionReplyRequest("  "), CancellationToken.None));

        Assert.Equal(ApiConstants.EmptyLessonSessionReplyMessageError, exception.Message);
    }

    [Fact]
    public async Task MissingOwnedSessionThrowsKeyNotFound()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateReplyAsync(Guid.NewGuid(), new LessonSessionReplyRequest("hello"), CancellationToken.None));
    }

    [Fact]
    public async Task SessionOwnedByAnotherUserThrowsKeyNotFound()
    {
        await using var dbContext = CreateDbContext();
        var ownerUserId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var session = await SeedSessionAsync(dbContext, ownerUserId, LessonSessionConstants.ActiveStatus);
        var service = CreateService(dbContext, currentUserId);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateReplyAsync(session.Id, new LessonSessionReplyRequest("hello"), CancellationToken.None));
    }

    [Fact]
    public async Task InactiveOwnedSessionThrowsEndedElsewhere()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var session = await SeedSessionAsync(dbContext, userId, LessonSessionConstants.FinishedStatus);
        var service = CreateService(dbContext, userId);

        var exception = await Assert.ThrowsAsync<LessonSessionEndedElsewhereException>(() =>
            service.CreateReplyAsync(session.Id, new LessonSessionReplyRequest("hello"), CancellationToken.None));

        Assert.Equal(session.Id, exception.SessionId);
        Assert.Equal(LessonSessionConstants.FinishedStatus, exception.Status);
    }

    [Fact]
    public async Task ActiveOwnedSessionReturnsSafeNotImplementedResult()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var session = await SeedSessionAsync(dbContext, userId, LessonSessionConstants.ActiveStatus);
        var service = CreateService(dbContext, userId);

        var result = await service.CreateReplyAsync(session.Id, new LessonSessionReplyRequest("hello"), CancellationToken.None);

        Assert.Equal(LessonSessionReplyResultStatus.NotImplemented, result.Status);
        Assert.NotNull(result.UnavailableResponse);
        Assert.Null(result.FreeLimitExceededResponse);
        Assert.Equal(session.Id, result.UnavailableResponse!.SessionId);
        Assert.Equal(LessonSessionReplyUnavailableResponse.ErrorCodeValue, result.UnavailableResponse.Error);
        Assert.Equal(LessonSessionReplyUnavailableResponse.ErrorCodeValue, result.UnavailableResponse.ErrorCode);
        Assert.Equal(LessonSessionReplyUnavailableResponse.ErrorCodeValue, result.UnavailableResponse.Code);
        Assert.Equal(LessonSessionReplyUnavailableResponse.UserMessage, result.UnavailableResponse.Message);
    }

    [Fact]
    public async Task FreeLimitExceededReturnsRateLimitResult()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var session = await SeedSessionAsync(dbContext, userId, LessonSessionConstants.ActiveStatus);
        var limitResponse = new FreeLimitExceededResponse
        {
            Error = "limit_reached",
            Operation = UsageConstants.Operations.LessonChatReply,
            PlanId = "free",
            LimitType = "chat_replies",
            Used = 20,
            Limit = 20,
            Remaining = 0,
            UsageDate = new DateOnly(2026, 7, 8),
            StudyLanguage = session.StudyLanguage,
            CheckedAtUtc = DateTimeOffset.UtcNow,
            Source = "authenticated"
        };

        var service = CreateService(dbContext, userId, new FakeFreeLimitGuardService(limitResponse));

        var result = await service.CreateReplyAsync(session.Id, new LessonSessionReplyRequest("hello"), CancellationToken.None);

        Assert.Equal(LessonSessionReplyResultStatus.FreeLimitExceeded, result.Status);
        Assert.Same(limitResponse, result.FreeLimitExceededResponse);
        Assert.Null(result.UnavailableResponse);
    }

    [Fact]
    public void SafeUnavailableResponseDoesNotExposeProviderOrPromptDetails()
    {
        var response = new LessonSessionReplyUnavailableResponse
        {
            SessionId = Guid.NewGuid()
        };

        Assert.DoesNotContain("OpenAI", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prompt", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("internal", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static LessonSessionReplyService CreateService(
        AppDbContext dbContext,
        Guid userId,
        IFreeLimitGuardService? freeLimitGuardService = null)
    {
        return new LessonSessionReplyService(
            dbContext,
            new FakeRequestUserResolver(userId),
            freeLimitGuardService ?? new FakeFreeLimitGuardService(),
            NullLogger<LessonSessionReplyService>.Instance);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<LessonSessionEntity> SeedSessionAsync(AppDbContext dbContext, Guid userId, string status)
    {
        dbContext.Users.Add(new UserEntity
        {
            Id = userId,
            Email = $"{userId:N}@example.test",
            PasswordHash = "hash",
            Status = "active",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var session = new LessonSessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LessonContentId = "lesson-content-id",
            StudyLanguage = "English",
            TopicId = "topic-id",
            TopicTitle = "Topic",
            SubtopicId = "subtopic-id",
            SubtopicTitle = "Subtopic",
            Level = "A1",
            SelectedContextId = "context-id",
            SelectedContextTitle = "Context",
            ModeUsed = LessonSessionConstants.TextMode,
            Status = status,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            LastHeartbeatAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            ValidTurnCount = 0,
            EstimatedCost = 0m,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        dbContext.LessonSessions.Add(session);
        await dbContext.SaveChangesAsync();
        return session;
    }

    private sealed class FakeRequestUserResolver(Guid userId) : IRequestUserResolver
    {
        public ResolvedRequestUser ResolveCurrentUser() => new(userId, RequestUserResolver.AuthenticatedSource);
    }

    private sealed class FakeFreeLimitGuardService(FreeLimitExceededResponse? response = null) : IFreeLimitGuardService
    {
        public Task<FreeLimitExceededResponse?> CheckChatReplyLimitAsync(string? studyLanguage, CancellationToken cancellationToken) =>
            Task.FromResult(response);

        public Task<FreeLimitExceededResponse?> CheckHintLimitAsync(string? studyLanguage, CancellationToken cancellationToken) =>
            Task.FromResult<FreeLimitExceededResponse?>(null);

        public Task<FreeLimitExceededResponse?> CheckTranscriptionLimitAsync(string? studyLanguage, CancellationToken cancellationToken) =>
            Task.FromResult<FreeLimitExceededResponse?>(null);

        public Task<FreeLimitExceededResponse?> CheckTtsLimitAsync(string? studyLanguage, CancellationToken cancellationToken) =>
            Task.FromResult<FreeLimitExceededResponse?>(null);
    }
}
