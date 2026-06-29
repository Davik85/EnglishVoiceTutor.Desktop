namespace EnglishVoiceTutor.Api.Services;

public sealed record OpenAiProviderErrorDetails(
    int? StatusCode,
    string SafeCategory,
    string? ProviderErrorType,
    string? ProviderErrorCode,
    string? ProviderErrorParam,
    string? SanitizedProviderMessage);
