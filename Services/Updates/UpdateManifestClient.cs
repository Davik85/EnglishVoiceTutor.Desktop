using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Models.Updates;

namespace EnglishVoiceTutor.Desktop.Services.Updates;

public sealed class UpdateManifestClient
{
    public const string LatestManifestUrl = "https://languagevoicetutor.com/releases/windows/direct/latest.json";
    public const string ExpectedProductName = "Language Voice Tutor";
    public const string ExpectedAppId = "LanguageVoiceTutor.Desktop";
    public const string ExpectedPlatform = "windows";
    public const string ExpectedArchitecture = "win-x64";
    private const int ManifestRequestTimeoutSeconds = 45;
    private const string ManifestFailureCategoryConfiguration = "configuration";
    private const string ManifestFailureCategoryHttpStatus = "http_status";
    private const string ManifestFailureCategoryNetwork = "network";
    private const string ManifestFailureCategoryTimeout = "timeout";
    private const string ManifestFailureCategoryParse = "parse";
    private const string ManifestFailureCategoryValidation = "validation";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient httpClient;

    public UpdateManifestClient(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? CreateHttpClient();
    }

    public async Task<UpdateCheckResult> LoadLatestAsync(CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(LatestManifestUrl, UriKind.Absolute, out var manifestUri) || !IsHttpsUri(manifestUri))
        {
            return UpdateCheckResult.Failure(
                "The update manifest address is not a valid HTTPS URL.",
                LatestManifestUrl,
                ManifestFailureCategoryConfiguration);
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(ManifestRequestTimeoutSeconds));

            using var request = new HttpRequestMessage(HttpMethod.Get, manifestUri);
            ConfigureManifestRequestHeaders(request);

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                return UpdateCheckResult.Failure(
                    "Could not load update information right now. Please try again later.",
                    manifestUri.AbsoluteUri,
                    ManifestFailureCategoryHttpStatus,
                    response.StatusCode,
                    response.ReasonPhrase ?? string.Empty);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            UpdateManifest? manifest;
            try
            {
                manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, JsonOptions, timeout.Token);
            }
            catch (JsonException exception)
            {
                return UpdateCheckResult.Failure(
                    "The update information could not be read as valid JSON.",
                    manifestUri.AbsoluteUri,
                    ManifestFailureCategoryParse,
                    exceptionMessage: exception.Message);
            }

            if (manifest is null)
            {
                return UpdateCheckResult.Failure(
                    "The update information was empty or unreadable.",
                    manifestUri.AbsoluteUri,
                    ManifestFailureCategoryParse);
            }

            var validation = Validate(manifest, manifestUri);
            return validation.IsValid
                ? UpdateCheckResult.Success(validation, manifestUri.AbsoluteUri)
                : UpdateCheckResult.Failure(
                    validation.ErrorMessage,
                    manifestUri.AbsoluteUri,
                    ManifestFailureCategoryValidation);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return UpdateCheckResult.Failure(
                "The update check took too long. Please try again later.",
                manifestUri.AbsoluteUri,
                ManifestFailureCategoryTimeout);
        }
        catch (HttpRequestException exception)
        {
            return UpdateCheckResult.Failure(
                "Could not check for updates right now. Please check your internet connection and try again.",
                manifestUri.AbsoluteUri,
                ManifestFailureCategoryNetwork,
                exception.StatusCode,
                exception.Message);
        }
        catch (Exception exception)
        {
            return UpdateCheckResult.Failure(
                "Could not check for updates right now. Please check your internet connection and try again.",
                manifestUri.AbsoluteUri,
                ManifestFailureCategoryNetwork,
                exceptionMessage: exception.Message);
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
        };

        return new HttpClient(handler);
    }

    private static void ConfigureManifestRequestHeaders(HttpRequestMessage request)
    {
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.Clear();
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("LanguageVoiceTutorDesktop", DesktopAppVersionProvider.GetCurrentVersionText()));
        request.Headers.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true,
            MaxAge = TimeSpan.Zero
        };
        request.Headers.Pragma.ParseAdd("no-cache");
    }

    public static UpdateManifestValidationResult Validate(UpdateManifest manifest, Uri manifestUri)
    {
        if (!StringEquals(manifest.ProductName, ExpectedProductName) ||
            !StringEquals(manifest.AppId, ExpectedAppId) ||
            !StringEquals(manifest.Platform, ExpectedPlatform) ||
            !StringEquals(manifest.Architecture, ExpectedArchitecture))
        {
            return UpdateManifestValidationResult.Failure(
                "The update information does not match this Language Voice Tutor Windows app.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            return UpdateManifestValidationResult.Failure("The update information is missing a version number.");
        }

        if (string.IsNullOrWhiteSpace(manifest.InstallerFileName) || string.IsNullOrWhiteSpace(manifest.InstallerRelativeUrl))
        {
            return UpdateManifestValidationResult.Failure("The update information is missing installer details.");
        }

        if (string.IsNullOrWhiteSpace(manifest.InstallerSha256) || manifest.InstallerSha256.Length != 64 ||
            !manifest.InstallerSha256.All(IsHexDigit))
        {
            return UpdateManifestValidationResult.Failure("The update information is missing a valid installer checksum.");
        }

        if (manifest.InstallerSizeBytes <= 0)
        {
            return UpdateManifestValidationResult.Failure("The update information is missing a valid installer size.");
        }

        if (!TryBuildInstallerUri(manifestUri, manifest.InstallerRelativeUrl, out var installerUri) || installerUri is null || !IsHttpsUri(installerUri))
        {
            return UpdateManifestValidationResult.Failure("The installer download address is not a valid HTTPS URL.");
        }

        return UpdateManifestValidationResult.Success(manifest, installerUri);
    }

    private static bool TryBuildInstallerUri(Uri manifestUri, string installerRelativeUrl, out Uri? installerUri)
    {
        installerUri = null;
        if (Uri.TryCreate(installerRelativeUrl, UriKind.Absolute, out var absoluteUri))
        {
            installerUri = absoluteUri;
            return true;
        }

        return Uri.TryCreate(manifestUri, installerRelativeUrl, out installerUri);
    }

    private static bool IsHttpsUri(Uri uri) => string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static bool IsHexDigit(char value) =>
        (value >= '0' && value <= '9') ||
        (value >= 'a' && value <= 'f') ||
        (value >= 'A' && value <= 'F');

    private static bool StringEquals(string actual, string expected) =>
        string.Equals(actual?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
}
