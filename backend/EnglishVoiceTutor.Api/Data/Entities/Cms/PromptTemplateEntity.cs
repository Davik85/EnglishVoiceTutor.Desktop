namespace EnglishVoiceTutor.Api.Data.Entities.Cms;

public sealed class PromptTemplateEntity
{
    public Guid Id { get; set; }
    public Guid ContentPackId { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string? TargetStudyLanguageId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string AllowedPlaceholdersJson { get; set; } = string.Empty;
    public string RequiredPlaceholdersJson { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public bool IsActive { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ContentPackEntity ContentPack { get; set; } = null!;
}
