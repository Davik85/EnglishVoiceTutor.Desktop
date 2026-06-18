using EnglishVoiceTutor.Api.Constants;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations;

[Migration("20260618090000_SeedBaseSubscriptionPlans")]
public partial class SeedBaseSubscriptionPlans : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            INSERT INTO "plans" ("Id", "PlanId", "DisplayName", "Tier", "IsActive", "CreatedAt", "UpdatedAt")
            VALUES
                (gen_random_uuid(), '{SubscriptionConstants.Plans.FreePlanId}', '{SubscriptionConstants.Plans.FreePlanName}', '{SubscriptionConstants.Plans.FreeTier}', TRUE, NOW(), NOW()),
                (gen_random_uuid(), '{SubscriptionConstants.Plans.PremiumPlanId}', '{SubscriptionConstants.Plans.PremiumPlanName}', '{SubscriptionConstants.Plans.PremiumTier}', TRUE, NOW(), NOW())
            ON CONFLICT ("PlanId") DO UPDATE
            SET
                "DisplayName" = EXCLUDED."DisplayName",
                "Tier" = EXCLUDED."Tier",
                "IsActive" = TRUE,
                "UpdatedAt" = NOW();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Base subscription plans are required reference data and are not removed automatically.
    }
}
