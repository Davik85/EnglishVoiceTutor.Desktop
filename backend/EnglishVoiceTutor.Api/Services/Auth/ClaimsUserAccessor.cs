using System.Security.Claims;
using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Services.Auth;

public static class ClaimsUserAccessor
{
    public static Guid? TryGetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(AuthClaimTypes.UserId);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
