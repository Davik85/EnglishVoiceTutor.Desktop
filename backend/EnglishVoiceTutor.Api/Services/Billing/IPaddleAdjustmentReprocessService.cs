namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IPaddleAdjustmentReprocessService
{
    Task<PaddleAdjustmentReprocessResult> ReprocessProviderEventAsync(string providerEventId, CancellationToken cancellationToken);
}
