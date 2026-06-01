using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonSessionHeartbeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastHeartbeatAtUtc",
                table: "lesson_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_lesson_sessions_LastHeartbeatAtUtc",
                table: "lesson_sessions",
                column: "LastHeartbeatAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_lesson_sessions_LastHeartbeatAtUtc",
                table: "lesson_sessions");

            migrationBuilder.DropColumn(
                name: "LastHeartbeatAtUtc",
                table: "lesson_sessions");
        }
    }
}
