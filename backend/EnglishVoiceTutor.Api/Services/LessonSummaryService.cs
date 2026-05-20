using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.LessonSummaries;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services;

public sealed class LessonSummaryService(AppDbContext dbContext, DevUserProvider devUserProvider) : ILessonSummaryService
{
    public async Task<LessonSummaryResponse> UpsertDevLessonSummaryAsync(Guid sessionId, UpsertLessonSummaryRequest request, CancellationToken cancellationToken)
    {
        ValidateUpsertRequest(request);

        var session = await GetDevUserSessionAsync(sessionId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var summary = await dbContext.LessonSummaries
            .SingleOrDefaultAsync(existing => existing.SessionId == sessionId, cancellationToken);

        if (summary is null)
        {
            summary = new LessonSummaryEntity
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                CreatedAt = now
            };

            dbContext.LessonSummaries.Add(summary);
        }

        summary.WhatWentWell = request.Summary.Trim();
        summary.WhatToImprove = TrimOrNull(request.Improvements);
        summary.UsefulPhrases = MergeVocabularyAndGrammar(request.Vocabulary, request.Grammar);
        summary.MistakesToReview = TrimOrNull(request.Strengths);
        summary.NextSteps = TrimOrNull(request.NextSteps);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(summary, session);
    }

    public async Task<LessonSummaryResponse> GetDevLessonSummaryAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await GetDevUserSessionAsync(sessionId, cancellationToken);
        var summary = await dbContext.LessonSummaries.SingleOrDefaultAsync(existing => existing.SessionId == sessionId, cancellationToken);

        if (summary is null)
        {
            throw new KeyNotFoundException($"Lesson summary for session '{sessionId}' was not found for the dev user.");
        }

        return ToResponse(summary, session);
    }

    public async Task<LessonSummaryListResponse> GetRecentDevLessonSummariesAsync(CancellationToken cancellationToken)
    {
        var userId = devUserProvider.GetDevUserId();

        var items = await dbContext.LessonSummaries
            .Where(summary => summary.Session.UserId == userId)
            .OrderByDescending(summary => summary.CreatedAt)
            .Take(LessonSummaryConstants.MaxRecentSummaries)
            .Select(summary => ToResponse(summary, summary.Session))
            .ToListAsync(cancellationToken);

        return new LessonSummaryListResponse(items);
    }

    private async Task<LessonSessionEntity> GetDevUserSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = devUserProvider.GetDevUserId();
        var session = await dbContext.LessonSessions
            .SingleOrDefaultAsync(existing => existing.Id == sessionId && existing.UserId == userId, cancellationToken);

        if (session is null)
        {
            throw new KeyNotFoundException($"Lesson session '{sessionId}' was not found for the dev user.");
        }

        return session;
    }

    private static LessonSummaryResponse ToResponse(LessonSummaryEntity summary, LessonSessionEntity session)
    {
        return new LessonSummaryResponse(
            summary.Id,
            summary.SessionId,
            session.UserId,
            session.LessonContentId,
            session.StudyLanguage,
            session.TopicTitle,
            session.SubtopicTitle,
            session.Level,
            summary.WhatWentWell ?? string.Empty,
            summary.MistakesToReview,
            summary.WhatToImprove,
            summary.UsefulPhrases,
            null,
            summary.NextSteps,
            summary.CreatedAt,
            summary.CreatedAt);
    }

    private static void ValidateUpsertRequest(UpsertLessonSummaryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Summary))
        {
            throw new LessonSummaryValidationException("Summary is required.");
        }
    }

    private static string? MergeVocabularyAndGrammar(string? vocabulary, string? grammar)
    {
        var v = TrimOrNull(vocabulary);
        var g = TrimOrNull(grammar);

        return (v, g) switch
        {
            (null, null) => null,
            (not null, null) => v,
            (null, not null) => g,
            _ => $"Vocabulary: {v}\nGrammar: {g}"
        };
    }

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
