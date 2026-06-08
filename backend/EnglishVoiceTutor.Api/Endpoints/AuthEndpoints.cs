using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Auth;
using EnglishVoiceTutor.Api.Services.Admin;
using EnglishVoiceTutor.Api.Services.Auth;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost(ApiConstants.AuthRegisterRoute, RegisterAsync);
        app.MapPost(ApiConstants.AuthLoginRoute, LoginAsync);
        app.MapGet(ApiConstants.AuthMeRoute, GetMeAsync).RequireAuthorization();
        app.MapPost(ApiConstants.AuthChangePasswordRoute, ChangePasswordAsync).RequireAuthorization();
        app.MapPost(ApiConstants.AuthPasswordResetRequestRoute, RequestPasswordResetAsync);
        app.MapPost(ApiConstants.AuthPasswordResetConfirmRoute, ConfirmPasswordResetAsync);
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IAuthService authService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AuthEndpoints");

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required." });
        }

        if (request.Password.Length < AuthConstants.MinimumPasswordLength)
        {
            return Results.BadRequest(new { error = $"Password must be at least {AuthConstants.MinimumPasswordLength} characters." });
        }

        try
        {
            var response = await authService.RegisterAsync(request, cancellationToken);
            logger.LogInformation("Auth register completed. Result=Created");
            return Results.Created(ApiConstants.AuthMeRoute, response);
        }
        catch (AuthDuplicateEmailException)
        {
            logger.LogInformation("Auth register completed. Result=Conflict; Reason=DuplicateEmail");
            return Results.Conflict(new { error = AuthConstants.DuplicateEmailError });
        }
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAuthService authService,
        IBootstrapAdminAccessService bootstrapAdminAccessService,
        HttpContext httpContext,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required." });
        }

        var response = await authService.LoginAsync(request, cancellationToken);
        if (response is null)
        {
            loggerFactory.CreateLogger("AuthEndpoints").LogInformation("Auth login completed. Result=Unauthorized");
            return Results.Unauthorized();
        }

        var principal = CreatePrincipal(response);
        if (bootstrapAdminAccessService.IsBootstrapAdmin(principal))
        {
            await httpContext.SignInAsync(
                AdminAuthorizationConstants.AdminCookieAuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    AllowRefresh = false,
                    ExpiresUtc = response.ExpiresAtUtc,
                    IsPersistent = false
                });
        }
        else
        {
            await httpContext.SignOutAsync(AdminAuthorizationConstants.AdminCookieAuthenticationScheme);
        }

        loggerFactory.CreateLogger("AuthEndpoints").LogInformation("Auth login completed. Result=Ok");
        return Results.Ok(response);
    }

    private static ClaimsPrincipal CreatePrincipal(AuthResponse response)
    {
        var claims = new List<Claim>
        {
            new(AuthClaimTypes.UserId, response.User.UserId.ToString()),
            new(ClaimTypes.Email, response.User.Email)
        };

        if (!string.IsNullOrWhiteSpace(response.User.DisplayName))
        {
            claims.Add(new Claim(AuthClaimTypes.DisplayName, response.User.DisplayName));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, AdminAuthorizationConstants.AdminCookieAuthenticationScheme));
    }

    private static async Task<IResult> RequestPasswordResetAsync(
        PasswordResetRequest request,
        IPasswordResetService passwordResetService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            await passwordResetService.RequestPasswordResetAsync(request, cancellationToken);
            loggerFactory.CreateLogger("AuthEndpoints").LogInformation("Password reset request completed. Result=Accepted");
            return Results.Ok(new PasswordResetResponse { Message = AuthConstants.PasswordResetAcceptedMessage });
        }
        catch (PasswordResetDeliveryUnavailableException)
        {
            loggerFactory.CreateLogger("AuthEndpoints").LogWarning("Password reset request completed. Result=DeliveryUnavailable");
            return Results.Json(new PasswordResetResponse { Message = AuthConstants.PasswordResetDeliveryUnavailableMessage }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static async Task<IResult> ConfirmPasswordResetAsync(
        PasswordResetConfirmRequest request,
        IPasswordResetService passwordResetService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var confirmed = await passwordResetService.ConfirmPasswordResetAsync(request, cancellationToken);
        loggerFactory.CreateLogger("AuthEndpoints").LogInformation("Password reset confirm completed. Result={Result}", confirmed ? "Ok" : "Rejected");

        if (!confirmed)
        {
            return Results.BadRequest(new PasswordResetResponse { Message = AuthConstants.PasswordResetInvalidMessage });
        }

        return Results.Ok(new PasswordResetResponse { Message = AuthConstants.PasswordResetConfirmedMessage });
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        IAuthService authService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var result = await authService.ChangePasswordAsync(userId.Value, request, cancellationToken);
        loggerFactory.CreateLogger("AuthEndpoints").LogInformation("Password change completed. Result={Result}", result);

        return result == ChangePasswordResult.Success
            ? Results.Ok(new ChangePasswordResponse { Message = AuthConstants.PasswordChangeSuccessMessage })
            : Results.BadRequest(new ChangePasswordResponse { Message = AuthConstants.PasswordChangeInvalidMessage });
    }

    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        IAuthService authService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var user = await authService.GetCurrentUserAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            loggerFactory.CreateLogger("AuthEndpoints").LogInformation("Auth me completed. Result=NotFound");
            return Results.NotFound(new { error = AuthConstants.MissingAuthUserError });
        }

        loggerFactory.CreateLogger("AuthEndpoints").LogInformation("Auth me completed. Result=Ok");
        return Results.Ok(user);
    }
}
