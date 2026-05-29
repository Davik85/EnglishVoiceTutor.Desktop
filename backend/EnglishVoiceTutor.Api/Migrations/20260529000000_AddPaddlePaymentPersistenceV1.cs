using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaddlePaymentPersistenceV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AmountMinor",
                table: "payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BilledAt",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FailedAt",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalPlanId",
                table: "payments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderCustomerId",
                table: "payments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderEventId",
                table: "payments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProviderEventOccurredAtUtc",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderEventType",
                table: "payments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPriceId",
                table: "payments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderProductId",
                table: "payments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderSubscriptionId",
                table: "payments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafeMetadataJson",
                table: "payments",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionId",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.CreateIndex(
                name: "IX_payments_SubscriptionId",
                table: "payments",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_Provider_ProviderPaymentId",
                table: "payments",
                columns: new[] { "Provider", "ProviderPaymentId" },
                unique: true,
                filter: "\"ProviderPaymentId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_SubscriptionId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_Provider_ProviderPaymentId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "AmountMinor",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "BilledAt",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "InternalPlanId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ProviderCustomerId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ProviderEventId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ProviderEventOccurredAtUtc",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ProviderEventType",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ProviderPriceId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ProviderProductId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ProviderSubscriptionId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "SafeMetadataJson",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "payments");
        }
    }
}
