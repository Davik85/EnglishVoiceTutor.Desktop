using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.Models;

public static class SpeechVoiceOptions
{
    public const string AlloyVoiceId = "alloy";
    public const string AshVoiceId = "ash";
    public const string CoralVoiceId = "coral";
    public const string EchoVoiceId = "echo";
    public const string FableVoiceId = "fable";
    public const string OnyxVoiceId = "onyx";
    public const string NovaVoiceId = "nova";
    public const string SageVoiceId = "sage";
    public const string ShimmerVoiceId = "shimmer";

    public static readonly SpeechVoiceOption Coral = new(
        Id: CoralVoiceId,
        DisplayName: "Coral — warm female-style voice",
        Description: "Warm female-style tutor voice");

    public static readonly SpeechVoiceOption Nova = new(
        Id: NovaVoiceId,
        DisplayName: "Nova — bright female-style voice",
        Description: "Bright female-style tutor voice");

    public static readonly SpeechVoiceOption Shimmer = new(
        Id: ShimmerVoiceId,
        DisplayName: "Shimmer — soft female-style voice",
        Description: "Soft female-style tutor voice");

    public static readonly SpeechVoiceOption Onyx = new(
        Id: OnyxVoiceId,
        DisplayName: "Onyx — deep male-style voice",
        Description: "Deep male-style tutor voice");

    public static readonly SpeechVoiceOption Echo = new(
        Id: EchoVoiceId,
        DisplayName: "Echo — clear male-style voice",
        Description: "Clear male-style tutor voice");

    public static readonly SpeechVoiceOption Fable = new(
        Id: FableVoiceId,
        DisplayName: "Fable — expressive male-style voice",
        Description: "Expressive male-style tutor voice");

    public static readonly SpeechVoiceOption Alloy = new(
        Id: AlloyVoiceId,
        DisplayName: "Alloy — neutral voice",
        Description: "Neutral tutor voice");

    public static readonly SpeechVoiceOption Ash = new(
        Id: AshVoiceId,
        DisplayName: "Ash — calm voice",
        Description: "Calm tutor voice");

    public static readonly SpeechVoiceOption Sage = new(
        Id: SageVoiceId,
        DisplayName: "Sage — calm voice",
        Description: "Calm tutor voice");

    public static readonly IReadOnlyList<SpeechVoiceOption> All =
    [
        Coral,
        Nova,
        Shimmer,
        Onyx,
        Echo,
        Fable,
        Alloy,
        Ash,
        Sage
    ];

    public static SpeechVoiceOption GetById(string? voiceId)
    {
        if (string.IsNullOrWhiteSpace(voiceId))
        {
            return Coral;
        }

        return All.FirstOrDefault(voice => string.Equals(voice.Id, voiceId.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? Coral;
    }

    public static string GetPreferredVoiceIdForTutor(string? avatarId)
    {
        var normalizedAvatarId = TutorAvatarOptions.GetById(avatarId).Id;
        return string.Equals(normalizedAvatarId, TutorAvatarOptions.DavidAvatarId, StringComparison.OrdinalIgnoreCase)
            ? OnyxVoiceId
            : BackendConstants.DefaultBackendSettingsSpeechVoice;
    }
}
