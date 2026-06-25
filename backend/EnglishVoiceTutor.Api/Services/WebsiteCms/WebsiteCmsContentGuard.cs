using System.Text.RegularExpressions;

namespace EnglishVoiceTutor.Api.Services.WebsiteCms;

public static partial class WebsiteCmsContentGuard
{
    public static IReadOnlyList<string> FindBlockedSecretLikeMarkers(params string?[] values)
    {
        var matches = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            foreach (var pattern in BlockedPatterns)
            {
                if (pattern.Regex().IsMatch(value))
                {
                    matches.Add(pattern.Label);
                }
            }
        }

        return matches.ToArray();
    }

    public static void ThrowIfBlocked(params string?[] values)
    {
        var matches = FindBlockedSecretLikeMarkers(values);
        if (matches.Count > 0)
        {
            throw new InvalidOperationException($"Website CMS content contains blocked secret-like marker(s): {string.Join(", ", matches)}.");
        }
    }

    private static readonly (string Label, Func<Regex> Regex)[] BlockedPatterns =
    [
        ("paddle secret/API key", PaddleSecretRegex),
        ("webhook secret/signature", WebhookSecretRegex),
        ("API key", ApiKeyRegex),
        ("JWT key/secret", JwtSecretRegex),
        ("connection string", ConnectionStringRegex),
        ("raw provider payload", RawProviderPayloadRegex),
        ("customer id", CustomerIdRegex),
        ("transaction id", TransactionIdRegex),
        ("subscription id", SubscriptionIdRegex)
    ];

    [GeneratedRegex(@"(?i)\b(paddle[_-]?(api[_-]?)?(key|secret|token)|pdl_(live|test)_[a-z0-9_]+)\b", RegexOptions.CultureInvariant)]
    private static partial Regex PaddleSecretRegex();

    [GeneratedRegex(@"(?i)\b(webhook[_-]?(secret|signature)|paddle-signature)\b", RegexOptions.CultureInvariant)]
    private static partial Regex WebhookSecretRegex();

    [GeneratedRegex(@"(?i)\b(api[_-]?key|openai[_-]?api[_-]?key|sk-[a-z0-9_-]{12,})\b", RegexOptions.CultureInvariant)]
    private static partial Regex ApiKeyRegex();

    [GeneratedRegex(@"(?i)\b(jwt[_-]?(signing)?[_-]?(key|secret)|bearer\s+[a-z0-9._-]{20,})\b", RegexOptions.CultureInvariant)]
    private static partial Regex JwtSecretRegex();

    [GeneratedRegex(@"(?i)\b(host=|server=|user\s*id=|password=|connection\s*string|DefaultConnection)\b", RegexOptions.CultureInvariant)]
    private static partial Regex ConnectionStringRegex();

    [GeneratedRegex(@"(?i)\b(raw[_-]?payload|provider[_-]?payload|event[_-]?payload|payload_json)\b", RegexOptions.CultureInvariant)]
    private static partial Regex RawProviderPayloadRegex();

    [GeneratedRegex(@"(?i)\b(ctm|cus)_[a-z0-9]{6,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex CustomerIdRegex();

    [GeneratedRegex(@"(?i)\b(txn|transaction)_[a-z0-9]{6,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex TransactionIdRegex();

    [GeneratedRegex(@"(?i)\b(sub|subscription)_[a-z0-9]{6,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex SubscriptionIdRegex();
}
