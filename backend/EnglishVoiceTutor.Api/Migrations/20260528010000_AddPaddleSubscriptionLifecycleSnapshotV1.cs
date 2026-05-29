using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaddleSubscriptionLifecycleSnapshotV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastProviderEventOccurredAtUtc",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastProviderEventId",
                table: "subscriptions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastProviderEventType",
                table: "subscriptions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSyncedAtUtc",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPriceId",
                table: "subscriptions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderProductId",
                table: "subscriptions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduledChangeAction",
                table: "subscriptions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledChangeEffectiveAtUtc",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_Provider_ProviderSubscriptionId",
                table: "subscriptions",
                columns: new[] { "Provider", "ProviderSubscriptionId" },
                unique: true,
                filter: "\"ProviderSubscriptionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_subscriptions_Provider_ProviderSubscriptionId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "LastProviderEventOccurredAtUtc",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "LastProviderEventId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "LastProviderEventType",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "LastSyncedAtUtc",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "ProviderPriceId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "ProviderProductId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "ScheduledChangeAction",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "ScheduledChangeEffectiveAtUtc",
                table: "subscriptions");
        }
    }
}
