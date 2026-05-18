using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations;

public partial class InitialProductStorageSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE users (
                "Id" uuid PRIMARY KEY,
                "Email" character varying(320) NOT NULL,
                "PasswordHash" character varying(512) NOT NULL,
                "Status" character varying(64) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "LastLoginAt" timestamp with time zone NULL
            );

            CREATE UNIQUE INDEX ix_users_email ON users ("Email");

            CREATE TABLE lessons (
                "Id" uuid PRIMARY KEY,
                "LessonContentId" character varying(128) NOT NULL,
                "TopicId" character varying(128) NOT NULL,
                "TopicTitle" character varying(256) NOT NULL,
                "SubtopicId" character varying(128) NOT NULL,
                "SubtopicTitle" character varying(256) NOT NULL,
                "Level" character varying(64) NOT NULL,
                "ContentVersion" integer NOT NULL,
                "IsActive" boolean NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );

            CREATE INDEX ix_lessons_lesson_content_id ON lessons ("LessonContentId");

            CREATE TABLE user_profiles (
                "Id" uuid PRIMARY KEY,
                "UserId" uuid NOT NULL,
                "DisplayName" character varying(160) NOT NULL,
                "NativeLanguage" character varying(64) NOT NULL,
                "CurrentLevel" character varying(64) NOT NULL,
                "SelectedTutorId" character varying(80) NULL,
                "Timezone" character varying(128) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT fk_user_profiles_users_user_id FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT
            );

            CREATE UNIQUE INDEX ix_user_profiles_user_id ON user_profiles ("UserId");

            CREATE TABLE user_settings (
                "Id" uuid PRIMARY KEY,
                "UserId" uuid NOT NULL,
                "StudyLanguage" character varying(64) NOT NULL,
                "ExplanationLanguage" character varying(64) NOT NULL,
                "SpeechVoice" character varying(512) NOT NULL,
                "SpeechSpeed" numeric(5,2) NOT NULL,
                "ConversationModeEnabled" boolean NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT fk_user_settings_users_user_id FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT
            );

            CREATE UNIQUE INDEX ix_user_settings_user_id ON user_settings ("UserId");

            CREATE TABLE lesson_sessions (
                "Id" uuid PRIMARY KEY,
                "UserId" uuid NOT NULL,
                "LessonContentId" character varying(128) NOT NULL,
                "StudyLanguage" character varying(64) NOT NULL,
                "TopicId" character varying(128) NOT NULL,
                "TopicTitle" character varying(256) NOT NULL,
                "SubtopicId" character varying(128) NOT NULL,
                "SubtopicTitle" character varying(256) NOT NULL,
                "Level" character varying(64) NOT NULL,
                "SelectedContextId" character varying(128) NULL,
                "SelectedContextTitle" character varying(256) NULL,
                "ModeUsed" character varying(64) NOT NULL,
                "Status" character varying(64) NOT NULL,
                "StartedAt" timestamp with time zone NOT NULL,
                "FinishedAt" timestamp with time zone NULL,
                "ValidTurnCount" integer NOT NULL,
                "EstimatedCost" numeric(18,6) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT fk_lesson_sessions_users_user_id FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX ix_lesson_sessions_started_at ON lesson_sessions ("StartedAt");
            CREATE INDEX ix_lesson_sessions_status ON lesson_sessions ("Status");
            CREATE INDEX ix_lesson_sessions_user_id ON lesson_sessions ("UserId");

            CREATE TABLE lesson_messages (
                "Id" uuid PRIMARY KEY,
                "SessionId" uuid NOT NULL,
                "Role" character varying(64) NOT NULL,
                "Text" character varying(20000) NOT NULL,
                "Source" character varying(64) NOT NULL,
                "TurnNumber" integer NOT NULL,
                "IsValidLessonTurn" boolean NOT NULL,
                "StudyLanguage" character varying(64) NOT NULL,
                "TranscriptConfidence" numeric(5,4) NULL,
                "AudioDurationMs" integer NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT fk_lesson_messages_lesson_sessions_session_id FOREIGN KEY ("SessionId") REFERENCES lesson_sessions ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX ix_lesson_messages_session_id ON lesson_messages ("SessionId");

            CREATE TABLE feedback_results (
                "Id" uuid PRIMARY KEY,
                "SessionId" uuid NOT NULL,
                "MessageId" uuid NOT NULL,
                "FeedbackType" character varying(80) NOT NULL,
                "CorrectedText" character varying(20000) NULL,
                "Explanation" character varying(4096) NULL,
                "GrammarTip" character varying(4096) NULL,
                "VocabularyTip" character varying(4096) NULL,
                "CultureTip" character varying(4096) NULL,
                "Praise" character varying(4096) NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT fk_feedback_results_lesson_messages_message_id FOREIGN KEY ("MessageId") REFERENCES lesson_messages ("Id") ON DELETE RESTRICT,
                CONSTRAINT fk_feedback_results_lesson_sessions_session_id FOREIGN KEY ("SessionId") REFERENCES lesson_sessions ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX ix_feedback_results_message_id ON feedback_results ("MessageId");
            CREATE INDEX ix_feedback_results_session_id ON feedback_results ("SessionId");

            CREATE TABLE lesson_summaries (
                "Id" uuid PRIMARY KEY,
                "SessionId" uuid NOT NULL,
                "WhatWentWell" character varying(4096) NULL,
                "WhatToImprove" character varying(4096) NULL,
                "UsefulPhrases" character varying(4096) NULL,
                "MistakesToReview" character varying(4096) NULL,
                "NextSteps" character varying(4096) NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT fk_lesson_summaries_lesson_sessions_session_id FOREIGN KEY ("SessionId") REFERENCES lesson_sessions ("Id") ON DELETE RESTRICT
            );

            CREATE UNIQUE INDEX ix_lesson_summaries_session_id ON lesson_summaries ("SessionId");

            CREATE TABLE usage_events (
                "Id" uuid PRIMARY KEY,
                "UserId" uuid NOT NULL,
                "SessionId" uuid NULL,
                "Operation" character varying(80) NOT NULL,
                "Model" character varying(128) NULL,
                "InputTokens" bigint NULL,
                "OutputTokens" bigint NULL,
                "AudioInputTokens" bigint NULL,
                "AudioOutputTokens" bigint NULL,
                "AudioDurationMs" integer NULL,
                "InputChars" bigint NULL,
                "OutputBytes" bigint NULL,
                "EstimatedCost" numeric(18,6) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT fk_usage_events_lesson_sessions_session_id FOREIGN KEY ("SessionId") REFERENCES lesson_sessions ("Id") ON DELETE RESTRICT,
                CONSTRAINT fk_usage_events_users_user_id FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX ix_usage_events_created_at ON usage_events ("CreatedAt");
            CREATE INDEX ix_usage_events_session_id ON usage_events ("SessionId");
            CREATE INDEX ix_usage_events_user_id ON usage_events ("UserId");

            CREATE TABLE daily_usage_counters (
                "Id" uuid PRIMARY KEY,
                "UserId" uuid NOT NULL,
                "UsageDate" date NOT NULL,
                "StudyLanguage" character varying(64) NOT NULL,
                "LessonsStarted" integer NOT NULL,
                "LessonsCompleted" integer NOT NULL,
                "HintsUsed" integer NOT NULL,
                "FeedbackRequests" integer NOT NULL,
                "TranscriptionSeconds" integer NOT NULL,
                "TtsSeconds" integer NOT NULL,
                "EstimatedCost" numeric(18,6) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT fk_daily_usage_counters_users_user_id FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT
            );

            CREATE UNIQUE INDEX ix_daily_usage_counters_user_id_usage_date_study_language ON daily_usage_counters ("UserId", "UsageDate", "StudyLanguage");

            CREATE TABLE subscriptions (
                "Id" uuid PRIMARY KEY,
                "UserId" uuid NOT NULL,
                "PlanId" character varying(128) NOT NULL,
                "Status" character varying(64) NOT NULL,
                "Provider" character varying(80) NOT NULL,
                "ProviderSubscriptionId" character varying(256) NULL,
                "StartedAt" timestamp with time zone NOT NULL,
                "ExpiresAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT fk_subscriptions_users_user_id FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX ix_subscriptions_user_id ON subscriptions ("UserId");

            CREATE TABLE payments (
                "Id" uuid PRIMARY KEY,
                "UserId" uuid NOT NULL,
                "Amount" numeric(18,2) NOT NULL,
                "Currency" character varying(3) NOT NULL,
                "Status" character varying(64) NOT NULL,
                "Provider" character varying(80) NOT NULL,
                "ProviderPaymentId" character varying(256) NULL,
                "ProviderPayloadJson" character varying(20000) NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "PaidAt" timestamp with time zone NULL,
                CONSTRAINT fk_payments_users_user_id FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX ix_payments_user_id ON payments ("UserId");

            CREATE TABLE devices (
                "Id" uuid PRIMARY KEY,
                "UserId" uuid NOT NULL,
                "Platform" character varying(80) NOT NULL,
                "DeviceName" character varying(160) NOT NULL,
                "AppVersion" character varying(80) NOT NULL,
                "LastSeenAt" timestamp with time zone NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT fk_devices_users_user_id FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX ix_devices_user_id ON devices ("UserId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS devices;
            DROP TABLE IF EXISTS payments;
            DROP TABLE IF EXISTS subscriptions;
            DROP TABLE IF EXISTS daily_usage_counters;
            DROP TABLE IF EXISTS usage_events;
            DROP TABLE IF EXISTS lesson_summaries;
            DROP TABLE IF EXISTS feedback_results;
            DROP TABLE IF EXISTS lesson_messages;
            DROP TABLE IF EXISTS lesson_sessions;
            DROP TABLE IF EXISTS user_settings;
            DROP TABLE IF EXISTS user_profiles;
            DROP TABLE IF EXISTS lessons;
            DROP TABLE IF EXISTS users;
            """);
    }
}
