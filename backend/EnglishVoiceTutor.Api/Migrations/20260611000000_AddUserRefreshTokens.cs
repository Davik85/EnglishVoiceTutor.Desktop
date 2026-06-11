using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations;

[Migration("20260611000000_AddUserRefreshTokens")]
public partial class AddUserRefreshTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "user_refresh_tokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ReplacedByTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                CreatedByIp = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                RevokedByIp = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                RevocationReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_refresh_tokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_user_refresh_tokens_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_user_refresh_tokens_ExpiresAtUtc",
            table: "user_refresh_tokens",
            column: "ExpiresAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_user_refresh_tokens_TokenHash",
            table: "user_refresh_tokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_user_refresh_tokens_UserId",
            table: "user_refresh_tokens",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "user_refresh_tokens");
    }
}
