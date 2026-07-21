using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services.Admin;
using EnglishVoiceTutor.Api.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnglishVoiceTutor.Api.Tests.Services.Admin;

public sealed class AdminFeedbackReportReplyServiceTests
{
    [Fact]
    public void OnlySuperAdminAndSupportReceiveFeedbackReportsReply()
    {
        var permissions = new AdminRolePermissionCatalogService().GetProductionRolePermissions();

        Assert.Contains(AdminPermissionConstants.FeedbackReportsReply, permissions[AdminRoleConstants.SuperAdmin]);
        Assert.Contains(AdminPermissionConstants.FeedbackReportsReply, permissions[AdminRoleConstants.Support]);
        Assert.DoesNotContain(AdminPermissionConstants.FeedbackReportsReply, permissions[AdminRoleConstants.ContentEditor]);
        Assert.DoesNotContain(AdminPermissionConstants.FeedbackReportsReply, permissions[AdminRoleConstants.BillingSupport]);
        Assert.DoesNotContain(AdminPermissionConstants.FeedbackReportsReply, permissions[AdminRoleConstants.ReadOnlyAuditor]);
    }

    [Fact]
    public async Task SuccessfulReplyPersistsPendingBeforeDeliverySendsSafeMessageAndReviewsNewReport()
    {
        await using var db = CreateDbContext();
        var fixture = await AddFixtureAsync(db, "new", null, "Learner");
        var sender = new FakeEmailSender { OnSend = () => Assert.Equal(1, db.UserFeedbackReportReplies.Count()) };
        var service = new AdminFeedbackReportReplyService(db, sender, new AdminAuditService(db));

        var result = await service.SendAsync(fixture.AdminUser.Id, fixture.Report.Id, "  Thank you for your report.  ", TestContext.Current.CancellationToken);

        Assert.NotNull(result.Response);
        Assert.Equal(UserFeedbackReportReplyConstants.DeliveryStatuses.Sent, result.Response.DeliveryStatus);
        Assert.Equal("reviewed", result.Response.ReportStatus);
        Assert.NotNull(result.Response.ReviewedAtUtc);
        Assert.Equal("learner@example.test", sender.Message?.RecipientEmail);
        Assert.Equal("Language Voice Tutor support", sender.Message?.Subject);
        Assert.Contains("Hello Learner,", sender.Message?.PlainTextBody);
        Assert.Contains("Thank you for your report.", sender.Message?.PlainTextBody);
        Assert.Contains("https://languagevoicetutor.com/", sender.Message?.PlainTextBody);
        Assert.Contains("Language Voice Tutor Support", sender.Message?.PlainTextBody);
        var attempt = await db.UserFeedbackReportReplies.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(fixture.AdminUser.Id, attempt.AdminUserId);
        Assert.Equal("Thank you for your report.", attempt.ReplyText);
        Assert.NotNull(attempt.SentAtUtc);
        var audits = await db.AdminActions.OrderBy(action => action.ActionType).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal([AdminAuditConstants.ActionTypes.FeedbackReportReplySent, AdminAuditConstants.ActionTypes.FeedbackReportStatusChanged], audits.Select(action => action.ActionType));
        Assert.All(audits, audit =>
        {
            Assert.DoesNotContain("Thank you for your report.", audit.SafeMetadataJson);
            Assert.DoesNotContain("learner@example.test", audit.SafeMetadataJson);
            Assert.DoesNotContain("Original report", audit.SafeMetadataJson);
        });
    }

    [Fact]
    public async Task AccountDeletionRequestUsesTheExistingReplyDeliveryFlow()
    {
        await using var db = CreateDbContext();
        var fixture = await AddFixtureAsync(db, "new", null, null, UserFeedbackReportConstants.AccountDeletionCategory);
        var sender = new FakeEmailSender();

        var result = await new AdminFeedbackReportReplyService(db, sender, new AdminAuditService(db)).SendAsync(
            fixture.AdminUser.Id, fixture.Report.Id, "We need more information.", TestContext.Current.CancellationToken);

        Assert.Equal(UserFeedbackReportReplyConstants.DeliveryStatuses.Sent, result.Response?.DeliveryStatus);
        Assert.Equal(UserFeedbackReportConstants.ReviewedStatus, result.Response?.ReportStatus);
        Assert.Equal("learner@example.test", sender.Message?.RecipientEmail);
        Assert.Single(await db.UserFeedbackReportReplies.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("reviewed")]
    [InlineData("resolved")]
    public async Task SuccessfulReplyPreservesExistingReportStatusAndReviewedTimestamp(string status)
    {
        await using var db = CreateDbContext();
        var reviewedAtUtc = DateTimeOffset.Parse("2026-07-01T00:00:00Z");
        var fixture = await AddFixtureAsync(db, status, reviewedAtUtc, null);
        var service = new AdminFeedbackReportReplyService(db, new FakeEmailSender(), new AdminAuditService(db));

        var result = await service.SendAsync(fixture.AdminUser.Id, fixture.Report.Id, "Reply", TestContext.Current.CancellationToken);

        Assert.Equal(status, result.Response?.ReportStatus);
        Assert.Equal(reviewedAtUtc, result.Response?.ReviewedAtUtc);
        Assert.Single(await db.AdminActions.Where(action => action.ActionType == AdminAuditConstants.ActionTypes.FeedbackReportReplySent).ToListAsync(TestContext.Current.CancellationToken));
        Assert.False(await db.AdminActions.AnyAsync(action => action.ActionType == AdminAuditConstants.ActionTypes.FeedbackReportStatusChanged, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnconfiguredAndFailedDeliveryPersistSeparateSafeFailedAttemptsWithoutStatusChange()
    {
        await using var db = CreateDbContext();
        var fixture = await AddFixtureAsync(db, "new", null, null);
        var unavailable = new FakeEmailSender { IsConfiguredValue = false };
        var failing = new FakeEmailSender { ThrowOnSend = true };

        var unavailableResult = await new AdminFeedbackReportReplyService(db, unavailable, new AdminAuditService(db)).SendAsync(
            fixture.AdminUser.Id, fixture.Report.Id, "First reply", TestContext.Current.CancellationToken);
        var failedResult = await new AdminFeedbackReportReplyService(db, failing, new AdminAuditService(db)).SendAsync(
            fixture.AdminUser.Id, fixture.Report.Id, "Second reply", TestContext.Current.CancellationToken);

        Assert.True(unavailableResult.IsDeliveryFailed);
        Assert.Equal("email_not_configured", unavailableResult.Response?.FailureCode);
        Assert.True(failedResult.IsDeliveryFailed);
        Assert.Equal("email_delivery_failed", failedResult.Response?.FailureCode);
        var attempts = await db.UserFeedbackReportReplies.OrderBy(attempt => attempt.ReplyText).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, attempts.Count);
        Assert.All(attempts, attempt => Assert.Equal(UserFeedbackReportReplyConstants.DeliveryStatuses.Failed, attempt.DeliveryStatus));
        Assert.DoesNotContain("raw transport failure", attempts.Single(attempt => attempt.ReplyText == "Second reply").FailureMessage);
        var report = await db.UserFeedbackReports.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("new", report.Status);
        Assert.Null(report.ReviewedAtUtc);
        Assert.Equal(2, await db.AdminActions.CountAsync(action => action.ActionType == AdminAuditConstants.ActionTypes.FeedbackReportReplyFailed, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InvalidUnknownRecipientUnavailableAndUnlinkedAdminCreateNoAttempt()
    {
        await using var db = CreateDbContext();
        var fixture = await AddFixtureAsync(db, "new", null, null);
        var service = new AdminFeedbackReportReplyService(db, new FakeEmailSender(), new AdminAuditService(db));

        var invalid = await service.SendAsync(fixture.AdminUser.Id, fixture.Report.Id, " ", TestContext.Current.CancellationToken);
        var missing = await service.SendAsync(fixture.AdminUser.Id, Guid.NewGuid(), "Reply", TestContext.Current.CancellationToken);
        fixture.User.Email = " ";
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var unavailableRecipient = await service.SendAsync(fixture.AdminUser.Id, fixture.Report.Id, "Reply", TestContext.Current.CancellationToken);
        fixture.User.Email = "learner@example.test";
        fixture.AdminUser.UserId = null;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var unavailableActor = await service.SendAsync(fixture.AdminUser.Id, fixture.Report.Id, "Reply", TestContext.Current.CancellationToken);

        Assert.True(invalid.IsInvalid);
        Assert.True(missing.IsNotFound);
        Assert.True(unavailableRecipient.IsRecipientUnavailable);
        Assert.True(unavailableActor.IsActorUnavailable);
        Assert.Equal(0, await db.UserFeedbackReportReplies.CountAsync(TestContext.Current.CancellationToken));
    }

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private static async Task<Fixture> AddFixtureAsync(AppDbContext db, string status, DateTimeOffset? reviewedAtUtc, string? displayName, string category = "app_issue")
    {
        var user = new UserEntity { Id = Guid.NewGuid(), Email = "learner@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        var adminAppUser = new UserEntity { Id = Guid.NewGuid(), Email = "admin@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        var adminUser = new AdminUserEntity { Id = Guid.NewGuid(), UserId = adminAppUser.Id, NormalizedEmail = "admin@example.test", Status = "active", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow };
        var report = new UserFeedbackReportEntity { Id = Guid.NewGuid(), UserId = user.Id, Category = category, Message = "Original report", ReportedAiText = "Original AI text", Status = status, ReviewedAtUtc = reviewedAtUtc, ClientPlatform = "windows", ClientVersion = "1.0.0", CreatedAtUtc = DateTimeOffset.UtcNow };
        db.AddRange(user, adminAppUser, adminUser, report);
        if (displayName is not null)
        {
            db.UserProfiles.Add(new UserProfileEntity { Id = Guid.NewGuid(), UserId = user.Id, DisplayName = displayName, NativeLanguage = "English", CurrentLevel = "A1", Timezone = "UTC", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new Fixture(user, adminUser, report);
    }

    private sealed record Fixture(UserEntity User, AdminUserEntity AdminUser, UserFeedbackReportEntity Report);

    private sealed class FakeEmailSender : IEmailSender
    {
        public bool IsConfiguredValue { get; init; } = true;
        public bool ThrowOnSend { get; init; }
        public Action? OnSend { get; init; }
        public bool IsConfigured => IsConfiguredValue;
        public EmailMessage? Message { get; private set; }
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            OnSend?.Invoke();
            Message = message;
            if (ThrowOnSend) throw new InvalidOperationException("raw transport failure");
            return Task.CompletedTask;
        }
    }
}
