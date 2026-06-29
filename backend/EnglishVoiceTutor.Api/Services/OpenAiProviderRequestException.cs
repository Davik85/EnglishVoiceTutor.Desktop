using System.Net;

namespace EnglishVoiceTutor.Api.Services;

public sealed class OpenAiProviderRequestException : InvalidOperationException
{
    public OpenAiProviderRequestException(
        string message,
        HttpStatusCode statusCode,
        string safeCategory,
        string safeProviderMessage,
        string? providerErrorType = null,
        string? providerErrorCode = null,
        string? providerErrorParam = null,
        string? sanitizedProviderMessage = null)
        : base(message)
    {
        StatusCode = statusCode;
        SafeCategory = safeCategory;
        SafeProviderMessage = safeProviderMessage;
        ProviderErrorType = providerErrorType;
        ProviderErrorCode = providerErrorCode;
        ProviderErrorParam = providerErrorParam;
        SanitizedProviderMessage = sanitizedProviderMessage;
    }

    public HttpStatusCode StatusCode { get; }
    public string SafeCategory { get; }
    public string SafeProviderMessage { get; }
    public string? ProviderErrorType { get; }
    public string? ProviderErrorCode { get; }
    public string? ProviderErrorParam { get; }
    public string? SanitizedProviderMessage { get; }
}
