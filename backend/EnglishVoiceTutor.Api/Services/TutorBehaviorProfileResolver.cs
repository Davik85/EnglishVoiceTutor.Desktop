using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services.Cms;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Models.LessonContent;

namespace EnglishVoiceTutor.Api.Services;

/// <summary>Resolves AI tutor behavior from the validated published runtime, with the existing static profile as fallback.</summary>
public sealed class TutorBehaviorProfileResolver(
    ICmsRuntimeLessonContentService runtimeContentService,
    TutorAvatarProfileProvider staticProfiles)
{
    private static readonly string[] RequiredLevels = ["a1", "a2", "b1", "b2"];

    public async Task<TutorAvatarProfile> ResolveAsync(string? tutorId, CancellationToken cancellationToken = default)
    {
        var canonicalId = CanonicalId(tutorId);
        var staticProfile = staticProfiles.GetById(canonicalId);
        try
        {
            var runtime = await runtimeContentService.ReadRuntimeLessonContentAsync(cancellationToken);
            if (runtime.Success
                && !runtime.FallbackUsed
                && string.Equals(runtime.Source, CmsContentConstants.Sources.CmsPublishedSnapshot, StringComparison.Ordinal)
                && runtime.Content is not null)
            {
                var matches = runtime.Content.TutorBehaviorProfiles
                    .Where(candidate => !string.IsNullOrWhiteSpace(candidate.TutorId)
                        && string.Equals(CanonicalId(candidate.TutorId), canonicalId, StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToArray();
                if (matches.Length == 1 && IsUsable(matches[0], canonicalId))
                {
                    return Map(matches[0].TutorProfile, staticProfile, canonicalId);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // The static profile remains available if the published runtime cannot be read.
        }

        return staticProfile;
    }

    private static string CanonicalId(string? tutorId)
    {
        var normalized = TutorAvatarOptions.ToCanonicalId(tutorId);
        return TutorAvatarOptions.All.FirstOrDefault(option => string.Equals(option.Id, normalized, StringComparison.OrdinalIgnoreCase))?.Id
            ?? normalized;
    }

    private static bool IsUsable(CmsPublishedTutorBehaviorProfile published, string canonicalId)
    {
        var profile = published.TutorProfile;
        return published.IsActive
            && !string.IsNullOrWhiteSpace(published.TutorId)
            && profile is not null
            && !string.IsNullOrWhiteSpace(profile.Id)
            && string.Equals(CanonicalId(profile.Id), canonicalId, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(published.DisplayName)
            && string.Equals(published.DisplayName, profile.DisplayName, StringComparison.Ordinal)
            && profile.CommunicationStyle is { Count: > 0 } && profile.CommunicationStyle.All(value => !string.IsNullOrWhiteSpace(value))
            && profile.SpeakingRules is not null
            && RequiredLevels.All(level => profile.SpeakingRules.Any(rule => string.Equals(rule.Key, level, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(rule.Value)))
            && profile.IdentityRules is { Count: > 0 } && profile.IdentityRules.All(value => !string.IsNullOrWhiteSpace(value));
    }

    private static TutorAvatarProfile Map(TutorProfile profile, TutorAvatarProfile staticProfile, string canonicalId) => new()
    {
        Id = canonicalId,
        DisplayName = profile.DisplayName,
        Age = staticProfile.Age,
        HomeCity = staticProfile.HomeCity,
        CountryOrRegion = staticProfile.CountryOrRegion,
        Studies = staticProfile.Studies,
        Hobbies = [.. staticProfile.Hobbies],
        CommunicationStyle = [.. profile.CommunicationStyle],
        SpeakingRules = new Dictionary<string, string>(profile.SpeakingRules, StringComparer.OrdinalIgnoreCase),
        IdentityRules = [.. profile.IdentityRules]
    };
}
