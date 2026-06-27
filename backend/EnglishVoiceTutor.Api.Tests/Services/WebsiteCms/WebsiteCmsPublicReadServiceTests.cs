using System.Text.Json;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities.WebsiteCms;
using EnglishVoiceTutor.Api.Services.WebsiteCms;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services.WebsiteCms;

public sealed class WebsiteCmsPublicReadServiceTests
{
    [Fact]
    public async Task GetPublicTextsAsync_ReturnsOnlyDraftBodyBySafeSectionKey()
    {
        await using var dbContext = CreateDbContext();
        dbContext.WebsiteCmsSections.Add(new WebsiteCmsSectionEntity
        {
            Id = Guid.NewGuid(),
            SectionKey = "legal_terms",
            DraftBody = "Admin saved public terms",
            PublishedBody = "Old published text",
            ReviewStatus = "legal_approved",
            InternalNotes = "InternalNotes must not leak",
            ChangeReason = "ChangeReason must not leak",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            PublishedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = await new WebsiteCmsPublicReadService(dbContext).GetPublicTextsAsync(TestContext.Current.CancellationToken);
        var json = JsonSerializer.Serialize(response);

        Assert.Equal("Admin saved public terms", response.Texts["legal_terms"]);
        Assert.DoesNotContain("InternalNotes", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ChangeReason", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("legal_approved", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Old published text", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("billing", json, StringComparison.OrdinalIgnoreCase);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"website-cms-public-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }
}
