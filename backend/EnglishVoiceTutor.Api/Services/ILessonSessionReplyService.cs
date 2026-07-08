using EnglishVoiceTutor.Api.Contracts.LessonSessions;

namespace EnglishVoiceTutor.Api.Services;

public interface ILessonSessionReplyService
{
    Task<LessonSessionReplyResult> CreateReplyAsync(
        Guid sessionId,
        LessonSessionReplyRequest request,
        CancellationToken cancellationToken);
}
