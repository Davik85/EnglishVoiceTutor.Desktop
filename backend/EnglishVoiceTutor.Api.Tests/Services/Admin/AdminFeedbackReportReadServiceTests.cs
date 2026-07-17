using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services.Admin;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EnglishVoiceTutor.Api.Tests.Services.Admin;

public sealed class AdminFeedbackReportReadServiceTests
{
    [Fact]
    public void OnlySuperAdminAndSupportReceiveFeedbackReportsRead()
    {
        var permissions = new AdminRolePermissionCatalogService().GetProductionRolePermissions();

        Assert.Contains(AdminPermissionConstants.FeedbackReportsRead, permissions[AdminRoleConstants.SuperAdmin]);
        Assert.Contains(AdminPermissionConstants.FeedbackReportsRead, permissions[AdminRoleConstants.Support]);
        Assert.DoesNotContain(AdminPermissionConstants.FeedbackReportsRead, permissions[AdminRoleConstants.ContentEditor]);
        Assert.DoesNotContain(AdminPermissionConstants.FeedbackReportsRead, permissions[AdminRoleConstants.BillingSupport]);
        Assert.DoesNotContain(AdminPermissionConstants.FeedbackReportsRead, permissions[AdminRoleConstants.ReadOnlyAuditor]);
    }

    [Fact]
    public async Task ListReturnsNewestFilteredPagedSafePreviewsAndUserIdentity()
    {
        await using var db = CreateDbContext();
        var user = AddUser(db, "learner@example.test", "Learner Name");
        var oldest = AddReport(user.Id, "suggestion", "old", "new", DateTimeOffset.Parse("2026-07-01T00:00:00Z"));
        var middle = AddReport(user.Id, "app_issue", "middle", "reviewed", DateTimeOffset.Parse("2026-07-02T00:00:00Z"));
        var newest = AddReport(user.Id, "app_issue", new string('x', 250), "new", DateTimeOffset.Parse("2026-07-03T00:00:00Z"));
        db.UserFeedbackReports.AddRange(oldest, middle, newest);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new AdminFeedbackReportReadService(db);

        var page = await service.ListAsync(null, null, 1, 2, TestContext.Current.CancellationToken);
        var filtered = await service.ListAsync("new", "app_issue", 1, 50, TestContext.Current.CancellationToken);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal([newest.Id, middle.Id], page.Items.Select(item => item.ReportId));
        Assert.Equal(200, page.Items[0].MessagePreview.Length);
        Assert.NotEqual(newest.Message, page.Items[0].MessagePreview);
        Assert.Equal("learner@example.test", page.Items[0].UserEmail);
        Assert.Equal("Learner Name", page.Items[0].UserDisplayName);
        Assert.Single(filtered.Items);
        Assert.Equal(newest.Id, filtered.Items[0].ReportId);
        Assert.Equal(1, filtered.TotalCount);
    }

    [Fact]
    public async Task DetailsReturnsSafeFieldsDoesNotMutateAndReturnsNullWhenMissing()
    {
        await using var db = CreateDbContext();
        var user = AddUser(db, "details@example.test", "Details Name");
        var reviewedAtUtc = DateTimeOffset.Parse("2026-07-04T00:00:00Z");
        var report = AddReport(user.Id, "ai_response", "Full report", "reviewed", DateTimeOffset.Parse("2026-07-03T00:00:00Z"));
        report.ReportedAiText = "Reported response";
        report.ReviewedAtUtc = reviewedAtUtc;
        db.UserFeedbackReports.Add(report);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new AdminFeedbackReportReadService(db);

        var result = await service.GetByIdAsync(report.Id, TestContext.Current.CancellationToken);
        var missing = await service.GetByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        var persisted = await db.UserFeedbackReports.SingleAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(report.Id, result.ReportId);
        Assert.Equal("Full report", result.Message);
        Assert.Equal("Reported response", result.ReportedAiText);
        Assert.Equal(user.Id, result.User.UserId);
        Assert.Equal("details@example.test", result.User.Email);
        Assert.Equal("Details Name", result.User.DisplayName);
        Assert.Equal("reviewed", persisted.Status);
        Assert.Equal(reviewedAtUtc, persisted.ReviewedAtUtc);
        Assert.Null(missing);
    }

    [Fact]
    public async Task DetailsReturnsRepliesNewestFirstWithOnlyApprovedReplyFields()
    {
        await using var db = CreateDbContext();
        var user = AddUser(db, "replies@example.test", "Reply User");
        var report = AddReport(user.Id, "suggestion", "Full report", "new", DateTimeOffset.Parse("2026-07-03T00:00:00Z"));
        var admin = new UserEntity { Id = Guid.NewGuid(), Email = "admin@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        var oldestCreatedAtUtc = DateTimeOffset.Parse("2026-07-04T00:00:00Z");
        var newestCreatedAtUtc = DateTimeOffset.Parse("2026-07-05T00:00:00Z");
        db.Users.Add(admin);
        db.UserFeedbackReports.Add(report);
        db.UserFeedbackReportReplies.AddRange(
            new UserFeedbackReportReplyEntity { Id = Guid.NewGuid(), FeedbackReportId = report.Id, AdminUserId = admin.Id, ReplyText = "Sent reply", RecipientEmail = "recipient@example.test", DeliveryStatus = "sent", CreatedAtUtc = oldestCreatedAtUtc, SentAtUtc = oldestCreatedAtUtc.AddMinutes(1) },
            new UserFeedbackReportReplyEntity { Id = Guid.NewGuid(), FeedbackReportId = report.Id, AdminUserId = admin.Id, ReplyText = "Failed reply", RecipientEmail = "recipient@example.test", DeliveryStatus = "failed", CreatedAtUtc = newestCreatedAtUtc, FailureCode = "email_delivery_failed", FailureMessage = "Sensitive provider error" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new AdminFeedbackReportReadService(db);

        var result = await service.GetByIdAsync(report.Id, TestContext.Current.CancellationToken);
        var list = await service.ListAsync(null, null, 1, 50, TestContext.Current.CancellationToken);
        var detailsJson = JsonSerializer.Serialize(result);
        var listJson = JsonSerializer.Serialize(list);

        Assert.NotNull(result);
        Assert.Equal(["Failed reply", "Sent reply"], result.Replies.Select(reply => reply.ReplyText));
        Assert.All(result.Replies, reply => Assert.Equal("recipient@example.test", reply.RecipientEmail));
        Assert.Null(result.Replies[0].SentAtUtc);
        Assert.Equal("email_delivery_failed", result.Replies[0].FailureCode);
        Assert.Equal(oldestCreatedAtUtc.AddMinutes(1), result.Replies[1].SentAtUtc);
        Assert.DoesNotContain("FailureMessage", detailsJson, StringComparison.Ordinal);
        Assert.DoesNotContain("Sensitive provider error", detailsJson, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminUserId", detailsJson, StringComparison.Ordinal);
        Assert.DoesNotContain("ReplyText", listJson, StringComparison.Ordinal);
        Assert.DoesNotContain("RecipientEmail", listJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DetailsReturnsAnEmptyReplyCollection()
    {
        await using var db = CreateDbContext();
        var user = AddUser(db, "empty-replies@example.test", "Reply User");
        var report = AddReport(user.Id, "suggestion", "Full report", "new", DateTimeOffset.Parse("2026-07-03T00:00:00Z"));
        db.UserFeedbackReports.Add(report);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new AdminFeedbackReportReadService(db).GetByIdAsync(report.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Empty(result.Replies);
    }

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static UserEntity AddUser(AppDbContext db, string email, string displayName)
    {
        var user = new UserEntity { Id = Guid.NewGuid(), Email = email, PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        db.Users.Add(user);
        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = Guid.NewGuid(), UserId = user.Id, DisplayName = displayName, NativeLanguage = "English",
            CurrentLevel = "A1", Timezone = "UTC", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });
        return user;
    }

    private static UserFeedbackReportEntity AddReport(Guid userId, string category, string message, string status, DateTimeOffset createdAtUtc) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, Category = category, Message = message, Status = status,
        ClientPlatform = "windows", ClientVersion = "1.0.0", CreatedAtUtc = createdAtUtc
    };
}
