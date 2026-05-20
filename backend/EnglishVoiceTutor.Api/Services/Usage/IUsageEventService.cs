namespace EnglishVoiceTutor.Api.Services.Usage;

public interface IUsageEventService
{
    Task TryRecordAsync(UsageEventRecord record, CancellationToken cancellationToken = default);
}
