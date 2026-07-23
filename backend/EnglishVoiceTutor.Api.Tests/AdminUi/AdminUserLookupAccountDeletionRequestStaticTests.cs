namespace EnglishVoiceTutor.Api.Tests.AdminUi;

public sealed class AdminUserLookupAccountDeletionRequestStaticTests
{
    private static readonly string AdminJs = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.js"));
    private static readonly string AdminIndex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/index.html"));

    [Fact]
    public void IntakeControlIsSelectedUserAndExecutePermissionGated()
    {
        Assert.Contains("id=\"account-deletion-request-card\"", AdminIndex);
        Assert.Contains("Start account deletion request", AdminIndex);
        Assert.Contains("accountAnonymizationExecute: \"account_anonymization.execute\"", AdminJs);
        Assert.Contains("setAccountDeletionRequestVisible(Boolean(selectedUserId) && hasAdminPermission(AdminPermissionIds.accountAnonymizationExecute));", AdminJs);
        Assert.Contains("adminUserAccountDeletionRequestsTemplate: \"/api/admin/users/{userId}/account-deletion-requests\"", AdminJs);
    }

    [Fact]
    public void IntakePostsOnlySelectedUserIdAndCommentWithPendingProtection()
    {
        var flow = AdminJs.Substring(AdminJs.IndexOf("async function createAccountDeletionRequestForSelectedUser"), AdminJs.IndexOf("async function grantPremiumForSelectedUser") - AdminJs.IndexOf("async function createAccountDeletionRequestForSelectedUser"));

        Assert.Contains("const userId = String(selectedUserId || \"\");", flow);
        Assert.Contains("const comment = String(accountDeletionRequestCommentInput.value || \"\").trim();", flow);
        Assert.Contains("accountDeletionRequestPending", flow);
        Assert.Contains("method: \"POST\"", flow);
        Assert.Contains("JSON.stringify({ comment })", flow);
        Assert.Contains("payload?.alreadyRequested === true", flow);
        Assert.Contains("textContent", flow);
        Assert.DoesNotContain("preflight", flow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("paddle", flow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("premium", flow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("entitlement", flow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", flow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("delete", flow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("innerHTML", flow);
    }
}
