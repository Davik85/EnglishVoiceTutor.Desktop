namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminCapabilitiesResponse
{
    public string AdminSource { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = [];
    public AdminCapabilitiesSnapshot Capabilities { get; init; } = new();
    public DateTimeOffset CheckedAtUtc { get; init; }
}
