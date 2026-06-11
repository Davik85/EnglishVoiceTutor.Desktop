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
        var normalizedUrl = NormalizeAbsoluteHttpUrl(backendBaseUrl);

        if (normalizedUrl is not null && IsProductionBackendUrl(normalizedFallback) && IsUnsafeReleaseOverride(normalizedUrl))
        {
            return normalizedFallback;
        }

        return normalizedUrl ?? normalizedFallback;
    }

    public static string ResolveSavedBaseUrlForCurrentBuild(string? savedBackendBaseUrl)
    {
        return NormalizeBaseUrl(savedBackendBaseUrl, BackendConstants.DefaultBackendBaseUrl);
    }

    public static bool IsProductionBackendUrl(string? backendBaseUrl)
    {
        var normalizedUrl = NormalizeAbsoluteHttpUrl(backendBaseUrl);
        var normalizedProductionUrl = NormalizeAbsoluteHttpUrl(BackendConstants.ProductionBackendBaseUrl);
        return string.Equals(normalizedUrl, normalizedProductionUrl, StringComparison.OrdinalIgnoreCase);
    }

    public static bool WouldIgnoreUnsafeReleaseOverride(string? backendBaseUrl)
    {
        var normalizedBuildDefault = NormalizeAbsoluteHttpUrl(BackendConstants.DefaultBackendBaseUrl);
        var normalizedUrl = NormalizeAbsoluteHttpUrl(backendBaseUrl);

        return normalizedUrl is not null
            && IsProductionBackendUrl(normalizedBuildDefault)
            && IsUnsafeReleaseOverride(normalizedUrl);
    }

    private static bool IsUnsafeReleaseOverride(string normalizedBackendBaseUrl)
    {
        if (!Uri.TryCreate(normalizedBackendBaseUrl, UriKind.Absolute, out var uri))
        {
            return true;
        }

        if (uri.IsLoopback)
        {
            return true;
        }

        if (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !IsProductionBackendUrl(normalizedBackendBaseUrl))
        {
            return true;
        }

        var host = uri.Host.Trim('[', ']');
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
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
