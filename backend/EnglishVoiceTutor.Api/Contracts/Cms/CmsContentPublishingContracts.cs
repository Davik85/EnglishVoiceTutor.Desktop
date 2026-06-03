namespace EnglishVoiceTutor.Api.Contracts.Cms;

public sealed class CmsContentVersionResponse
{
    public Guid Id { get; set; }
    public Guid ContentPackId { get; set; }
    public string ContentPackSlug { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public string SnapshotHash { get; set; } = string.Empty;
    public string PublishStatus { get; set; } = string.Empty;
    public Guid? PublishedByUserId { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public string ChangeSummary { get; set; } = string.Empty;
    public Guid? RestoredFromVersionId { get; set; }
    public int? RestoredFromVersionNumber { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public bool SnapshotHashValid { get; set; }
    public CmsContentValidationResponse Validation { get; set; } = new();
    public CmsContentVersionSnapshotSummaryResponse SnapshotSummary { get; set; } = new();
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class CmsContentVersionSnapshotSummaryResponse
{
    public int Topics { get; set; }
    public int Scenarios { get; set; }
    public int PromptTemplates { get; set; }
    public int TutorBehaviorProfiles { get; set; }
}

public sealed class CmsContentVersionListResponse
{
    public bool Success { get; set; }
    public string ContentPackSlug { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<CmsContentVersionResponse> Versions { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class PublishCmsContentRequest
{
    public string? ChangeSummary { get; set; }
}

public sealed class PublishCmsContentResponse
{
    public bool Success { get; set; }
    public string ContentPackSlug { get; set; } = string.Empty;
    public int? VersionNumber { get; set; }
    public string? SnapshotHash { get; set; }
    public string? PreviousSnapshotHash { get; set; }
    public bool Created { get; set; }
    public bool Skipped { get; set; }
    public bool NoChanges { get; set; }
    public CmsContentValidationResponse Validation { get; set; } = new();
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public DateTimeOffset CompletedAtUtc { get; set; }
}

public sealed class RestoreCmsContentVersionRequest
{
    public string? Reason { get; set; }
    public bool PublishRestoredVersion { get; set; } = true;
}

public sealed class RestoreCmsContentVersionResponse
{
    public bool Success { get; set; }
    public string ContentPackSlug { get; set; } = string.Empty;
    public int RestoredFromVersionNumber { get; set; }
    public Guid? RestoredFromVersionId { get; set; }
    public string? RestoredSnapshotHash { get; set; }
    public bool DraftRestored { get; set; }
    public bool PublishedNewVersion { get; set; }
    public bool Skipped { get; set; }
    public bool NoChanges { get; set; }
    public int? NewVersionNumber { get; set; }
    public string? NewSnapshotHash { get; set; }
    public CmsContentValidationResponse Validation { get; set; } = new();
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public DateTimeOffset CompletedAtUtc { get; set; }
}
