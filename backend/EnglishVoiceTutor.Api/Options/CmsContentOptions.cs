using EnglishVoiceTutor.Api.Data;

namespace EnglishVoiceTutor.Api.Options;

public sealed class CmsContentOptions
{
    public const string SectionName = "CmsContent";

    public bool ReadPublishedSnapshotEnabled { get; set; } = true;

    public bool UsePublishedSnapshotForRuntime { get; set; } = true;

    public string ContentPackSlug { get; set; } = CmsContentConstants.StaticImport.ContentPackSlug;

    public bool FallbackToStaticJson { get; set; } = true;
}
