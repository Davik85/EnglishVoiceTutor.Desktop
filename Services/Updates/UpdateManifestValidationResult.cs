using EnglishVoiceTutor.Desktop.Models.Updates;

namespace EnglishVoiceTutor.Desktop.Services.Updates;

public sealed record UpdateManifestValidationResult(
    bool IsValid,
    UpdateManifest? Manifest,
    Uri? InstallerUri,
    string ErrorMessage)
{
    public static UpdateManifestValidationResult Success(UpdateManifest manifest, Uri installerUri) =>
        new(true, manifest, installerUri, string.Empty);

    public static UpdateManifestValidationResult Failure(string errorMessage) =>
        new(false, null, null, errorMessage);
}
