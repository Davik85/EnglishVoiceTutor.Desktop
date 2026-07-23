using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountAnonymizationExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAtUtc",
                table: "account_anonymization_operations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureCode",
                table: "account_anonymization_operations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultCountsJson",
                table: "account_anonymization_operations",
                type: "character varying(12000)",
                maxLength: 12000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAtUtc",
                table: "account_anonymization_operations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationState",
                table: "account_anonymization_operations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "account_anonymization_operations");

            migrationBuilder.DropColumn(
                name: "FailureCode",
                table: "account_anonymization_operations");

            migrationBuilder.DropColumn(
                name: "ResultCountsJson",
                table: "account_anonymization_operations");

            migrationBuilder.DropColumn(
                name: "StartedAtUtc",
                table: "account_anonymization_operations");

            migrationBuilder.DropColumn(
                name: "VerificationState",
                table: "account_anonymization_operations");
        }
    }
}
