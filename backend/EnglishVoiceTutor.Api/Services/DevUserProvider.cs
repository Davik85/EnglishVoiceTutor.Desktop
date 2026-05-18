namespace EnglishVoiceTutor.Api.Services;

public sealed class DevUserProvider
{
    private const string DevUserIdValue = "7a0f6073-09a0-47c2-b1f2-91f2a727f5e9";

    // Temporary stable development user until real authentication is added.
    public Guid GetDevUserId()
    {
        if (!Guid.TryParse(DevUserIdValue, out var devUserId))
        {
            throw new InvalidOperationException("The configured development user id is invalid.");
        }

        return devUserId;
    }
}
