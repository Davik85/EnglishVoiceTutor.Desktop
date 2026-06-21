namespace EnglishVoiceTutor.Api.Services.Admin;

public static class AdminRoleAssignmentAuditConstants
{
    public static class ActionTypes
    {
        public const string AssignRole = "assign_role";
        public const string RevokeRole = "revoke_role";
        public const string DisableAdmin = "disable_admin";
        public const string EnableAdmin = "enable_admin";
        public const string InviteCreated = "invite_created";
        public const string InviteRevoked = "invite_revoked";
        public const string LastOwnerBlocked = "last_owner_blocked";
        public const string SelfEscalationBlocked = "self_escalation_blocked";
        public const string ValidationDenied = "validation_denied";
        public const string FirstOwnerBootstrap = "first_owner_bootstrap";
        public const string AdminUserProvisioned = "admin_user_provisioned";
        public const string AdminUserProvisioningDenied = "admin_user_provisioning_denied";
    }

    public static class Results
    {
        public const string Succeeded = "succeeded";
        public const string Denied = "denied";
        public const string FailedValidation = "failed_validation";
        public const string FailedConflict = "failed_conflict";
    }
}
