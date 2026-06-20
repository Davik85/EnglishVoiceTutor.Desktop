using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminRoleAssignmentReadService(
    AppDbContext dbContext,
    IAdminRolePermissionCatalogService adminRolePermissionCatalogService) : IAdminRoleAssignmentReadService
{
    private const string ActiveStatus = "active";

    private readonly AppDbContext _dbContext = dbContext;
    private readonly IAdminRolePermissionCatalogService _adminRolePermissionCatalogService = adminRolePermissionCatalogService;

    public Task<AdminRoleAssignmentReadResult> GetEffectiveRolesByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return GetEffectiveRolesAsync(
            adminUser => adminUser.UserId == userId,
            cancellationToken);
    }

    public Task<AdminRoleAssignmentReadResult> GetEffectiveRolesByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Task.FromResult(AdminRoleAssignmentReadResult.NotFound);
        }

        var trimmedNormalizedEmail = normalizedEmail.Trim();
        return GetEffectiveRolesAsync(
            adminUser => adminUser.NormalizedEmail == trimmedNormalizedEmail,
            cancellationToken);
    }

    private async Task<AdminRoleAssignmentReadResult> GetEffectiveRolesAsync(
        System.Linq.Expressions.Expression<Func<Data.Entities.AdminUserEntity, bool>> adminUserFilter,
        CancellationToken cancellationToken)
    {
        var adminUser = await _dbContext.AdminUsers
            .AsNoTracking()
            .Where(adminUserFilter)
            .Select(adminUser => new
            {
                adminUser.Id,
                adminUser.Status,
                adminUser.DisabledAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (adminUser is null)
        {
            return AdminRoleAssignmentReadResult.NotFound;
        }

        if (adminUser.DisabledAtUtc.HasValue || !string.Equals(adminUser.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return AdminRoleAssignmentReadResult.Disabled(adminUser.Id);
        }

        var knownRoleIds = _adminRolePermissionCatalogService
            .GetProductionRolePermissions()
            .Keys
            .ToArray();

        var roleIds = await _dbContext.AdminUserRoles
            .AsNoTracking()
            .Where(role => role.AdminUserId == adminUser.Id
                && role.RevokedAtUtc == null
                && knownRoleIds.Contains(role.RoleId))
            .OrderBy(role => role.RoleId)
            .Select(role => role.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new AdminRoleAssignmentReadResult(
            AdminUserId: adminUser.Id,
            IsAdminUserFound: true,
            IsDisabled: false,
            RoleIds: roleIds);
    }
}
