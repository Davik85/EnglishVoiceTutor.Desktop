using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IPaddleWebhookSignatureVerifier
{
    PaddleWebhookSignatureVerificationResult Verify(
        string rawBody,
        string? signatureHeader,
        string secretKey,
        DateTimeOffset nowUtc,
        TimeSpan timestampTolerance);
}

public sealed class PaddleWebhookSignatureVerifier : IPaddleWebhookSignatureVerifier
{
    public PaddleWebhookSignatureVerificationResult Verify(
        string rawBody,
        string? signatureHeader,
        string secretKey,
        DateTimeOffset nowUtc,
        TimeSpan timestampTolerance)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return PaddleWebhookSignatureVerificationResult.Invalid("missing_signature", "Paddle-Signature header is required.");
        }

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return PaddleWebhookSignatureVerificationResult.Invalid("missing_secret", "Paddle webhook secret is not configured.");
        }

        var components = ParseSignatureComponents(signatureHeader);
        if (!components.TryGetValue("ts", out var timestampValue) || string.IsNullOrWhiteSpace(timestampValue))
        {
            return PaddleWebhookSignatureVerificationResult.Invalid("missing_timestamp", "Paddle-Signature timestamp is required.");
        }

        if (!long.TryParse(timestampValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixTimestamp))
        {
            return PaddleWebhookSignatureVerificationResult.Invalid("invalid_timestamp", "Paddle-Signature timestamp is invalid.");
        }

        DateTimeOffset timestampUtc;
        try
        {
            timestampUtc = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            return PaddleWebhookSignatureVerificationResult.Invalid("invalid_timestamp", "Paddle-Signature timestamp is out of range.");
        }

        var age = (nowUtc - timestampUtc).Duration();
        if (age > timestampTolerance)
        {
            return PaddleWebhookSignatureVerificationResult.Invalid("stale_timestamp", "Paddle-Signature timestamp is outside the allowed tolerance.", timestampUtc);
        }

        if (!components.TryGetValue("h1", out var expectedSignature) || string.IsNullOrWhiteSpace(expectedSignature))
        {
            return PaddleWebhookSignatureVerificationResult.Invalid("missing_h1", "Paddle-Signature h1 value is required.", timestampUtc);
        }

        if (!TryDecodeHex(expectedSignature, out var expectedSignatureBytes))
        {
            return PaddleWebhookSignatureVerificationResult.Invalid("invalid_h1", "Paddle-Signature h1 value is invalid.", timestampUtc);
        }

        var signedPayload = string.Concat(unixTimestamp.ToString(CultureInfo.InvariantCulture), ":", rawBody);
        var secretBytes = Encoding.UTF8.GetBytes(secretKey);
        var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);
        var computedSignatureBytes = HMACSHA256.HashData(secretBytes, payloadBytes);

        if (!CryptographicOperations.FixedTimeEquals(computedSignatureBytes, expectedSignatureBytes))
        {
            return PaddleWebhookSignatureVerificationResult.Invalid("invalid_signature", "Paddle webhook signature is invalid.", timestampUtc);
        }

        return PaddleWebhookSignatureVerificationResult.Valid(timestampUtc);
    }

    private static Dictionary<string, string> ParseSignatureComponents(string signatureHeader)
    {
        var components = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in signatureHeader.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = component.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0 || separatorIndex == component.Length - 1)
            {
                continue;
            }

            var key = component[..separatorIndex].Trim();
            var value = component[(separatorIndex + 1)..].Trim();
            components[key] = value;
        }

        return components;
    }

    private static bool TryDecodeHex(string hex, out byte[] bytes)
    {
        bytes = [];
        if (hex.Length % 2 != 0)
        {
            return false;
        }

        try
        {
            bytes = Convert.FromHexString(hex);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed record PaddleWebhookSignatureVerificationResult(
    bool IsValid,
    string ErrorCode,
    string Message,
    DateTimeOffset? Timestamp)
{
    public static PaddleWebhookSignatureVerificationResult Valid(DateTimeOffset timestamp) =>
        new(true, string.Empty, string.Empty, timestamp);

    public static PaddleWebhookSignatureVerificationResult Invalid(string errorCode, string message, DateTimeOffset? timestamp = null) =>
        new(false, errorCode, message, timestamp);
}
