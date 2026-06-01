using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.LessonMessages;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using Microsoft.Extensions.Logging;

namespace EnglishVoiceTutor.Api.Services;

public sealed class LessonMessageService(AppDbContext dbContext, IRequestUserResolver requestUserResolver, IFreeLessonConsumptionService freeLessonConsumptionService, ILogger<LessonMessageService> logger) : ILessonMessageService
{
    private const decimal MinTranscriptConfidence = 0m;
    private const decimal MaxTranscriptConfidence = 1m;
    private const int MinAudioDurationMs = 0;

    public async Task<LessonMessageResponse> CreateLessonMessageAsync(Guid sessionId, CreateLessonMessageRequest request, CancellationToken cancellationToken)
    {
        ValidateCreateRequest(request);

        var userId = requestUserResolver.ResolveCurrentUser().UserId;
        var session = await dbContext.LessonSessions
            .SingleOrDefaultAsync(existing => existing.Id == sessionId && existing.UserId == userId, cancellationToken);

        if (session is null)
        {
            throw new KeyNotFoundException($"Lesson session '{sessionId}' was not found for the current user.");
        }

        if (!LessonSessionConstants.IsActiveStatus(session.Status))
        {
            throw new LessonSessionEndedElsewhereException(session.Id, session.Status);
        }

        var now = DateTimeOffset.UtcNow;
        var message = new LessonMessageEntity
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = LessonMessageConstants.ToCanonicalRole(request.Role),
            Text = request.Text.Trim(),
            Source = LessonMessageConstants.ToCanonicalSource(request.Source),
            TurnNumber = request.TurnNumber,
            IsValidLessonTurn = request.IsValidLessonTurn,
            StudyLanguage = StudyLanguageConstants.ToCanonicalValue(request.StudyLanguage),
            TranscriptConfidence = request.TranscriptConfidence,
            AudioDurationMs = request.AudioDurationMs,
            CreatedAt = now
        };

        dbContext.LessonMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (message.Role == LessonMessageConstants.User && message.IsValidLessonTurn)
        {
            try
            {
                await freeLessonConsumptionService.TryRecordConsumptionAsync(
                    message.SessionId,
                    userId,
                    message.StudyLanguage,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception,
                    "Free lesson consumption tracking failed for SessionId={SessionId}, UserId={UserId}.",
                    message.SessionId,
                    userId);
            }
        }

        return ToResponse(message);
    }

    public async Task<LessonMessageListResponse> GetLessonMessagesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = requestUserResolver.ResolveCurrentUser().UserId;
        var sessionExists = await dbContext.LessonSessions
            .AnyAsync(session => session.Id == sessionId && session.UserId == userId, cancellationToken);

        if (!sessionExists)
        {
            throw new KeyNotFoundException($"Lesson session '{sessionId}' was not found for the dev user.");
        }

        var items = await dbContext.LessonMessages
            .Where(message => message.SessionId == sessionId)
            .OrderBy(message => message.TurnNumber)
            .ThenBy(message => message.CreatedAt)
            .Select(message => ToResponse(message))
            .ToListAsync(cancellationToken);

        return new LessonMessageListResponse(items);
    }

    private static LessonMessageResponse ToResponse(LessonMessageEntity message)
    {
        return new LessonMessageResponse(
            message.Id,
            message.SessionId,
            message.Role,
            message.Text,
            message.Source,
            message.TurnNumber,
            message.IsValidLessonTurn,
            message.StudyLanguage,
            message.TranscriptConfidence,
            message.AudioDurationMs,
            message.CreatedAt);
    }

    private static void ValidateCreateRequest(CreateLessonMessageRequest request)
    {
        if (!LessonMessageConstants.IsSupportedRole(request.Role))
        {
            throw new LessonMessageValidationException($"Role must be one of: {string.Join(", ", LessonMessageConstants.SupportedRoles)}.");
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new LessonMessageValidationException("Text is required.");
        }

        if (!LessonMessageConstants.IsSupportedSource(request.Source))
        {
            throw new LessonMessageValidationException($"Source must be one of: {string.Join(", ", LessonMessageConstants.SupportedSources)}.");
        }

        if (request.TurnNumber < LessonMessageConstants.MinTurnNumber)
        {
            throw new LessonMessageValidationException($"TurnNumber must be {LessonMessageConstants.MinTurnNumber} or greater.");
        }

        if (!StudyLanguageConstants.IsSupported(request.StudyLanguage))
        {
            throw new LessonMessageValidationException($"StudyLanguage must be one of: {string.Join(", ", StudyLanguageConstants.SupportedStudyLanguages)}.");
        }

        if (request.TranscriptConfidence is < MinTranscriptConfidence or > MaxTranscriptConfidence)
        {
            throw new LessonMessageValidationException($"TranscriptConfidence must be between {MinTranscriptConfidence} and {MaxTranscriptConfidence}.");
        }

        if (request.AudioDurationMs is < MinAudioDurationMs)
        {
            throw new LessonMessageValidationException($"AudioDurationMs must be {MinAudioDurationMs} or greater.");
        }
    }
}
