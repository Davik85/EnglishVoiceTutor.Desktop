using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Desktop.Models.Updates;

public sealed class UpdateManifest
{
    [JsonPropertyName("productName")]
    public string ProductName { get; init; } = string.Empty;

    [JsonPropertyName("appId")]
    public string AppId { get; init; } = string.Empty;

    [JsonPropertyName("platform")]
    public string Platform { get; init; } = string.Empty;

    [JsonPropertyName("architecture")]
    public string Architecture { get; init; } = string.Empty;

    [JsonPropertyName("channel")]
    public string Channel { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("installerFileName")]
    public string InstallerFileName { get; init; } = string.Empty;

    [JsonPropertyName("installerRelativeUrl")]
    public string InstallerRelativeUrl { get; init; } = string.Empty;

    [JsonPropertyName("installerSha256")]
    public string InstallerSha256 { get; init; } = string.Empty;

    [JsonPropertyName("installerSizeBytes")]
    public long InstallerSizeBytes { get; init; }

    [JsonPropertyName("backendBaseUrl")]
    public string BackendBaseUrl { get; init; } = string.Empty;

    [JsonPropertyName("updateMode")]
    public string UpdateMode { get; init; } = string.Empty;

    [JsonPropertyName("notes")]
    [JsonConverter(typeof(UpdateManifestNotesJsonConverter))]
    public IReadOnlyList<string> Notes { get; init; } = [];
}
