namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminAuditService
{
    Task RecordTargetUserActionAsync(
        Guid adminUserId,
        Guid targetUserId,
        string actionType,
        string reason,
        string? safeMetadataJson,
        CancellationToken cancellationToken);
}
