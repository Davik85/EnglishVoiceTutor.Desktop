using System.Text.Json;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class TutorAvatarProfileProvider
{
    private const string DefaultAvatarId = "elena";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<TutorAvatarProfileProvider> logger;

    public TutorAvatarProfileProvider(ILogger<TutorAvatarProfileProvider> logger)
    {
        this.logger = logger;
    }

    public TutorAvatarProfile GetDefault()
    {
        return GetById(DefaultAvatarId);
    }

    public TutorAvatarProfile GetById(string? avatarId)
    {
        var normalizedAvatarId = string.IsNullOrWhiteSpace(avatarId) ? DefaultAvatarId : avatarId.Trim();
        var profilePath = ResolveProfilePath(normalizedAvatarId);

        if (profilePath is null)
        {
            logger.LogWarning("Tutor profile file was not found. TutorProfileId={TutorProfileId}; FallingBackToMinimalProfile={DefaultTutorProfileId}.", normalizedAvatarId, DefaultAvatarId);
            return CreateMinimalFallbackProfile(normalizedAvatarId);
        }

        try
        {
            var json = File.ReadAllText(profilePath);
            var profile = JsonSerializer.Deserialize<TutorAvatarProfile>(json, JsonOptions);
            if (profile is not null && !string.IsNullOrWhiteSpace(profile.Id) && !string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                return profile;
            }

            logger.LogWarning("Tutor profile file was empty or invalid. TutorProfileId={TutorProfileId}; ProfilePath={ProfilePath}.", normalizedAvatarId, profilePath);
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Tutor profile file could not be loaded. TutorProfileId={TutorProfileId}; ProfilePath={ProfilePath}.", normalizedAvatarId, profilePath);
        }

        return CreateMinimalFallbackProfile(normalizedAvatarId);
    }

    private static string? ResolveProfilePath(string avatarId)
    {
        var fileName = $"{avatarId}.json";
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Content", "Tutors", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Content", "Tutors", fileName),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Content", "Tutors", fileName)),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "Content", "Tutors", fileName))
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static TutorAvatarProfile CreateMinimalFallbackProfile(string requestedAvatarId)
    {
        return new TutorAvatarProfile
        {
            Id = string.IsNullOrWhiteSpace(requestedAvatarId) ? DefaultAvatarId : requestedAvatarId,
            DisplayName = "Elena",
            Age = 22,
            HomeCity = "London",
            CountryOrRegion = "United Kingdom",
            Studies = "fashion design",
            Hobbies = ["padel", "art"],
            CommunicationStyle = ["friendly", "warm", "supportive", "clear", "brief"],
            SpeakingRules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["a1"] = "Use very short sentences. Ask one simple question at a time.",
                ["a2"] = "Use short sentences and simple follow-up questions.",
                ["b1"] = "Use natural but concise conversation.",
                ["b2"] = "Use natural conversation with more nuance, but avoid long monologues."
            },
            IdentityRules =
            [
                "Always introduce yourself as Elena when asked your name.",
                "Do not claim to be from another city or country.",
                "Do not invent a different job, age, hobby, or background.",
                "In roleplay, adapt to the role, but keep the avatar identity when personal questions arise."
            ]
        };
    }
}
