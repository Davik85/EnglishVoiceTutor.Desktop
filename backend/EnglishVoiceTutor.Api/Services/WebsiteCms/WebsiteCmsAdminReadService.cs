using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.WebsiteCms;

public sealed class WebsiteCmsAdminReadService(AppDbContext dbContext) : IWebsiteCmsAdminReadService
{
    private static readonly IReadOnlyList<ExpectedWebsiteCmsSection> ExpectedSections = WebsiteCmsExpectedSections.All;

    private readonly AppDbContext _dbContext = dbContext;

    public async Task<AdminWebsiteCmsSectionOverviewResponse> GetSectionOverviewAsync(CancellationToken cancellationToken)
    {
        var sectionKeys = ExpectedSections.Select(section => section.SectionKey).ToArray();
        var storedSectionRows = await _dbContext.WebsiteCmsSections
            .AsNoTracking()
            .Where(section => sectionKeys.Contains(section.SectionKey))
            .ToListAsync(cancellationToken);
        var storedSections = storedSectionRows.ToDictionary(section => section.SectionKey, StringComparer.Ordinal);

        return new AdminWebsiteCmsSectionOverviewResponse
        {
            CheckedAtUtc = DateTimeOffset.UtcNow,
            Sections = ExpectedSections.Select(expected =>
            {
                storedSections.TryGetValue(expected.SectionKey, out var stored);
                return new AdminWebsiteCmsSectionOverviewItem
                {
                    SectionKey = expected.SectionKey,
                    DisplayName = expected.DisplayName,
                    Description = expected.Description,
                    StoredRowExists = stored is not null,
                    ReviewStatus = stored?.ReviewStatus,
                    EffectiveDate = stored?.EffectiveDate,
                    UpdatedAtUtc = stored?.UpdatedAtUtc,
                    PublishedAtUtc = stored?.PublishedAtUtc,
                    DraftBodyExists = !string.IsNullOrWhiteSpace(stored?.DraftBody),
                    PublishedBodyExists = !string.IsNullOrWhiteSpace(stored?.PublishedBody)
                };
            }).ToArray()
        };
    }

    public async Task<AdminWebsiteCmsSectionDetailResponse?> GetSectionDetailAsync(string sectionKey, CancellationToken cancellationToken)
    {
        var expected = ExpectedSections.SingleOrDefault(section => string.Equals(section.SectionKey, sectionKey, StringComparison.Ordinal));
        if (expected is null)
        {
            return null;
        }

        var stored = await _dbContext.WebsiteCmsSections
            .AsNoTracking()
            .SingleOrDefaultAsync(section => section.SectionKey == sectionKey, cancellationToken);

        if (stored is null)
        {
            return null;
        }

        return new AdminWebsiteCmsSectionDetailResponse
        {
            SectionKey = expected.SectionKey,
            DisplayName = expected.DisplayName,
            Description = expected.Description,
            ReviewStatus = stored.ReviewStatus,
            EffectiveDate = stored.EffectiveDate,
            DraftBody = stored.DraftBody,
            PublishedBodyExists = !string.IsNullOrWhiteSpace(stored.PublishedBody),
            PublishedAtUtc = stored.PublishedAtUtc,
            InternalNotes = stored.InternalNotes,
            ChangeReason = stored.ChangeReason,
            CreatedAtUtc = stored.CreatedAtUtc,
            UpdatedAtUtc = stored.UpdatedAtUtc,
            CheckedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
