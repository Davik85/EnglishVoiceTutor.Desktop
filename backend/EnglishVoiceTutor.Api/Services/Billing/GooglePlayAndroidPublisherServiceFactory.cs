using Google.Apis.AndroidPublisher.v3;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayAndroidPublisherServiceFactory : IGooglePlayAndroidPublisherServiceFactory
{
    public async Task<AndroidPublisherService> CreateAsync(CancellationToken cancellationToken)
    {
        var credential = await GoogleCredential.GetApplicationDefaultAsync(cancellationToken);
        var scopedCredential = credential.CreateScoped(AndroidPublisherService.Scope.Androidpublisher);
        return new AndroidPublisherService(new BaseClientService.Initializer
        {
            HttpClientInitializer = scopedCredential,
            ApplicationName = "Language Voice Tutor Backend"
        });
    }
}
