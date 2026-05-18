using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations;

/// <inheritdoc />
public partial class InitialProductStorageSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "lessons",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                LessonContentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                TopicId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                TopicTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                SubtopicId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                SubtopicTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ContentVersion = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_lessons", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "daily_usage_counters",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                UsageDate = table.Column<DateOnly>(type: "date", nullable: false),
                StudyLanguage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                LessonsStarted = table.Column<int>(type: "integer", nullable: false),
                LessonsCompleted = table.Column<int>(type: "integer", nullable: false),
                HintsUsed = table.Column<int>(type: "integer", nullable: false),
                FeedbackRequests = table.Column<int>(type: "integer", nullable: false),
                TranscriptionSeconds = table.Column<int>(type: "integer", nullable: false),
                TtsSeconds = table.Column<int>(type: "integer", nullable: false),
                EstimatedCost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_daily_usage_counters", x => x.Id);
                table.ForeignKey(
                    name: "FK_daily_usage_counters_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "devices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Platform = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                DeviceName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                AppVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_devices", x => x.Id);
                table.ForeignKey(
                    name: "FK_devices_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "lesson_sessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                LessonContentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                StudyLanguage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                TopicId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                TopicTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                SubtopicId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                SubtopicTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SelectedContextId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                SelectedContextTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                ModeUsed = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ValidTurnCount = table.Column<int>(type: "integer", nullable: false),
                EstimatedCost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_lesson_sessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_lesson_sessions_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                ProviderPaymentId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                ProviderPayloadJson = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_payments_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "subscriptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                PlanId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                ProviderSubscriptionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_subscriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_subscriptions_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "user_profiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                NativeLanguage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CurrentLevel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SelectedTutorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                Timezone = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_profiles", x => x.Id);
                table.ForeignKey(
                    name: "FK_user_profiles_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "user_settings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                StudyLanguage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ExplanationLanguage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SpeechVoice = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                SpeechSpeed = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                ConversationModeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_settings", x => x.Id);
                table.ForeignKey(
                    name: "FK_user_settings_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "lesson_messages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Text = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                TurnNumber = table.Column<int>(type: "integer", nullable: false),
                IsValidLessonTurn = table.Column<bool>(type: "boolean", nullable: false),
                StudyLanguage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                TranscriptConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                AudioDurationMs = table.Column<int>(type: "integer", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_lesson_messages", x => x.Id);
                table.ForeignKey(
                    name: "FK_lesson_messages_lesson_sessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "lesson_sessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "lesson_summaries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                WhatWentWell = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                WhatToImprove = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                UsefulPhrases = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                MistakesToReview = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                NextSteps = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_lesson_summaries", x => x.Id);
                table.ForeignKey(
                    name: "FK_lesson_summaries_lesson_sessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "lesson_sessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "usage_events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                Operation = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                InputTokens = table.Column<long>(type: "bigint", nullable: true),
                OutputTokens = table.Column<long>(type: "bigint", nullable: true),
                AudioInputTokens = table.Column<long>(type: "bigint", nullable: true),
                AudioOutputTokens = table.Column<long>(type: "bigint", nullable: true),
                AudioDurationMs = table.Column<int>(type: "integer", nullable: true),
                InputChars = table.Column<long>(type: "bigint", nullable: true),
                OutputBytes = table.Column<long>(type: "bigint", nullable: true),
                EstimatedCost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_usage_events", x => x.Id);
                table.ForeignKey(
                    name: "FK_usage_events_lesson_sessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "lesson_sessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_usage_events_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "feedback_results",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                FeedbackType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                CorrectedText = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                Explanation = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                GrammarTip = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                VocabularyTip = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                CultureTip = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                Praise = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_feedback_results", x => x.Id);
                table.ForeignKey(
                    name: "FK_feedback_results_lesson_messages_MessageId",
                    column: x => x.MessageId,
                    principalTable: "lesson_messages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_feedback_results_lesson_sessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "lesson_sessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_daily_usage_counters_UserId_UsageDate_StudyLanguage", table: "daily_usage_counters", columns: new[] { "UserId", "UsageDate", "StudyLanguage" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_devices_UserId", table: "devices", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_feedback_results_MessageId", table: "feedback_results", column: "MessageId");
        migrationBuilder.CreateIndex(name: "IX_feedback_results_SessionId", table: "feedback_results", column: "SessionId");
        migrationBuilder.CreateIndex(name: "IX_lesson_messages_SessionId", table: "lesson_messages", column: "SessionId");
        migrationBuilder.CreateIndex(name: "IX_lesson_sessions_StartedAt", table: "lesson_sessions", column: "StartedAt");
        migrationBuilder.CreateIndex(name: "IX_lesson_sessions_Status", table: "lesson_sessions", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_lesson_sessions_UserId", table: "lesson_sessions", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_lesson_summaries_SessionId", table: "lesson_summaries", column: "SessionId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_lessons_LessonContentId", table: "lessons", column: "LessonContentId");
        migrationBuilder.CreateIndex(name: "IX_payments_UserId", table: "payments", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_subscriptions_UserId", table: "subscriptions", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_usage_events_CreatedAt", table: "usage_events", column: "CreatedAt");
        migrationBuilder.CreateIndex(name: "IX_usage_events_SessionId", table: "usage_events", column: "SessionId");
        migrationBuilder.CreateIndex(name: "IX_usage_events_UserId", table: "usage_events", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_user_profiles_UserId", table: "user_profiles", column: "UserId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_user_settings_UserId", table: "user_settings", column: "UserId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_users_Email", table: "users", column: "Email", unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "daily_usage_counters");
        migrationBuilder.DropTable(name: "devices");
        migrationBuilder.DropTable(name: "feedback_results");
        migrationBuilder.DropTable(name: "lesson_summaries");
        migrationBuilder.DropTable(name: "lessons");
        migrationBuilder.DropTable(name: "payments");
        migrationBuilder.DropTable(name: "subscriptions");
        migrationBuilder.DropTable(name: "usage_events");
        migrationBuilder.DropTable(name: "user_profiles");
        migrationBuilder.DropTable(name: "user_settings");
        migrationBuilder.DropTable(name: "lesson_messages");
        migrationBuilder.DropTable(name: "lesson_sessions");
        migrationBuilder.DropTable(name: "users");
    }
}
