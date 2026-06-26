using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.WebsiteCms;

public sealed class WebsiteCmsAdminReadService(AppDbContext dbContext) : IWebsiteCmsAdminReadService
{
    private static readonly IReadOnlyList<ExpectedWebsiteCmsSection> ExpectedSections =
    [
        new("seller_company", "Seller / Company", "Seller identity, company profile, and public business-contact context."),
        new("support", "Support", "Customer support contact, response expectations, and help-channel guidance."),
        new("pricing", "Pricing", "Public pricing-plan description and review-safe billing explanation."),
        new("terms", "Terms", "Terms of service overview and legal policy copy."),
        new("privacy", "Privacy", "Privacy policy overview and data-handling policy copy."),
        new("refunds", "Refunds", "Refund policy and customer support expectations for refund requests."),
        new("cancellation", "Cancellation", "Cancellation policy and subscription-renewal explanation copy."),
        new("ai_data_disclosures", "AI / Data Disclosures", "AI usage, learner data handling, and safety disclosure copy."),
        new("platform_status", "Platform Status", "Platform availability, service status, and operational notice copy.")
    ];

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

    private sealed record ExpectedWebsiteCmsSection(string SectionKey, string DisplayName, string Description);
}
