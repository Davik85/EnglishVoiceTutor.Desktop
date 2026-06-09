using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Auth;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IBootstrapAdminAccessService
{
    bool IsBootstrapAdmin(ClaimsPrincipal principal);
}

public sealed class BootstrapAdminAccessService(
    IOptions<AdminBootstrapOptions> optionsAccessor) : IBootstrapAdminAccessService
{
    private readonly AdminBootstrapOptions _options = optionsAccessor.Value;

    public bool IsBootstrapAdmin(ClaimsPrincipal principal)
    {
        if (!_options.Enabled)
        {
            return false;
        }

        var email = ClaimsUserAccessor.TryGetUserEmail(principal);
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return false;
        }

        return _options.AdminEmails
            .Select(NormalizeEmail)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Contains(normalizedEmail, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim();
    }
}
