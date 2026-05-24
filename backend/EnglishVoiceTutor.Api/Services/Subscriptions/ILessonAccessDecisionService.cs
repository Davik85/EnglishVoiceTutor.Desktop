using EnglishVoiceTutor.Api.Contracts.Subscription;

namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public interface ILessonAccessDecisionService
{
    Task<LessonAccessDecisionResponse> GetDecisionAsync(Guid userId, string source, CancellationToken cancellationToken);
}
