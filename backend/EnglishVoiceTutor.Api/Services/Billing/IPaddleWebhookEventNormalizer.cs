namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IPaddleWebhookEventNormalizer
{
    Task<PaddleWebhookEventNormalizationResult> NormalizeReceivedEventsAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<PaddleWebhookEventNormalizationResult> NormalizeReceivedEventAsync(
        string paddleEventId,
        CancellationToken cancellationToken);
}
