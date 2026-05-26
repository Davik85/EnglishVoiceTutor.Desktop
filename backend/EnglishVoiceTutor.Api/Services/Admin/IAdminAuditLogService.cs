using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminAuditLogService
{
    Task<AdminAuditActionsResult> GetTargetUserAuditActionsAsync(
        Guid targetUserId,
        int? limit,
        CancellationToken cancellationToken);
}

public sealed class AdminAuditActionsResult
{
    public bool IsInvalid { get; init; }
    public bool IsNotFound { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public AdminAuditActionsResponse? Response { get; init; }
}
