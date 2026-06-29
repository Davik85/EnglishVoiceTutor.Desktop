using System.Net;

namespace EnglishVoiceTutor.Api.Services;

public sealed class OpenAiProviderRequestException : InvalidOperationException
{
    public OpenAiProviderRequestException(string message, HttpStatusCode statusCode, string safeCategory, string safeProviderMessage)
        : base(message)
    {
        StatusCode = statusCode;
        SafeCategory = safeCategory;
        SafeProviderMessage = safeProviderMessage;
    }

    public HttpStatusCode StatusCode { get; }
    public string SafeCategory { get; }
    public string SafeProviderMessage { get; }
}
