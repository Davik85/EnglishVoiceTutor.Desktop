namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed record PaddleWebhookEventNormalizationResult(
    int CheckedCount,
    int NormalizedCount,
    int AlreadyNormalizedCount,
    int FailedCount,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc);
