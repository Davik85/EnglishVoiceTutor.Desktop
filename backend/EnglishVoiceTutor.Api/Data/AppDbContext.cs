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
        entity.Property(subscription => subscription.StartedAt).IsRequired();
        entity.Property(subscription => subscription.CreatedAt).IsRequired();
        entity.Property(subscription => subscription.UpdatedAt).IsRequired();
        entity.HasIndex(subscription => subscription.UserId);
        entity.HasOne(subscription => subscription.User)
            .WithMany(user => user.Subscriptions)
            .HasForeignKey(subscription => subscription.UserId)
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
}
