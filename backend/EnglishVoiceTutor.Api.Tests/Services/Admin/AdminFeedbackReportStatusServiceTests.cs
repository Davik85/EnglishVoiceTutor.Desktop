using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnglishVoiceTutor.Api.Tests.Services.Admin;

public sealed class AdminFeedbackReportStatusServiceTests
{
    [Fact]
    public void OnlySuperAdminAndSupportReceiveFeedbackReportsStatusManage()
    {
        var permissions = new AdminRolePermissionCatalogService().GetProductionRolePermissions();

        Assert.Contains(AdminPermissionConstants.FeedbackReportsStatusManage, permissions[AdminRoleConstants.SuperAdmin]);
        Assert.Contains(AdminPermissionConstants.FeedbackReportsStatusManage, permissions[AdminRoleConstants.Support]);
        Assert.DoesNotContain(AdminPermissionConstants.FeedbackReportsStatusManage, permissions[AdminRoleConstants.ContentEditor]);
        Assert.DoesNotContain(AdminPermissionConstants.FeedbackReportsStatusManage, permissions[AdminRoleConstants.BillingSupport]);
        Assert.DoesNotContain(AdminPermissionConstants.FeedbackReportsStatusManage, permissions[AdminRoleConstants.ReadOnlyAuditor]);
    }

    [Theory]
    [InlineData("reviewed")]
    [InlineData("needs_information")]
    [InlineData("processing")]
    [InlineData("resolved")]
    [InlineData("rejected")]
    public async Task NewReportTransitionsSetFirstReviewedTimestampAndWriteSafeAudit(string requestedStatus)
    {
        await using var db = CreateDbContext();
        var adminUserId = await AddUserAsync(db, "admin@example.test");
        var targetUserId = await AddUserAsync(db, "target@example.test");
        var report = AddReport(targetUserId, "new");
        report.Message = "Private report message";
        report.ReportedAiText = "Private AI text";
        db.UserFeedbackReports.Add(report);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new AdminFeedbackReportStatusService(db, new AdminAuditService(db));

        var result = await service.ChangeStatusAsync(adminUserId, report.Id, requestedStatus, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Response);
        Assert.Equal(requestedStatus, result.Response.Status);
        Assert.NotNull(result.Response.ReviewedAtUtc);
        var audit = await db.AdminActions.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AdminAuditConstants.ActionTypes.FeedbackReportStatusChanged, audit.ActionType);
        Assert.Equal(adminUserId, audit.AdminUserId);
        Assert.Equal(targetUserId, audit.TargetUserId);
        Assert.Contains("feedbackReportId", audit.SafeMetadataJson);
        Assert.Contains("previousStatus", audit.SafeMetadataJson);
        Assert.Contains("newStatus", audit.SafeMetadataJson);
        Assert.Contains("category", audit.SafeMetadataJson);
        Assert.DoesNotContain(report.Message, audit.SafeMetadataJson);
        Assert.DoesNotContain(report.ReportedAiText, audit.SafeMetadataJson);
    }

    [Theory]
    [InlineData("reviewed", "resolved")]
    [InlineData("resolved", "reviewed")]
    public async Task ReviewedAndResolvedTransitionsPreserveOriginalReviewedTimestamp(string initialStatus, string requestedStatus)
    {
        await using var db = CreateDbContext();
        var adminUserId = await AddUserAsync(db, "admin@example.test");
        var targetUserId = await AddUserAsync(db, "target@example.test");
        var firstReviewedAtUtc = DateTimeOffset.Parse("2026-07-10T00:00:00Z");
        var report = AddReport(targetUserId, initialStatus);
        report.ReviewedAtUtc = firstReviewedAtUtc;
        db.UserFeedbackReports.Add(report);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new AdminFeedbackReportStatusService(db, new AdminAuditService(db)).ChangeStatusAsync(
            adminUserId, report.Id, requestedStatus, TestContext.Current.CancellationToken);

        Assert.Equal(requestedStatus, result.Response?.Status);
        Assert.Equal(firstReviewedAtUtc, result.Response?.ReviewedAtUtc);
    }

    [Fact]
    public async Task SameStatusIsIdempotentAndDoesNotWriteAnotherAuditEvent()
    {
        await using var db = CreateDbContext();
        var adminUserId = await AddUserAsync(db, "admin@example.test");
        var targetUserId = await AddUserAsync(db, "target@example.test");
        var firstReviewedAtUtc = DateTimeOffset.Parse("2026-07-10T00:00:00Z");
        var report = AddReport(targetUserId, "reviewed");
        report.ReviewedAtUtc = firstReviewedAtUtc;
        db.UserFeedbackReports.Add(report);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new AdminFeedbackReportStatusService(db, new AdminAuditService(db));

        var result = await service.ChangeStatusAsync(adminUserId, report.Id, "reviewed", TestContext.Current.CancellationToken);

        Assert.Equal(firstReviewedAtUtc, result.Response?.ReviewedAtUtc);
        Assert.Equal(0, await db.AdminActions.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnsupportedStatusIsInvalidAndUnknownReportIsNotFound()
    {
        await using var db = CreateDbContext();
        var service = new AdminFeedbackReportStatusService(db, new AdminAuditService(db));

        var invalid = await service.ChangeStatusAsync(Guid.NewGuid(), Guid.NewGuid(), "new", TestContext.Current.CancellationToken);
        var missing = await service.ChangeStatusAsync(Guid.NewGuid(), Guid.NewGuid(), "reviewed", TestContext.Current.CancellationToken);

        Assert.True(invalid.IsInvalid);
        Assert.True(missing.IsNotFound);
    }

    [Fact]
    public async Task AccountDeletionCannotBeManuallyResolvedBeforeAnonymizationCompletes()
    {
        await using var db = CreateDbContext();
        var adminUserId = await AddUserAsync(db, "admin@example.test");
        var targetUserId = await AddUserAsync(db, "target@example.test");
        var report = AddReport(targetUserId, UserFeedbackReportConstants.ProcessingStatus, UserFeedbackReportConstants.AccountDeletionCategory);
        db.UserFeedbackReports.Add(report);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new AdminFeedbackReportStatusService(db, new AdminAuditService(db)).ChangeStatusAsync(adminUserId, report.Id, UserFeedbackReportConstants.ResolvedStatus, TestContext.Current.CancellationToken);

        Assert.True(result.IsAnonymizationNotCompleted);
        Assert.Equal(UserFeedbackReportConstants.ProcessingStatus, (await db.UserFeedbackReports.SingleAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Empty(await db.AdminActions.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CompletedAnonymizationAllowsIdempotentResolvedStatusAndRejectionRemainsManual()
    {
        await using var db = CreateDbContext();
        var adminUserId = await AddUserAsync(db, "admin@example.test");
        var targetUserId = await AddUserAsync(db, "target@example.test");
        var completedReport = AddReport(targetUserId, UserFeedbackReportConstants.ProcessingStatus, UserFeedbackReportConstants.AccountDeletionCategory);
        var rejectedReport = AddReport(targetUserId, UserFeedbackReportConstants.NewStatus, UserFeedbackReportConstants.AccountDeletionCategory);
        db.AddRange(completedReport, rejectedReport);
        db.AccountAnonymizationOperations.Add(new AccountAnonymizationOperationEntity { Id = Guid.NewGuid(), ReportId = completedReport.Id, TargetUserId = targetUserId, ActorAdminUserId = adminUserId, PolicySnapshotId = Guid.NewGuid(), State = AccountAnonymizationExecutionService.CompletedState, PreflightFingerprint = "fingerprint", ProcedureVersion = "policy", ExpiresAtUtc = DateTimeOffset.UtcNow, CategoryCountsJson = "{}", BlockingCodesJson = "[]", RetentionSummaryJson = "{}", ProviderStatesJson = "[]", BackupReconciliationState = "standard_retention", VerificationState = "verified", ResultCountsJson = "{}", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new AdminFeedbackReportStatusService(db, new AdminAuditService(db));

        var resolved = await service.ChangeStatusAsync(adminUserId, completedReport.Id, UserFeedbackReportConstants.ResolvedStatus, TestContext.Current.CancellationToken);
        var rejected = await service.ChangeStatusAsync(adminUserId, rejectedReport.Id, UserFeedbackReportConstants.RejectedStatus, TestContext.Current.CancellationToken);

        Assert.Equal(UserFeedbackReportConstants.ResolvedStatus, resolved.Response?.Status);
        Assert.Equal(UserFeedbackReportConstants.RejectedStatus, rejected.Response?.Status);
    }

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private static async Task<Guid> AddUserAsync(AppDbContext db, string email)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new UserEntity { Id = userId, Email = email, PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return userId;
    }

    private static UserFeedbackReportEntity AddReport(Guid userId, string status, string category = "app_issue") => new()
    {
        Id = Guid.NewGuid(), UserId = userId, Category = category, Message = "Report", Status = status,
        ClientPlatform = "windows", ClientVersion = "1.0.0", CreatedAtUtc = DateTimeOffset.UtcNow
    };
}
