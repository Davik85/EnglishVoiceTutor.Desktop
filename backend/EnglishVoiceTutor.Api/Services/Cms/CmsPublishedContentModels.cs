using EnglishVoiceTutor.Desktop.Models.LessonContent;

namespace EnglishVoiceTutor.Api.Services.Cms;

public sealed class CmsPublishedContentReadResult
{
    public bool Success { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool ReadPublishedSnapshotEnabled { get; set; }
    public bool FallbackToStaticJson { get; set; }
    public bool FallbackUsed { get; set; }
    public string ContentPackSlug { get; set; } = string.Empty;
    public int? VersionNumber { get; set; }
    public string? SnapshotHash { get; set; }
    public string? ComputedSnapshotHash { get; set; }
    public CmsPublishedContentSummary Summary { get; set; } = new();
    public CmsPublishedLessonContent? Content { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class CmsPublishedContentSummary
{
    public int TopicCount { get; set; }
    public int ScenarioCount { get; set; }
    public int PromptTemplateCount { get; set; }
    public int TutorBehaviorProfileCount { get; set; }
    public bool HashValid { get; set; }
    public bool ValidationPassed { get; set; }
}

public sealed class CmsPublishedContentStatusResponse
{
    public bool Success { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool ReadPublishedSnapshotEnabled { get; set; }
    public bool FallbackToStaticJson { get; set; }
    public bool FallbackUsed { get; set; }
    public string ContentPackSlug { get; set; } = string.Empty;
    public int? VersionNumber { get; set; }
    public string? SnapshotHash { get; set; }
    public string? ComputedSnapshotHash { get; set; }
    public int TopicCount { get; set; }
    public int ScenarioCount { get; set; }
    public int PromptTemplateCount { get; set; }
    public int TutorBehaviorProfileCount { get; set; }
    public bool HashValid { get; set; }
    public bool ValidationPassed { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];

    public static CmsPublishedContentStatusResponse FromResult(CmsPublishedContentReadResult result)
    {
        return new CmsPublishedContentStatusResponse
        {
            Success = result.Success,
            Source = result.Source,
            ReadPublishedSnapshotEnabled = result.ReadPublishedSnapshotEnabled,
            FallbackToStaticJson = result.FallbackToStaticJson,
            FallbackUsed = result.FallbackUsed,
            ContentPackSlug = result.ContentPackSlug,
            VersionNumber = result.VersionNumber,
            SnapshotHash = result.SnapshotHash,
            ComputedSnapshotHash = result.ComputedSnapshotHash,
            TopicCount = result.Summary.TopicCount,
            ScenarioCount = result.Summary.ScenarioCount,
            PromptTemplateCount = result.Summary.PromptTemplateCount,
            TutorBehaviorProfileCount = result.Summary.TutorBehaviorProfileCount,
            HashValid = result.Summary.HashValid,
            ValidationPassed = result.Summary.ValidationPassed,
            Errors = result.Errors,
            Warnings = result.Warnings
        };
    }
}

public sealed class CmsPublishedLessonContent
{
    public List<CmsPublishedLessonTopic> Topics { get; set; } = [];
    public List<CmsPublishedLessonScenario> Scenarios { get; set; } = [];
    public List<CmsPublishedPromptTemplate> PromptTemplates { get; set; } = [];
    public List<CmsPublishedTutorBehaviorProfile> TutorBehaviorProfiles { get; set; } = [];
}

public sealed class CmsPublishedLessonTopic
{
    public string StableTopicKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CmsPublishedLessonScenario
{
    public string StableScenarioKey { get; set; } = string.Empty;
    public string TopicKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LessonType { get; set; } = string.Empty;
    public string? DefinitionJson { get; set; }
    public LessonScenario Lesson { get; set; } = new();
}

public sealed class CmsPublishedPromptTemplate
{
    public string TemplateKey { get; set; } = string.Empty;
    public string AllowedPlaceholdersJson { get; set; } = string.Empty;
    public string RequiredPlaceholdersJson { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public bool IsActive { get; set; }
    public string Body { get; set; } = string.Empty;
}

public sealed class CmsPublishedTutorBehaviorProfile
{
    public string TutorId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public TutorProfile TutorProfile { get; set; } = new();
}
