using System.Net;
using System.Text.Json;

namespace EnglishVoiceTutor.Api.Services;

public static class OpenAiProviderErrorMapper
{
    private const int MaxSanitizedProviderMessageLength = 500;

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

    public static OpenAiProviderErrorDetails MapProviderError(HttpStatusCode statusCode, string? responseBody)
    {
        var providerType = ExtractErrorString(responseBody, "type");
        var providerCode = ExtractErrorString(responseBody, "code");
        var providerParam = ExtractErrorString(responseBody, "param");
        var providerMessage = SanitizeProviderMessage(ExtractErrorString(responseBody, "message"));
        var category = MapStatusCode(statusCode, providerMessage);
        return new OpenAiProviderErrorDetails((int)statusCode, category, providerType, providerCode, providerParam, providerMessage);
    }

    public static string? SanitizeProviderMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var sanitized = new string(message.Where(ch => !char.IsControl(ch)).ToArray()).Trim();
        return sanitized.Length <= MaxSanitizedProviderMessageLength
            ? sanitized
            : sanitized[..MaxSanitizedProviderMessageLength];
    }

    private static string? ExtractErrorString(string? responseBody, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("error", out var error) || error.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return error.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

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
