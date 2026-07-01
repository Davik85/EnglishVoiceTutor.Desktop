using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminActivityService(AppDbContext dbContext) : IAdminActivityService
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;
    private const string AdminActionsSource = "admin_actions";
    private const string AdminRoleAssignmentEventsSource = "admin_role_assignment_events";
    private const string CmsContentAuditLogsSource = "cms_content_audit_logs";
    private const string SucceededResult = "succeeded";

    private readonly AppDbContext _dbContext = dbContext;

    public async Task<AdminActivityEventsResponse> ListActivityAsync(AdminActivityQuery query, CancellationToken cancellationToken)
    {
        var limit = query.Limit ?? DefaultLimit;
        if (limit is < 1 or > MaxLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(query), $"limit must be between 1 and {MaxLimit}.");
        }

        // This first safe slice is read-only. Login/logout/failure audit persistence is intentionally not synthesized here;
        // it requires a later unified audit table or an explicit approved schema change.
        var items = new List<AdminActivityEventSnapshot>();
        if (MatchesSource(query.Source, AdminActionsSource))
        {
            var adminActionRows = await (from action in _dbContext.AdminActions.AsNoTracking()
                    join actorAdminUser in _dbContext.AdminUsers.AsNoTracking()
                        on action.AdminUserId equals actorAdminUser.UserId into actorAdminUsers
                    from actorAdminUser in actorAdminUsers.DefaultIfEmpty()
                    join actorUser in _dbContext.Users.AsNoTracking()
                        on action.AdminUserId equals actorUser.Id into actorUsers
                    from actorUser in actorUsers.DefaultIfEmpty()
                    join targetUser in _dbContext.Users.AsNoTracking()
                        on action.TargetUserId equals targetUser.Id
                    where !query.ActorUserId.HasValue || action.AdminUserId == query.ActorUserId
                    where !query.ActorAdminUserId.HasValue || (actorAdminUser != null && actorAdminUser.Id == query.ActorAdminUserId)
                    where !query.TargetUserId.HasValue || action.TargetUserId == query.TargetUserId
                    where query.TargetAdminUserId == null
                    where string.IsNullOrWhiteSpace(query.ActionType) || action.ActionType == query.ActionType
                    where string.IsNullOrWhiteSpace(query.Result) || SucceededResult == query.Result
                    where !query.FromUtc.HasValue || action.CreatedAtUtc >= query.FromUtc.Value
                    where !query.ToUtc.HasValue || action.CreatedAtUtc <= query.ToUtc.Value
                    orderby action.CreatedAtUtc descending
                    select new AdminActivityEventSnapshot
                    {
                        EventId = action.Id.ToString(), Source = AdminActionsSource, OccurredAtUtc = action.CreatedAtUtc,
                        ActorAdminUserId = actorAdminUser == null ? null : actorAdminUser.Id, ActorUserId = action.AdminUserId,
                        ActorEmail = actorAdminUser == null ? actorUser == null ? null : actorUser.Email : actorAdminUser.NormalizedEmail,
                        ActionType = action.ActionType, Result = SucceededResult, TargetType = "user",
                        TargetUserId = action.TargetUserId, TargetUserEmail = targetUser.Email,
                        Reason = action.Reason, AdminNote = action.Reason, SafeMetadataJson = action.SafeMetadataJson
                    })
                .Take(limit)
                .ToListAsync(cancellationToken);

            items.AddRange(adminActionRows);
        }

        if (MatchesSource(query.Source, AdminRoleAssignmentEventsSource))
        {
            items.AddRange(await _dbContext.AdminRoleAssignmentEvents.AsNoTracking()
                .Include(roleEvent => roleEvent.ActorAdminUser).ThenInclude(adminUser => adminUser!.User)
                .Include(roleEvent => roleEvent.TargetAdminUser).ThenInclude(adminUser => adminUser.User)
                .Where(roleEvent => !query.ActorAdminUserId.HasValue || roleEvent.ActorAdminUserId == query.ActorAdminUserId)
                .Where(roleEvent => !query.TargetAdminUserId.HasValue || roleEvent.TargetAdminUserId == query.TargetAdminUserId)
                .Where(roleEvent => query.ActorUserId == null && query.TargetUserId == null)
                .Where(roleEvent => string.IsNullOrWhiteSpace(query.ActionType) || roleEvent.ActionType == query.ActionType)
                .Where(roleEvent => string.IsNullOrWhiteSpace(query.Result) || roleEvent.Result == query.Result)
                .Where(roleEvent => !query.FromUtc.HasValue || roleEvent.OccurredAtUtc >= query.FromUtc.Value)
                .Where(roleEvent => !query.ToUtc.HasValue || roleEvent.OccurredAtUtc <= query.ToUtc.Value)
                .OrderByDescending(roleEvent => roleEvent.OccurredAtUtc)
                .Take(limit)
                .Select(roleEvent => new AdminActivityEventSnapshot
                {
                    EventId = roleEvent.Id.ToString(), Source = AdminRoleAssignmentEventsSource, OccurredAtUtc = roleEvent.OccurredAtUtc,
                    ActorAdminUserId = roleEvent.ActorAdminUserId, ActorUserId = roleEvent.ActorAdminUser == null ? null : roleEvent.ActorAdminUser.UserId,
                    ActorEmail = roleEvent.ActorAdminUser == null ? null : roleEvent.ActorAdminUser.NormalizedEmail,
                    ActionType = roleEvent.ActionType, Result = roleEvent.Result, TargetType = "admin_user",
                    TargetAdminUserId = roleEvent.TargetAdminUserId, TargetAdminUserEmail = roleEvent.TargetAdminUser.NormalizedEmail,
                    Reason = roleEvent.Reason, AdminNote = roleEvent.Reason, SafeMetadataJson = roleEvent.SafeMetadataJson
                }).ToListAsync(cancellationToken));
        }

        if (MatchesSource(query.Source, CmsContentAuditLogsSource))
        {
            items.AddRange(await _dbContext.ContentAuditLogs.AsNoTracking()
                .Where(log => !query.ActorUserId.HasValue || log.ActorUserId == query.ActorUserId)
                .Where(log => query.ActorAdminUserId == null && query.TargetUserId == null && query.TargetAdminUserId == null)
                .Where(log => string.IsNullOrWhiteSpace(query.ActionType) || log.Action == query.ActionType)
                .Where(log => string.IsNullOrWhiteSpace(query.Result) || log.Status == query.Result)
                .Where(log => !query.FromUtc.HasValue || log.CreatedAtUtc >= query.FromUtc.Value)
                .Where(log => !query.ToUtc.HasValue || log.CreatedAtUtc <= query.ToUtc.Value)
                .OrderByDescending(log => log.CreatedAtUtc)
                .Take(limit)
                .Select(log => new AdminActivityEventSnapshot
                {
                    EventId = log.Id.ToString(), Source = CmsContentAuditLogsSource, OccurredAtUtc = log.CreatedAtUtc,
                    ActorUserId = log.ActorUserId, ActorEmail = log.ActorEmail,
                    ActionType = log.Action, Result = log.Status, TargetType = "cms_entity",
                    EntityType = log.EntityType, EntityId = log.EntityId.ToString(), StableKey = log.StableKey,
                    Reason = log.Reason, AdminNote = log.Reason, SafeMetadataJson = log.RequestMetadataJson
                }).ToListAsync(cancellationToken));
        }

        return new AdminActivityEventsResponse
        {
            Items = items.OrderByDescending(item => item.OccurredAtUtc).Take(limit).ToList(),
            Limit = limit,
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static bool MatchesSource(string? requestedSource, string source) =>
        string.IsNullOrWhiteSpace(requestedSource) || string.Equals(requestedSource.Trim(), source, StringComparison.OrdinalIgnoreCase);
}
