using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.Services;

public static partial class BackendRequestDiagnosticsService
{
    private const int MaxSafeResponseSnippetLength = 240;
    private const string RedactedText = "[redacted]";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string LogFilePath
    {
        get
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, StorageConstants.AppDataFolderName, StorageConstants.BackendRequestDiagnosticsFileName);
        }
    }

    public static string GetBaseUrlSource(string? backendBaseUrl)
    {
#if DEBUG
        var normalizedInput = BackendEndpointBuilder.NormalizeBaseUrl(backendBaseUrl);
        var normalizedDefault = BackendEndpointBuilder.NormalizeBaseUrl(BackendConstants.DefaultBackendBaseUrl);
        return string.Equals(normalizedInput, normalizedDefault, StringComparison.OrdinalIgnoreCase)
            ? "packaged default"
            : "developer override";
#else
        _ = backendBaseUrl;
        return "packaged production server";
#endif
    }

    public static async Task RecordAsync(
        string requestName,
        HttpMethod method,
        Uri absoluteUrl,
        string? effectiveBackendBaseUrl,
        HttpStatusCode? statusCode = null,
        Exception? exception = null,
        string? responseBodySnippet = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestName);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(absoluteUrl);

        var normalizedBaseUrl = BackendEndpointBuilder.NormalizeBaseUrl(effectiveBackendBaseUrl);
        var entry = new BackendRequestDiagnosticEntry(
            DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            requestName,
            method.Method,
            absoluteUrl.AbsoluteUri,
            statusCode is null ? null : (int)statusCode.Value,
            exception?.GetType().Name,
            SanitizeSnippet(responseBodySnippet),
            normalizedBaseUrl,
            GetBaseUrlSource(effectiveBackendBaseUrl));

        try
        {
            var directoryPath = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var line = JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;
            await File.AppendAllTextAsync(LogFilePath, line, cancellationToken);
        }
        catch (Exception logException) when (logException is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Backend request diagnostics write failed: {logException.GetType().Name}.");
        }
    }

    public static async Task<string> ReadReportAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(LogFilePath))
        {
            return $"Backend request diagnostics log not found: {LogFilePath}";
        }

        return await File.ReadAllTextAsync(LogFilePath, cancellationToken);
    }

    public static string SanitizeSnippet(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var safeValue = value.ReplaceLineEndings(" ").Trim();
        safeValue = SensitiveAssignmentPattern().Replace(safeValue, match => $"{match.Groups[1].Value}{RedactedText}");
        safeValue = BearerTokenPattern().Replace(safeValue, $"Bearer {RedactedText}");
        safeValue = EmailPattern().Replace(safeValue, RedactedText);
        if (safeValue.Length > MaxSafeResponseSnippetLength)
        {
            safeValue = string.Concat(safeValue.AsSpan(0, MaxSafeResponseSnippetLength), "...");
        }

        return safeValue;
    }

    [GeneratedRegex("(?i)(password|access[_-]?token|refresh[_-]?token|authorization|secret)([\\\"'\\s:=]+)([^,}\\s]+)")]
    private static partial Regex SensitiveAssignmentPattern();

    [GeneratedRegex("(?i)Bearer\\s+[A-Za-z0-9._~+/=-]+")]
    private static partial Regex BearerTokenPattern();

    [GeneratedRegex("[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    private sealed record BackendRequestDiagnosticEntry(
        string TimestampUtc,
        string RequestName,
        string Method,
        string AbsoluteUrl,
        int? HttpStatusCode,
        string? ExceptionType,
        string SafeResponseBodySnippet,
        string EffectiveBackendBaseUrl,
        string BackendBaseUrlSource);
}
