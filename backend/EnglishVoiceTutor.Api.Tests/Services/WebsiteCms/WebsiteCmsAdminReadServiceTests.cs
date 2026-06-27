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

        var response = await new WebsiteCmsAdminReadService(dbContext).GetSectionOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal(12, response.Sections.Count);
        Assert.Contains(response.Sections, section => section.SectionKey == "legal_seller_company" && !section.StoredRowExists);
        Assert.Contains(response.Sections, section => section.SectionKey == "legal_platform_status" && !section.DraftBodyExists && !section.PublishedBodyExists);
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
            SectionKey = "legal_privacy",
            DraftBody = "Draft body must not be exposed.",
            PublishedBody = "Published body must not be exposed.",
            ReviewStatus = "approved",
            EffectiveDate = new DateOnly(2026, 7, 1),
            CreatedAtUtc = updatedAt.AddDays(-1),
            UpdatedAtUtc = updatedAt,
            PublishedAtUtc = publishedAt
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = await new WebsiteCmsAdminReadService(dbContext).GetSectionOverviewAsync(TestContext.Current.CancellationToken);
        var privacy = Assert.Single(response.Sections, section => section.SectionKey == "legal_privacy");

        Assert.True(privacy.StoredRowExists);
        Assert.Equal("approved", privacy.ReviewStatus);
        Assert.Equal(new DateOnly(2026, 7, 1), privacy.EffectiveDate);
        Assert.Equal(updatedAt, privacy.UpdatedAtUtc);
        Assert.Equal(publishedAt, privacy.PublishedAtUtc);
        Assert.True(privacy.DraftBodyExists);
        Assert.True(privacy.PublishedBodyExists);
    }

    [Fact]
    public async Task GetSectionDetailAsync_ReturnsInitializedRowAndRejectsUnknownKey()
    {
        await using var dbContext = CreateDbContext();
        var now = DateTimeOffset.Parse("2026-06-26T12:00:00Z");
        dbContext.WebsiteCmsSections.Add(new WebsiteCmsSectionEntity
        {
            Id = Guid.NewGuid(),
            SectionKey = "legal_support",
            DraftBody = "Support draft",
            PublishedBody = null,
            ReviewStatus = "draft",
            EffectiveDate = new DateOnly(2026, 7, 1),
            InternalNotes = "Internal",
            ChangeReason = "Initial draft",
            CreatedAtUtc = now.AddHours(-1),
            UpdatedAtUtc = now,
            PublishedAtUtc = null
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new WebsiteCmsAdminReadService(dbContext);
        var detail = await service.GetSectionDetailAsync("legal_support", TestContext.Current.CancellationToken);
        var unknown = await service.GetSectionDetailAsync("unknown", TestContext.Current.CancellationToken);

        Assert.NotNull(detail);
        Assert.Equal("legal_support", detail.SectionKey);
        Assert.Equal("Support draft", detail.DraftBody);
        Assert.False(detail.PublishedBodyExists);
        Assert.Null(unknown);
    }

    [Fact]
    public async Task ValidateDraftAsync_ReturnsWarningForEmptyDraftAndBlocksSecretLikeDraft()
    {
        await using var dbContext = CreateDbContext();
        dbContext.WebsiteCmsSections.AddRange(
            new WebsiteCmsSectionEntity
            {
                Id = Guid.NewGuid(), SectionKey = "legal_privacy", DraftBody = " ", PublishedBody = null, ReviewStatus = "not_started", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow
            },
            new WebsiteCmsSectionEntity
            {
                Id = Guid.NewGuid(), SectionKey = "legal_terms", DraftBody = "bearer abcdefghijklmnopqrstuvwxyz123456", PublishedBody = null, ReviewStatus = "draft", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow
            });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new WebsiteCmsAdminReadService(dbContext);
        var empty = await service.ValidateDraftAsync("legal_privacy", TestContext.Current.CancellationToken);
        var blocked = await service.ValidateDraftAsync("legal_terms", TestContext.Current.CancellationToken);

        Assert.NotNull(empty);
        Assert.Equal("warning", empty.Status);
        Assert.NotEmpty(empty.Warnings);
        Assert.Empty(empty.Errors);
        Assert.NotNull(blocked);
        Assert.Equal("blocked", blocked.Status);
        Assert.Contains(blocked.Errors, error => error.Contains("blocked secret-like marker", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetDraftPreviewAsync_ReturnsAdminOnlyDraftPreviewWithoutModifyingDatabase()
    {
        await using var dbContext = CreateDbContext();
        var updatedAt = DateTimeOffset.Parse("2026-06-26T10:00:00Z");
        dbContext.WebsiteCmsSections.Add(new WebsiteCmsSectionEntity
        {
            Id = Guid.NewGuid(), SectionKey = "legal_support", DraftBody = "Support draft", PublishedBody = "Published", ReviewStatus = "owner_approved", EffectiveDate = new DateOnly(2026, 7, 1), InternalNotes = "Admin note", ChangeReason = "Existing", CreatedAtUtc = updatedAt.AddDays(-1), UpdatedAtUtc = updatedAt, PublishedAtUtc = updatedAt.AddHours(1)
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var preview = await new WebsiteCmsAdminReadService(dbContext).GetDraftPreviewAsync("legal_support", TestContext.Current.CancellationToken);
        var row = await dbContext.WebsiteCmsSections.SingleAsync(section => section.SectionKey == "legal_support", TestContext.Current.CancellationToken);

        Assert.NotNull(preview);
        Assert.Equal("legal_support", preview.SectionKey);
        Assert.Equal("Support draft", preview.DraftBody);
        Assert.Equal("owner_approved", preview.ReviewStatus);
        Assert.Equal("Admin note", preview.AdminOnlyInternalNotes);
        Assert.Equal("Published", row.PublishedBody);
        Assert.Equal(updatedAt, row.UpdatedAtUtc);
        Assert.Equal(updatedAt.AddHours(1), row.PublishedAtUtc);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
