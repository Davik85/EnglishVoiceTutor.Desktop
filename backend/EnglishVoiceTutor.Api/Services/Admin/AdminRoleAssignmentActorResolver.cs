using EnglishVoiceTutor.Api.Services.Auth;
using System.Security.Claims;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminRoleAssignmentActorResolver(
    IAdminRoleAssignmentReadService adminRoleAssignmentReadService) : IAdminRoleAssignmentActorResolver
{
    public const string ActorMappingUnavailableErrorCode = "admin_role_assignment_actor_mapping_unavailable";

    private readonly IAdminRoleAssignmentReadService _adminRoleAssignmentReadService = adminRoleAssignmentReadService;

    public async Task<AdminRoleAssignmentActorResolutionResult> ResolveActorAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        if (userId.HasValue)
        {
            var userIdResult = await _adminRoleAssignmentReadService.GetEffectiveRolesByUserIdAsync(
                userId.Value,
                cancellationToken);

            if (TryCreateSuccess(userIdResult, out var success))
            {
                return success;
            }

            if (userIdResult.IsDisabled)
            {
                return AdminRoleAssignmentActorResolutionResult.Unavailable(
                    "Persistent Admin actor mapping is disabled, so role assignment revocation is unavailable for this principal.");
            }
        }

        var email = ClaimsUserAccessor.TryGetUserEmail(principal);
        var normalizedEmail = NormalizeEmail(email);
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var emailResult = await _adminRoleAssignmentReadService.GetEffectiveRolesByNormalizedEmailAsync(
                normalizedEmail,
                cancellationToken);

            if (TryCreateSuccess(emailResult, out var success))
            {
                return success;
            }

            if (emailResult.IsDisabled)
            {
                return AdminRoleAssignmentActorResolutionResult.Unavailable(
                    "Persistent Admin actor mapping is disabled, so role assignment revocation is unavailable for this principal.");
            }
        }

        return AdminRoleAssignmentActorResolutionResult.Unavailable(
            "Persistent Admin actor mapping is not available for the authenticated principal, so role assignment revocation is disabled until safe actor identity and actor role resolution are available.");
    }

    private static bool TryCreateSuccess(
        AdminRoleAssignmentReadResult readResult,
        out AdminRoleAssignmentActorResolutionResult result)
    {
        if (readResult is { IsAdminUserFound: true, IsDisabled: false, AdminUserId: { } adminUserId }
            && readResult.RoleIds.Count > 0)
        {
            result = AdminRoleAssignmentActorResolutionResult.Success(adminUserId, readResult.RoleIds);
            return true;
        }

        result = AdminRoleAssignmentActorResolutionResult.Unavailable(string.Empty);
        return false;
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }
}
