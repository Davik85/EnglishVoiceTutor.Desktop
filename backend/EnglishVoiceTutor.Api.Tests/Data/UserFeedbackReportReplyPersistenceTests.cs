using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Data;

public sealed class UserFeedbackReportReplyPersistenceTests
{
    [Fact]
    public void ModelUsesRequiredBoundedFieldsRestrictRelationshipsAndHistoryIndexes()
    {
        using var db = CreateDbContext();
        var entity = db.Model.FindEntityType(typeof(UserFeedbackReportReplyEntity));

        Assert.NotNull(entity);
        Assert.Equal(EntityConstants.TableNames.UserFeedbackReportReplies, entity.GetTableName());
        AssertProperty(entity, nameof(UserFeedbackReportReplyEntity.ReplyText), false, EntityConstants.Lengths.FeedbackReportMessageMaxLength);
        AssertProperty(entity, nameof(UserFeedbackReportReplyEntity.RecipientEmail), false, EntityConstants.Lengths.EmailMaxLength);
        AssertProperty(entity, nameof(UserFeedbackReportReplyEntity.DeliveryStatus), false, EntityConstants.Lengths.FeedbackReportReplyDeliveryStatusMaxLength);
        AssertProperty(entity, nameof(UserFeedbackReportReplyEntity.FailureCode), true, EntityConstants.Lengths.FeedbackReportReplyFailureCodeMaxLength);
        AssertProperty(entity, nameof(UserFeedbackReportReplyEntity.FailureMessage), true, EntityConstants.Lengths.ErrorMessageMaxLength);
        Assert.False(entity.FindProperty(nameof(UserFeedbackReportReplyEntity.CreatedAtUtc))!.IsNullable);
        Assert.True(entity.FindProperty(nameof(UserFeedbackReportReplyEntity.SentAtUtc))!.IsNullable);
        Assert.Equal(DeleteBehavior.Restrict, entity.GetForeignKeys().Single(foreignKey => foreignKey.Properties.Single().Name == nameof(UserFeedbackReportReplyEntity.FeedbackReportId)).DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, entity.GetForeignKeys().Single(foreignKey => foreignKey.Properties.Single().Name == nameof(UserFeedbackReportReplyEntity.AdminUserId)).DeleteBehavior);
        Assert.Contains(entity.GetIndexes(), index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(UserFeedbackReportReplyEntity.FeedbackReportId)]));
        Assert.Contains(entity.GetIndexes(), index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(UserFeedbackReportReplyEntity.AdminUserId)]));
        Assert.Contains(entity.GetIndexes(), index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(UserFeedbackReportReplyEntity.FeedbackReportId), nameof(UserFeedbackReportReplyEntity.CreatedAtUtc)]));
    }

    [Fact]
    public async Task StoresIndependentPendingSentAndFailedReplyAttemptsForOneReport()
    {
        await using var db = CreateDbContext();
        var user = new UserEntity { Id = Guid.NewGuid(), Email = "original@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        var report = new UserFeedbackReportEntity
        {
            Id = Guid.NewGuid(), UserId = user.Id, Category = "app_issue", Message = "Report", Status = "new",
            ClientPlatform = "windows", ClientVersion = "1.0.0", CreatedAtUtc = DateTimeOffset.UtcNow
        };
        var adminUser = new AdminUserEntity
        {
            Id = Guid.NewGuid(), UserId = user.Id, NormalizedEmail = "admin@example.test", Status = "active",
            CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        db.AddRange(user, report, adminUser);
        var sentAtUtc = DateTimeOffset.UtcNow;
        db.UserFeedbackReportReplies.AddRange(
            new UserFeedbackReportReplyEntity
            {
                Id = Guid.NewGuid(), FeedbackReportId = report.Id, AdminUserId = adminUser.Id, ReplyText = "Pending reply",
                RecipientEmail = "original@example.test", DeliveryStatus = UserFeedbackReportReplyConstants.DeliveryStatuses.Pending, CreatedAtUtc = DateTimeOffset.UtcNow
            },
            new UserFeedbackReportReplyEntity
            {
                Id = Guid.NewGuid(), FeedbackReportId = report.Id, AdminUserId = adminUser.Id, ReplyText = "Sent reply",
                RecipientEmail = "original@example.test", DeliveryStatus = UserFeedbackReportReplyConstants.DeliveryStatuses.Sent, CreatedAtUtc = DateTimeOffset.UtcNow, SentAtUtc = sentAtUtc
            },
            new UserFeedbackReportReplyEntity
            {
                Id = Guid.NewGuid(), FeedbackReportId = report.Id, AdminUserId = adminUser.Id, ReplyText = "Failed reply",
                RecipientEmail = "original@example.test", DeliveryStatus = UserFeedbackReportReplyConstants.DeliveryStatuses.Failed, CreatedAtUtc = DateTimeOffset.UtcNow,
                FailureCode = "temporary_failure", FailureMessage = "Delivery temporarily unavailable."
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        user.Email = "changed@example.test";
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var attempts = await db.UserFeedbackReportReplies.OrderBy(reply => reply.ReplyText).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, attempts.Count);
        Assert.Equal(sentAtUtc, attempts.Single(reply => reply.DeliveryStatus == UserFeedbackReportReplyConstants.DeliveryStatuses.Sent).SentAtUtc);
        var failed = attempts.Single(reply => reply.DeliveryStatus == UserFeedbackReportReplyConstants.DeliveryStatuses.Failed);
        Assert.Equal("temporary_failure", failed.FailureCode);
        Assert.Equal("Delivery temporarily unavailable.", failed.FailureMessage);
        Assert.All(attempts, reply => Assert.Equal("original@example.test", reply.RecipientEmail));
    }

    private static void AssertProperty(Microsoft.EntityFrameworkCore.Metadata.IEntityType entity, string name, bool nullable, int maxLength)
    {
        var property = entity.FindProperty(name);
        Assert.NotNull(property);
        Assert.Equal(nullable, property.IsNullable);
        Assert.Equal(maxLength, property.GetMaxLength());
    }

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
