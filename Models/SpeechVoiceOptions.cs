namespace EnglishVoiceTutor.Desktop.Models;

public static class SpeechVoiceOptions
{
    public const string AlloyVoiceId = "alloy";
    public const string AshVoiceId = "ash";
    public const string BalladVoiceId = "ballad";
    public const string CoralVoiceId = "coral";
    public const string EchoVoiceId = "echo";
    public const string SageVoiceId = "sage";
    public const string ShimmerVoiceId = "shimmer";
    public const string VerseVoiceId = "verse";
    public const string MarinVoiceId = "marin";
    public const string CedarVoiceId = "cedar";

    public static readonly SpeechVoiceOption Alloy = new(
        Id: AlloyVoiceId,
        DisplayName: "Alloy — neutral voice",
        Description: "Neutral tutor voice");

    public static readonly SpeechVoiceOption Ash = new(
        Id: AshVoiceId,
        DisplayName: "Ash — calm voice",
        Description: "Calm tutor voice");

    public static readonly SpeechVoiceOption Ballad = new(
        Id: BalladVoiceId,
        DisplayName: "Ballad — tutor voice",
        Description: "Ballad tutor voice");

    public static readonly SpeechVoiceOption Coral = new(
        Id: CoralVoiceId,
        DisplayName: "Coral — warm female-style voice",
        Description: "Warm female-style tutor voice");

    public static readonly SpeechVoiceOption Echo = new(
        Id: EchoVoiceId,
        DisplayName: "Echo — clear male-style voice",
        Description: "Clear male-style tutor voice");

    public static readonly SpeechVoiceOption Sage = new(
        Id: SageVoiceId,
        DisplayName: "Sage — calm voice",
        Description: "Calm tutor voice");

    public static readonly SpeechVoiceOption Shimmer = new(
        Id: ShimmerVoiceId,
        DisplayName: "Shimmer — soft female-style voice",
        Description: "Soft female-style tutor voice");

    public static readonly SpeechVoiceOption Verse = new(
        Id: VerseVoiceId,
        DisplayName: "Verse — tutor voice",
        Description: "Verse tutor voice");

    public static readonly SpeechVoiceOption Marin = new(
        Id: MarinVoiceId,
        DisplayName: "Marin — tutor voice",
        Description: "Marin tutor voice");

    public static readonly SpeechVoiceOption Cedar = new(
        Id: CedarVoiceId,
        DisplayName: "Cedar — tutor voice",
        Description: "Cedar tutor voice");

    public static readonly IReadOnlyList<SpeechVoiceOption> All =
    [
        Alloy,
        Ash,
        Ballad,
        Coral,
        Echo,
        Sage,
        Shimmer,
        Verse,
        Marin,
        Cedar
    ];

    public static bool TryGetSupportedId(string? voiceId, out string canonicalId)
    {
        var supportedVoice = All.FirstOrDefault(voice => string.Equals(voice.Id, voiceId?.Trim(), StringComparison.OrdinalIgnoreCase));
        canonicalId = supportedVoice?.Id ?? string.Empty;
        return supportedVoice is not null;
    }

    public static string ResolveSupportedVoiceId(string? voiceId, string? avatarId = null)
    {
        return TryGetSupportedId(voiceId, out var canonicalId)
            ? canonicalId
            : GetPreferredVoiceIdForTutor(avatarId);
    }

    public static SpeechVoiceOption GetById(string? voiceId, string? avatarId = null)
    {
        var canonicalId = ResolveSupportedVoiceId(voiceId, avatarId);
        return All.First(voice => voice.Id == canonicalId);
    }

    public static string GetPreferredVoiceIdForTutor(string? avatarId)
    {
        var normalizedAvatarId = TutorAvatarOptions.GetById(avatarId).Id;
        return string.Equals(normalizedAvatarId, TutorAvatarOptions.DavidAvatarId, StringComparison.OrdinalIgnoreCase)
            ? CedarVoiceId
            : CoralVoiceId;
    }
}
