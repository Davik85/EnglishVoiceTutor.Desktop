using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Devices;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Devices;
using System.Security.Claims;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this WebApplication app)
    {
        app.MapPost(ApiConstants.MeDevicesRoute, RegisterAuthenticatedDeviceAsync).RequireAuthorization();
    }

    private static async Task<IResult> RegisterAuthenticatedDeviceAsync(
        DeviceRegistrationRequest request,
        ClaimsPrincipal principal,
        IDeviceRegistrationService deviceRegistrationService,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var response = await deviceRegistrationService.RegisterAsync(userId.Value, request, cancellationToken);
        return Results.Ok(response);
    }
}
