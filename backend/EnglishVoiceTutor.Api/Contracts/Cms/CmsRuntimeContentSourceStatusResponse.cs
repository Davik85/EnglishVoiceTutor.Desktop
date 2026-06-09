namespace EnglishVoiceTutor.Api.Contracts.Cms;

public sealed class CmsRuntimeContentSourceStatusResponse
{
    public string EnvironmentName { get; init; } = string.Empty;
    public string RuntimeSource { get; init; } = string.Empty;
    public bool ReadPublishedSnapshotEnabled { get; init; }
    public bool UsePublishedSnapshotForRuntime { get; init; }
    public bool FallbackToStaticJson { get; init; }
    public string ContentPackSlug { get; init; } = string.Empty;
}
