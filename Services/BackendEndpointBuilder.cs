using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.Services;

public static class BackendEndpointBuilder
{
    public static string NormalizeBaseUrl(string? backendBaseUrl)
    {
        return NormalizeBaseUrl(backendBaseUrl, BackendConstants.DefaultBackendBaseUrl);
    }

    public static string NormalizeBaseUrl(string? backendBaseUrl, string fallbackBaseUrl)
    {
        var normalizedFallback = NormalizeAbsoluteHttpUrl(fallbackBaseUrl) ?? BackendConstants.LegacyLocalBackendBaseUrl;

        return NormalizeAbsoluteHttpUrl(backendBaseUrl) ?? normalizedFallback;
    }

    public static string ResolveSavedBaseUrlForCurrentBuild(string? savedBackendBaseUrl)
    {
        var normalizedBuildDefault = NormalizeBaseUrl(BackendConstants.DefaultBackendBaseUrl, BackendConstants.LegacyLocalBackendBaseUrl);
        var normalizedSavedUrl = NormalizeAbsoluteHttpUrl(savedBackendBaseUrl);

        if (normalizedSavedUrl is null)
        {
            return normalizedBuildDefault;
        }

        var normalizedLegacyLocalDefault = NormalizeBaseUrl(BackendConstants.LegacyLocalBackendBaseUrl, BackendConstants.LegacyLocalBackendBaseUrl);
        if (!string.Equals(normalizedBuildDefault, normalizedLegacyLocalDefault, StringComparison.OrdinalIgnoreCase)
            && string.Equals(normalizedSavedUrl, normalizedLegacyLocalDefault, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedBuildDefault;
        }

        return normalizedSavedUrl;
    }

    private static string? NormalizeAbsoluteHttpUrl(string? backendBaseUrl)
    {
        var trimmedUrl = backendBaseUrl?.Trim().TrimEnd('/');

        if (string.IsNullOrWhiteSpace(trimmedUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null;
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
