using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260721120000_AddActiveAccountDeletionRequestConstraint")]
    public partial class AddActiveAccountDeletionRequestConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_user_feedback_reports_ActiveAccountDeletionRequest_UserId"
                ON user_feedback_reports ("UserId")
                WHERE "Category" = 'account_deletion'
                  AND "Status" IN ('new', 'reviewed', 'needs_information', 'processing');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX \"IX_user_feedback_reports_ActiveAccountDeletionRequest_UserId\";");
        }
    }
}
