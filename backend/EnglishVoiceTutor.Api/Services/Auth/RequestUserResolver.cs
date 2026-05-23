using System.Security.Claims;

namespace EnglishVoiceTutor.Api.Services.Auth;

public sealed record ResolvedRequestUser(Guid UserId, string Source);

public interface IRequestUserResolver
{
    ResolvedRequestUser ResolveCurrentUser();
}

public sealed class RequestUserResolver(
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment hostEnvironment,
    DevUserProvider devUserProvider,
    ILogger<RequestUserResolver> logger) : IRequestUserResolver
{
    public const string AuthenticatedSource = "authenticated";
    public const string DevelopmentSource = "development";

    public ResolvedRequestUser ResolveCurrentUser()
    {
        var hasAuthorizationHeader = !string.IsNullOrWhiteSpace(httpContextAccessor.HttpContext?.Request.Headers.Authorization);
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var authenticatedUserId = ClaimsUserAccessor.TryGetUserId(principal);
            if (authenticatedUserId.HasValue)
            {
                logger.LogInformation("Request user resolved: Source={Source}; AuthorizationHeaderPresent={AuthorizationHeaderPresent}.", AuthenticatedSource, hasAuthorizationHeader);
                return new ResolvedRequestUser(authenticatedUserId.Value, AuthenticatedSource);
            }
        }

        if (hostEnvironment.IsDevelopment())
        {
            logger.LogInformation("Request user resolved: Source={Source}; AuthorizationHeaderPresent={AuthorizationHeaderPresent}.", DevelopmentSource, hasAuthorizationHeader);
            return new ResolvedRequestUser(devUserProvider.GetDevUserId(), DevelopmentSource);
        }

        logger.LogWarning("Request user resolution fell back outside Development without an authenticated JWT user.");
        return new ResolvedRequestUser(devUserProvider.GetDevUserId(), DevelopmentSource);
    }
}
