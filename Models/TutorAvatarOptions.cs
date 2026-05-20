namespace EnglishVoiceTutor.Desktop.Models;

public static class TutorAvatarOptions
{
    public const string DefaultAvatarId = "elena";
    public const string NelliAvatarId = "nelli";

    public static readonly TutorAvatarOption Elena = new(
        Id: DefaultAvatarId,
        DisplayName: "Elena");

    public static readonly TutorAvatarOption Nelli = new(
        Id: NelliAvatarId,
        DisplayName: "Nelli",
        ConversationImageScale: 0.94,
        ConversationOffsetY: 22.0);

    public static readonly IReadOnlyList<TutorAvatarOption> All =
    [
        Elena,
        Nelli
    ];

    public static TutorAvatarOption GetById(string? avatarId)
    {
        if (string.IsNullOrWhiteSpace(avatarId))
        {
            return Elena;
        }

        return All.FirstOrDefault(avatar => string.Equals(avatar.Id, avatarId.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? Elena;
    }
}
