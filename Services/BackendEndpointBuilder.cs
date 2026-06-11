using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.Services;

public static class BackendEndpointBuilder
{
    public static string NormalizeBaseUrl(string? backendBaseUrl)
    {
#if DEBUG
        return NormalizeBaseUrl(backendBaseUrl, BackendConstants.DefaultBackendBaseUrl);
#else
        return BackendConstants.ProductionBackendBaseUrl;
#endif
    }

    public static string NormalizeBaseUrl(string? backendBaseUrl, string fallbackBaseUrl)
    {
#if DEBUG
        var normalizedFallback = NormalizeAbsoluteHttpUrl(fallbackBaseUrl)
            ?? NormalizeAbsoluteHttpUrl(BackendConstants.DeveloperBackendBaseUrl)
            ?? BackendConstants.ProductionBackendBaseUrl;
        var normalizedUrl = NormalizeAbsoluteHttpUrl(backendBaseUrl);
        return normalizedUrl ?? normalizedFallback;
#else
        _ = backendBaseUrl;
        _ = fallbackBaseUrl;
        return BackendConstants.ProductionBackendBaseUrl;
#endif
    }

    public static string ResolveBuildDefaultBaseUrl(string? configuredBackendBaseUrl)
    {
#if DEBUG
        return NormalizeBaseUrl(configuredBackendBaseUrl, BackendConstants.DeveloperBackendBaseUrl);
#else
        _ = configuredBackendBaseUrl;
        return BackendConstants.ProductionBackendBaseUrl;
#endif
    }

    public static string ResolveSavedBaseUrlForCurrentBuild(string? savedBackendBaseUrl)
    {
#if DEBUG
        return NormalizeBaseUrl(savedBackendBaseUrl, BackendConstants.DefaultBackendBaseUrl);
#else
        _ = savedBackendBaseUrl;
        return BackendConstants.ProductionBackendBaseUrl;
#endif
    }

    public static bool IsProductionBackendUrl(string? backendBaseUrl)
    {
        var normalizedUrl = NormalizeAbsoluteHttpUrl(backendBaseUrl);
        var normalizedProductionUrl = NormalizeAbsoluteHttpUrl(BackendConstants.ProductionBackendBaseUrl);
        return string.Equals(normalizedUrl, normalizedProductionUrl, StringComparison.OrdinalIgnoreCase);
    }

    public static bool WouldIgnoreUnsafeReleaseOverride(string? backendBaseUrl)
    {
#if DEBUG
        _ = backendBaseUrl;
        return false;
#else
        var normalizedUrl = NormalizeAbsoluteHttpUrl(backendBaseUrl);
        return normalizedUrl is not null && !IsProductionBackendUrl(normalizedUrl);
#endif
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

        var builder = new UriBuilder(uri)
        {
            Path = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty
        };

        return builder.Uri.ToString().TrimEnd('/');
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
