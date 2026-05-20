using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.LessonHistory;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services;

public sealed class LessonHistoryService(AppDbContext dbContext, DevUserProvider devUserProvider) : ILessonHistoryService
{
    public async Task<LessonHistoryListResponse> GetRecentDevLessonHistoryAsync(CancellationToken cancellationToken)
    {
        var userId = devUserProvider.GetDevUserId();

        var items = await dbContext.LessonSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.StartedAt)
            .Take(LessonHistoryConstants.MaxRecentHistoryItems)
            .Select(session => new LessonHistoryItemResponse(
                session.Id,
                session.LessonContentId,
                session.StudyLanguage,
                session.TopicTitle,
                session.SubtopicTitle,
                session.Level,
                session.SelectedContextTitle,
                session.ModeUsed,
                session.Status,
                session.StartedAt,
                session.FinishedAt,
                session.ValidTurnCount,
                session.EstimatedCost,
                session.Summary != null,
                session.Summary == null ? null : BuildSummaryPreview(session.Summary.Summary),
                session.Messages.Count,
                session.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new LessonHistoryListResponse(items);
    }

    public async Task<LessonHistoryDetailResponse?> GetDevLessonHistoryDetailAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = devUserProvider.GetDevUserId();

        var session = await dbContext.LessonSessions
            .AsNoTracking()
            .Where(existing => existing.Id == sessionId && existing.UserId == userId)
            .Select(existing => new
            {
                existing.Id,
                existing.UserId,
                existing.LessonContentId,
                existing.StudyLanguage,
                existing.TopicId,
                existing.TopicTitle,
                existing.SubtopicId,
                existing.SubtopicTitle,
                existing.Level,
                existing.SelectedContextId,
                existing.SelectedContextTitle,
                existing.ModeUsed,
                existing.Status,
                existing.StartedAt,
                existing.FinishedAt,
                existing.ValidTurnCount,
                existing.EstimatedCost,
                existing.CreatedAt,
                existing.UpdatedAt,
                Summary = existing.Summary == null
                    ? null
                    : new LessonHistorySummaryResponse(
                        existing.Summary.Id,
                        existing.Summary.Summary,
                        existing.Summary.Strengths,
                        existing.Summary.Improvements,
                        existing.Summary.Vocabulary,
                        existing.Summary.Grammar,
                        existing.Summary.NextSteps,
                        existing.Summary.CreatedAt,
                        existing.Summary.UpdatedAt)
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return null;
        }

        var rawMessages = await dbContext.LessonMessages
            .AsNoTracking()
            .Where(message => message.SessionId == sessionId)
            .OrderBy(message => message.TurnNumber)
            .ThenBy(message => message.CreatedAt)
            .Select(message => new
            {
                message.Id,
                message.Role,
                message.Text,
                message.Source,
                message.TurnNumber,
                message.IsValidLessonTurn,
                message.StudyLanguage,
                message.TranscriptConfidence,
                message.AudioDurationMs,
                message.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var messages = rawMessages
            .OrderBy(message => message.TurnNumber)
            .ThenBy(message => GetMessageRoleDisplayOrder(message.Role))
            .ThenBy(message => message.CreatedAt)
            .Select(message => new LessonHistoryMessageResponse(
                message.Id,
                message.Role,
                message.Text,
                message.Source,
                message.TurnNumber,
                message.IsValidLessonTurn,
                message.StudyLanguage,
                message.TranscriptConfidence,
                message.AudioDurationMs,
                message.CreatedAt))
            .ToList();

        return new LessonHistoryDetailResponse(
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
            session.UpdatedAt,
            session.Summary,
            messages);
    }

    private static int GetMessageRoleDisplayOrder(string role)
    {
        if (string.Equals(role, LessonMessageConstants.User, StringComparison.OrdinalIgnoreCase))
        {
            return LessonHistoryConstants.UserMessageDisplayOrder;
        }

        if (string.Equals(role, LessonMessageConstants.Assistant, StringComparison.OrdinalIgnoreCase))
        {
            return LessonHistoryConstants.AssistantMessageDisplayOrder;
        }

        if (string.Equals(role, LessonMessageConstants.System, StringComparison.OrdinalIgnoreCase))
        {
            return LessonHistoryConstants.SystemMessageDisplayOrder;
        }

        return LessonHistoryConstants.UnknownMessageDisplayOrder;
    }

    private static string? BuildSummaryPreview(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return null;
        }

        var normalized = summary.Trim();

        if (normalized.Length <= LessonHistoryConstants.SummaryPreviewLength)
        {
            return normalized;
        }

        return normalized[..LessonHistoryConstants.SummaryPreviewLength];
    }
}
