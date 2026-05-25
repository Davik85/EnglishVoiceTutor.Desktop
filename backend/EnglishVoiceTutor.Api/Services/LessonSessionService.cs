using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.LessonSessions;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;

using EnglishVoiceTutor.Api.Services.Auth;

namespace EnglishVoiceTutor.Api.Services;

public sealed class LessonSessionService(
    AppDbContext dbContext,
    IRequestUserResolver requestUserResolver,
    ILessonAccessDecisionService lessonAccessDecisionService,
    ILogger<LessonSessionService> logger) : ILessonSessionService
{
    private const string DefaultUserEmail = "dev-user@local.test";
    private const string DefaultUserPasswordHash = "temporary-dev-user-no-password-login";
    private const string DefaultUserStatus = "active";
    private const int MinValidTurnCount = 0;

    public async Task<LessonSessionResponse> StartLessonSessionAsync(StartLessonSessionRequest request, CancellationToken cancellationToken)
    {
        ValidateStartRequest(request);

        var resolvedUser = requestUserResolver.ResolveCurrentUser();
        var user = await EnsureUserExistsAsync(resolvedUser.UserId, cancellationToken);
        var lessonAccessSource = ResolveLessonAccessSource(resolvedUser.Source);
        var lessonAccessDecision = await lessonAccessDecisionService.GetDecisionAsync(user.Id, lessonAccessSource, cancellationToken);

        logger.LogInformation(
            "Lesson session start access dry-run: Source={Source}; CanStartNewLesson={CanStartNewLesson}; Decision={Decision}; Reason={Reason}; EnforcementEnabled={EnforcementEnabled}; FreeLessonRemainingToday={FreeLessonRemainingToday}; FreeLessonUsedToday={FreeLessonUsedToday}; PremiumActive={PremiumActive}; TrialActive={TrialActive}.",
            lessonAccessDecision.Source,
            lessonAccessDecision.CanStartNewLesson,
            lessonAccessDecision.Decision,
            lessonAccessDecision.Reason,
            lessonAccessDecision.EnforcementEnabled,
            lessonAccessDecision.FreeLessonRemainingToday,
            lessonAccessDecision.FreeLessonUsedToday,
            lessonAccessDecision.PremiumActive,
            lessonAccessDecision.TrialActive);

        // Lesson access is dry-run only until enforcement is explicitly enabled.
        var now = DateTimeOffset.UtcNow;

        var session = new LessonSessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            LessonContentId = request.LessonContentId.Trim(),
            StudyLanguage = StudyLanguageConstants.ToCanonicalValue(request.StudyLanguage),
            TopicId = request.TopicId.Trim(),
            TopicTitle = request.TopicTitle.Trim(),
            SubtopicId = request.SubtopicId.Trim(),
            SubtopicTitle = request.SubtopicTitle.Trim(),
            Level = request.Level.Trim(),
            SelectedContextId = string.IsNullOrWhiteSpace(request.SelectedContextId) ? null : request.SelectedContextId.Trim(),
            SelectedContextTitle = string.IsNullOrWhiteSpace(request.SelectedContextTitle) ? null : request.SelectedContextTitle.Trim(),
            ModeUsed = LessonSessionConstants.ToCanonicalMode(request.ModeUsed),
            Status = LessonSessionConstants.ActiveStatus,
            StartedAt = now,
            FinishedAt = null,
            ValidTurnCount = MinValidTurnCount,
            EstimatedCost = LessonSessionConstants.DefaultEstimatedCost,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.LessonSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(session);
    }

    public async Task<LessonSessionResponse> FinishLessonSessionAsync(Guid sessionId, FinishLessonSessionRequest request, CancellationToken cancellationToken)
    {
        ValidateFinishRequest(request);

        var userId = requestUserResolver.ResolveCurrentUser().UserId;
        var session = await dbContext.LessonSessions
            .SingleOrDefaultAsync(existing => existing.Id == sessionId && existing.UserId == userId, cancellationToken);

        if (session is null)
        {
            throw new KeyNotFoundException($"Lesson session '{sessionId}' was not found for the dev user.");
        }

        var now = DateTimeOffset.UtcNow;
        session.Status = LessonSessionConstants.FinishedStatus;
        session.FinishedAt = now;
        session.ValidTurnCount = request.ValidTurnCount;
        session.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(session);
    }

    public async Task<LessonSessionListResponse> GetRecentLessonSessionsAsync(CancellationToken cancellationToken)
    {
        var userId = requestUserResolver.ResolveCurrentUser().UserId;

        var sessions = await dbContext.LessonSessions
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.StartedAt)
            .ThenByDescending(session => session.CreatedAt)
            .Take(LessonSessionConstants.MaxRecentSessions)
            .Select(session => ToResponse(session))
            .ToListAsync(cancellationToken);

        return new LessonSessionListResponse(sessions);
    }

    public async Task<LessonSessionResponse?> GetLessonSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = requestUserResolver.ResolveCurrentUser().UserId;

        return await dbContext.LessonSessions
            .Where(session => session.Id == sessionId && session.UserId == userId)
            .Select(session => ToResponse(session))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static string ResolveLessonAccessSource(string source)
    {
        return source switch
        {
            RequestUserResolver.AuthenticatedSource => SubscriptionConstants.LessonAccessSources.Authenticated,
            RequestUserResolver.DevelopmentSource => SubscriptionConstants.LessonAccessSources.Development,
            _ => SubscriptionConstants.LessonAccessSources.Development
        };
    }

    private async Task<UserEntity> EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(existing => existing.Id == userId, cancellationToken);

        if (user is not null)
        {
            return user;
        }

        user = new UserEntity
        {
            Id = userId,
            Email = DefaultUserEmail,
            PasswordHash = DefaultUserPasswordHash,
            Status = DefaultUserStatus,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    private static LessonSessionResponse ToResponse(LessonSessionEntity session)
    {
        return new LessonSessionResponse(
            session.Id,
            session.UserId,
            session.LessonContentId,
            session.StudyLanguage,
            session.TopicId,
            session.TopicTitle,
            session.SubtopicId,
            session.SubtopicTitle,
            session.Level,
            session.SelectedContextId,
            session.SelectedContextTitle,
            session.ModeUsed,
            session.Status,
            session.StartedAt,
            session.FinishedAt,
            session.ValidTurnCount,
            session.EstimatedCost,
            session.CreatedAt,
            session.UpdatedAt);
    }

    private static void ValidateStartRequest(StartLessonSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LessonContentId)) throw new LessonSessionValidationException("LessonContentId is required.");
        if (!StudyLanguageConstants.IsSupported(request.StudyLanguage)) throw new LessonSessionValidationException($"StudyLanguage must be one of: {string.Join(", ", StudyLanguageConstants.SupportedStudyLanguages)}.");
        if (string.IsNullOrWhiteSpace(request.TopicId)) throw new LessonSessionValidationException("TopicId is required.");
        if (string.IsNullOrWhiteSpace(request.TopicTitle)) throw new LessonSessionValidationException("TopicTitle is required.");
        if (string.IsNullOrWhiteSpace(request.SubtopicId)) throw new LessonSessionValidationException("SubtopicId is required.");
        if (string.IsNullOrWhiteSpace(request.SubtopicTitle)) throw new LessonSessionValidationException("SubtopicTitle is required.");
        if (string.IsNullOrWhiteSpace(request.Level)) throw new LessonSessionValidationException("Level is required.");
        if (!LessonSessionConstants.IsSupportedMode(request.ModeUsed)) throw new LessonSessionValidationException($"ModeUsed must be one of: {string.Join(", ", LessonSessionConstants.SupportedModes)}.");
    }

    private static void ValidateFinishRequest(FinishLessonSessionRequest request)
    {
        if (request.ValidTurnCount < MinValidTurnCount)
        {
            throw new LessonSessionValidationException($"ValidTurnCount must be {MinValidTurnCount} or greater.");
        }
    }
}
