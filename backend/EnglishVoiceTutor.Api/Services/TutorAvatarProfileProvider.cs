using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class TutorAvatarProfileProvider
{
    public TutorAvatarProfile GetDefault()
    {
        return TutorAvatarProfiles.Elena;
    }

    public TutorAvatarProfile GetById(string? avatarId)
    {
        if (string.IsNullOrWhiteSpace(avatarId))
        {
            return GetDefault();
        }

        return TutorAvatarProfiles.All.FirstOrDefault(avatar => string.Equals(avatar.Id, avatarId.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? GetDefault();
    }
}
