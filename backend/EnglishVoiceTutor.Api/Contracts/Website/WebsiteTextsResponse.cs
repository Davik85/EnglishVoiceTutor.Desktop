namespace EnglishVoiceTutor.Api.Contracts.Website;

public sealed class WebsiteTextsResponse
{
    public IReadOnlyDictionary<string, string> Texts { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
    public DateTimeOffset CheckedAtUtc { get; set; }
}
