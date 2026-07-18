using EnglishVoiceTutor.Api.Contracts.LessonHistory;

namespace EnglishVoiceTutor.Api.Services;

public interface ILessonHistoryService
{
    Task<LessonHistoryListResponse> GetRecentLessonHistoryAsync(CancellationToken cancellationToken);
    Task<LessonHistoryDetailResponse?> GetLessonHistoryDetailAsync(Guid sessionId, CancellationToken cancellationToken);
}
