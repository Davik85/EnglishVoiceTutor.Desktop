using System.Security.Claims;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminRoleAssignmentActorResolver
{
    Task<AdminRoleAssignmentActorResolutionResult> ResolveActorAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
