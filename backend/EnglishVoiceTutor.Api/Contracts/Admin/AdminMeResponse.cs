namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminMeResponse
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public bool IsAdmin { get; init; }

    public string AdminSource { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];

    public IReadOnlyList<string> Permissions { get; init; } = [];

    public bool IsBootstrapAdmin { get; init; }

    public DateTimeOffset CheckedAtUtc { get; init; }
}
