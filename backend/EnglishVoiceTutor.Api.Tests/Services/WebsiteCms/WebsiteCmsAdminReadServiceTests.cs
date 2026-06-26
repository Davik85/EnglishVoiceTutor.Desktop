using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities.WebsiteCms;
using EnglishVoiceTutor.Api.Services.WebsiteCms;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services.WebsiteCms;

public sealed class WebsiteCmsAdminReadServiceTests
{
    [Fact]
    public async Task GetSectionOverviewAsync_ReturnsAllExpectedSectionsWhenDatabaseIsEmpty()
    {
        await using var dbContext = CreateDbContext();

        var response = await new WebsiteCmsAdminReadService(dbContext).GetSectionOverviewAsync(CancellationToken.None);

        Assert.Equal(9, response.Sections.Count);
        Assert.Contains(response.Sections, section => section.SectionKey == "seller_company" && !section.StoredRowExists);
        Assert.Contains(response.Sections, section => section.SectionKey == "platform_status" && !section.DraftBodyExists && !section.PublishedBodyExists);
    }

    [Fact]
    public async Task GetSectionOverviewAsync_ReturnsMetadataOnlyForStoredRows()
    {
        await using var dbContext = CreateDbContext();
        var updatedAt = DateTimeOffset.Parse("2026-06-26T10:00:00Z");
        var publishedAt = DateTimeOffset.Parse("2026-06-26T11:00:00Z");
        dbContext.WebsiteCmsSections.Add(new WebsiteCmsSectionEntity
        {
            Id = Guid.NewGuid(),
            SectionKey = "privacy",
            DraftBody = "Draft body must not be exposed.",
            PublishedBody = "Published body must not be exposed.",
            ReviewStatus = "approved",
            EffectiveDate = new DateOnly(2026, 7, 1),
            CreatedAtUtc = updatedAt.AddDays(-1),
            UpdatedAtUtc = updatedAt,
            PublishedAtUtc = publishedAt
        });
        await dbContext.SaveChangesAsync();

        var response = await new WebsiteCmsAdminReadService(dbContext).GetSectionOverviewAsync(CancellationToken.None);
        var privacy = Assert.Single(response.Sections.Where(section => section.SectionKey == "privacy"));

        Assert.True(privacy.StoredRowExists);
        Assert.Equal("approved", privacy.ReviewStatus);
        Assert.Equal(new DateOnly(2026, 7, 1), privacy.EffectiveDate);
        Assert.Equal(updatedAt, privacy.UpdatedAtUtc);
        Assert.Equal(publishedAt, privacy.PublishedAtUtc);
        Assert.True(privacy.DraftBodyExists);
        Assert.True(privacy.PublishedBodyExists);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
