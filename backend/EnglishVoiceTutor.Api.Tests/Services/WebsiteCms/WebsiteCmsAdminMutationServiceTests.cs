using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities.WebsiteCms;
using EnglishVoiceTutor.Api.Services.WebsiteCms;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services.WebsiteCms;

public sealed class WebsiteCmsAdminMutationServiceTests
{
    [Fact]
    public async Task InitializeMissingSectionsAsync_EmptyDatabaseCreatesAllExpectedSections()
    {
        await using var dbContext = CreateDbContext();

        var response = await new WebsiteCmsAdminMutationService(dbContext).InitializeMissingSectionsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(9, response.CreatedCount);
        Assert.Equal(0, response.ExistingCount);
        Assert.Equal(9, response.TotalExpectedCount);
        Assert.All(response.Sections, section => Assert.True(section.Created));
        var rows = await dbContext.WebsiteCmsSections.OrderBy(section => section.SectionKey).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(9, rows.Count);
        Assert.All(rows, row =>
        {
            Assert.Equal(string.Empty, row.DraftBody);
            Assert.Null(row.PublishedBody);
            Assert.Equal("not_started", row.ReviewStatus);
            Assert.Null(row.EffectiveDate);
            Assert.Null(row.InternalNotes);
            Assert.Equal("Initialize Website CMS section metadata", row.ChangeReason);
            Assert.Null(row.PublishedAtUtc);
        });
    }

    [Fact]
    public async Task InitializeMissingSectionsAsync_SecondCallIsIdempotentAndCreatesNoDuplicates()
    {
        await using var dbContext = CreateDbContext();
        var service = new WebsiteCmsAdminMutationService(dbContext);

        await service.InitializeMissingSectionsAsync(TestContext.Current.CancellationToken);
        var secondResponse = await service.InitializeMissingSectionsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, secondResponse.CreatedCount);
        Assert.Equal(9, secondResponse.ExistingCount);
        Assert.Equal(9, await dbContext.WebsiteCmsSections.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(9, await dbContext.WebsiteCmsSections.Select(section => section.SectionKey).Distinct().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InitializeMissingSectionsAsync_DoesNotOverwriteExistingRows()
    {
        await using var dbContext = CreateDbContext();
        var createdAt = DateTimeOffset.Parse("2026-06-20T10:00:00Z");
        var updatedAt = DateTimeOffset.Parse("2026-06-21T10:00:00Z");
        dbContext.WebsiteCmsSections.Add(new WebsiteCmsSectionEntity
        {
            Id = Guid.NewGuid(),
            SectionKey = "privacy",
            DraftBody = "Existing draft",
            PublishedBody = "Existing published",
            ReviewStatus = "approved",
            EffectiveDate = new DateOnly(2026, 7, 1),
            InternalNotes = "Keep notes",
            ChangeReason = "Existing reason",
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = updatedAt,
            PublishedAtUtc = updatedAt
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = await new WebsiteCmsAdminMutationService(dbContext).InitializeMissingSectionsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(8, response.CreatedCount);
        Assert.Equal(1, response.ExistingCount);
        var privacy = await dbContext.WebsiteCmsSections.SingleAsync(section => section.SectionKey == "privacy", TestContext.Current.CancellationToken);
        Assert.Equal("Existing draft", privacy.DraftBody);
        Assert.Equal("Existing published", privacy.PublishedBody);
        Assert.Equal("approved", privacy.ReviewStatus);
        Assert.Equal(new DateOnly(2026, 7, 1), privacy.EffectiveDate);
        Assert.Equal("Keep notes", privacy.InternalNotes);
        Assert.Equal("Existing reason", privacy.ChangeReason);
        Assert.Equal(createdAt, privacy.CreatedAtUtc);
        Assert.Equal(updatedAt, privacy.UpdatedAtUtc);
        Assert.Equal(updatedAt, privacy.PublishedAtUtc);
    }

    [Fact]
    public async Task SaveDraftAsync_RequiresChangeReason()
    {
        await using var dbContext = CreateDbContext();
        await new WebsiteCmsAdminMutationService(dbContext).InitializeMissingSectionsAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new WebsiteCmsAdminMutationService(dbContext).SaveDraftAsync("privacy", new()
        {
            DraftBody = "Draft",
            ReviewStatus = "draft",
            ChangeReason = " "
        }, TestContext.Current.CancellationToken));

        Assert.Contains("Change reason is required", ex.Message);
    }

    [Fact]
    public async Task SaveDraftAsync_UpdatesDraftFieldsWithoutChangingPublishedFields()
    {
        await using var dbContext = CreateDbContext();
        var now = DateTimeOffset.Parse("2026-06-26T10:00:00Z");
        dbContext.WebsiteCmsSections.Add(new WebsiteCmsSectionEntity
        {
            Id = Guid.NewGuid(),
            SectionKey = "terms",
            DraftBody = "Old draft",
            PublishedBody = "Published remains",
            ReviewStatus = "not_started",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PublishedAtUtc = now.AddHours(1)
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = await new WebsiteCmsAdminMutationService(dbContext).SaveDraftAsync("terms", new()
        {
            DraftBody = "New draft",
            InternalNotes = "Notes",
            EffectiveDate = new DateOnly(2026, 8, 1),
            ReviewStatus = "owner_review_needed",
            ChangeReason = "Owner review prep"
        }, TestContext.Current.CancellationToken);

        var row = await dbContext.WebsiteCmsSections.SingleAsync(section => section.SectionKey == "terms", TestContext.Current.CancellationToken);
        Assert.NotNull(response);
        Assert.Equal("New draft", row.DraftBody);
        Assert.Equal("owner_review_needed", row.ReviewStatus);
        Assert.Equal(new DateOnly(2026, 8, 1), row.EffectiveDate);
        Assert.Equal("Notes", row.InternalNotes);
        Assert.Equal("Owner review prep", row.ChangeReason);
        Assert.Equal("Published remains", row.PublishedBody);
        Assert.Equal(now.AddHours(1), row.PublishedAtUtc);
    }

    [Fact]
    public async Task SaveDraftAsync_BlocksSecretLikeValues()
    {
        await using var dbContext = CreateDbContext();
        await new WebsiteCmsAdminMutationService(dbContext).InitializeMissingSectionsAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new WebsiteCmsAdminMutationService(dbContext).SaveDraftAsync("pricing", new()
        {
            DraftBody = "Do not save bearer abcdefghijklmnopqrstuvwxyz123456",
            ReviewStatus = "draft",
            ChangeReason = "Test guard"
        }, TestContext.Current.CancellationToken));

        Assert.Contains("blocked secret-like marker", ex.Message);
    }

    [Fact]
    public async Task SaveDraftAsync_RejectsUnknownKey()
    {
        await using var dbContext = CreateDbContext();

        var response = await new WebsiteCmsAdminMutationService(dbContext).SaveDraftAsync("unknown", new()
        {
            DraftBody = "Draft",
            ReviewStatus = "draft",
            ChangeReason = "Reason"
        }, TestContext.Current.CancellationToken);

        Assert.Null(response);
    }

    [Fact]
    public async Task UpdateReviewStatusAsync_RequiresChangeReason()
    {
        await using var dbContext = CreateDbContext();
        await new WebsiteCmsAdminMutationService(dbContext).InitializeMissingSectionsAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new WebsiteCmsAdminMutationService(dbContext).UpdateReviewStatusAsync("privacy", new()
        {
            ReviewStatus = "owner_review_needed",
            ChangeReason = " "
        }, TestContext.Current.CancellationToken));

        Assert.Contains("Change reason is required", ex.Message);
    }

    [Theory]
    [InlineData("owner_approved")]
    [InlineData("legal_approved")]
    public async Task UpdateReviewStatusAsync_UpdatesOnlyReviewMetadataAndNeverPublishes(string reviewStatus)
    {
        await using var dbContext = CreateDbContext();
        var now = DateTimeOffset.Parse("2026-06-26T10:00:00Z");
        dbContext.WebsiteCmsSections.Add(new WebsiteCmsSectionEntity
        {
            Id = Guid.NewGuid(), SectionKey = "terms", DraftBody = "Draft remains", PublishedBody = null, ReviewStatus = "draft", EffectiveDate = new DateOnly(2026, 8, 1), InternalNotes = "Notes", ChangeReason = "Old", CreatedAtUtc = now, UpdatedAtUtc = now, PublishedAtUtc = null
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = await new WebsiteCmsAdminMutationService(dbContext).UpdateReviewStatusAsync("terms", new()
        {
            ReviewStatus = reviewStatus,
            ChangeReason = "Internal review marker only"
        }, TestContext.Current.CancellationToken);

        var row = await dbContext.WebsiteCmsSections.SingleAsync(section => section.SectionKey == "terms", TestContext.Current.CancellationToken);
        Assert.NotNull(response);
        Assert.Equal(reviewStatus, row.ReviewStatus);
        Assert.Equal("Internal review marker only", row.ChangeReason);
        Assert.Equal("Draft remains", row.DraftBody);
        Assert.Null(row.PublishedBody);
        Assert.Null(row.PublishedAtUtc);
        Assert.Equal(new DateOnly(2026, 8, 1), row.EffectiveDate);
        Assert.Equal("Notes", row.InternalNotes);
    }


    [Fact]
    public async Task PublishSectionAsync_RejectsUnknownKey()
    {
        await using var dbContext = CreateDbContext();

        var response = await new WebsiteCmsAdminMutationService(dbContext).PublishSectionAsync("unknown", new() { ChangeReason = "Publish approved copy" }, TestContext.Current.CancellationToken);

        Assert.Null(response);
    }

    [Fact]
    public async Task PublishSectionAsync_RejectsEmptyDraft()
    {
        await using var dbContext = CreateDbContext();
        await new WebsiteCmsAdminMutationService(dbContext).InitializeMissingSectionsAsync(TestContext.Current.CancellationToken);
        var row = await dbContext.WebsiteCmsSections.SingleAsync(section => section.SectionKey == "privacy", TestContext.Current.CancellationToken);
        row.ReviewStatus = "legal_approved";
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new WebsiteCmsAdminMutationService(dbContext).PublishSectionAsync("privacy", new() { ChangeReason = "Publish approved copy" }, TestContext.Current.CancellationToken));

        Assert.Contains("DraftBody is required", ex.Message);
    }

    [Fact]
    public async Task PublishSectionAsync_RejectsMissingChangeReason()
    {
        await using var dbContext = CreateDbContext();
        SeedPublishableSection(dbContext, "terms", "Terms draft", "legal_approved");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new WebsiteCmsAdminMutationService(dbContext).PublishSectionAsync("terms", new() { ChangeReason = " " }, TestContext.Current.CancellationToken));

        Assert.Contains("Change reason is required", ex.Message);
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("owner_approved")]
    public async Task PublishSectionAsync_RejectsNonLegalApprovedReviewStatus(string reviewStatus)
    {
        await using var dbContext = CreateDbContext();
        SeedPublishableSection(dbContext, "support", "Support draft", reviewStatus);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new WebsiteCmsAdminMutationService(dbContext).PublishSectionAsync("support", new() { ChangeReason = "Publish approved copy" }, TestContext.Current.CancellationToken));

        Assert.Contains("requires legal_approved", ex.Message);
    }

    [Fact]
    public async Task PublishSectionAsync_BlocksSecretLikeDraftContent()
    {
        await using var dbContext = CreateDbContext();
        SeedPublishableSection(dbContext, "pricing", "Do not publish bearer abcdefghijklmnopqrstuvwxyz123456", "legal_approved");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new WebsiteCmsAdminMutationService(dbContext).PublishSectionAsync("pricing", new() { ChangeReason = "Publish approved copy" }, TestContext.Current.CancellationToken));

        Assert.Contains("blocked secret-like marker", ex.Message);
    }

    [Fact]
    public async Task PublishSectionAsync_CopiesDraftBodyToPublishedBodyAndSetsPublishedAtUtc()
    {
        await using var dbContext = CreateDbContext();
        SeedPublishableSection(dbContext, "privacy", "Approved privacy draft", "legal_approved");

        var response = await new WebsiteCmsAdminMutationService(dbContext).PublishSectionAsync("privacy", new() { ChangeReason = "Explicit legal-approved Website CMS publish" }, TestContext.Current.CancellationToken);

        var row = await dbContext.WebsiteCmsSections.SingleAsync(section => section.SectionKey == "privacy", TestContext.Current.CancellationToken);
        Assert.NotNull(response);
        Assert.Equal("Approved privacy draft", row.PublishedBody);
        Assert.NotNull(row.PublishedAtUtc);
        Assert.True(response.PublishedBodyExists);
        Assert.Equal(row.PublishedAtUtc, response.PublishedAtUtc);
        Assert.Equal("legal_approved", response.ReviewStatus);
        Assert.Contains("does not update public website rendering", response.Message);
        Assert.Contains("does not modify site/public", response.Message);
        Assert.Contains("does not enable live Paddle", response.Message);
    }

    private static void SeedPublishableSection(AppDbContext dbContext, string sectionKey, string draftBody, string reviewStatus)
    {
        var now = DateTimeOffset.Parse("2026-06-27T10:00:00Z");
        dbContext.WebsiteCmsSections.Add(new WebsiteCmsSectionEntity
        {
            Id = Guid.NewGuid(),
            SectionKey = sectionKey,
            DraftBody = draftBody,
            PublishedBody = null,
            ReviewStatus = reviewStatus,
            ChangeReason = "Seed",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PublishedAtUtc = null
        });
        dbContext.SaveChanges();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
