using System.Net.Http;
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

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient httpClient;

    public UpdateManifestClient(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient();
    }

    public async Task<UpdateCheckResult> LoadLatestAsync(CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(LatestManifestUrl, UriKind.Absolute, out var manifestUri) || !IsHttpsUri(manifestUri))
        {
            return UpdateCheckResult.Failure("The update manifest address is not a valid HTTPS URL.");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));

            using var response = await httpClient.GetAsync(manifestUri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                return UpdateCheckResult.Failure("Could not load update information right now. Please try again later.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, JsonOptions, timeout.Token);
            if (manifest is null)
            {
                return UpdateCheckResult.Failure("The update information was empty or unreadable.");
            }

            var validation = Validate(manifest, manifestUri);
            return validation.IsValid
                ? UpdateCheckResult.Success(validation)
                : UpdateCheckResult.Failure(validation.ErrorMessage);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return UpdateCheckResult.Failure("The update check took too long. Please try again later.");
        }
        catch (Exception)
        {
            return UpdateCheckResult.Failure("Could not check for updates right now. Please check your internet connection and try again.");
        }
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
