namespace EnglishVoiceTutor.Api.Services.Cms;

public sealed class CmsContentImportResult
{
    public bool Success { get; set; }
    public string ContentPackSlug { get; set; } = string.Empty;
    public string ContentPackName { get; set; } = string.Empty;
    public Guid? ContentPackId { get; set; }
    public int? PublishedVersionNumber { get; set; }
    public string? SnapshotHash { get; set; }
    public bool PublishedSnapshotCreated { get; set; }
    public bool IdempotentNoChanges { get; set; }
    public bool ContentPackCreated { get; set; }
    public bool ContentPackAlreadyExisted { get; set; }
    public bool DraftInitialized { get; set; }
    public bool DraftPreserved { get; set; }
    public bool RuntimeUnchanged { get; set; }
    public List<string> Messages { get; set; } = [];
    public CmsContentImportCounts Counts { get; set; } = new();
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class CmsContentImportCounts
{
    public int TopicsRead { get; set; }
    public int TopicsCreated { get; set; }
    public int TopicsUpdated { get; set; }
    public int TopicsSkipped { get; set; }
    public int ScenariosRead { get; set; }
    public int ScenariosCreated { get; set; }
    public int ScenariosUpdated { get; set; }
    public int ScenariosSkipped { get; set; }
    public int PromptTemplatesRead { get; set; }
    public int PromptTemplatesCreated { get; set; }
    public int PromptTemplatesUpdated { get; set; }
    public int PromptTemplatesSkipped { get; set; }
    public int TutorBehaviorProfilesRead { get; set; }
    public int TutorBehaviorProfilesCreated { get; set; }
    public int TutorBehaviorProfilesUpdated { get; set; }
    public int TutorBehaviorProfilesSkipped { get; set; }
    public int ContentVersionsCreated { get; set; }
    public int ContentVersionsSkipped { get; set; }
    public int PublishedSnapshotsCreated { get; set; }
    public int PublishedSnapshotsSkipped { get; set; }
    public int AuditLogEntriesCreated { get; set; }
}

public sealed class CmsSetupLocalizationImportPreviewResult
{
    public bool SafeToApply { get; set; }
    public int PackagedScenarioCount { get; set; }
    public int CmsDraftScenarioCount { get; set; }
    public int MatchedScenarioCount { get; set; }
    public int LanguageCount { get; set; }
    public int TemplateCount { get; set; }
    public int ContextTitleCount { get; set; }
    public int ScenariosRequiringChanges { get; set; }
    public int LanguageBlocksToAdd { get; set; }
    public int LanguageBlocksAlreadyIdentical { get; set; }
    public List<string> MissingDraftScenarioKeys { get; set; } = [];
    public List<string> UnknownDraftScenarioKeys { get; set; } = [];
    public List<string> ConflictingExistingLocalizationBlocks { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public bool PublishedSnapshotUnchanged { get; set; } = true;
    public bool RuntimeUnchanged { get; set; } = true;
}

public sealed class CmsSetupLocalizationImportResult
{
    public bool Success { get; set; }
    public int ScenariosUpdated { get; set; }
    public int LanguageBlocksAdded { get; set; }
    public int TemplatesAdded { get; set; }
    public int ContextTitlesAdded { get; set; }
    public int AlreadyIdenticalBlocks { get; set; }
    public List<string> Conflicts { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public int AuditEntriesCreated { get; set; }
    public bool PublishedVersionChanged { get; set; }
    public bool PublishedSnapshotCreated { get; set; }
    public bool RuntimeChanged { get; set; }
}

public sealed class CmsContentValidationResult
{
    public bool Success => Errors.Count == 0;
    public CmsContentValidationCounts Counts { get; set; } = new();
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class CmsContentValidationCounts
{
    public int Topics { get; set; }
    public int Scenarios { get; set; }
    public int PromptTemplates { get; set; }
    public int TutorBehaviorProfiles { get; set; }
}
