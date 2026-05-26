using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Services.Admin;
using EnglishVoiceTutor.Api.Services.Auth;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        app.MapGet(ApiConstants.AdminMeRoute, GetAdminMe)
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);
    }

    private static IResult GetAdminMe(ClaimsPrincipal principal)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        var email = ClaimsUserAccessor.TryGetUserEmail(principal);

        if (!userId.HasValue || string.IsNullOrWhiteSpace(email))
        {
            return Results.Unauthorized();
        }

        var response = new AdminMeResponse
        {
            UserId = userId.Value,
            Email = email,
            IsAdmin = true,
            AdminSource = AdminAuthorizationConstants.BootstrapAdminSource,
            CheckedAtUtc = DateTimeOffset.UtcNow
        };

        return Results.Ok(response);
    }
}
