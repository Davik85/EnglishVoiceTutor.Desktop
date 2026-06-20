using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminRoleAssignmentDiagnosticsService(AppDbContext dbContext) : IAdminRoleAssignmentDiagnosticsService
{
    private const string ActiveStatus = "active";
    private const string PendingInviteStatus = "pending_invite";

    private readonly AppDbContext _dbContext = dbContext;

    public async Task<AdminRoleAssignmentDiagnosticsResult> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        var adminUsers = await _dbContext.AdminUsers
            .AsNoTracking()
            .Select(adminUser => new
            {
                adminUser.Id,
                adminUser.UserId,
                adminUser.Status,
                adminUser.DisabledAtUtc,
                adminUser.CreatedAtUtc
            })
            .OrderBy(adminUser => adminUser.CreatedAtUtc)
            .ThenBy(adminUser => adminUser.Id)
            .ToListAsync(cancellationToken);

        var roleAssignments = await _dbContext.AdminUserRoles
            .AsNoTracking()
            .Select(role => new
            {
                role.AdminUserId,
                role.RoleId,
                role.RevokedAtUtc
            })
            .ToListAsync(cancellationToken);

        var totalRoleAssignmentEvents = await _dbContext.AdminRoleAssignmentEvents
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var activeRoleIdsByAdminUserId = roleAssignments
            .Where(role => role.RevokedAtUtc is null)
            .GroupBy(role => role.AdminUserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(role => role.RoleId)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(roleId => roleId, StringComparer.Ordinal)
                    .ToArray());

        var adminUserDiagnostics = adminUsers
            .Select(adminUser =>
            {
                var roleIds = activeRoleIdsByAdminUserId.TryGetValue(adminUser.Id, out var activeRoleIds)
                    ? activeRoleIds
                    : [];

                return new AdminRoleAssignmentDiagnosticsUserResult(
                    AdminUserId: adminUser.Id,
                    LinkedUserId: adminUser.UserId,
                    Status: adminUser.Status,
                    RoleIds: roleIds,
                    ActiveRoleCount: roleIds.Length,
                    DisabledAtUtc: adminUser.DisabledAtUtc,
                    CreatedAtUtc: adminUser.CreatedAtUtc);
            })
            .ToArray();

        return new AdminRoleAssignmentDiagnosticsResult(
            TotalAdminUsers: adminUsers.Count,
            ActiveAdminUsers: adminUsers.Count(adminUser => adminUser.DisabledAtUtc is null
                && string.Equals(adminUser.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase)),
            DisabledAdminUsers: adminUsers.Count(adminUser => adminUser.DisabledAtUtc is not null
                || !string.Equals(adminUser.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase)),
            PendingInviteAdminUsers: adminUsers.Count(adminUser => string.Equals(adminUser.Status, PendingInviteStatus, StringComparison.OrdinalIgnoreCase)),
            TotalRoleAssignments: roleAssignments.Count,
            ActiveRoleAssignments: roleAssignments.Count(role => role.RevokedAtUtc is null),
            RevokedRoleAssignments: roleAssignments.Count(role => role.RevokedAtUtc is not null),
            TotalRoleAssignmentEvents: totalRoleAssignmentEvents,
            RolesInUse: roleAssignments
                .Where(role => role.RevokedAtUtc is null)
                .Select(role => role.RoleId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(roleId => roleId, StringComparer.Ordinal)
                .ToArray(),
            AdminUsers: adminUserDiagnostics,
            GeneratedAtUtc: DateTimeOffset.UtcNow);
    }
}
