namespace EnglishVoiceTutor.Api.Data.Entities.WebsiteCms;

public sealed class WebsiteCmsSectionEntity
{
    public Guid Id { get; set; }
    public string SectionKey { get; set; } = string.Empty;
    public string DraftBody { get; set; } = string.Empty;
    public string? PublishedBody { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
    public DateOnly? EffectiveDate { get; set; }
    public string? InternalNotes { get; set; }
    public string? ChangeReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
}
