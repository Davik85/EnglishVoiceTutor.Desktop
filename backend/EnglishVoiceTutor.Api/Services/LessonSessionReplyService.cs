using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.LessonSessions;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services;

public sealed class LessonSessionReplyService(
    AppDbContext dbContext,
    IRequestUserResolver requestUserResolver,
    ILogger<LessonSessionReplyService> logger) : ILessonSessionReplyService
{
    public async Task<LessonSessionReplyResult> CreateReplyAsync(
        Guid sessionId,
        LessonSessionReplyRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MessageText))
        {
            throw new LessonSessionReplyValidationException(ApiConstants.EmptyLessonSessionReplyMessageError);
        }

        var userId = requestUserResolver.ResolveCurrentUser().UserId;
        var session = await dbContext.LessonSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(existing => existing.Id == sessionId && existing.UserId == userId, cancellationToken);

        if (session is null)
        {
            throw new KeyNotFoundException($"Lesson session '{sessionId}' was not found for the current user.");
        }

        if (!LessonSessionConstants.IsActiveStatus(session.Status))
        {
            throw new LessonSessionEndedElsewhereException(session.Id, session.Status);
        }

        logger.LogInformation(
            "Mobile lesson reply reached safe backend boundary. SessionId={SessionId}; LessonContentId={LessonContentId}; StudyLanguage={StudyLanguage}; Level={Level}; TopicId={TopicId}; SubtopicId={SubtopicId}; ModeUsed={ModeUsed}. Server-side lesson chat hydration is not implemented.",
            session.Id,
            session.LessonContentId,
            session.StudyLanguage,
            session.Level,
            session.TopicId,
            session.SubtopicId,
            session.ModeUsed);

        return LessonSessionReplyResult.NotImplemented(session.Id);
    }
}
