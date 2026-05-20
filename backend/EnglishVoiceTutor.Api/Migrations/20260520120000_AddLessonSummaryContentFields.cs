using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations;

/// <inheritdoc />
public partial class AddLessonSummaryContentFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Grammar",
            table: "lesson_summaries",
            type: "character varying(4096)",
            maxLength: 4096,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Improvements",
            table: "lesson_summaries",
            type: "character varying(4096)",
            maxLength: 4096,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "NextSteps",
            table: "lesson_summaries",
            type: "character varying(4096)",
            maxLength: 4096,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Strengths",
            table: "lesson_summaries",
            type: "character varying(4096)",
            maxLength: 4096,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Summary",
            table: "lesson_summaries",
            type: "character varying(4096)",
            maxLength: 4096,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "UpdatedAt",
            table: "lesson_summaries",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTimeOffset(new DateTime(1, 1, 1), TimeSpan.Zero));

        migrationBuilder.AddColumn<string>(
            name: "Vocabulary",
            table: "lesson_summaries",
            type: "character varying(4096)",
            maxLength: 4096,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE lesson_summaries
            SET \"Summary\" = COALESCE(\"WhatWentWell\", ''),
                \"Strengths\" = \"MistakesToReview\",
                \"Improvements\" = \"WhatToImprove\",
                \"UpdatedAt\" = \"CreatedAt\"
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Grammar", table: "lesson_summaries");
        migrationBuilder.DropColumn(name: "Improvements", table: "lesson_summaries");
        migrationBuilder.DropColumn(name: "NextSteps", table: "lesson_summaries");
        migrationBuilder.DropColumn(name: "Strengths", table: "lesson_summaries");
        migrationBuilder.DropColumn(name: "Summary", table: "lesson_summaries");
        migrationBuilder.DropColumn(name: "UpdatedAt", table: "lesson_summaries");
        migrationBuilder.DropColumn(name: "Vocabulary", table: "lesson_summaries");
    }
}
