namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public interface IFreeLessonConsumptionService
{
    Task TryRecordConsumptionAsync(Guid sessionId, Guid userId, string studyLanguage, CancellationToken cancellationToken);
}
