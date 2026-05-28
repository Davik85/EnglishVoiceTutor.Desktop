using EnglishVoiceTutor.Api.Data.Entities;
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
    public DbSet<PaddleWebhookEventEntity> PaddleWebhookEvents => Set<PaddleWebhookEventEntity>();
    public DbSet<AdminActionEntity> AdminActions => Set<AdminActionEntity>();

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
        ConfigurePaddleWebhookEvents(modelBuilder);
        ConfigureAdminActions(modelBuilder);
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
        entity.Property(session => session.CreatedAt).IsRequired();
        entity.Property(session => session.UpdatedAt).IsRequired();
        entity.HasIndex(session => session.UserId);
        entity.HasIndex(session => session.StartedAt);
        entity.HasIndex(session => session.Status);
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
        entity.Property(subscription => subscription.CancelAtPeriodEnd).IsRequired();
        entity.Property(subscription => subscription.StartedAt).IsRequired();
        entity.Property(subscription => subscription.CreatedAt).IsRequired();
        entity.Property(subscription => subscription.UpdatedAt).IsRequired();
        entity.HasIndex(subscription => new { subscription.UserId, subscription.Status, subscription.Provider, subscription.ProviderSubscriptionId });
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
        entity.Property(payment => payment.Amount).HasPrecision(EntityConstants.Precision.MoneyPrecision, EntityConstants.Precision.MoneyScale);
        entity.Property(payment => payment.Currency).IsRequired().HasMaxLength(EntityConstants.Lengths.CurrencyMaxLength);
        entity.Property(payment => payment.Status).IsRequired().HasMaxLength(EntityConstants.Lengths.StatusMaxLength);
        entity.Property(payment => payment.Provider).IsRequired().HasMaxLength(EntityConstants.Lengths.ProviderMaxLength);
        entity.Property(payment => payment.ProviderPaymentId).HasMaxLength(EntityConstants.Lengths.ExternalIdMaxLength);
        entity.Property(payment => payment.ProviderPayloadJson).HasMaxLength(EntityConstants.Lengths.LongTextMaxLength);
        entity.Property(payment => payment.CreatedAt).IsRequired();
        entity.HasIndex(payment => payment.UserId);
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

}
