using Google.Apis.AndroidPublisher.v3;

namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlayAndroidPublisherServiceFactory
{
    Task<AndroidPublisherService> CreateAsync(CancellationToken cancellationToken);
}
