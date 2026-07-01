namespace EnglishVoiceTutor.Api.Tests.Migrations;

public sealed class AdminAuthAuditMigrationTests
{
    private static readonly string Migration = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/Migrations/20260701000000_AddAdminAuthAuditEvents.cs"));
    private static readonly string Designer = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/Migrations/20260701000000_AddAdminAuthAuditEvents.Designer.cs"));
    private static readonly string Snapshot = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/Migrations/AppDbContextModelSnapshot.cs"));

    [Fact]
    public void MigrationCreatesAdminAuthAuditEventsTable()
    {
        Assert.Contains("admin_auth_audit_events", Migration);
        foreach (var column in new[] { "OccurredAtUtc", "EventType", "Result", "ActorUserId", "ActorAdminUserId", "ActorEmail", "AttemptedEmail", "AdminSource", "RoleIdsJson", "FailureReasonCode", "SafeMetadataJson" })
        {
            Assert.Contains(column, Migration);
        }

        Assert.Contains("FK_admin_auth_audit_events_users_ActorUserId", Migration);
        Assert.Contains("FK_admin_auth_audit_events_admin_users_ActorAdminUserId", Migration);
    }

    [Fact]
    public void DesignerAndSnapshotIncludeAdminAuthAuditEventsModel()
    {
        Assert.Contains("AdminAuthAuditEventEntity", Designer);
        Assert.Contains("admin_auth_audit_events", Designer);
        Assert.Contains("AdminAuthAuditEventEntity", Snapshot);
        Assert.Contains("admin_auth_audit_events", Snapshot);
    }
}
