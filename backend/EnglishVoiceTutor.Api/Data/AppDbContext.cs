using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<UserProfileEntity> UserProfiles => Set<UserProfileEntity>();
    public DbSet<UserSettingsEntity> UserSettings => Set<UserSettingsEntity>();
    public DbSet<LessonEntity> Lessons => Set<LessonEntity>();
    public DbSet<LessonSessionEntity> LessonSessions => Set<LessonSessionEntity>();
    public DbSet<LessonMessageEntity> LessonMessages => Set<LessonMessageEntity>();
    public DbSet<FeedbackResultEntity> FeedbackResults => Set<FeedbackResultEntity>();
    public DbSet<UserFeedbackReportEntity> UserFeedbackReports => Set<UserFeedbackReportEntity>();
    public DbSet<UserFeedbackReportReplyEntity> UserFeedbackReportReplies => Set<UserFeedbackReportReplyEntity>();
    public DbSet<LessonSummaryEntity> LessonSummaries => Set<LessonSummaryEntity>();
    public DbSet<UsageEventEntity> UsageEvents => Set<UsageEventEntity>();
    public DbSet<DailyUsageCounterEntity> DailyUsageCounters => Set<DailyUsageCounterEntity>();
    public DbSet<SubscriptionEntity> Subscriptions => Set<SubscriptionEntity>();
    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();
    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<PlanEntity> Plans => Set<PlanEntity>();
    public DbSet<EntitlementEntity> Entitlements => Set<EntitlementEntity>();
    public DbSet<TrialGrantEntity> TrialGrants => Set<TrialGrantEntity>();
    public DbSet<DailyFreeLessonUsageEntity> DailyFreeLessonUsages => Set<DailyFreeLessonUsageEntity>();
    public DbSet<BillingEventEntity> BillingEvents => Set<BillingEventEntity>();
    public DbSet<GooglePlayPurchaseClaimEntity> GooglePlayPurchaseClaims => Set<GooglePlayPurchaseClaimEntity>();
    public DbSet<PaddleWebhookEventEntity> PaddleWebhookEvents => Set<PaddleWebhookEventEntity>();
    public DbSet<AdminActionEntity> AdminActions => Set<AdminActionEntity>();
    public DbSet<AdminUserEntity> AdminUsers => Set<AdminUserEntity>();
    public DbSet<AdminUserRoleEntity> AdminUserRoles => Set<AdminUserRoleEntity>();
    public DbSet<AdminRoleAssignmentEventEntity> AdminRoleAssignmentEvents => Set<AdminRoleAssignmentEventEntity>();
    public DbSet<AdminAuthAuditEventEntity> AdminAuthAuditEvents => Set<AdminAuthAuditEventEntity>();
    public DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();
    public DbSet<UserRefreshTokenEntity> UserRefreshTokens => Set<UserRefreshTokenEntity>();
    public DbSet<AccountAnonymizationOperationEntity> AccountAnonymizationOperations => Set<AccountAnonymizationOperationEntity>();
    public DbSet<AccountAnonymizationPolicySnapshotEntity> AccountAnonymizationPolicySnapshots => Set<AccountAnonymizationPolicySnapshotEntity>();
    public DbSet<ContentPackEntity> ContentPacks => Set<ContentPackEntity>();
    public DbSet<CmsLessonTopicEntity> CmsLessonTopics => Set<CmsLessonTopicEntity>();
    public DbSet<CmsLessonScenarioEntity> CmsLessonScenarios => Set<CmsLessonScenarioEntity>();
    public DbSet<PromptTemplateEntity> PromptTemplates => Set<PromptTemplateEntity>();
    public DbSet<TutorBehaviorProfileEntity> TutorBehaviorProfiles => Set<TutorBehaviorProfileEntity>();
    public DbSet<ContentVersionEntity> ContentVersions => Set<ContentVersionEntity>();
    public DbSet<PublishedContentSnapshotEntity> PublishedContentSnapshots => Set<PublishedContentSnapshotEntity>();
    public DbSet<ContentAuditLogEntity> ContentAuditLogs => Set<ContentAuditLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Most product-history relationships use Restrict so a user, session, or message cannot be deleted accidentally with dependent history still attached.
        ConfigureUsers(modelBuilder);
        ConfigureUserProfiles(modelBuilder);
        ConfigureUserSettings(modelBuilder);
        ConfigureLessons(modelBuilder);
        ConfigureLessonSessions(modelBuilder);
        ConfigureLessonMessages(modelBuilder);
        ConfigureFeedbackResults(modelBuilder);
        ConfigureUserFeedbackReports(modelBuilder);
        ConfigureUserFeedbackReportReplies(modelBuilder);
        ConfigureLessonSummaries(modelBuilder);
        ConfigureUsageEvents(modelBuilder);
        ConfigureDailyUsageCounters(modelBuilder);
        ConfigureSubscriptions(modelBuilder);
        ConfigurePayments(modelBuilder);
        ConfigureDevices(modelBuilder);
        ConfigurePlans(modelBuilder);
        ConfigureEntitlements(modelBuilder);
        ConfigureTrialGrants(modelBuilder);
        ConfigureDailyFreeLessonUsage(modelBuilder);
        ConfigureBillingEvents(modelBuilder);
        ConfigureGooglePlayPurchaseClaims(modelBuilder);
        ConfigurePaddleWebhookEvents(modelBuilder);
        ConfigureAdminActions(modelBuilder);
        ConfigureAdminRoleAssignmentPersistence(modelBuilder);
        ConfigureAdminAuthAuditEvents(modelBuilder);
        ConfigurePasswordResetTokens(modelBuilder);
        ConfigureUserRefreshTokens(modelBuilder);
        ConfigureAccountAnonymizationPreflightFoundation(modelBuilder);
        ConfigureCmsContent(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserEntity>();
        entity.ToTable(EntityConstants.TableNames.Users);
        entity.HasKey(user => user.Id);
        entity.Property(user => user.Email).IsRequired().HasMaxLength(EntityConstants.Lengths.EmailMaxLength);
        entity.Property(user => user.PasswordHash).IsRequired().HasMaxLength(EntityConstants.Lengths.PasswordHashMaxLength);
        entity.Property(user => user.Status).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(user => user.CreatedAt).IsRequired();
        entity.HasIndex(user => user.Email).IsUnique();
    }

    private static void ConfigureUserProfiles(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserProfileEntity>();
        entity.ToTable(EntityConstants.TableNames.UserProfiles);
        entity.HasKey(profile => profile.Id);
        entity.Property(profile => profile.DisplayName).IsRequired().HasMaxLength(EntityConstants.Lengths.DisplayNameMaxLength);
        entity.Property(profile => profile.NativeLanguage).IsRequired().HasMaxLength(EntityConstants.Lengths.LanguageCodeMaxLength);
        entity.Property(profile => profile.CurrentLevel).IsRequired().HasMaxLength(EntityConstants.Lengths.LevelMaxLength);
        entity.Property(profile => profile.SelectedTutorId).HasMaxLength(EntityConstants.Lengths.TutorIdMaxLength);
        entity.Property(profile => profile.Timezone).IsRequired().HasMaxLength(EntityConstants.Lengths.TimezoneMaxLength);
        entity.Property(profile => profile.CreatedAt).IsRequired();
        entity.Property(profile => profile.UpdatedAt).IsRequired();
        entity.HasIndex(profile => profile.UserId).IsUnique();
        entity.HasOne(profile => profile.User)
            .WithOne(user => user.Profile)
            .HasForeignKey<UserProfileEntity>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUserSettings(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserSettingsEntity>();
        entity.ToTable(EntityConstants.TableNames.UserSettings);
        entity.HasKey(settings => settings.Id);
        entity.Property(settings => settings.StudyLanguage).IsRequired().HasMaxLength(EntityConstants.Lengths.LanguageCodeMaxLength);
        entity.Property(settings => settings.ExplanationLanguage).IsRequired().HasMaxLength(EntityConstants.Lengths.LanguageCodeMaxLength);
        entity.Property(settings => settings.SpeechVoice).IsRequired().HasMaxLength(EntityConstants.Lengths.ShortTextMaxLength);
        entity.Property(settings => settings.SpeechSpeed).HasPrecision(EntityConstants.Precision.SpeechSpeedPrecision, EntityConstants.Precision.SpeechSpeedScale);
        entity.Property(settings => settings.CreatedAt).IsRequired();
        entity.Property(settings => settings.UpdatedAt).IsRequired();
        entity.HasIndex(settings => settings.UserId).IsUnique();
        entity.HasOne(settings => settings.User)
            .WithOne(user => user.Settings)
            .HasForeignKey<UserSettingsEntity>(settings => settings.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureLessons(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LessonEntity>();
        entity.ToTable(EntityConstants.TableNames.Lessons);
        entity.HasKey(lesson => lesson.Id);
        entity.Property(lesson => lesson.LessonContentId).IsRequired().HasMaxLength(EntityConstants.Lengths.LessonContentIdMaxLength);
        entity.Property(lesson => lesson.TopicId).IsRequired().HasMaxLength(EntityConstants.Lengths.TopicIdMaxLength);
        entity.Property(lesson => lesson.TopicTitle).IsRequired().HasMaxLength(EntityConstants.Lengths.TopicTitleMaxLength);
        entity.Property(lesson => lesson.SubtopicId).IsRequired().HasMaxLength(EntityConstants.Lengths.SubtopicIdMaxLength);
        entity.Property(lesson => lesson.SubtopicTitle).IsRequired().HasMaxLength(EntityConstants.Lengths.SubtopicTitleMaxLength);
        entity.Property(lesson => lesson.Level).IsRequired().HasMaxLength(EntityConstants.Lengths.LevelMaxLength);
        entity.Property(lesson => lesson.CreatedAt).IsRequired();
        entity.Property(lesson => lesson.UpdatedAt).IsRequired();
        entity.HasIndex(lesson => lesson.LessonContentId);
    }

    private static void ConfigureLessonSessions(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LessonSessionEntity>();
        entity.ToTable(EntityConstants.TableNames.LessonSessions);
        entity.HasKey(session => session.Id);
        entity.Property(session => session.LessonContentId).IsRequired().HasMaxLength(EntityConstants.Lengths.LessonContentIdMaxLength);
        entity.Property(session => session.StudyLanguage).IsRequired().HasMaxLength(EntityConstants.Lengths.LanguageCodeMaxLength);
        entity.Property(session => session.TopicId).IsRequired().HasMaxLength(EntityConstants.Lengths.TopicIdMaxLength);
        entity.Property(session => session.TopicTitle).IsRequired().HasMaxLength(EntityConstants.Lengths.TopicTitleMaxLength);
        entity.Property(session => session.SubtopicId).IsRequired().HasMaxLength(EntityConstants.Lengths.SubtopicIdMaxLength);
        entity.Property(session => session.SubtopicTitle).IsRequired().HasMaxLength(EntityConstants.Lengths.SubtopicTitleMaxLength);
        entity.Property(session => session.Level).IsRequired().HasMaxLength(EntityConstants.Lengths.LevelMaxLength);
        entity.Property(session => session.SelectedContextId).HasMaxLength(EntityConstants.Lengths.ContextIdMaxLength);
        entity.Property(session => session.SelectedContextTitle).HasMaxLength(EntityConstants.Lengths.ContextTitleMaxLength);
        entity.Property(session => session.ModeUsed).IsRequired().HasMaxLength(EntityConstants.Lengths.ModeMaxLength);
        entity.Property(session => session.Status).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(session => session.EstimatedCost).HasPrecision(EntityConstants.Precision.CostPrecision, EntityConstants.Precision.CostScale);
        entity.Property(session => session.StartedAt).IsRequired();
        entity.Property(session => session.LastHeartbeatAtUtc);
        entity.Property(session => session.CreatedAt).IsRequired();
        entity.Property(session => session.UpdatedAt).IsRequired();
        entity.HasIndex(session => session.UserId);
        entity.HasIndex(session => session.StartedAt);
        entity.HasIndex(session => session.Status);
        entity.HasIndex(session => session.LastHeartbeatAtUtc);
        entity.HasOne(session => session.User)
            .WithMany(user => user.LessonSessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureLessonMessages(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LessonMessageEntity>();
        entity.ToTable(EntityConstants.TableNames.LessonMessages);
        entity.HasKey(message => message.Id);
        entity.Property(message => message.Role).IsRequired().HasMaxLength(EntityConstants.Lengths.RoleMaxLength);
        entity.Property(message => message.Text).IsRequired().HasMaxLength(EntityConstants.Lengths.LongTextMaxLength);
        entity.Property(message => message.Source).IsRequired().HasMaxLength(EntityConstants.Lengths.SourceMaxLength);
        entity.Property(message => message.StudyLanguage).IsRequired().HasMaxLength(EntityConstants.Lengths.LanguageCodeMaxLength);
        entity.Property(message => message.TranscriptConfidence).HasPrecision(EntityConstants.Precision.TranscriptConfidencePrecision, EntityConstants.Precision.TranscriptConfidenceScale);
        entity.Property(message => message.CreatedAt).IsRequired();
        entity.HasIndex(message => message.SessionId);
        entity.HasOne(message => message.Session)
            .WithMany(session => session.Messages)
            .HasForeignKey(message => message.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureFeedbackResults(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<FeedbackResultEntity>();
        entity.ToTable(EntityConstants.TableNames.FeedbackResults);
        entity.HasKey(feedback => feedback.Id);
        entity.Property(feedback => feedback.FeedbackType).IsRequired().HasMaxLength(EntityConstants.Lengths.FeedbackTypeMaxLength);
        entity.Property(feedback => feedback.CorrectedText).HasMaxLength(EntityConstants.Lengths.LongTextMaxLength);
        entity.Property(feedback => feedback.Explanation).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(feedback => feedback.GrammarTip).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(feedback => feedback.VocabularyTip).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(feedback => feedback.CultureTip).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(feedback => feedback.Praise).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(feedback => feedback.CreatedAt).IsRequired();
        entity.HasIndex(feedback => feedback.SessionId);
        entity.HasIndex(feedback => feedback.MessageId);
        entity.HasOne(feedback => feedback.Session)
            .WithMany(session => session.FeedbackResults)
            .HasForeignKey(feedback => feedback.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(feedback => feedback.Message)
            .WithMany(message => message.FeedbackResults)
            .HasForeignKey(feedback => feedback.MessageId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUserFeedbackReports(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserFeedbackReportEntity>();
        entity.ToTable(EntityConstants.TableNames.UserFeedbackReports);
        entity.HasKey(report => report.Id);
        entity.Property(report => report.Category).IsRequired().HasMaxLength(EntityConstants.Lengths.FeedbackReportCategoryMaxLength);
        entity.Property(report => report.Message).IsRequired().HasMaxLength(EntityConstants.Lengths.FeedbackReportMessageMaxLength);
        entity.Property(report => report.ReportedAiText).HasMaxLength(EntityConstants.Lengths.FeedbackReportMessageMaxLength);
        entity.Property(report => report.Status).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(report => report.ClientPlatform).IsRequired().HasMaxLength(32);
        entity.Property(report => report.ClientVersion).IsRequired().HasMaxLength(EntityConstants.Lengths.FeedbackReportClientVersionMaxLength);
        entity.Property(report => report.CreatedAtUtc).IsRequired();
        entity.HasIndex(report => report.UserId);
        entity.HasIndex(report => report.CreatedAtUtc);
        entity.HasOne(report => report.User).WithMany().HasForeignKey(report => report.UserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUserFeedbackReportReplies(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserFeedbackReportReplyEntity>();
        entity.ToTable(EntityConstants.TableNames.UserFeedbackReportReplies);
        entity.HasKey(reply => reply.Id);
        entity.Property(reply => reply.ReplyText).IsRequired().HasMaxLength(EntityConstants.Lengths.FeedbackReportMessageMaxLength);
        entity.Property(reply => reply.RecipientEmail).IsRequired().HasMaxLength(EntityConstants.Lengths.EmailMaxLength);
        entity.Property(reply => reply.DeliveryStatus).IsRequired().HasMaxLength(EntityConstants.Lengths.FeedbackReportReplyDeliveryStatusMaxLength);
        entity.Property(reply => reply.CreatedAtUtc).IsRequired();
        entity.Property(reply => reply.FailureCode).HasMaxLength(EntityConstants.Lengths.FeedbackReportReplyFailureCodeMaxLength);
        entity.Property(reply => reply.FailureMessage).HasMaxLength(EntityConstants.Lengths.ErrorMessageMaxLength);
        entity.HasIndex(reply => reply.FeedbackReportId);
        entity.HasIndex(reply => reply.AdminUserId);
        entity.HasIndex(reply => new { reply.FeedbackReportId, reply.CreatedAtUtc });
        entity.HasOne(reply => reply.FeedbackReport)
            .WithMany(report => report.Replies)
            .HasForeignKey(reply => reply.FeedbackReportId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(reply => reply.AdminUser)
            .WithMany(adminUser => adminUser.FeedbackReportReplies)
            .HasForeignKey(reply => reply.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureLessonSummaries(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LessonSummaryEntity>();
        entity.ToTable(EntityConstants.TableNames.LessonSummaries);
        entity.HasKey(summary => summary.Id);
        entity.Property(summary => summary.WhatWentWell).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(summary => summary.WhatToImprove).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(summary => summary.UsefulPhrases).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(summary => summary.MistakesToReview).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(summary => summary.Summary).IsRequired().HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(summary => summary.Strengths).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(summary => summary.Improvements).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(summary => summary.Vocabulary).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(summary => summary.Grammar).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(summary => summary.NextSteps).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(summary => summary.CreatedAt).IsRequired();
        entity.Property(summary => summary.UpdatedAt).IsRequired();
        entity.HasIndex(summary => summary.SessionId).IsUnique();
        entity.HasOne(summary => summary.Session)
            .WithOne(session => session.Summary)
            .HasForeignKey<LessonSummaryEntity>(summary => summary.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUsageEvents(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UsageEventEntity>();
        entity.ToTable(EntityConstants.TableNames.UsageEvents);
        entity.HasKey(usageEvent => usageEvent.Id);
        entity.Property(usageEvent => usageEvent.Operation).IsRequired().HasMaxLength(EntityConstants.Lengths.OperationMaxLength);
        entity.Property(usageEvent => usageEvent.Model).HasMaxLength(EntityConstants.Lengths.ModelMaxLength);
        entity.Property(usageEvent => usageEvent.StudyLanguage).HasMaxLength(EntityConstants.Lengths.LanguageCodeMaxLength);
        entity.Property(usageEvent => usageEvent.Status).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(usageEvent => usageEvent.EstimatedCost).HasPrecision(EntityConstants.Precision.CostPrecision, EntityConstants.Precision.CostScale);
        entity.Property(usageEvent => usageEvent.CreatedAt).IsRequired();
        entity.HasIndex(usageEvent => usageEvent.UserId);
        entity.HasIndex(usageEvent => usageEvent.SessionId);
        entity.HasIndex(usageEvent => usageEvent.CreatedAt);
        entity.HasOne(usageEvent => usageEvent.User)
            .WithMany(user => user.UsageEvents)
            .HasForeignKey(usageEvent => usageEvent.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(usageEvent => usageEvent.Session)
            .WithMany(session => session.UsageEvents)
            .HasForeignKey(usageEvent => usageEvent.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureDailyUsageCounters(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<DailyUsageCounterEntity>();
        entity.ToTable(EntityConstants.TableNames.DailyUsageCounters);
        entity.HasKey(counter => counter.Id);
        entity.Property(counter => counter.StudyLanguage).IsRequired().HasMaxLength(EntityConstants.Lengths.LanguageCodeMaxLength);
        entity.Property(counter => counter.EstimatedCost).HasPrecision(EntityConstants.Precision.CostPrecision, EntityConstants.Precision.CostScale);
        entity.Property(counter => counter.CreatedAt).IsRequired();
        entity.Property(counter => counter.UpdatedAt).IsRequired();
        entity.HasIndex(counter => new { counter.UserId, counter.UsageDate, counter.StudyLanguage }).IsUnique();
        entity.HasOne(counter => counter.User)
            .WithMany(user => user.DailyUsageCounters)
            .HasForeignKey(counter => counter.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureSubscriptions(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SubscriptionEntity>();
        entity.ToTable(EntityConstants.TableNames.Subscriptions);
        entity.HasKey(subscription => subscription.Id);
        entity.Property(subscription => subscription.PlanId).IsRequired().HasMaxLength(EntityConstants.Lengths.PlanIdMaxLength);
        entity.Property(subscription => subscription.Status).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(subscription => subscription.Provider).IsRequired().HasMaxLength(EntityConstants.Lengths.ProviderMaxLength);
        entity.Property(subscription => subscription.ProviderSubscriptionId).HasMaxLength(EntityConstants.Lengths.ExternalIdMaxLength);
        entity.Property(subscription => subscription.ProviderCustomerId).HasMaxLength(EntityConstants.Lengths.ExternalIdMaxLength);
        entity.Property(subscription => subscription.ProviderPriceId).HasMaxLength(EntityConstants.Lengths.ExternalIdMaxLength);
        entity.Property(subscription => subscription.ProviderProductId).HasMaxLength(EntityConstants.Lengths.ExternalIdMaxLength);
        entity.Property(subscription => subscription.CancelAtPeriodEnd).IsRequired();
        entity.Property(subscription => subscription.ScheduledChangeAction).HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(subscription => subscription.LastProviderEventId).HasMaxLength(EntityConstants.Lengths.ProviderEventIdMaxLength);
        entity.Property(subscription => subscription.LastProviderEventType).HasMaxLength(EntityConstants.Lengths.BillingEventTypeMaxLength);
        entity.Property(subscription => subscription.StartedAt).IsRequired();
        entity.Property(subscription => subscription.CreatedAt).IsRequired();
        entity.Property(subscription => subscription.UpdatedAt).IsRequired();
        entity.HasIndex(subscription => new { subscription.UserId, subscription.Status, subscription.Provider, subscription.ProviderSubscriptionId });
        entity.HasIndex(subscription => new { subscription.Provider, subscription.ProviderSubscriptionId })
            .IsUnique()
            .HasFilter("\"ProviderSubscriptionId\" IS NOT NULL");
        entity.HasOne(subscription => subscription.User)
            .WithMany(user => user.Subscriptions)
            .HasForeignKey(subscription => subscription.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<PlanEntity>()
            .WithMany(plan => plan.Subscriptions)
            .HasPrincipalKey(plan => plan.PlanId)
            .HasForeignKey(subscription => subscription.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePayments(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PaymentEntity>();
        entity.ToTable(EntityConstants.TableNames.Payments);
        entity.HasKey(payment => payment.Id);
        entity.Property(payment => payment.InternalPlanId).IsRequired().HasMaxLength(EntityConstants.Lengths.PlanIdMaxLength);
        entity.Property(payment => payment.Amount).HasPrecision(EntityConstants.Precision.MoneyPrecision, EntityConstants.Precision.MoneyScale);
        entity.Property(payment => payment.Currency).IsRequired().HasMaxLength(EntityConstants.Lengths.CurrencyMaxLength);
        entity.Property(payment => payment.Status).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(payment => payment.Provider).IsRequired().HasMaxLength(EntityConstants.Lengths.ProviderMaxLength);
        entity.Property(payment => payment.ProviderPaymentId).HasMaxLength(EntityConstants.Lengths.ExternalIdMaxLength);
        entity.Property(payment => payment.ProviderCustomerId).HasMaxLength(EntityConstants.Lengths.ExternalIdMaxLength);
        entity.Property(payment => payment.ProviderSubscriptionId).HasMaxLength(EntityConstants.Lengths.ExternalIdMaxLength);
        entity.Property(payment => payment.ProviderPriceId).HasMaxLength(EntityConstants.Lengths.ExternalIdMaxLength);
        entity.Property(payment => payment.ProviderProductId).HasMaxLength(EntityConstants.Lengths.ExternalIdMaxLength);
        entity.Property(payment => payment.ProviderEventId).HasMaxLength(EntityConstants.Lengths.ProviderEventIdMaxLength);
        entity.Property(payment => payment.ProviderEventType).HasMaxLength(EntityConstants.Lengths.BillingEventTypeMaxLength);
        entity.Property(payment => payment.SafeMetadataJson).HasMaxLength(EntityConstants.Lengths.MetadataJsonMaxLength);
        entity.Property(payment => payment.ProviderPayloadJson).HasMaxLength(EntityConstants.Lengths.LongTextMaxLength);
        entity.Property(payment => payment.CreatedAt).IsRequired();
        entity.Property(payment => payment.UpdatedAt).IsRequired();
        entity.HasIndex(payment => payment.UserId);
        entity.HasIndex(payment => payment.SubscriptionId);
        entity.HasIndex(payment => new { payment.Provider, payment.ProviderPaymentId })
            .IsUnique()
            .HasFilter("\"ProviderPaymentId\" IS NOT NULL");
        entity.HasOne(payment => payment.User)
            .WithMany(user => user.Payments)
            .HasForeignKey(payment => payment.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureDevices(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<DeviceEntity>();
        entity.ToTable(EntityConstants.TableNames.Devices);
        entity.HasKey(device => device.Id);
        entity.Property(device => device.Platform).IsRequired().HasMaxLength(EntityConstants.Lengths.PlatformMaxLength);
        entity.Property(device => device.DeviceName).IsRequired().HasMaxLength(EntityConstants.Lengths.DeviceNameMaxLength);
        entity.Property(device => device.AppVersion).IsRequired().HasMaxLength(EntityConstants.Lengths.AppVersionMaxLength);
        entity.Property(device => device.LastSeenAt).IsRequired();
        entity.Property(device => device.CreatedAt).IsRequired();
        entity.HasIndex(device => device.UserId);
        entity.HasOne(device => device.User)
            .WithMany(user => user.Devices)
            .HasForeignKey(device => device.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePlans(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PlanEntity>();
        entity.ToTable(EntityConstants.TableNames.Plans);
        entity.HasKey(plan => plan.Id);
        entity.Property(plan => plan.PlanId).IsRequired().HasMaxLength(EntityConstants.Lengths.PlanIdMaxLength);
        entity.Property(plan => plan.DisplayName).IsRequired().HasMaxLength(EntityConstants.Lengths.PlanDisplayNameMaxLength);
        entity.Property(plan => plan.Tier).IsRequired().HasMaxLength(EntityConstants.Lengths.PlanTierMaxLength);
        entity.Property(plan => plan.IsActive).IsRequired();
        entity.Property(plan => plan.CreatedAt).IsRequired();
        entity.Property(plan => plan.UpdatedAt).IsRequired();
        entity.HasIndex(plan => plan.PlanId).IsUnique();
    }

    private static void ConfigureEntitlements(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EntitlementEntity>();
        entity.ToTable(EntityConstants.TableNames.Entitlements);
        entity.HasKey(entitlement => entitlement.Id);
        entity.Property(entitlement => entitlement.PlanId).IsRequired().HasMaxLength(EntityConstants.Lengths.PlanIdMaxLength);
        entity.Property(entitlement => entitlement.EntitlementType).IsRequired().HasMaxLength(EntityConstants.Lengths.EntitlementTypeMaxLength);
        entity.Property(entitlement => entitlement.Source).IsRequired().HasMaxLength(EntityConstants.Lengths.EntitlementSourceMaxLength);
        entity.Property(entitlement => entitlement.Status).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(entitlement => entitlement.Reason).HasMaxLength(EntityConstants.Lengths.EntitlementReasonMaxLength);
        entity.Property(entitlement => entitlement.CreatedAt).IsRequired();
        entity.Property(entitlement => entitlement.UpdatedAt).IsRequired();
        entity.HasIndex(entitlement => new { entitlement.UserId, entitlement.Status, entitlement.StartsAtUtc, entitlement.ExpiresAtUtc });
        entity.HasOne(entitlement => entitlement.User).WithMany(user => user.Entitlements).HasForeignKey(entitlement => entitlement.UserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(entitlement => entitlement.Subscription).WithMany(subscription => subscription.Entitlements).HasForeignKey(entitlement => entitlement.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<PlanEntity>().WithMany(plan => plan.Entitlements).HasPrincipalKey(plan => plan.PlanId).HasForeignKey(entitlement => entitlement.PlanId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureTrialGrants(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<TrialGrantEntity>();
        entity.ToTable(EntityConstants.TableNames.TrialGrants);
        entity.HasKey(trial => trial.Id);
        entity.Property(trial => trial.SourcePlatform).IsRequired().HasMaxLength(EntityConstants.Lengths.PlatformMaxLength);
        entity.Property(trial => trial.DeviceFingerprintHash).HasMaxLength(EntityConstants.Lengths.DeviceFingerprintHashMaxLength);
        entity.Property(trial => trial.Status).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(trial => trial.CreatedAt).IsRequired();
        entity.HasIndex(trial => new { trial.UserId, trial.Status });
        entity.HasOne(trial => trial.User).WithMany(user => user.TrialGrants).HasForeignKey(trial => trial.UserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureDailyFreeLessonUsage(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<DailyFreeLessonUsageEntity>();
        entity.ToTable(EntityConstants.TableNames.DailyFreeLessonUsage);
        entity.HasKey(usage => usage.Id);
        entity.Property(usage => usage.StudyLanguage).IsRequired().HasMaxLength(EntityConstants.Lengths.LanguageCodeMaxLength);
        entity.Property(usage => usage.CreatedAt).IsRequired();
        entity.HasIndex(usage => new { usage.UserId, usage.UsageDate }).IsUnique();
        entity.HasOne(usage => usage.User).WithMany(user => user.DailyFreeLessonUsages).HasForeignKey(usage => usage.UserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(usage => usage.LessonSession).WithMany().HasForeignKey(usage => usage.LessonSessionId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureBillingEvents(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<BillingEventEntity>();
        entity.ToTable(EntityConstants.TableNames.BillingEvents);
        entity.HasKey(billingEvent => billingEvent.Id);
        entity.Property(billingEvent => billingEvent.BillingProvider).IsRequired().HasMaxLength(EntityConstants.Lengths.ProviderMaxLength);
        entity.Property(billingEvent => billingEvent.EventType).IsRequired().HasMaxLength(EntityConstants.Lengths.BillingEventTypeMaxLength);
        entity.Property(billingEvent => billingEvent.ProviderEventId).IsRequired().HasMaxLength(EntityConstants.Lengths.ProviderEventIdMaxLength);
        entity.Property(billingEvent => billingEvent.Status).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(billingEvent => billingEvent.SafeMetadataJson).HasMaxLength(EntityConstants.Lengths.MetadataJsonMaxLength);
        entity.Property(billingEvent => billingEvent.ErrorMessage).HasMaxLength(EntityConstants.Lengths.ErrorMessageMaxLength);
        entity.HasIndex(billingEvent => new { billingEvent.BillingProvider, billingEvent.ProviderEventId }).IsUnique();
    }


    private static void ConfigurePaddleWebhookEvents(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PaddleWebhookEventEntity>();
        entity.ToTable(EntityConstants.TableNames.PaddleWebhookEvents);
        entity.HasKey(webhookEvent => webhookEvent.Id);
        entity.Property(webhookEvent => webhookEvent.PaddleEventId).IsRequired().HasMaxLength(EntityConstants.Lengths.ProviderEventIdMaxLength);
        entity.Property(webhookEvent => webhookEvent.EventType).IsRequired().HasMaxLength(EntityConstants.Lengths.BillingEventTypeMaxLength);
        entity.Property(webhookEvent => webhookEvent.ProcessingStatus).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(webhookEvent => webhookEvent.PaddleNotificationId).HasMaxLength(EntityConstants.Lengths.ProviderEventIdMaxLength);
        entity.Property(webhookEvent => webhookEvent.PaddleTransactionId).HasMaxLength(EntityConstants.Lengths.ProviderEventIdMaxLength);
        entity.Property(webhookEvent => webhookEvent.PaddleSubscriptionId).HasMaxLength(EntityConstants.Lengths.ProviderEventIdMaxLength);
        entity.Property(webhookEvent => webhookEvent.PaddleCustomerId).HasMaxLength(EntityConstants.Lengths.ProviderEventIdMaxLength);
        entity.Property(webhookEvent => webhookEvent.InternalPlanId).HasMaxLength(EntityConstants.Lengths.PlanIdMaxLength);
        entity.Property(webhookEvent => webhookEvent.RawPayload).IsRequired();
        entity.Property(webhookEvent => webhookEvent.SignatureHeader).HasMaxLength(EntityConstants.Lengths.PaddleWebhookSignatureHeaderMaxLength);
        entity.Property(webhookEvent => webhookEvent.ReceivedAtUtc).IsRequired();
        entity.Property(webhookEvent => webhookEvent.CreatedAt).IsRequired();
        entity.Property(webhookEvent => webhookEvent.UpdatedAt).IsRequired();
        entity.HasIndex(webhookEvent => webhookEvent.PaddleEventId).IsUnique();
        entity.HasIndex(webhookEvent => webhookEvent.EventType);
        entity.HasIndex(webhookEvent => webhookEvent.PaddleTransactionId);
        entity.HasIndex(webhookEvent => webhookEvent.PaddleSubscriptionId);
        entity.HasIndex(webhookEvent => webhookEvent.InternalUserId);
        entity.HasIndex(webhookEvent => webhookEvent.ProcessingStatus);
        entity.HasIndex(webhookEvent => webhookEvent.ReceivedAtUtc);
    }

    private static void ConfigureAdminActions(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AdminActionEntity>();
        entity.ToTable(EntityConstants.TableNames.AdminActions);
        entity.HasKey(action => action.Id);
        entity.Property(action => action.ActionType).IsRequired().HasMaxLength(EntityConstants.Lengths.ActionTypeMaxLength);
        entity.Property(action => action.Reason).IsRequired().HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(action => action.SafeMetadataJson).HasMaxLength(EntityConstants.Lengths.MetadataJsonMaxLength);
        entity.HasIndex(action => new { action.TargetUserId, action.CreatedAtUtc });
        entity.HasOne(action => action.AdminUser).WithMany(user => user.AdminActionsCreated).HasForeignKey(action => action.AdminUserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(action => action.TargetUser).WithMany(user => user.AdminActionsReceived).HasForeignKey(action => action.TargetUserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureAdminRoleAssignmentPersistence(ModelBuilder modelBuilder)
    {
        ConfigureAdminUsers(modelBuilder);
        ConfigureAdminUserRoles(modelBuilder);
        ConfigureAdminRoleAssignmentEvents(modelBuilder);
    }

    private static void ConfigureAdminUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AdminUserEntity>();
        entity.ToTable(EntityConstants.TableNames.AdminUsers);
        entity.HasKey(adminUser => adminUser.Id);
        entity.Property(adminUser => adminUser.NormalizedEmail).HasMaxLength(EntityConstants.Lengths.EmailMaxLength);
        entity.Property(adminUser => adminUser.Status).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(adminUser => adminUser.CreatedAtUtc).IsRequired();
        entity.Property(adminUser => adminUser.UpdatedAtUtc).IsRequired();
        entity.HasIndex(adminUser => adminUser.UserId);
        entity.HasIndex(adminUser => adminUser.NormalizedEmail);
        entity.HasIndex(adminUser => adminUser.Status);
        entity.HasOne(adminUser => adminUser.User)
            .WithMany()
            .HasForeignKey(adminUser => adminUser.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(adminUser => adminUser.CreatedByAdminUser)
            .WithMany(adminUser => adminUser.CreatedAdminUsers)
            .HasForeignKey(adminUser => adminUser.CreatedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureAdminUserRoles(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AdminUserRoleEntity>();
        entity.ToTable(EntityConstants.TableNames.AdminUserRoles);
        entity.HasKey(role => role.Id);
        entity.Property(role => role.RoleId).IsRequired().HasMaxLength(EntityConstants.Lengths.AdminRoleIdMaxLength);
        entity.Property(role => role.AssignedAtUtc).IsRequired();
        entity.Property(role => role.Reason).IsRequired().HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(role => role.RevokeReason).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.HasIndex(role => role.AdminUserId);
        entity.HasIndex(role => role.RoleId);
        entity.HasIndex(role => role.AssignedByAdminUserId);
        entity.HasIndex(role => role.RevokedAtUtc);
        entity.HasIndex(role => new { role.AdminUserId, role.RoleId })
            .IsUnique()
            .HasFilter("\"RevokedAtUtc\" IS NULL");
        entity.HasOne(role => role.AdminUser)
            .WithMany(adminUser => adminUser.RoleAssignments)
            .HasForeignKey(role => role.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(role => role.AssignedByAdminUser)
            .WithMany(adminUser => adminUser.RoleAssignmentsCreated)
            .HasForeignKey(role => role.AssignedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(role => role.RevokedByAdminUser)
            .WithMany(adminUser => adminUser.RoleAssignmentsRevoked)
            .HasForeignKey(role => role.RevokedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureAdminRoleAssignmentEvents(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AdminRoleAssignmentEventEntity>();
        entity.ToTable(EntityConstants.TableNames.AdminRoleAssignmentEvents);
        entity.HasKey(roleEvent => roleEvent.Id);
        entity.Property(roleEvent => roleEvent.ActionType).IsRequired().HasMaxLength(EntityConstants.Lengths.ActionTypeMaxLength);
        entity.Property(roleEvent => roleEvent.RoleId).HasMaxLength(EntityConstants.Lengths.AdminRoleIdMaxLength);
        entity.Property(roleEvent => roleEvent.Reason).HasMaxLength(EntityConstants.Lengths.MediumTextMaxLength);
        entity.Property(roleEvent => roleEvent.OldRolesJson).HasMaxLength(EntityConstants.Lengths.MetadataJsonMaxLength);
        entity.Property(roleEvent => roleEvent.NewRolesJson).HasMaxLength(EntityConstants.Lengths.MetadataJsonMaxLength);
        entity.Property(roleEvent => roleEvent.OccurredAtUtc).IsRequired();
        entity.Property(roleEvent => roleEvent.Result).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(roleEvent => roleEvent.SafeMetadataJson).HasMaxLength(EntityConstants.Lengths.MetadataJsonMaxLength);
        entity.HasIndex(roleEvent => roleEvent.ActorAdminUserId);
        entity.HasIndex(roleEvent => roleEvent.TargetAdminUserId);
        entity.HasIndex(roleEvent => roleEvent.RoleId);
        entity.HasIndex(roleEvent => roleEvent.ActionType);
        entity.HasIndex(roleEvent => roleEvent.Result);
        entity.HasIndex(roleEvent => roleEvent.OccurredAtUtc);
        entity.HasOne(roleEvent => roleEvent.ActorAdminUser)
            .WithMany(adminUser => adminUser.ActorEvents)
            .HasForeignKey(roleEvent => roleEvent.ActorAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(roleEvent => roleEvent.TargetAdminUser)
            .WithMany(adminUser => adminUser.TargetEvents)
            .HasForeignKey(roleEvent => roleEvent.TargetAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureAdminAuthAuditEvents(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AdminAuthAuditEventEntity>();
        entity.ToTable(EntityConstants.TableNames.AdminAuthAuditEvents);
        entity.HasKey(auditEvent => auditEvent.Id);
        entity.Property(auditEvent => auditEvent.OccurredAtUtc).IsRequired();
        entity.Property(auditEvent => auditEvent.EventType).IsRequired().HasMaxLength(EntityConstants.Lengths.AdminAuthEventTypeMaxLength);
        entity.Property(auditEvent => auditEvent.Result).IsRequired().HasMaxLength(EntityConstants.Lengths.AdminAuthResultMaxLength);
        entity.Property(auditEvent => auditEvent.ActorEmail).HasMaxLength(EntityConstants.Lengths.EmailMaxLength);
        entity.Property(auditEvent => auditEvent.AttemptedEmail).HasMaxLength(EntityConstants.Lengths.EmailMaxLength);
        entity.Property(auditEvent => auditEvent.AdminSource).HasMaxLength(EntityConstants.Lengths.AdminAuthSourceMaxLength);
        entity.Property(auditEvent => auditEvent.RoleIdsJson).HasMaxLength(EntityConstants.Lengths.MetadataJsonMaxLength);
        entity.Property(auditEvent => auditEvent.FailureReasonCode).HasMaxLength(EntityConstants.Lengths.AdminAuthFailureReasonMaxLength);
        entity.Property(auditEvent => auditEvent.SafeMetadataJson).HasMaxLength(EntityConstants.Lengths.MetadataJsonMaxLength);
        entity.HasIndex(auditEvent => auditEvent.OccurredAtUtc);
        entity.HasIndex(auditEvent => auditEvent.EventType);
        entity.HasIndex(auditEvent => auditEvent.Result);
        entity.HasIndex(auditEvent => auditEvent.ActorUserId);
        entity.HasIndex(auditEvent => auditEvent.ActorAdminUserId);
        entity.HasOne(auditEvent => auditEvent.ActorUser)
            .WithMany()
            .HasForeignKey(auditEvent => auditEvent.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(auditEvent => auditEvent.ActorAdminUser)
            .WithMany()
            .HasForeignKey(auditEvent => auditEvent.ActorAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePasswordResetTokens(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PasswordResetTokenEntity>();
        entity.ToTable(EntityConstants.TableNames.PasswordResetTokens);
        entity.HasKey(token => token.Id);
        entity.Property(token => token.TokenHash).IsRequired().HasMaxLength(EntityConstants.Lengths.TokenHashMaxLength);
        entity.Property(token => token.CreatedAtUtc).IsRequired();
        entity.Property(token => token.ExpiresAtUtc).IsRequired();
        entity.HasIndex(token => token.UserId);
        entity.HasIndex(token => token.TokenHash).IsUnique();
        entity.HasIndex(token => token.ExpiresAtUtc);
        entity.HasOne(token => token.User)
            .WithMany(user => user.PasswordResetTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }


    private static void ConfigureUserRefreshTokens(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserRefreshTokenEntity>();
        entity.ToTable(EntityConstants.TableNames.UserRefreshTokens);
        entity.HasKey(token => token.Id);
        entity.Property(token => token.TokenHash).IsRequired().HasMaxLength(EntityConstants.Lengths.TokenHashMaxLength);
        entity.Property(token => token.CreatedAtUtc).IsRequired();
        entity.Property(token => token.ExpiresAtUtc).IsRequired();
        entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(EntityConstants.Lengths.TokenHashMaxLength);
        entity.Property(token => token.UserAgent).HasMaxLength(EntityConstants.Lengths.ShortTextMaxLength);
        entity.Property(token => token.CreatedByIp).HasMaxLength(EntityConstants.Lengths.ShortTextMaxLength);
        entity.Property(token => token.RevokedByIp).HasMaxLength(EntityConstants.Lengths.ShortTextMaxLength);
        entity.Property(token => token.RevocationReason).HasMaxLength(EntityConstants.Lengths.ShortTextMaxLength);
        entity.HasIndex(token => token.TokenHash).IsUnique();
        entity.HasIndex(token => token.UserId);
        entity.HasIndex(token => token.ExpiresAtUtc);
        entity.HasOne(token => token.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureGooglePlayPurchaseClaims(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<GooglePlayPurchaseClaimEntity>();
        entity.ToTable(EntityConstants.TableNames.GooglePlayPurchaseClaims);
        entity.HasKey(claim => claim.Id);
        entity.Property(claim => claim.PurchaseTokenFingerprint).IsRequired().HasMaxLength(EntityConstants.Lengths.GooglePlayPurchaseTokenFingerprintLength);
        entity.Property(claim => claim.ProductId).IsRequired().HasMaxLength(EntityConstants.Lengths.ExternalIdMaxLength);
        entity.Property(claim => claim.CreatedAtUtc).IsRequired();
        entity.Property(claim => claim.LastSeenAtUtc).IsRequired();
        entity.HasIndex(claim => claim.PurchaseTokenFingerprint).IsUnique();
        entity.HasIndex(claim => claim.UserId);
        entity.HasOne<UserEntity>().WithMany().HasForeignKey(claim => claim.UserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureAccountAnonymizationPreflightFoundation(ModelBuilder modelBuilder)
    {
        var policy = modelBuilder.Entity<AccountAnonymizationPolicySnapshotEntity>();
        policy.ToTable(EntityConstants.TableNames.AccountAnonymizationPolicySnapshots);
        policy.HasKey(snapshot => snapshot.Id);
        policy.Property(snapshot => snapshot.PolicyVersion).IsRequired().HasMaxLength(EntityConstants.Lengths.AccountAnonymizationPolicyVersionMaxLength);
        policy.Property(snapshot => snapshot.VersionHash).IsRequired().HasMaxLength(EntityConstants.Lengths.AccountAnonymizationFingerprintMaxLength);
        policy.Property(snapshot => snapshot.CategoryDecisionsJson).IsRequired().HasMaxLength(EntityConstants.Lengths.AccountAnonymizationJsonMaxLength);
        policy.Property(snapshot => snapshot.CreatedAtUtc).IsRequired();
        policy.HasIndex(snapshot => snapshot.PolicyVersion).IsUnique();
        policy.HasIndex(snapshot => snapshot.VersionHash).IsUnique();

        var operation = modelBuilder.Entity<AccountAnonymizationOperationEntity>();
        operation.ToTable(EntityConstants.TableNames.AccountAnonymizationOperations);
        operation.HasKey(item => item.Id);
        operation.Property(item => item.State).IsRequired().HasMaxLength(EntityConstants.Lengths.AccountAnonymizationStateMaxLength);
        operation.Property(item => item.PreflightFingerprint).IsRequired().HasMaxLength(EntityConstants.Lengths.AccountAnonymizationFingerprintMaxLength);
        operation.Property(item => item.ProcedureVersion).IsRequired().HasMaxLength(EntityConstants.Lengths.AccountAnonymizationPolicyVersionMaxLength);
        operation.Property(item => item.CategoryCountsJson).IsRequired().HasMaxLength(EntityConstants.Lengths.AccountAnonymizationJsonMaxLength);
        operation.Property(item => item.BlockingCodesJson).IsRequired().HasMaxLength(EntityConstants.Lengths.AccountAnonymizationJsonMaxLength);
        operation.Property(item => item.RetentionSummaryJson).IsRequired().HasMaxLength(EntityConstants.Lengths.AccountAnonymizationJsonMaxLength);
        operation.Property(item => item.ProviderStatesJson).IsRequired().HasMaxLength(EntityConstants.Lengths.AccountAnonymizationJsonMaxLength);
        operation.Property(item => item.BackupReconciliationState).IsRequired().HasMaxLength(EntityConstants.Lengths.AccountAnonymizationStateMaxLength);
        operation.Property(item => item.FailureCode).HasMaxLength(EntityConstants.Lengths.AccountAnonymizationStateMaxLength);
        operation.Property(item => item.VerificationState).IsRequired().HasMaxLength(EntityConstants.Lengths.AccountAnonymizationStateMaxLength);
        operation.Property(item => item.ResultCountsJson).IsRequired().HasMaxLength(EntityConstants.Lengths.AccountAnonymizationJsonMaxLength);
        operation.Property(item => item.ConcurrencyRevision).IsConcurrencyToken();
        operation.HasIndex(item => item.ReportId).IsUnique();
        operation.HasIndex(item => item.TargetUserId);
        operation.HasIndex(item => new { item.State, item.UpdatedAtUtc });
        operation.HasOne(item => item.Report).WithMany().HasForeignKey(item => item.ReportId).OnDelete(DeleteBehavior.Restrict);
        operation.HasOne(item => item.TargetUser).WithMany().HasForeignKey(item => item.TargetUserId).OnDelete(DeleteBehavior.Restrict);
        operation.HasOne(item => item.PolicySnapshot).WithMany().HasForeignKey(item => item.PolicySnapshotId).OnDelete(DeleteBehavior.Restrict);
        operation.HasOne(item => item.ActorAdminUser).WithMany().HasForeignKey(item => item.ActorAdminUserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureCmsContent(ModelBuilder modelBuilder)
    {
        ConfigureContentPacks(modelBuilder);
        ConfigureCmsLessonTopics(modelBuilder);
        ConfigureCmsLessonScenarios(modelBuilder);
        ConfigurePromptTemplates(modelBuilder);
        ConfigureTutorBehaviorProfiles(modelBuilder);
        ConfigureContentVersions(modelBuilder);
        ConfigurePublishedContentSnapshots(modelBuilder);
        ConfigureContentAuditLogs(modelBuilder);
    }

    private static void ConfigureContentPacks(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ContentPackEntity>();
        entity.ToTable(EntityConstants.TableNames.ContentPacks);
        entity.HasKey(pack => pack.Id);
        entity.Property(pack => pack.Slug).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsSlugKeyMaxLength);
        entity.Property(pack => pack.Name).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsShortNameMaxLength);
        entity.Property(pack => pack.Description).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsDescriptionMaxLength);
        entity.Property(pack => pack.Status).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsStatusMaxLength);
        entity.Property(pack => pack.BaseStaticContentVersion).HasMaxLength(EntityConstants.Lengths.CmsSlugKeyMaxLength);
        entity.Property(pack => pack.CreatedAtUtc).IsRequired();
        entity.Property(pack => pack.UpdatedAtUtc).IsRequired();
        entity.HasIndex(pack => pack.Slug).IsUnique();
        entity.HasIndex(pack => pack.Status);
        entity.HasOne<UserEntity>().WithMany().HasForeignKey(pack => pack.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UserEntity>().WithMany().HasForeignKey(pack => pack.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureCmsLessonTopics(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CmsLessonTopicEntity>();
        entity.ToTable(EntityConstants.TableNames.CmsLessonTopics);
        entity.HasKey(topic => topic.Id);
        entity.Property(topic => topic.StableTopicKey).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsSlugKeyMaxLength);
        entity.Property(topic => topic.Title).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsShortNameMaxLength);
        entity.Property(topic => topic.Description).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsDescriptionMaxLength);
        entity.Property(topic => topic.CreatedAtUtc).IsRequired();
        entity.Property(topic => topic.UpdatedAtUtc).IsRequired();
        entity.HasIndex(topic => new { topic.ContentPackId, topic.StableTopicKey }).IsUnique();
        entity.HasIndex(topic => new { topic.ContentPackId, topic.SortOrder });
        entity.HasOne(topic => topic.ContentPack).WithMany(pack => pack.LessonTopics).HasForeignKey(topic => topic.ContentPackId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureCmsLessonScenarios(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CmsLessonScenarioEntity>();
        entity.ToTable(EntityConstants.TableNames.CmsLessonScenarios);
        entity.HasKey(scenario => scenario.Id);
        entity.Property(scenario => scenario.StableScenarioKey).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsSlugKeyMaxLength);
        entity.Property(scenario => scenario.Title).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsShortNameMaxLength);
        entity.Property(scenario => scenario.Description).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsDescriptionMaxLength);
        entity.Property(scenario => scenario.LessonType).IsRequired().HasMaxLength(EntityConstants.Lengths.ModeMaxLength);
        entity.Property(scenario => scenario.SupportedLevelIdsJson).IsRequired();
        entity.Property(scenario => scenario.SetupMessage).IsRequired();
        entity.Property(scenario => scenario.ContextSelectionJson).IsRequired();
        entity.Property(scenario => scenario.LearningGoalJson).IsRequired();
        entity.Property(scenario => scenario.SituationJson).IsRequired();
        entity.Property(scenario => scenario.RolesJson).IsRequired();
        entity.Property(scenario => scenario.TargetLanguageJson).IsRequired();
        entity.Property(scenario => scenario.LevelProfilesJson).IsRequired();
        entity.Property(scenario => scenario.ConversationFlowJson).IsRequired();
        entity.Property(scenario => scenario.RoleplayBeatsJson).IsRequired();
        entity.Property(scenario => scenario.ReciprocalQuestionHandlingJson).IsRequired();
        entity.Property(scenario => scenario.ExpectedScenarioProgressionJson).IsRequired();
        entity.Property(scenario => scenario.ControlledVariationJson).IsRequired();
        entity.Property(scenario => scenario.OffTopicHandlingJson).IsRequired();
        entity.Property(scenario => scenario.FeedbackRulesJson).IsRequired();
        entity.Property(scenario => scenario.HintRulesJson).IsRequired();
        entity.Property(scenario => scenario.RepetitionLogicJson).IsRequired();
        entity.Property(scenario => scenario.AiTutorPromptInstructionsJson).IsRequired();
        entity.Property(scenario => scenario.DefinitionJson);
        entity.Property(scenario => scenario.CreatedAtUtc).IsRequired();
        entity.Property(scenario => scenario.UpdatedAtUtc).IsRequired();
        entity.HasIndex(scenario => new { scenario.ContentPackId, scenario.StableScenarioKey }).IsUnique();
        entity.HasIndex(scenario => scenario.TopicId);
        entity.HasOne(scenario => scenario.ContentPack).WithMany(pack => pack.LessonScenarios).HasForeignKey(scenario => scenario.ContentPackId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(scenario => scenario.Topic).WithMany(topic => topic.LessonScenarios).HasForeignKey(scenario => scenario.TopicId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePromptTemplates(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PromptTemplateEntity>();
        entity.ToTable(EntityConstants.TableNames.PromptTemplates);
        entity.HasKey(template => template.Id);
        entity.Property(template => template.TemplateKey).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsTemplateKeyMaxLength);
        entity.Property(template => template.TargetStudyLanguageId).HasMaxLength(EntityConstants.Lengths.LanguageCodeMaxLength);
        entity.Property(template => template.Body).IsRequired();
        entity.Property(template => template.AllowedPlaceholdersJson).IsRequired();
        entity.Property(template => template.RequiredPlaceholdersJson).IsRequired();
        entity.Property(template => template.CreatedAtUtc).IsRequired();
        entity.Property(template => template.UpdatedAtUtc).IsRequired();
        entity.HasIndex(template => new { template.ContentPackId, template.TemplateKey, template.TargetStudyLanguageId }).IsUnique();
        entity.HasOne(template => template.ContentPack).WithMany(pack => pack.PromptTemplates).HasForeignKey(template => template.ContentPackId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UserEntity>().WithMany().HasForeignKey(template => template.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureTutorBehaviorProfiles(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<TutorBehaviorProfileEntity>();
        entity.ToTable(EntityConstants.TableNames.TutorBehaviorProfiles);
        entity.HasKey(profile => profile.Id);
        entity.Property(profile => profile.TutorId).IsRequired().HasMaxLength(EntityConstants.Lengths.TutorIdMaxLength);
        entity.Property(profile => profile.DisplayName).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsShortNameMaxLength);
        entity.Property(profile => profile.CommunicationStyleJson).IsRequired();
        entity.Property(profile => profile.SafetyNotesJson).IsRequired();
        entity.Property(profile => profile.CreatedAtUtc).IsRequired();
        entity.Property(profile => profile.UpdatedAtUtc).IsRequired();
        entity.HasIndex(profile => new { profile.ContentPackId, profile.TutorId }).IsUnique();
        entity.HasOne(profile => profile.ContentPack).WithMany(pack => pack.TutorBehaviorProfiles).HasForeignKey(profile => profile.ContentPackId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureContentVersions(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ContentVersionEntity>();
        entity.ToTable(EntityConstants.TableNames.ContentVersions);
        entity.HasKey(version => version.Id);
        entity.Property(version => version.SnapshotHash).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsHashMaxLength);
        entity.Property(version => version.PublishStatus).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsStatusMaxLength);
        entity.Property(version => version.ValidationSummaryJson).IsRequired();
        entity.Property(version => version.ChangeSummary).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsDescriptionMaxLength);
        entity.Property(version => version.CreatedAtUtc).IsRequired();
        entity.HasIndex(version => new { version.ContentPackId, version.VersionNumber }).IsUnique();
        entity.HasOne(version => version.ContentPack).WithMany(pack => pack.ContentVersions).HasForeignKey(version => version.ContentPackId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(version => version.RestoredFromVersion).WithMany().HasForeignKey(version => version.RestoredFromVersionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UserEntity>().WithMany().HasForeignKey(version => version.PublishedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePublishedContentSnapshots(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PublishedContentSnapshotEntity>();
        entity.ToTable(EntityConstants.TableNames.PublishedContentSnapshots);
        entity.HasKey(snapshot => snapshot.Id);
        entity.Property(snapshot => snapshot.SnapshotJson).IsRequired();
        entity.Property(snapshot => snapshot.SnapshotHash).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsHashMaxLength);
        entity.Property(snapshot => snapshot.CreatedAtUtc).IsRequired();
        entity.HasIndex(snapshot => snapshot.ContentVersionId).IsUnique();
        entity.HasOne(snapshot => snapshot.ContentVersion).WithOne(version => version.PublishedSnapshot).HasForeignKey<PublishedContentSnapshotEntity>(snapshot => snapshot.ContentVersionId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureContentAuditLogs(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ContentAuditLogEntity>();
        entity.ToTable(EntityConstants.TableNames.ContentAuditLogs);
        entity.HasKey(log => log.Id);
        entity.Property(log => log.ActorEmail).HasMaxLength(EntityConstants.Lengths.EmailMaxLength);
        entity.Property(log => log.Action).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsAuditActionMaxLength);
        entity.Property(log => log.EntityType).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsEntityTypeMaxLength);
        entity.Property(log => log.ContentPackSlug).HasMaxLength(EntityConstants.Lengths.CmsSlugKeyMaxLength);
        entity.Property(log => log.StableKey).HasMaxLength(EntityConstants.Lengths.CmsStableKeyMaxLength);
        entity.Property(log => log.BeforeHash).HasMaxLength(EntityConstants.Lengths.CmsHashMaxLength);
        entity.Property(log => log.AfterHash).HasMaxLength(EntityConstants.Lengths.CmsHashMaxLength);
        entity.Property(log => log.ChangedFieldsJson).IsRequired();
        entity.Property(log => log.Reason).IsRequired().HasMaxLength(EntityConstants.Lengths.CmsReasonMaxLength);
        entity.Property(log => log.Source).IsRequired().HasMaxLength(EntityConstants.Lengths.SourceMaxLength);
        entity.Property(log => log.Status).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(log => log.CreatedAtUtc).IsRequired();
        entity.Property(log => log.RequestMetadataJson).HasMaxLength(EntityConstants.Lengths.MetadataJsonMaxLength);
        entity.HasIndex(log => new { log.ContentPackId, log.CreatedAtUtc });
        entity.HasIndex(log => new { log.ContentPackSlug, log.CreatedAtUtc });
        entity.HasIndex(log => new { log.EntityType, log.CreatedAtUtc });
        entity.HasIndex(log => new { log.StableKey, log.CreatedAtUtc });
        entity.HasIndex(log => new { log.ActorUserId, log.CreatedAtUtc });
        entity.HasOne(log => log.ContentPack).WithMany(pack => pack.AuditLogs).HasForeignKey(log => log.ContentPackId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UserEntity>().WithMany().HasForeignKey(log => log.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }

}
