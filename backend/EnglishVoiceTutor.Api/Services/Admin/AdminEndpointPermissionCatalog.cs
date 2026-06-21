using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed record AdminEndpointPermissionMapping(
    string ActionKey,
    string HttpMethod,
    string? RoutePattern,
    string RequiredPermission,
    string Description);

public static class AdminEndpointPermissionCatalog
{
    public static IReadOnlyList<AdminEndpointPermissionMapping> Mappings { get; } =
    [
        new("admin.identity.read", "GET", ApiConstants.AdminMeRoute, AdminPermissionConstants.AdminSelfRead, "Read the signed-in admin identity and bootstrap role/permission capabilities."),
        new("admin.capabilities.read", "GET", ApiConstants.AdminCapabilitiesRoute, AdminPermissionConstants.AdminCapabilitiesRead, "Read Admin feature capability flags."),
        new("admin.product_overview.read", "GET", ApiConstants.AdminStatisticsOverviewRoute, AdminPermissionConstants.ProductStatisticsRead, "Read product statistics overview."),
        new("admin.role_assignments.diagnostics.read", "GET", ApiConstants.AdminRoleAssignmentDiagnosticsRoute, AdminPermissionConstants.AdminRolesManage, "Read-only diagnostics for future persistent Admin role assignments."),
        new("admin.rbac.cutover_status.read", "GET", ApiConstants.AdminRbacCutoverStatusRoute, AdminPermissionConstants.AdminRolesManage, "Read safe Admin RBAC cutover status, including the effective BootstrapAdmin fallback state for AdminPermission policies."),
        new("admin.role_assignments.actor.read", "GET", ApiConstants.AdminRoleAssignmentActorRoute, AdminPermissionConstants.AdminRolesManage, "Read the authenticated admin's persistent actor mapping status without mutating role assignment state."),
        new("admin.role_assignments.revoke", "POST", ApiConstants.AdminRoleAssignmentRevokeRoute, AdminPermissionConstants.AdminRolesManage, "Revoke an existing persistent Admin role assignment through the guarded write-service seam."),
        new("admin.role_assignments.assign", "POST", ApiConstants.AdminRoleAssignmentAssignRoute, AdminPermissionConstants.AdminRolesManage, "Assign a persistent Admin role to an existing persistent AdminUser through the guarded write-service seam."),
        new("admin.role_assignments.disable_admin", "POST", ApiConstants.AdminRoleAssignmentDisableAdminRoute, AdminPermissionConstants.AdminRolesManage, "Disable an existing persistent AdminUser through the guarded write-service seam."),
        new("admin.role_assignments.enable_admin", "POST", ApiConstants.AdminRoleAssignmentEnableAdminRoute, AdminPermissionConstants.AdminRolesManage, "Re-enable an existing disabled persistent AdminUser through the guarded write-service seam."),
        new("admin.role_assignments.provision_admin_user", "POST", ApiConstants.AdminRoleAssignmentProvisionAdminUserRoute, AdminPermissionConstants.AdminRolesManage, "Provision a persistent AdminUser mapping for an existing app user through the guarded provisioning-service seam."),
        new("admin.role_assignments.bootstrap_first_owner", "POST", ApiConstants.AdminRoleAssignmentBootstrapFirstOwnerRoute, AdminPermissionConstants.AdminRolesManage, "Bootstrap the first persistent owner-equivalent Admin mapping for the authenticated admin user."),
        new("admin.users.lookup_by_email", "GET", ApiConstants.AdminUserByEmailRoute, AdminPermissionConstants.UserLookupRead, "Look up an Admin-visible user record by email."),
        new("admin.users.overview.read", "GET", ApiConstants.AdminUserByIdRoute, AdminPermissionConstants.UserOverviewRead, "Read an Admin-visible user overview by user id."),
        new("admin.users.audit.read", "GET", ApiConstants.AdminUserAuditActionsRoute, AdminPermissionConstants.AuditRead, "Read audit actions for a target user."),
        new("admin.premium.grant", "POST", ApiConstants.AdminUserPremiumGrantsRoute, AdminPermissionConstants.PremiumGrant, "Grant manual Premium access to a user."),
        new("admin.premium.revoke", "POST", ApiConstants.AdminUserPremiumGrantRevokeRoute, AdminPermissionConstants.PremiumRevoke, "Revoke a manual Premium entitlement from a user."),
        new("admin.free_lesson_allowance.reset", "POST", ApiConstants.AdminUserFreeLessonAllowanceResetRoute, AdminPermissionConstants.FreeLessonAllowanceReset, "Reset a user's free lesson allowance."),
        new("admin.billing.cancel_renewal", "POST", ApiConstants.AdminUserBillingCancelRenewalRoute, AdminPermissionConstants.BillingCancelRenewal, "Cancel renewal for a user's billing subscription through the backend provider abstraction."),
        new("admin.cms.static_content.import", "POST", ApiConstants.AdminDevCmsStaticContentImportRoute, AdminPermissionConstants.CmsContentWriteDraft, "Import static CMS content into the draft CMS store."),
        new("admin.cms.static_json_v1.initialize", "POST", ApiConstants.AdminDevCmsStaticJsonV1InitializeRoute, AdminPermissionConstants.CmsContentWriteDraft, "Initialize the static-json-v1 CMS draft content pack."),
        new("admin.cms.published_status.read", "GET", ApiConstants.AdminDevCmsPublishedContentStatusRoute, AdminPermissionConstants.CmsContentRead, "Read published CMS content status."),
        new("admin.cms.runtime_content_status.read", "GET", ApiConstants.AdminDevCmsRuntimeContentStatusRoute, AdminPermissionConstants.CmsRuntimeStatusRead, "Read runtime CMS content status."),
        new("admin.cms.runtime_status.read", "GET", ApiConstants.AdminDevCmsRuntimeStatusRoute, AdminPermissionConstants.CmsRuntimeStatusRead, "Read runtime CMS status."),
        new("admin.cms.content_packs.list", "GET", ApiConstants.AdminDevCmsContentPacksRoute, AdminPermissionConstants.CmsContentRead, "List CMS content packs."),
        new("admin.cms.content_pack.read", "GET", ApiConstants.AdminDevCmsContentPackRoute, AdminPermissionConstants.CmsContentRead, "Read a CMS content pack summary."),
        new("admin.cms.topics.list", "GET", ApiConstants.AdminDevCmsContentPackTopicsRoute, AdminPermissionConstants.CmsContentRead, "List CMS topics."),
        new("admin.cms.topic.read", "GET", ApiConstants.AdminDevCmsContentPackTopicRoute, AdminPermissionConstants.CmsContentRead, "Read a CMS topic."),
        new("admin.cms.topic.draft_save", "PUT", ApiConstants.AdminDevCmsContentPackTopicRoute, AdminPermissionConstants.CmsContentWriteDraft, "Save a CMS topic draft change."),
        new("admin.cms.scenarios.list", "GET", ApiConstants.AdminDevCmsContentPackScenariosRoute, AdminPermissionConstants.CmsContentRead, "List CMS scenarios."),
        new("admin.cms.scenario.read", "GET", ApiConstants.AdminDevCmsContentPackScenarioRoute, AdminPermissionConstants.CmsContentRead, "Read a CMS scenario."),
        new("admin.cms.scenario.draft_save", "PUT", ApiConstants.AdminDevCmsContentPackScenarioRoute, AdminPermissionConstants.CmsContentWriteDraft, "Save a CMS scenario draft change."),
        new("admin.cms.prompt_templates.list", "GET", ApiConstants.AdminDevCmsContentPackPromptTemplatesRoute, AdminPermissionConstants.CmsContentRead, "List CMS prompt templates."),
        new("admin.cms.prompt_template.read", "GET", ApiConstants.AdminDevCmsContentPackPromptTemplateRoute, AdminPermissionConstants.CmsContentRead, "Read a CMS prompt template."),
        new("admin.cms.prompt_template.draft_save", "PUT", ApiConstants.AdminDevCmsContentPackPromptTemplateRoute, AdminPermissionConstants.CmsContentWriteDraft, "Save a CMS prompt template draft change."),
        new("admin.cms.tutor_behavior_profiles.list", "GET", ApiConstants.AdminDevCmsContentPackTutorBehaviorProfilesRoute, AdminPermissionConstants.CmsContentRead, "List CMS tutor behavior profiles."),
        new("admin.cms.tutor_behavior_profile.read", "GET", ApiConstants.AdminDevCmsContentPackTutorBehaviorProfileRoute, AdminPermissionConstants.CmsContentRead, "Read a CMS tutor behavior profile."),
        new("admin.cms.tutor_behavior_profile.draft_save", "PUT", ApiConstants.AdminDevCmsContentPackTutorBehaviorProfileRoute, AdminPermissionConstants.CmsContentWriteDraft, "Save a CMS tutor behavior profile draft change."),
        new("admin.cms.audit.read", "GET", ApiConstants.AdminDevCmsAuditEntriesRoute, AdminPermissionConstants.AuditRead, "Read CMS audit entries."),
        new("admin.cms.content_pack_audit.read", "GET", ApiConstants.AdminDevCmsContentPackAuditEntriesRoute, AdminPermissionConstants.AuditRead, "Read CMS audit entries scoped to a content pack."),
        new("admin.cms.validate", "POST", ApiConstants.AdminDevCmsContentPackValidateRoute, AdminPermissionConstants.CmsContentRead, "Validate a CMS draft content pack without saving or publishing changes."),
        new("admin.cms.preview_summary.read", "GET", ApiConstants.AdminDevCmsContentPackPreviewSummaryRoute, AdminPermissionConstants.CmsContentRead, "Read a CMS draft preview summary."),
        new("admin.cms.versions.list", "GET", ApiConstants.AdminDevCmsContentPackVersionsRoute, AdminPermissionConstants.CmsContentRead, "List CMS published versions."),
        new("admin.cms.version.read", "GET", ApiConstants.AdminDevCmsContentPackVersionRoute, AdminPermissionConstants.CmsContentRead, "Read a CMS published version."),
        new("admin.cms.publish", "POST", ApiConstants.AdminDevCmsContentPackPublishRoute, AdminPermissionConstants.CmsContentPublish, "Publish a CMS draft content pack."),
        new("admin.cms.restore", "POST", ApiConstants.AdminDevCmsContentPackVersionRestoreRoute, AdminPermissionConstants.CmsContentRestore, "Restore a CMS content pack version."),
        new("admin.users.read", "FUTURE", null, AdminPermissionConstants.UsersRead, "Future broad user-list/read seam; no active endpoint is switched to this permission yet."),
        new("admin.users.diagnostics.read", "FUTURE", null, AdminPermissionConstants.UsersDiagnosticsRead, "Future user diagnostics seam; no active endpoint is switched to this permission yet."),
        new("admin.lesson_history.diagnostics.read", "FUTURE", null, AdminPermissionConstants.LessonHistoryDiagnosticsRead, "Future lesson-history diagnostics seam; no active endpoint is switched to this permission yet."),
        new("admin.subscriptions.diagnostics.read", "FUTURE", null, AdminPermissionConstants.SubscriptionsDiagnosticsRead, "Future subscription diagnostics seam; no active endpoint is switched to this permission yet."),
        new("admin.premium.diagnostics.read", "FUTURE", null, AdminPermissionConstants.PremiumDiagnosticsRead, "Future Premium diagnostics seam; no active endpoint is switched to this permission yet."),
        new("admin.billing.diagnostics.read", "FUTURE", null, AdminPermissionConstants.BillingDiagnosticsRead, "Future billing/provider diagnostics seam; no active endpoint is switched to this permission yet."),
        new("admin.system.diagnostics.read", "FUTURE", null, AdminPermissionConstants.SystemDiagnosticsRead, "Future system diagnostics seam; no active endpoint is switched to this permission yet."),
        new("admin.roles.manage", "FUTURE", null, AdminPermissionConstants.AdminRolesManage, "Future broader Admin role-assignment management seam; additional role-management endpoints are not active yet.")
    ];
}
