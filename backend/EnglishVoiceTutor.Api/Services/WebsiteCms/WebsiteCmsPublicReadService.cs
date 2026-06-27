using EnglishVoiceTutor.Api.Contracts.Website;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.WebsiteCms;

public sealed class WebsiteCmsPublicReadService(AppDbContext dbContext) : IWebsiteCmsPublicReadService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<WebsiteTextsResponse> GetPublicTextsAsync(CancellationToken cancellationToken)
    {
        var expectedKeys = WebsiteCmsExpectedSections.All.Select(section => section.SectionKey).ToArray();
        var rows = await _dbContext.WebsiteCmsSections
            .AsNoTracking()
            .Where(section => expectedKeys.Contains(section.SectionKey))
            .Select(section => new { section.SectionKey, section.DraftBody })
            .ToListAsync(cancellationToken);

        var texts = rows
            .Where(row => !string.IsNullOrWhiteSpace(row.DraftBody))
            .ToDictionary(row => row.SectionKey, row => row.DraftBody, StringComparer.Ordinal);

        return new WebsiteTextsResponse
        {
            Texts = texts,
            CheckedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
