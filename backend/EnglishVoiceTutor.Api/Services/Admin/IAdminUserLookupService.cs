using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminUserLookupService
{
    Task<AdminUserLookupResult> GetByEmailAsync(string? email, CancellationToken cancellationToken);
}

public sealed class AdminUserLookupResult
{
    public bool IsInvalidEmail { get; init; }
    public AdminUserLookupResponse? Response { get; init; }
}
