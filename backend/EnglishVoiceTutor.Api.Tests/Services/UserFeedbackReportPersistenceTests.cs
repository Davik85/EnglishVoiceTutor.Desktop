using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class UserFeedbackReportPersistenceTests
{
    [Fact]
    public async Task SuggestionIsStoredForItsAuthenticatedUserWithNewStatus()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.UserFeedbackReports.Add(Create(userId, "suggestion", "A useful suggestion."));
        await db.SaveChangesAsync();
        var report = await db.UserFeedbackReports.SingleAsync();
        Assert.Equal(userId, report.UserId);
        Assert.Equal("suggestion", report.Category);
        Assert.Equal("new", report.Status);
    }

    [Fact]
    public async Task AppIssueIsStored()
    {
        await using var db = CreateDbContext();
        db.UserFeedbackReports.Add(Create(Guid.NewGuid(), "app_issue", "The screen stopped responding."));
        await db.SaveChangesAsync();
        Assert.Equal("app_issue", (await db.UserFeedbackReports.SingleAsync()).Category);
    }

    [Fact]
    public async Task AiResponseReportStoresOptionalText()
    {
        await using var db = CreateDbContext();
        db.UserFeedbackReports.Add(Create(Guid.NewGuid(), "ai_response", "Incorrect reply.", "The AI text."));
        await db.SaveChangesAsync();
        Assert.Equal("The AI text.", (await db.UserFeedbackReports.SingleAsync()).ReportedAiText);
    }

    private static UserFeedbackReportEntity Create(Guid userId, string category, string message, string? reportedAiText = null) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, Category = category, Message = message, ReportedAiText = reportedAiText,
        Status = "new", ClientPlatform = "android", ClientVersion = "0.1.0+1", CreatedAtUtc = DateTimeOffset.UtcNow
    };

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
