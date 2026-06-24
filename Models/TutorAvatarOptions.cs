namespace EnglishVoiceTutor.Desktop.Models;

public static class TutorAvatarOptions
{
    public const string DefaultAvatarId = "lana";
    public const string LegacyElenaTutorAlias = "elena";
    public const string NelliAvatarId = "nelli";
    public const string DavidAvatarId = "david";

    public static readonly TutorAvatarOption Lana = new(
        Id: DefaultAvatarId,
        DisplayName: "Lana");

    public static readonly TutorAvatarOption Nelli = new(
        Id: NelliAvatarId,
        DisplayName: "Nelli");

    public static readonly TutorAvatarOption David = new(
        Id: DavidAvatarId,
        DisplayName: "David");

    public static readonly IReadOnlyList<TutorAvatarOption> All =
    [
        Lana,
        Nelli,
        David
    ];

    public static TutorAvatarOption GetById(string? avatarId)
    {
        var canonicalAvatarId = ToCanonicalId(avatarId);

        return All.FirstOrDefault(avatar => string.Equals(avatar.Id, canonicalAvatarId, StringComparison.OrdinalIgnoreCase))
            ?? Lana;
    }

    public static string ToCanonicalId(string? avatarId)
    {
        if (string.IsNullOrWhiteSpace(avatarId))
        {
            return DefaultAvatarId;
        }

        var trimmedAvatarId = avatarId.Trim();
        return string.Equals(trimmedAvatarId, LegacyElenaTutorAlias, StringComparison.OrdinalIgnoreCase)
            ? DefaultAvatarId
            : trimmedAvatarId;
    }
}
