using EnglishVoiceTutor.Api.Services.Admin;
using Microsoft.AspNetCore.Authorization;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class BootstrapAdminRequirement : IAuthorizationRequirement
{
}

public sealed class BootstrapAdminAuthorizationHandler(
    IBootstrapAdminAccessService bootstrapAdminAccessService) : AuthorizationHandler<BootstrapAdminRequirement>
{
    private readonly IBootstrapAdminAccessService _bootstrapAdminAccessService = bootstrapAdminAccessService;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BootstrapAdminRequirement requirement)
    {
        if (_bootstrapAdminAccessService.IsBootstrapAdmin(context.User))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
