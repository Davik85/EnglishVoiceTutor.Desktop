using EnglishVoiceTutor.Desktop.Models.LessonContent;

namespace EnglishVoiceTutor.Api.Services.Cms;

public sealed class CmsRuntimeLessonContentReadResult
{
    public bool Success { get; set; }
    public string Source { get; set; } = string.Empty;
    public string EffectiveSource { get; set; } = string.Empty;
    public bool UsePublishedSnapshotForRuntime { get; set; }
    public bool ReadPublishedSnapshotEnabled { get; set; }
    public bool FallbackToStaticJson { get; set; }
    public bool FallbackUsed { get; set; }
    public string ContentPackSlug { get; set; } = string.Empty;
    public int? VersionNumber { get; set; }
    public int? PublishedVersionNumber { get; set; }
    public string? SnapshotHash { get; set; }
    public CmsRuntimeLessonContentSummary Summary { get; set; } = new();
    public CmsRuntimeLessonContent? Content { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public string Message { get; set; } = string.Empty;
}

public sealed class CmsRuntimeLessonContentSummary
{
    public int TopicCount { get; set; }
    public int ScenarioCount { get; set; }
    public int PromptTemplateCount { get; set; }
    public int TutorBehaviorProfileCount { get; set; }
    public int LevelProfileCount { get; set; }
    public bool HashValid { get; set; }
    public bool ValidationPassed { get; set; }
    public bool ValidationSuccess { get; set; }
}

public sealed class CmsRuntimeLessonContent
{
    public List<CmsPublishedLessonTopic> Topics { get; set; } = [];
    public List<CmsPublishedLessonScenario> Scenarios { get; set; } = [];
    public List<CmsPublishedPromptTemplate> PromptTemplates { get; set; } = [];
    public List<CmsPublishedTutorBehaviorProfile> TutorBehaviorProfiles { get; set; } = [];
    public List<CmsLevelProfile> LevelProfiles { get; set; } = [];
}

public sealed class CmsRuntimeLessonContentStatusResponse
{
    private const int MaxDiagnosticMessages = 10;

    public DateTimeOffset CheckedAtUtc { get; set; }
    public bool Success { get; set; }
    public string Source { get; set; } = string.Empty;
    public string EffectiveSource { get; set; } = string.Empty;
    public bool UsePublishedSnapshotForRuntime { get; set; }
    public bool ReadPublishedSnapshotEnabled { get; set; }
    public bool FallbackToStaticJson { get; set; }
    public bool FallbackUsed { get; set; }
    public string ContentPackSlug { get; set; } = string.Empty;
    public int? VersionNumber { get; set; }
    public int? PublishedVersionNumber { get; set; }
    public string? SnapshotHash { get; set; }
    public int TopicCount { get; set; }
    public int ScenarioCount { get; set; }
    public int PromptTemplateCount { get; set; }
    public int TutorBehaviorProfileCount { get; set; }
    public int LevelProfileCount { get; set; }
    public bool HashValid { get; set; }
    public bool ValidationPassed { get; set; }
    public bool ValidationSuccess { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public string Message { get; set; } = string.Empty;

    public static CmsRuntimeLessonContentStatusResponse FromResult(CmsRuntimeLessonContentReadResult result)
    {
        var validationPassed = result.Summary.ValidationPassed && result.Errors.Count == 0;
        return new CmsRuntimeLessonContentStatusResponse
        {
            CheckedAtUtc = DateTimeOffset.UtcNow,
            Success = result.Success,
            Source = result.Source,
            EffectiveSource = result.Source,
            UsePublishedSnapshotForRuntime = result.UsePublishedSnapshotForRuntime,
            ReadPublishedSnapshotEnabled = result.ReadPublishedSnapshotEnabled,
            FallbackToStaticJson = result.FallbackToStaticJson,
            FallbackUsed = result.FallbackUsed,
            ContentPackSlug = result.ContentPackSlug,
            VersionNumber = result.VersionNumber,
            PublishedVersionNumber = result.VersionNumber,
            SnapshotHash = result.SnapshotHash,
            TopicCount = result.Summary.TopicCount,
            ScenarioCount = result.Summary.ScenarioCount,
            PromptTemplateCount = result.Summary.PromptTemplateCount,
            TutorBehaviorProfileCount = result.Summary.TutorBehaviorProfileCount,
            LevelProfileCount = result.Summary.LevelProfileCount,
            HashValid = result.Summary.HashValid,
            ValidationPassed = validationPassed,
            ValidationSuccess = validationPassed,
            Errors = result.Errors.Take(MaxDiagnosticMessages).ToList(),
            Warnings = CreateWarnings(result),
            Message = CreateMessage(result)
        };
    }

    private static List<string> CreateWarnings(CmsRuntimeLessonContentReadResult result)
    {
        var warnings = result.Warnings.Take(MaxDiagnosticMessages).ToList();
        if (result.FallbackUsed)
        {
            const string fallbackWarning = "Static JSON emergency fallback is active; CMS draft or published edits may not affect learner lessons until the CMS published snapshot is enabled, valid, and effectively active.";
            if (!warnings.Contains(fallbackWarning, StringComparer.Ordinal))
            {
                warnings.Insert(0, fallbackWarning);
            }
        }

        return warnings.Take(MaxDiagnosticMessages).ToList();
    }

    private static string CreateMessage(CmsRuntimeLessonContentReadResult result)
    {
        if (result.FallbackUsed)
        {
            return "Static JSON emergency fallback is active; learner runtime is using packaged static JSON, so CMS draft or published edits may not affect learner lessons until the CMS published snapshot is enabled, valid, and effectively active.";
        }

        if (string.Equals(result.Source, EnglishVoiceTutor.Api.Data.CmsContentConstants.Sources.CmsPublishedSnapshot, StringComparison.Ordinal))
        {
            return "CMS published snapshot is active for learner runtime because both CMS runtime flags are enabled and the published snapshot validated.";
        }

        return "CMS published snapshot is not effectively active for learner runtime. Treat packaged static JSON usage as an attention state unless this is an intentional emergency fallback or initialization step.";
    }
}
