namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminWebsiteCmsSectionDetailResponse
{
    public string SectionKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = string.Empty;
    public DateOnly? EffectiveDate { get; set; }
    public string DraftBody { get; set; } = string.Empty;
    public bool PublishedBodyExists { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public string? InternalNotes { get; set; }
    public string? ChangeReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset CheckedAtUtc { get; set; }
}

public sealed class AdminWebsiteCmsSectionDraftSaveRequest
{
    public string DraftBody { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = string.Empty;
    public DateOnly? EffectiveDate { get; set; }
    public string? InternalNotes { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
}

public sealed class AdminWebsiteCmsDraftValidationResponse
{
    public string SectionKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<string> Errors { get; set; } = [];
    public IReadOnlyList<string> Warnings { get; set; } = [];
    public DateTimeOffset CheckedAtUtc { get; set; }
}

public sealed class AdminWebsiteCmsDraftPreviewResponse
{
    public string SectionKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = string.Empty;
    public DateOnly? EffectiveDate { get; set; }
    public string DraftBody { get; set; } = string.Empty;
    public IReadOnlyList<string> Warnings { get; set; } = [];
    public string? AdminOnlyInternalNotes { get; set; }
    public DateTimeOffset CheckedAtUtc { get; set; }
}

public sealed class AdminWebsiteCmsSectionReviewStatusUpdateRequest
{
    public string ReviewStatus { get; set; } = string.Empty;
    public string ChangeReason { get; set; } = string.Empty;
}
