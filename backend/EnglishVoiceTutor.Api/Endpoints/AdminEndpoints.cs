using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Services.Admin;
using EnglishVoiceTutor.Api.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class AdminEndpoints
{
    private const string EmailQueryKey = "email";
    private const string EmailRequiredError = "Email query parameter is required.";

    public static void MapAdminEndpoints(this WebApplication app)
    {
        app.MapGet(ApiConstants.AdminMeRoute, GetAdminMe)
            .RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName);

        app.MapGet(ApiConstants.AdminUserByEmailRoute, GetAdminUserByEmailAsync)
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

    private static async Task<IResult> GetAdminUserByEmailAsync(
        [AsParameters] AdminUserLookupQuery query,
        IAdminUserLookupService adminUserLookupService,
        CancellationToken cancellationToken)
    {
        var lookupResult = await adminUserLookupService.GetByEmailAsync(query.Email, cancellationToken);

        if (lookupResult.IsInvalidEmail)
        {
            return Results.BadRequest(new Dictionary<string, string[]>
            {
                [EmailQueryKey] = [EmailRequiredError]
            });
        }

        if (lookupResult.Response is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(lookupResult.Response);
    }
}

public sealed class AdminUserLookupQuery
{
    public string? Email { get; init; }
}
