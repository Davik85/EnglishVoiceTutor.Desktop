using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminActivityService
{
    Task<AdminActivityEventsResponse> ListActivityAsync(AdminActivityQuery query, CancellationToken cancellationToken);
}

public sealed record AdminActivityQuery(
    Guid? ActorAdminUserId,
    Guid? ActorUserId,
    Guid? TargetUserId,
    Guid? TargetAdminUserId,
    string? Source,
    string? ActionType,
    string? Result,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    int? Limit);
