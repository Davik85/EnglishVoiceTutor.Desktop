using EnglishVoiceTutor.Desktop.Models.LessonContent;

namespace EnglishVoiceTutor.Api.Services.Cms;

public sealed class CmsStaticContentImportDraft
{
    public string ContentRootPath { get; set; } = string.Empty;
    public List<CmsStaticTopicDraft> Topics { get; set; } = [];
    public List<CmsStaticScenarioDraft> Scenarios { get; set; } = [];
    public List<CmsStaticPromptTemplateDraft> PromptTemplates { get; set; } = [];
    public List<CmsStaticTutorProfileDraft> TutorBehaviorProfiles { get; set; } = [];
    public List<string> StudyLanguageIds { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class CmsStaticTopicDraft
{
    public string StableTopicKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CmsStaticScenarioDraft
{
    public string StableScenarioKey { get; set; } = string.Empty;
    public string TopicKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LessonType { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = string.Empty;
    public LessonScenario Scenario { get; set; } = new();
}

public sealed class CmsStaticPromptTemplateDraft
{
    public string TemplateKey { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string AllowedPlaceholdersJson { get; set; } = string.Empty;
    public string RequiredPlaceholdersJson { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CmsStaticTutorProfileDraft
{
    public string TutorId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public TutorProfile TutorProfile { get; set; } = new();
    public bool IsActive { get; set; } = true;
}
