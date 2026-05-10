using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class TutorAvatarProfileProvider
{
    public TutorAvatarProfile GetDefault()
    {
        return TutorAvatarProfiles.Default;
    }
}
