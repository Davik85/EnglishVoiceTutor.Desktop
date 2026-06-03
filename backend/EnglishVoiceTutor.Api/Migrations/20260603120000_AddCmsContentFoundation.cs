using System;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260603120000_AddCmsContentFoundation")]
    public partial class AddCmsContentFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cms_content_packs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    BaseStaticContentVersion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cms_content_packs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cms_content_packs_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cms_content_packs_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cms_lesson_topics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    StableTopicKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cms_lesson_topics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cms_lesson_topics_cms_content_packs_ContentPackId",
                        column: x => x.ContentPackId,
                        principalTable: "cms_content_packs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cms_prompt_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TargetStudyLanguageId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    AllowedPlaceholdersJson = table.Column<string>(type: "text", nullable: false),
                    RequiredPlaceholdersJson = table.Column<string>(type: "text", nullable: false),
                    MaxLength = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cms_prompt_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cms_prompt_templates_cms_content_packs_ContentPackId",
                        column: x => x.ContentPackId,
                        principalTable: "cms_content_packs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cms_prompt_templates_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cms_tutor_behavior_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CommunicationStyleJson = table.Column<string>(type: "text", nullable: false),
                    SafetyNotesJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cms_tutor_behavior_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cms_tutor_behavior_profiles_cms_content_packs_ContentPackId",
                        column: x => x.ContentPackId,
                        principalTable: "cms_content_packs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cms_content_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    SnapshotHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PublishStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PublishedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidationSummaryJson = table.Column<string>(type: "text", nullable: false),
                    ChangeSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RestoredFromVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cms_content_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cms_content_versions_cms_content_packs_ContentPackId",
                        column: x => x.ContentPackId,
                        principalTable: "cms_content_packs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cms_content_versions_cms_content_versions_RestoredFromVersi~",
                        column: x => x.RestoredFromVersionId,
                        principalTable: "cms_content_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cms_content_versions_users_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cms_content_audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentPackId = table.Column<Guid>(type: "uuid", nullable: true),
                    BeforeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AfterHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ChangedFieldsJson = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestMetadataJson = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cms_content_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cms_content_audit_logs_cms_content_packs_ContentPackId",
                        column: x => x.ContentPackId,
                        principalTable: "cms_content_packs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cms_content_audit_logs_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cms_lesson_scenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    StableScenarioKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LessonType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SupportedLevelIdsJson = table.Column<string>(type: "text", nullable: false),
                    SetupMessage = table.Column<string>(type: "text", nullable: false),
                    ContextSelectionJson = table.Column<string>(type: "text", nullable: false),
                    LearningGoalJson = table.Column<string>(type: "text", nullable: false),
                    SituationJson = table.Column<string>(type: "text", nullable: false),
                    RolesJson = table.Column<string>(type: "text", nullable: false),
                    TargetLanguageJson = table.Column<string>(type: "text", nullable: false),
                    LevelProfilesJson = table.Column<string>(type: "text", nullable: false),
                    ConversationFlowJson = table.Column<string>(type: "text", nullable: false),
                    RoleplayBeatsJson = table.Column<string>(type: "text", nullable: false),
                    ReciprocalQuestionHandlingJson = table.Column<string>(type: "text", nullable: false),
                    ExpectedScenarioProgressionJson = table.Column<string>(type: "text", nullable: false),
                    ControlledVariationJson = table.Column<string>(type: "text", nullable: false),
                    OffTopicHandlingJson = table.Column<string>(type: "text", nullable: false),
                    FeedbackRulesJson = table.Column<string>(type: "text", nullable: false),
                    HintRulesJson = table.Column<string>(type: "text", nullable: false),
                    RepetitionLogicJson = table.Column<string>(type: "text", nullable: false),
                    AiTutorPromptInstructionsJson = table.Column<string>(type: "text", nullable: false),
                    SoftWrapUpAfterUserTurn = table.Column<int>(type: "integer", nullable: true),
                    FinalMessageAtUserTurn = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cms_lesson_scenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cms_lesson_scenarios_cms_content_packs_ContentPackId",
                        column: x => x.ContentPackId,
                        principalTable: "cms_content_packs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cms_lesson_scenarios_cms_lesson_topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "cms_lesson_topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cms_published_content_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    SnapshotHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cms_published_content_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cms_published_content_snapshots_cms_content_versions_Content~",
                        column: x => x.ContentVersionId,
                        principalTable: "cms_content_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cms_content_audit_logs_ActorUserId_CreatedAtUtc",
                table: "cms_content_audit_logs",
                columns: new[] { "ActorUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_cms_content_audit_logs_ContentPackId_CreatedAtUtc",
                table: "cms_content_audit_logs",
                columns: new[] { "ContentPackId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_cms_content_packs_CreatedByUserId",
                table: "cms_content_packs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_cms_content_packs_Slug",
                table: "cms_content_packs",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cms_content_packs_Status",
                table: "cms_content_packs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cms_content_packs_UpdatedByUserId",
                table: "cms_content_packs",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_cms_content_versions_ContentPackId_VersionNumber",
                table: "cms_content_versions",
                columns: new[] { "ContentPackId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cms_content_versions_PublishedByUserId",
                table: "cms_content_versions",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_cms_content_versions_RestoredFromVersionId",
                table: "cms_content_versions",
                column: "RestoredFromVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_cms_lesson_scenarios_ContentPackId_StableScenarioKey",
                table: "cms_lesson_scenarios",
                columns: new[] { "ContentPackId", "StableScenarioKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cms_lesson_scenarios_TopicId",
                table: "cms_lesson_scenarios",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_cms_lesson_topics_ContentPackId_SortOrder",
                table: "cms_lesson_topics",
                columns: new[] { "ContentPackId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_cms_lesson_topics_ContentPackId_StableTopicKey",
                table: "cms_lesson_topics",
                columns: new[] { "ContentPackId", "StableTopicKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cms_prompt_templates_ContentPackId_TemplateKey_TargetStudyLan~",
                table: "cms_prompt_templates",
                columns: new[] { "ContentPackId", "TemplateKey", "TargetStudyLanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cms_prompt_templates_UpdatedByUserId",
                table: "cms_prompt_templates",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_cms_published_content_snapshots_ContentVersionId",
                table: "cms_published_content_snapshots",
                column: "ContentVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cms_tutor_behavior_profiles_ContentPackId_TutorId",
                table: "cms_tutor_behavior_profiles",
                columns: new[] { "ContentPackId", "TutorId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "cms_content_audit_logs");
            migrationBuilder.DropTable(name: "cms_lesson_scenarios");
            migrationBuilder.DropTable(name: "cms_prompt_templates");
            migrationBuilder.DropTable(name: "cms_published_content_snapshots");
            migrationBuilder.DropTable(name: "cms_tutor_behavior_profiles");
            migrationBuilder.DropTable(name: "cms_lesson_topics");
            migrationBuilder.DropTable(name: "cms_content_versions");
            migrationBuilder.DropTable(name: "cms_content_packs");
        }
    }
}
