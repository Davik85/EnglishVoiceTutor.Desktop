using EnglishVoiceTutor.Desktop.Models.LessonContent;

namespace EnglishVoiceTutor.Api.Services.Cms;

public sealed class CmsRuntimeLessonContentReadResult
{
    public bool Success { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool UsePublishedSnapshotForRuntime { get; set; }
    public bool ReadPublishedSnapshotEnabled { get; set; }
    public bool FallbackToStaticJson { get; set; }
    public bool FallbackUsed { get; set; }
    public string ContentPackSlug { get; set; } = string.Empty;
    public int? VersionNumber { get; set; }
    public string? SnapshotHash { get; set; }
    public CmsRuntimeLessonContentSummary Summary { get; set; } = new();
    public CmsRuntimeLessonContent? Content { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class CmsRuntimeLessonContentSummary
{
    public int TopicCount { get; set; }
    public int ScenarioCount { get; set; }
    public int PromptTemplateCount { get; set; }
    public int TutorBehaviorProfileCount { get; set; }
    public bool HashValid { get; set; }
    public bool ValidationPassed { get; set; }
}

public sealed class CmsRuntimeLessonContent
{
    public List<CmsPublishedLessonTopic> Topics { get; set; } = [];
    public List<CmsPublishedLessonScenario> Scenarios { get; set; } = [];
    public List<CmsPublishedPromptTemplate> PromptTemplates { get; set; } = [];
    public List<CmsPublishedTutorBehaviorProfile> TutorBehaviorProfiles { get; set; } = [];
}

public sealed class CmsRuntimeLessonContentStatusResponse
{
    public bool Success { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool UsePublishedSnapshotForRuntime { get; set; }
    public bool ReadPublishedSnapshotEnabled { get; set; }
    public bool FallbackToStaticJson { get; set; }
    public bool FallbackUsed { get; set; }
    public string ContentPackSlug { get; set; } = string.Empty;
    public int? VersionNumber { get; set; }
    public string? SnapshotHash { get; set; }
    public int TopicCount { get; set; }
    public int ScenarioCount { get; set; }
    public int PromptTemplateCount { get; set; }
    public int TutorBehaviorProfileCount { get; set; }
    public bool HashValid { get; set; }
    public bool ValidationPassed { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];

    public static CmsRuntimeLessonContentStatusResponse FromResult(CmsRuntimeLessonContentReadResult result)
    {
        return new CmsRuntimeLessonContentStatusResponse
        {
            Success = result.Success,
            Source = result.Source,
            UsePublishedSnapshotForRuntime = result.UsePublishedSnapshotForRuntime,
            ReadPublishedSnapshotEnabled = result.ReadPublishedSnapshotEnabled,
            FallbackToStaticJson = result.FallbackToStaticJson,
            FallbackUsed = result.FallbackUsed,
            ContentPackSlug = result.ContentPackSlug,
            VersionNumber = result.VersionNumber,
            SnapshotHash = result.SnapshotHash,
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
