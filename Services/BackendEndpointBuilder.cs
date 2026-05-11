using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.Services;

public static class BackendEndpointBuilder
{
    public static string NormalizeBaseUrl(string? backendBaseUrl)
    {
        var trimmedUrl = backendBaseUrl?.Trim().TrimEnd('/');

        if (string.IsNullOrWhiteSpace(trimmedUrl))
        {
            return BackendConstants.DefaultBackendBaseUrl;
        }

        if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri))
        {
            return BackendConstants.DefaultBackendBaseUrl;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return BackendConstants.DefaultBackendBaseUrl;
        }

        return uri.ToString().TrimEnd('/');
    }

    public static Uri BuildEndpointUri(string? backendBaseUrl, string endpointPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointPath);

        var normalizedBaseUrl = NormalizeBaseUrl(backendBaseUrl);
        var normalizedEndpointPath = endpointPath.TrimStart('/');
        var baseUri = new Uri($"{normalizedBaseUrl}/", UriKind.Absolute);

        return new Uri(baseUri, normalizedEndpointPath);
    }
}
