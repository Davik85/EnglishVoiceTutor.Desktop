using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.Models;

public static class SpeechVoiceOptions
{
    public const string CoralVoiceId = "coral";
    public const string OnyxVoiceId = "onyx";

    public static readonly SpeechVoiceOption Coral = new(
        Id: CoralVoiceId,
        DisplayName: "Coral",
        Description: "Friendly tutor voice");

    public static readonly SpeechVoiceOption Onyx = new(
        Id: OnyxVoiceId,
        DisplayName: "Onyx (male)",
        Description: "Warm male tutor voice");

    public static readonly IReadOnlyList<SpeechVoiceOption> All =
    [
        Coral,
        Onyx
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
