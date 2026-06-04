using EnglishVoiceTutor.Api.Contracts.Cms;

namespace EnglishVoiceTutor.Api.Services.Cms;

public interface ICmsContentAdminService
{
    Task<IReadOnlyList<CmsContentPackSummaryResponse>> ListContentPacksAsync(CancellationToken cancellationToken);
    Task<CmsContentPackSummaryResponse?> GetContentPackSummaryAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<CmsContentTopicResponse>> ListTopicsAsync(string slug, CancellationToken cancellationToken);
    Task<CmsContentTopicResponse?> GetTopicAsync(string slug, string topicIdOrKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<CmsContentScenarioResponse>> ListScenariosAsync(string slug, string? topicIdOrKey, CancellationToken cancellationToken);
    Task<CmsContentScenarioResponse?> GetScenarioAsync(string slug, string scenarioIdOrKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<CmsPromptTemplateResponse>> ListPromptTemplatesAsync(string slug, CancellationToken cancellationToken);
    Task<CmsPromptTemplateResponse?> GetPromptTemplateAsync(string slug, string templateIdOrKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<CmsTutorBehaviorProfileResponse>> ListTutorBehaviorProfilesAsync(string slug, CancellationToken cancellationToken);
    Task<CmsTutorBehaviorProfileResponse?> GetTutorBehaviorProfileAsync(string slug, string profileIdOrTutorId, CancellationToken cancellationToken);
    Task<CmsContentUpdateResponse?> UpdateTopicAsync(string slug, string topicIdOrKey, UpdateCmsTopicRequest request, Guid actorUserId, string? actorEmail, string? requestId, CancellationToken cancellationToken);
    Task<CmsContentUpdateResponse?> UpdateScenarioAsync(string slug, string scenarioIdOrKey, UpdateCmsScenarioRequest request, Guid actorUserId, string? actorEmail, string? requestId, CancellationToken cancellationToken);
    Task<CmsContentUpdateResponse?> UpdatePromptTemplateAsync(string slug, string templateIdOrKey, UpdateCmsPromptTemplateRequest request, Guid actorUserId, string? actorEmail, string? requestId, CancellationToken cancellationToken);
    Task<CmsContentUpdateResponse?> UpdateTutorBehaviorProfileAsync(string slug, string profileIdOrTutorId, UpdateCmsTutorBehaviorProfileRequest request, Guid actorUserId, string? actorEmail, string? requestId, CancellationToken cancellationToken);
    Task<CmsContentValidationResponse?> ValidateDraftAsync(string slug, CancellationToken cancellationToken);
    Task<CmsContentPreviewResponse?> GetPreviewSummaryAsync(string slug, CancellationToken cancellationToken);
    Task<CmsContentAuditEntriesResponse> ListAuditEntriesAsync(string? contentPackSlug, string? entityType, string? stableKey, int? limit, CancellationToken cancellationToken);
}
