using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities.WebsiteCms;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.WebsiteCms;

public sealed class WebsiteCmsAdminMutationService(AppDbContext dbContext) : IWebsiteCmsAdminMutationService
{
    private const string InitialReviewStatus = "not_started";
    private const string InitializationChangeReason = "Initialize Website CMS section metadata";

    private readonly AppDbContext _dbContext = dbContext;

    public async Task<AdminWebsiteCmsSectionInitializationResponse> InitializeMissingSectionsAsync(CancellationToken cancellationToken)
    {
        var expectedSections = WebsiteCmsExpectedSections.All;
        var expectedKeys = expectedSections.Select(section => section.SectionKey).ToArray();
        var existingKeys = await _dbContext.WebsiteCmsSections
            .Where(section => expectedKeys.Contains(section.SectionKey))
            .Select(section => section.SectionKey)
            .ToListAsync(cancellationToken);
        var existing = new HashSet<string>(existingKeys, StringComparer.Ordinal);
        var now = DateTimeOffset.UtcNow;
        var results = new List<AdminWebsiteCmsSectionInitializationResult>(expectedSections.Count);

        foreach (var expected in expectedSections)
        {
            if (existing.Contains(expected.SectionKey))
            {
                results.Add(new AdminWebsiteCmsSectionInitializationResult { SectionKey = expected.SectionKey, State = "existing", Created = false });
                continue;
            }

            _dbContext.WebsiteCmsSections.Add(new WebsiteCmsSectionEntity
            {
                Id = Guid.NewGuid(),
                SectionKey = expected.SectionKey,
                DraftBody = string.Empty,
                PublishedBody = null,
                ReviewStatus = InitialReviewStatus,
                EffectiveDate = null,
                InternalNotes = null,
                ChangeReason = InitializationChangeReason,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                PublishedAtUtc = null
            });
            existing.Add(expected.SectionKey);
            results.Add(new AdminWebsiteCmsSectionInitializationResult { SectionKey = expected.SectionKey, State = "created", Created = true });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminWebsiteCmsSectionInitializationResponse
        {
            CreatedCount = results.Count(result => result.Created),
            ExistingCount = results.Count(result => !result.Created),
            TotalExpectedCount = expectedSections.Count,
            Sections = results,
            CheckedAtUtc = now
        };
    }
}
