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

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
