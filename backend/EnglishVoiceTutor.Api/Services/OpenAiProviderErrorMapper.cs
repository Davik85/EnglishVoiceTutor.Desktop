using System.Net;

namespace EnglishVoiceTutor.Api.Services;

public static class OpenAiProviderErrorMapper
{
    public static string MapStatusCode(HttpStatusCode? statusCode, string? safeProviderMessage = null)
    {
        if (statusCode is null)
        {
            return "unknown";
        }

        return statusCode.Value switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "unauthorized_or_forbidden",
            HttpStatusCode.NotFound => "unavailable_or_not_found",
            HttpStatusCode.TooManyRequests => ContainsQuotaSignal(safeProviderMessage) ? "quota_or_billing" : "rate_limited",
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => "invalid_request",
            HttpStatusCode.PaymentRequired => "quota_or_billing",
            >= HttpStatusCode.InternalServerError => "provider_error",
            _ => "unknown"
        };
    }

    public static string MapException(Exception exception) =>
        exception is OperationCanceledException or TimeoutException ? "timeout" : "unknown";

    private static bool ContainsQuotaSignal(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("quota", StringComparison.OrdinalIgnoreCase)
            || message.Contains("billing", StringComparison.OrdinalIgnoreCase)
            || message.Contains("insufficient", StringComparison.OrdinalIgnoreCase);
    }
}
