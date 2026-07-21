(() => {
    const ApiPaths = {
        login: "/api/auth/login",
        adminSession: "/api/admin/session",
        adminMe: "/api/admin/me",
        capabilities: "/api/admin/capabilities",
        statisticsOverview: "/api/admin/statistics/overview",
        userLookupByEmail: "/api/admin/users/by-email",
        userLookupByIdTemplate: "/api/admin/users/{userId}",
        auditActionsTemplate: "/api/admin/users/{userId}/audit-actions",
        adminActivity: "/api/admin/activity",
        feedbackReports: "/api/admin/feedback-reports",
        feedbackReportTemplate: "/api/admin/feedback-reports/{reportId}",
        feedbackReportStatusTemplate: "/api/admin/feedback-reports/{reportId}/status",
        feedbackReportRepliesTemplate: "/api/admin/feedback-reports/{reportId}/replies",
        manualPremiumGrantTemplate: "/api/admin/users/{userId}/premium-grants",
        manualPremiumRevokeTemplate: "/api/admin/users/{userId}/premium-grants/{entitlementId}/revoke",
        freeLessonAllowanceResetTemplate: "/api/admin/users/{userId}/free-lesson-allowance/reset",
        billingCancelRenewalTemplate: "/api/admin/users/{userId}/billing/cancel-renewal",
        cmsContentPacks: "/api/admin/dev/cms/content-packs",
        cmsContentPackTemplate: "/api/admin/dev/cms/content-packs/{slug}",
        cmsTopicsTemplate: "/api/admin/dev/cms/content-packs/{slug}/topics",
        cmsTopicTemplate: "/api/admin/dev/cms/content-packs/{slug}/topics/{topicId}",
        cmsScenariosTemplate: "/api/admin/dev/cms/content-packs/{slug}/scenarios",
        cmsScenarioTemplate: "/api/admin/dev/cms/content-packs/{slug}/scenarios/{scenarioId}",
        cmsPromptTemplatesTemplate: "/api/admin/dev/cms/content-packs/{slug}/prompt-templates",
        cmsPromptTemplateTemplate: "/api/admin/dev/cms/content-packs/{slug}/prompt-templates/{templateId}",
        cmsTutorProfilesTemplate: "/api/admin/dev/cms/content-packs/{slug}/tutor-behavior-profiles",
        cmsTutorProfileTemplate: "/api/admin/dev/cms/content-packs/{slug}/tutor-behavior-profiles/{profileId}",
        cmsValidateTemplate: "/api/admin/dev/cms/content-packs/{slug}/validate",
        cmsPreviewSummaryTemplate: "/api/admin/dev/cms/content-packs/{slug}/preview-summary",
        cmsVersionsTemplate: "/api/admin/dev/cms/content-packs/{slug}/versions",
        cmsPublishTemplate: "/api/admin/dev/cms/content-packs/{slug}/publish",
        cmsRestoreTemplate: "/api/admin/dev/cms/content-packs/{slug}/versions/{versionNumber}/restore",
        cmsAuditEntriesTemplate: "/api/admin/dev/cms/content-packs/{slug}/audit-entries",
        cmsStaticJsonV1Initialize: "/api/admin/dev/cms/content-packs/static-json-v1/initialize-from-static-json",
        cmsRuntimeStatus: "/api/admin/dev/cms/runtime-status",
        roleAssignmentActor: "/api/admin/role-assignments/actor",
        roleAssignmentDiagnostics: "/api/admin/role-assignments/diagnostics",
        rbacCutoverStatus: "/api/admin/rbac/cutover-status",
        roleAssignmentProvisionAdminUser: "/api/admin/role-assignments/provision-admin-user",
        roleAssignmentAssign: "/api/admin/role-assignments/assign",
        roleAssignmentRevoke: "/api/admin/role-assignments/revoke",
        roleAssignmentDisableAdmin: "/api/admin/role-assignments/disable-admin",
        roleAssignmentEnableAdmin: "/api/admin/role-assignments/enable-admin",
        websiteContent: "/api/admin/website/content",
        websiteContentDraft: "/api/admin/website/content/draft",
        websiteContentPublish: "/api/admin/website/content/publish",
        websiteContentPreview: "/api/admin/website/content/preview",
        aiModelSettings: "/api/admin/system/ai-models",
        aiModelSettingsDraft: "/api/admin/system/ai-models/draft",
        aiModelSettingsValidate: "/api/admin/system/ai-models/validate",
        aiModelSettingsProviderTest: "/api/admin/system/ai-models/test-provider-access",
        aiModelSettingsPublish: "/api/admin/system/ai-models/publish",
        aiModelSettingsResetDraft: "/api/admin/system/ai-models/reset-draft"
    };

    const HttpStatus = { badRequest: 400, unauthorized: 401, forbidden: 403, notFound: 404, conflict: 409, serviceUnavailable: 503 };
    const ErrorMessages = {
        emailRequired: "Email is required or invalid.",
        userNotFound: "User was not found.",
        signInAgain: "Please sign in again.",
        sessionExpired: "Admin session expired or is no longer valid. Please sign in again.",
        accessDenied: "Access denied. This account is not an admin.",
        lookupFailed: "Unable to load user.",
        invalidAuditLimit: "Invalid audit log limit.",
        auditTargetNotFound: "User or audit log target was not found.",
        auditLoadFailed: "Unable to load audit log.",
        activityLoadFailed: "Unable to load Admin Activity.",
        invalidActivityLimit: "Invalid Admin Activity limit.",
        grantInvalid: "Grant request is invalid. Check duration and reason.",
        grantUserNotFound: "Selected user was not found.",
        grantFailed: "Unable to grant Premium.",
        revokeInvalid: "Revoke request is invalid. Reason is required.",
        revokeNotFound: "Selected user or entitlement was not found.",
        revokeConflict: "This entitlement cannot be revoked.",
        revokeFailed: "Unable to revoke Premium.",
        revokeNoEntitlements: "No active Premium entitlements are available for emergency revoke.",
        resetInvalid: "Reset request is invalid. Check usage date and reason.",
        resetNotFound: "No consumed free lesson allowance was found for this user and date.",
        resetFailed: "Unable to reset free lesson allowance.",
        statisticsLoadFailed: "Unable to load product statistics.",
        billingCancelInvalid: "Cancel paid renewal reason is required.",
        billingCancelNotFound: "Selected user was not found.",
        billingCancelFailed: "Unable to cancel paid renewal.",
        roleManagementLoadFailed: "Unable to load role management data.",
        roleManagementMutationFailed: "Unable to update persistent admin access.",
        roleManagementReasonRequired: "Reason is required.",
        roleManagementConfirmationRequired: "Confirm that this action changes persistent admin access."
    };

    const SummaryFields = ["userId", "email", "status", "createdAt", "lastLoginAt"];
    const SubscriptionFields = ["planId", "planName", "premiumActive", "trialActive", "trialEndsAtUtc", "subscriptionStatus", "billingProvider", "renewalStatus", "nextRenewalState", "cancelAtPeriodEnd", "scheduledChangeAction", "scheduledChangeEffectiveAtUtc", "currentPeriodEndUtc", "paidAccessUntilUtc", "hasActivePaidProviderSubscription", "providerSubscriptionPresent", "canRequestCancelRenewal", "cancellationExplanationCode", "lastProviderEventId", "lastProviderEventType", "lastProviderEventOccurredAtUtc", "freeLessonUsedToday", "freeLessonRemainingToday", "enforcementEnabled", "source", "checkedAtUtc"];
    const EntitlementColumns = ["entitlementId", "planId", "entitlementType", "source", "status", "startsAtUtc", "expiresAtUtc", "reason", "createdAt", "updatedAt"];
    const LessonSessionColumns = ["sessionId", "lessonContentId", "studyLanguage", "topicTitle", "subtopicTitle", "level", "modeUsed", "status", "startedAt", "finishedAt", "validTurnCount", "estimatedCost"];
    const DailyUsageColumns = ["usageDate", "studyLanguage", "lessonsStarted", "lessonsCompleted", "chatReplyCount", "hintsUsed", "feedbackRequests", "transcriptionSeconds", "ttsSeconds", "estimatedCost", "updatedAt"];
    const UsageEventColumns = ["usageEventId", "sessionId", "operation", "model", "studyLanguage", "status", "inputTokens", "outputTokens", "audioDurationMs", "inputChars", "outputBytes", "estimatedCost", "createdAt"];
    const AuditColumns = ["createdAtUtc", "actionType", "reason", "adminUserId", "adminActionId", "safeMetadataJson"];
    const ActivityColumns = ["occurredAtUtc", "actorEmail", "actionType", "result", "targetType", "targetUserEmail", "targetAdminUserEmail", "source", { key: "adminNote", label: "Admin note", className: "admin-note-cell" }, "safeMetadataJson"];
    const ActivityTableOptions = Object.freeze({ wrapClassName: "table-wrap admin-activity-table-wrapper", topScroll: true });
    const Tabs = Object.freeze({ overview: "overview", userLookup: "user-lookup", premium: "premium", freeLesson: "free-lesson", auditLog: "audit-log", adminActivity: "admin-activity", feedbackReports: "feedback-reports", cmsContent: "cms-content", website: "website", roleManagement: "role-management", system: "system" });
    const AdminPermissionIds = Object.freeze({
        usersRead: "users.read",
        userLookupRead: "users.lookup.read",
        userOverviewRead: "users.overview.read",
        usersDiagnosticsRead: "users.diagnostics.read",
        lessonHistoryDiagnosticsRead: "lesson_history.diagnostics.read",
        subscriptionsDiagnosticsRead: "subscriptions.diagnostics.read",
        premiumDiagnosticsRead: "premium.diagnostics.read",
        billingDiagnosticsRead: "billing.diagnostics.read",
        premiumGrant: "premium.grant",
        premiumRevoke: "premium.revoke",
        freeLessonAllowanceReset: "free_lesson_allowance.reset",
        billingCancelRenewal: "billing.cancel_renewal",
        auditRead: "audit.read",
        feedbackReportsRead: "feedback_reports.read",
        feedbackReportsStatusManage: "feedback_reports.status.manage",
        feedbackReportsReply: "feedback_reports.reply",
        cmsContentRead: "cms.content.read",
        cmsContentWriteDraft: "cms.content.write_draft",
        cmsContentPublish: "cms.content.publish",
        cmsContentRestore: "cms.content.restore",
        cmsRuntimeStatusRead: "cms.runtime_status.read",
        productStatisticsRead: "product_statistics.read",
        adminRolesManage: "admin.roles.manage",
        systemAiModelSettingsManage: "system.ai_model_settings.manage"
    });
    const WorkflowAvailabilityDefinitions = Object.freeze([
        { label: "User Lookup", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.userLookupRead, AdminPermissionIds.userOverviewRead] },
        { label: "User Diagnostics", statusWhenAvailable: "read-only / available", anyPermissions: [AdminPermissionIds.usersDiagnosticsRead] },
        { label: "Lesson History Diagnostics", statusWhenAvailable: "read-only / available", anyPermissions: [AdminPermissionIds.lessonHistoryDiagnosticsRead] },
        { label: "Subscription Diagnostics", statusWhenAvailable: "read-only / available", anyPermissions: [AdminPermissionIds.subscriptionsDiagnosticsRead] },
        { label: "Premium Diagnostics", statusWhenAvailable: "read-only / available", anyPermissions: [AdminPermissionIds.premiumDiagnosticsRead] },
        { label: "Billing Diagnostics", statusWhenAvailable: "read-only / available", anyPermissions: [AdminPermissionIds.billingDiagnosticsRead] },
        { label: "Premium Grant", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.premiumGrant] },
        { label: "Premium Revoke", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.premiumRevoke] },
        { label: "Free Lesson Reset", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.freeLessonAllowanceReset] },
        { label: "Billing Cancel Renewal", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.billingCancelRenewal] },
        { label: "Audit Log", statusWhenAvailable: "read-only / available", anyPermissions: [AdminPermissionIds.auditRead] },
        { label: "Admin Activity", statusWhenAvailable: "read-only / available", anyPermissions: [AdminPermissionIds.auditRead] },
        { label: "CMS Content", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.cmsContentRead] },
        { label: "CMS Draft Editing", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.cmsContentWriteDraft] },
        { label: "CMS Publish", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.cmsContentPublish] },
        { label: "CMS Restore", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.cmsContentRestore] },
        { label: "Runtime Status", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.cmsRuntimeStatusRead] },
        { label: "Product Statistics", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.productStatisticsRead] },
        { label: "Persistent Admin Roles", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.adminRolesManage] }
    ]);


    const TabPermissionDefinitions = Object.freeze({
        [Tabs.overview]: { anyPermissions: [] },
        [Tabs.userLookup]: { allPermissions: [AdminPermissionIds.userLookupRead, AdminPermissionIds.userOverviewRead] },
        [Tabs.premium]: { anyPermissions: [AdminPermissionIds.premiumGrant, AdminPermissionIds.premiumRevoke, AdminPermissionIds.billingCancelRenewal] },
        [Tabs.freeLesson]: { anyPermissions: [AdminPermissionIds.freeLessonAllowanceReset] },
        [Tabs.auditLog]: { anyPermissions: [AdminPermissionIds.auditRead] },
        [Tabs.adminActivity]: { anyPermissions: [AdminPermissionIds.auditRead] },
        [Tabs.feedbackReports]: { anyPermissions: [AdminPermissionIds.feedbackReportsRead] },
        [Tabs.cmsContent]: { anyPermissions: [AdminPermissionIds.cmsContentRead] },
        [Tabs.website]: { bootstrapAdminOnly: true },
        [Tabs.roleManagement]: { anyPermissions: [AdminPermissionIds.adminRolesManage] },
        [Tabs.system]: { anyPermissions: [AdminPermissionIds.systemAiModelSettingsManage] }
    });
    const NotAvailableForRoleMessage = "Not available for this role.";

    const CmsSubTabs = Object.freeze({ overview: "overview", topics: "topics", scenarios: "scenarios", levels: "levels", prompts: "prompts", tutors: "tutors", validationPreview: "validation-preview", versionsPublish: "versions-publish", audit: "audit" });
    const LookupSources = Object.freeze({ userLookup: "user-lookup", premium: "premium", freeLesson: "free-lesson" });
    let accessToken = null;
    let selectedUserId = null;
    let selectedUserEmail = null;
    let selectedUserLookupPayload = null;
    let cmsHasLoadedOnce = false;
    let websiteHasLoadedOnce = false;
    let aiModelsHaveLoadedOnce = false;
    let cmsSelectedTopic = null;
    let cmsSelectedScenario = null;
    let cmsSelectedPromptTemplate = null;
    let cmsSelectedLevel = null;
    let cmsSelectedTutorProfile = null;
    let cmsTopics = [];
    let cmsScenarios = [];
    let cmsPromptTemplates = [];
    let cmsLevels = [];
    let cmsTutorProfiles = [];
    let tabsInitialized = false;
    let restoringCmsSelection = false;
    let cmsDraftSavedInSession = false;
    let cmsDraftLikelyHasChangesInSession = false;
    let adminAccessSnapshot = { roles: [], permissions: [], isBootstrapAdmin: false, productionRolesAvailable: false, adminSource: "", environment: "", checkedAtUtc: "" };
    const CmsDefaultLevelProfiles = [
        { stableLevelKey: "a1", displayName: "A1 Beginner", isActive: true, sortOrder: 1, wrapUpAfterUserTurn: 10, finalMessageAtUserTurn: 15, botLanguageComplexityGuidance: "Use simple short sentences, simple words, and one question at a time. Give more support.", correctionGuidance: "Correct one important mistake gently and give a short model answer.", answerLengthGuidance: "Use 1-2 short sentences.", adminNotes: "Default required A1 level profile." },
        { stableLevelKey: "a2", displayName: "A2 Elementary", isActive: true, sortOrder: 2, wrapUpAfterUserTurn: 14, finalMessageAtUserTurn: 20, botLanguageComplexityGuidance: "Use simple but slightly more varied language. Ask one clear question at a time.", correctionGuidance: "Correct lightly with short examples.", answerLengthGuidance: "Use 1-3 short sentences.", adminNotes: "Default required A2 level profile." },
        { stableLevelKey: "b1", displayName: "B1 Intermediate", isActive: true, sortOrder: 3, wrapUpAfterUserTurn: 18, finalMessageAtUserTurn: 25, botLanguageComplexityGuidance: "Use more natural dialogue with moderate detail.", correctionGuidance: "Give moderate corrections for clarity, grammar, and natural phrasing.", answerLengthGuidance: "Use concise natural turns with one useful detail.", adminNotes: "Default required B1 level profile." },
        { stableLevelKey: "b2", displayName: "B2 Upper-Intermediate", isActive: true, sortOrder: 4, wrapUpAfterUserTurn: 24, finalMessageAtUserTurn: 32, botLanguageComplexityGuidance: "Support longer discussion, natural expressions, and deeper corrections.", correctionGuidance: "Give deeper corrections for precision, register, and naturalness.", answerLengthGuidance: "Use natural but not monologue-length responses.", adminNotes: "Default required B2 level profile." }
    ];

    const loginCard = document.getElementById("login-card");
    const dashboard = document.getElementById("dashboard");
    const loginForm = document.getElementById("login-form");
    const loginError = document.getElementById("login-error");
    const signInButton = document.getElementById("sign-in-button");
    const logoutButton = document.getElementById("logout-button");
    const selectedUserSummaryElement = document.getElementById("selected-user-summary");
    const tabButtons = Array.from(document.querySelectorAll(".admin-tab-button"));
    const tabPanels = Array.from(document.querySelectorAll(".tab-panel"));
    const adminSourceElement = document.getElementById("admin-source");
    const environmentElement = document.getElementById("environment");
    const checkedAtElement = document.getElementById("checked-at");
    const capabilitiesListElement = document.getElementById("capabilities-list");
    const bootstrapAdminStatusElement = document.getElementById("bootstrap-admin-status");
    const adminRolesBadgesElement = document.getElementById("admin-roles-badges");
    const adminPermissionCountElement = document.getElementById("admin-permission-count");
    const workflowAvailabilityListElement = document.getElementById("workflow-availability-list");
    const rolesPermissionsRolesElement = document.getElementById("roles-permissions-roles");
    const rolesPermissionsListElement = document.getElementById("roles-permissions-list");
    const systemProductionRolesAvailableElement = document.getElementById("system-production-roles-available");
    const systemBillingPaddleStatusElement = document.getElementById("system-billing-paddle-status");
    const websiteTabButton = document.getElementById("tab-button-website");
    const websiteTabPanel = document.getElementById("tab-panel-website");
    const roleManagementRefreshButton = document.getElementById("role-management-refresh-button");
    const roleManagementLoadingElement = document.getElementById("role-management-loading");
    const roleManagementErrorElement = document.getElementById("role-management-error");
    const roleManagementWarningElement = document.getElementById("role-management-warning");
    const roleManagementActorElement = document.getElementById("role-management-actor");
    const roleManagementDiagnosticsElement = document.getElementById("role-management-diagnostics");
    const roleManagementCutoverStatusElement = document.getElementById("role-management-cutover-status");
    const roleManagementUsersElement = document.getElementById("role-management-users");
    const roleManagementForms = Array.from(document.querySelectorAll(".role-management-form"));
    let roleManagementActorMappingFound = false;
    const refreshStatisticsButton = document.getElementById("refresh-statistics-button");
    const statisticsLoadingElement = document.getElementById("statistics-loading");
    const statisticsErrorElement = document.getElementById("statistics-error");
    const statisticsCardsElement = document.getElementById("statistics-cards");
    const statisticsCheckedAtElement = document.getElementById("statistics-checked-at");
    const studyLanguageDistributionElement = document.getElementById("study-language-distribution");
    const nativeLanguageDistributionElement = document.getElementById("native-language-distribution");
    const explanationLanguageDistributionElement = document.getElementById("explanation-language-distribution");
    const practicedStudyLanguageDistributionElement = document.getElementById("practiced-study-language-distribution");

    const lookupForm = document.getElementById("lookup-form");
    const lookupEmailInput = document.getElementById("lookup-email");
    const searchUserButton = document.getElementById("search-user-button");
    const lookupLoadingElement = document.getElementById("lookup-loading");
    const lookupErrorElement = document.getElementById("lookup-error");
    const lookupResultElement = document.getElementById("lookup-result");
    const premiumLookupForm = document.getElementById("premium-lookup-form");
    const premiumLookupEmailInput = document.getElementById("premium-lookup-email");
    const premiumSearchUserButton = document.getElementById("premium-search-user-button");
    const premiumLookupLoadingElement = document.getElementById("premium-lookup-loading");
    const premiumLookupErrorElement = document.getElementById("premium-lookup-error");
    const freeLessonLookupForm = document.getElementById("free-lesson-lookup-form");
    const freeLessonLookupEmailInput = document.getElementById("free-lesson-lookup-email");
    const freeLessonSearchUserButton = document.getElementById("free-lesson-search-user-button");
    const freeLessonLookupLoadingElement = document.getElementById("free-lesson-lookup-loading");
    const freeLessonLookupErrorElement = document.getElementById("free-lesson-lookup-error");
    const premiumScheduleResultElement = document.getElementById("premium-entitlement-schedule-result");
    const activeEntitlementsResultElement = document.getElementById("active-entitlements-result");
    const premiumContentElement = document.getElementById("premium-content");
    const premiumEmptyStateElement = document.getElementById("premium-empty-state");
    const freeLessonEmptyStateElement = document.getElementById("free-lesson-empty-state");
    const auditEmptyStateElement = document.getElementById("audit-empty-state");

    const grantCard = document.getElementById("grant-card");
    const grantForm = document.getElementById("grant-form");
    const grantSelectedUserEmailElement = document.getElementById("grant-selected-user-email");
    const grantSelectedUserIdElement = document.getElementById("grant-selected-user-id");
    const grantDurationDaysInput = document.getElementById("grant-duration-days");
    const grantReasonInput = document.getElementById("grant-reason");
    const grantButton = document.getElementById("grant-button");
    const grantLoadingElement = document.getElementById("grant-loading");
    const grantErrorElement = document.getElementById("grant-error");
    const grantSuccessElement = document.getElementById("grant-success");

    const revokeCard = document.getElementById("revoke-card");
    const revokeForm = document.getElementById("revoke-form");
    const revokeSelectedUserEmailElement = document.getElementById("revoke-selected-user-email");
    const revokeSelectedUserIdElement = document.getElementById("revoke-selected-user-id");
    const revokeEntitlementIdElement = document.getElementById("revoke-entitlement-id");
    const revokeEntitlementPreviewElement = document.getElementById("revoke-entitlement-preview");
    const revokeReasonInput = document.getElementById("revoke-reason");
    const revokeButton = document.getElementById("revoke-button");
    const revokeLoadingElement = document.getElementById("revoke-loading");
    const revokeErrorElement = document.getElementById("revoke-error");
    const revokeSuccessElement = document.getElementById("revoke-success");

    const auditCardElement = document.getElementById("audit-card");
    const auditSelectedUserIdElement = document.getElementById("audit-selected-user-id");
    const auditLimitElement = document.getElementById("audit-limit");
    const loadAuditButton = document.getElementById("load-audit-button");
    const auditLoadingElement = document.getElementById("audit-loading");
    const auditErrorElement = document.getElementById("audit-error");
    const auditResultElement = document.getElementById("audit-result");
    const activityActorAdminUserIdInput = document.getElementById("activity-actor-admin-user-id");
    const activityActorUserIdInput = document.getElementById("activity-actor-user-id");
    const activityTargetUserIdInput = document.getElementById("activity-target-user-id");
    const activityTargetAdminUserIdInput = document.getElementById("activity-target-admin-user-id");
    const activitySourceInput = document.getElementById("activity-source");
    const activityResultInput = document.getElementById("activity-result");
    const activityActionTypeInput = document.getElementById("activity-action-type");
    const activityFromUtcInput = document.getElementById("activity-from-utc");
    const activityToUtcInput = document.getElementById("activity-to-utc");
    const activityLimitInput = document.getElementById("activity-limit");
    const loadActivityButton = document.getElementById("load-activity-button");
    const activityLoadingElement = document.getElementById("activity-loading");
    const activityErrorElement = document.getElementById("activity-error");
    const activityResultElement = document.getElementById("activity-result-table");
    const feedbackReportsStatusFilter = document.getElementById("feedback-reports-status-filter");
    const feedbackReportsCategoryFilter = document.getElementById("feedback-reports-category-filter");
    const feedbackReportsLoadingElement = document.getElementById("feedback-reports-loading");
    const feedbackReportsErrorElement = document.getElementById("feedback-reports-error");
    const feedbackReportsListElement = document.getElementById("feedback-reports-list");
    const feedbackReportsPreviousButton = document.getElementById("feedback-reports-previous");
    const feedbackReportsNextButton = document.getElementById("feedback-reports-next");
    const feedbackReportsSummaryElement = document.getElementById("feedback-reports-summary");
    const feedbackReportDetailsCard = document.getElementById("feedback-report-details-card");
    const feedbackReportDetailsLoadingElement = document.getElementById("feedback-report-details-loading");
    const feedbackReportDetailsErrorElement = document.getElementById("feedback-report-details-error");
    const feedbackReportDetailsElement = document.getElementById("feedback-report-details");
    const feedbackReportStatusActionsElement = document.getElementById("feedback-report-status-actions");
    const feedbackReportCurrentStatusElement = document.getElementById("feedback-report-current-status");
    const feedbackReportStatusButtonsElement = document.getElementById("feedback-report-status-buttons");
    const feedbackReportStatusProgressElement = document.getElementById("feedback-report-status-progress");
    const feedbackReportStatusErrorElement = document.getElementById("feedback-report-status-error");
    const feedbackReportStatusSuccessElement = document.getElementById("feedback-report-status-success");
    const feedbackReportReplyActionsElement = document.getElementById("feedback-report-reply-actions");
    const feedbackReportReplyRecipientElement = document.getElementById("feedback-report-reply-recipient");
    const feedbackReportReplyTextInput = document.getElementById("feedback-report-reply-text");
    const feedbackReportReplyLengthElement = document.getElementById("feedback-report-reply-length");
    const feedbackReportSendReplyButton = document.getElementById("feedback-report-send-reply");
    const feedbackReportReplyProgressElement = document.getElementById("feedback-report-reply-progress");
    const feedbackReportReplyErrorElement = document.getElementById("feedback-report-reply-error");
    const feedbackReportReplySuccessElement = document.getElementById("feedback-report-reply-success");
    const feedbackReportReplyHistoryElement = document.getElementById("feedback-report-reply-history");
    const feedbackReportReplyHistoryContentElement = document.getElementById("feedback-report-reply-history-content");
    const FeedbackReportPageSize = 50;
    const FeedbackReportStatuses = Object.freeze(["new", "reviewed", "needs_information", "processing", "resolved", "rejected"]);
    const FeedbackReportCategories = Object.freeze(["suggestion", "app_issue", "ai_response", "account_deletion"]);
    let feedbackReportsState = { page: 1, totalCount: 0, items: [], selectedReportId: null, selectedReport: null, statusRequestPending: false, replyRequestPending: false, replyUnavailable: false, statusPermissionDenied: false, replyPermissionDenied: false };

    const freeLessonResetCard = document.getElementById("free-lesson-reset-card");
    const freeLessonResetForm = document.getElementById("free-lesson-reset-form");
    const freeLessonResetSelectedUserEmailElement = document.getElementById("free-lesson-reset-selected-user-email");
    const freeLessonResetSelectedUserIdElement = document.getElementById("free-lesson-reset-selected-user-id");
    const freeLessonResetUsedTodayElement = document.getElementById("free-lesson-reset-used-today");
    const freeLessonResetRemainingTodayElement = document.getElementById("free-lesson-reset-remaining-today");
    const freeLessonResetEnforcementEnabledElement = document.getElementById("free-lesson-reset-enforcement-enabled");
    const freeLessonResetCheckedAtUtcElement = document.getElementById("free-lesson-reset-checked-at-utc");
    const freeLessonResetUsageDateInput = document.getElementById("free-lesson-reset-usage-date");
    const freeLessonResetReasonInput = document.getElementById("free-lesson-reset-reason");
    const freeLessonResetButton = document.getElementById("free-lesson-reset-button");

    const billingCancelRenewalCard = document.getElementById("billing-cancel-renewal-card");
    const billingCancelRenewalForm = document.getElementById("billing-cancel-renewal-form");
    const billingCancelRenewalSelectedUserEmailElement = document.getElementById("billing-cancel-renewal-selected-user-email");
    const billingCancelRenewalSelectedUserIdElement = document.getElementById("billing-cancel-renewal-selected-user-id");
    const billingCancelRenewalAvailabilityElement = document.getElementById("billing-cancel-renewal-availability");
    const billingCancelRenewalReasonInput = document.getElementById("billing-cancel-renewal-reason");
    const billingCancelRenewalButton = document.getElementById("billing-cancel-renewal-button");
    const billingCancelRenewalLoadingElement = document.getElementById("billing-cancel-renewal-loading");
    const billingCancelRenewalErrorElement = document.getElementById("billing-cancel-renewal-error");
    const billingCancelRenewalSuccessElement = document.getElementById("billing-cancel-renewal-success");
    const freeLessonResetLoadingElement = document.getElementById("free-lesson-reset-loading");
    const freeLessonResetErrorElement = document.getElementById("free-lesson-reset-error");
    const freeLessonResetSuccessElement = document.getElementById("free-lesson-reset-success");


    const cmsSubTabButtons = Array.from(document.querySelectorAll(".cms-sub-tab-button"));
    const cmsSubPanels = Array.from(document.querySelectorAll(".cms-sub-panel"));
    const cmsLoadContentPacksButton = document.getElementById("cms-load-content-packs-button");
    const cmsContentPackSelect = document.getElementById("cms-content-pack-select");
    const cmsRefreshButton = document.getElementById("cms-refresh-button");
    const cmsInitializeStaticJsonButton = document.getElementById("cms-initialize-static-json-button");
    const cmsStaticJsonInitializePanel = document.getElementById("cms-static-json-initialize-panel");
    const cmsLoadingElement = document.getElementById("cms-loading");
    const cmsErrorElement = document.getElementById("cms-error");
    const cmsSuccessElement = document.getElementById("cms-success");
    const cmsPublishDiscoveryElements = Array.from(document.querySelectorAll("[data-cms-publish-discovery]"));
    const cmsGoToPublishButtons = Array.from(document.querySelectorAll(".cms-go-to-publish-button"));
    const cmsSummarySlugElement = document.getElementById("cms-summary-slug");
    const cmsSummaryNameElement = document.getElementById("cms-summary-name");
    const cmsSummaryStatusElement = document.getElementById("cms-summary-status");
    const cmsSummaryTopicCountElement = document.getElementById("cms-summary-topic-count");
    const cmsSummaryScenarioCountElement = document.getElementById("cms-summary-scenario-count");
    const cmsSummaryPromptTemplateCountElement = document.getElementById("cms-summary-prompt-template-count");
    const cmsSummaryTutorProfileCountElement = document.getElementById("cms-summary-tutor-profile-count");
    const cmsSummaryPublishedVersionElement = document.getElementById("cms-summary-published-version");
    const cmsTopicsListElement = document.getElementById("cms-topics-list");
    const cmsTopicFilterInput = document.getElementById("cms-topic-filter");
    const cmsScenariosListElement = document.getElementById("cms-scenarios-list");
    const cmsScenarioFilterInput = document.getElementById("cms-scenario-filter");
    const cmsScenarioTopicFilterSelect = document.getElementById("cms-scenario-topic-filter");
    const cmsPromptTemplatesListElement = document.getElementById("cms-prompt-templates-list");
    const cmsTutorProfilesListElement = document.getElementById("cms-tutor-profiles-list");
    const cmsTopicForm = document.getElementById("cms-topic-form");
    const cmsTopicTitleInput = document.getElementById("cms-topic-title");
    const cmsTopicDescriptionInput = document.getElementById("cms-topic-description");
    const cmsTopicSortOrderInput = document.getElementById("cms-topic-sort-order");
    const cmsTopicIsActiveInput = document.getElementById("cms-topic-is-active");
    const cmsSelectedTopicIdentityElement = document.getElementById("cms-selected-topic-identity");
    const cmsTopicResetButton = document.getElementById("cms-topic-reset-button");
    const cmsTopicMessageElement = document.getElementById("cms-topic-message");
    const cmsTopicDirtyStatusElement = document.getElementById("cms-topic-dirty-status");
    const cmsScenarioForm = document.getElementById("cms-scenario-form");
    const cmsScenarioTitleInput = document.getElementById("cms-scenario-title");
    const cmsScenarioDescriptionInput = document.getElementById("cms-scenario-description");
    const cmsScenarioSetupMessageInput = document.getElementById("cms-scenario-setup-message");
    const cmsScenarioIsActiveInput = document.getElementById("cms-scenario-is-active");
    const cmsScenarioDefinitionJsonInput = document.getElementById("cms-scenario-definition-json");
    const cmsScenarioFirstBotMessageLinesInput = document.getElementById("cms-scenario-first-bot-message-lines");
    const cmsScenarioContextOptionLinesInput = document.getElementById("cms-scenario-context-option-lines");
    const cmsScenarioValidContextKeywordsLinesInput = document.getElementById("cms-scenario-valid-context-keywords-lines");
    const cmsScenarioCustomContextRulesLinesInput = document.getElementById("cms-scenario-custom-context-rules-lines");
    const cmsScenarioInvalidContextRedirectInput = document.getElementById("cms-scenario-invalid-context-redirect");
    const cmsScenarioGoalTextInput = document.getElementById("cms-scenario-goal-text");
    const cmsScenarioCanDoLinesInput = document.getElementById("cms-scenario-can-do-lines");
    const cmsScenarioOpeningTextInput = document.getElementById("cms-scenario-opening-text");
    const cmsScenarioFirstUserTaskInput = document.getElementById("cms-scenario-first-user-task");
    const cmsScenarioGuidedFollowUpLinesInput = document.getElementById("cms-scenario-guided-follow-up-lines");
    const cmsScenarioAiInstructionLinesInput = document.getElementById("cms-scenario-ai-instruction-lines");
    const cmsScenarioWrapUpMessageInput = document.getElementById("cms-scenario-wrap-up-message");
    const cmsScenarioFinalMessageInput = document.getElementById("cms-scenario-final-message");
    const cmsScenarioHintExampleInput = document.getElementById("cms-scenario-hint-example");
    const cmsScenarioValidateStructuredButton = document.getElementById("cms-scenario-validate-structured-button");
    const cmsScenarioStructuredStatusElement = document.getElementById("cms-scenario-structured-status");
    const cmsScenarioFormatJsonButton = document.getElementById("cms-scenario-format-json-button");
    const cmsScenarioValidateJsonButton = document.getElementById("cms-scenario-validate-json-button");
    const cmsScenarioJsonStatusElement = document.getElementById("cms-scenario-json-status");
    const cmsSelectedScenarioIdentityElement = document.getElementById("cms-selected-scenario-identity");
    const cmsScenarioResetButton = document.getElementById("cms-scenario-reset-button");
    const cmsScenarioStructuredResetButton = document.getElementById("cms-scenario-structured-reset-button");
    const cmsScenarioMessageElement = document.getElementById("cms-scenario-message");
    const cmsScenarioStructuredPublishDiscoveryElement = document.getElementById("cms-scenario-structured-publish-discovery");
    const cmsScenarioJsonPublishDiscoveryElement = document.getElementById("cms-scenario-json-publish-discovery");
    const cmsScenarioDirtyStatusElement = document.getElementById("cms-scenario-dirty-status");
    const cmsScenarioSectionNavButtons = [...document.querySelectorAll("[data-cms-scenario-section-target]")];
    const cmsLevelForm = document.getElementById("cms-level-form");
    const cmsLevelsListElement = document.getElementById("cms-levels-list");
    const cmsSelectedLevelIdentityElement = document.getElementById("cms-selected-level-identity");
    const cmsLevelDisplayNameInput = document.getElementById("cms-level-display-name");
    const cmsLevelSortOrderInput = document.getElementById("cms-level-sort-order");
    const cmsLevelWrapUpTurnInput = document.getElementById("cms-level-wrap-up-turn");
    const cmsLevelFinalTurnInput = document.getElementById("cms-level-final-turn");
    const cmsLevelComplexityGuidanceInput = document.getElementById("cms-level-complexity-guidance");
    const cmsLevelCorrectionGuidanceInput = document.getElementById("cms-level-correction-guidance");
    const cmsLevelAnswerGuidanceInput = document.getElementById("cms-level-answer-guidance");
    const cmsLevelAdminNotesInput = document.getElementById("cms-level-admin-notes");
    const cmsLevelIsActiveInput = document.getElementById("cms-level-is-active");
    const cmsLevelResetButton = document.getElementById("cms-level-reset-button");
    const cmsLevelInitializeButton = document.getElementById("cms-level-initialize-button");
    const cmsLevelMessageElement = document.getElementById("cms-level-message");
    const cmsLevelDirtyStatusElement = document.getElementById("cms-level-dirty-status");
    const cmsPromptTemplateForm = document.getElementById("cms-prompt-template-form");
    const cmsPromptTemplateBodyInput = document.getElementById("cms-prompt-template-body");
    const cmsPromptTemplateIsActiveInput = document.getElementById("cms-prompt-template-is-active");
    const cmsSelectedPromptTemplateIdentityElement = document.getElementById("cms-selected-prompt-template-identity");
    const cmsPromptTemplateResetButton = document.getElementById("cms-prompt-template-reset-button");
    const cmsPromptTemplateMessageElement = document.getElementById("cms-prompt-template-message");
    const cmsPromptTemplateDirtyStatusElement = document.getElementById("cms-prompt-template-dirty-status");
    const cmsTutorProfileForm = document.getElementById("cms-tutor-profile-form");
    const cmsTutorProfileDisplayNameInput = document.getElementById("cms-tutor-profile-display-name");
    const cmsTutorProfileCommunicationStyleJsonInput = document.getElementById("cms-tutor-profile-communication-style-json");
    const cmsTutorProfileSafetyNotesJsonInput = document.getElementById("cms-tutor-profile-safety-notes-json");
    const cmsTutorProfileIsActiveInput = document.getElementById("cms-tutor-profile-is-active");
    const cmsSelectedTutorProfileIdentityElement = document.getElementById("cms-selected-tutor-profile-identity");
    const cmsTutorProfileResetButton = document.getElementById("cms-tutor-profile-reset-button");
    const cmsTutorProfileMessageElement = document.getElementById("cms-tutor-profile-message");
    const cmsTutorProfileDirtyStatusElement = document.getElementById("cms-tutor-profile-dirty-status");
    const cmsRunValidationButton = document.getElementById("cms-run-validation-button");
    const cmsValidationResultElement = document.getElementById("cms-validation-result");
    const cmsLoadPreviewButton = document.getElementById("cms-load-preview-button");
    const cmsPreviewSummaryElement = document.getElementById("cms-preview-summary");
    const cmsLoadRuntimeStatusButton = document.getElementById("cms-load-runtime-status-button");
    const cmsOverviewLoadRuntimeStatusButton = document.getElementById("cms-overview-load-runtime-status-button");
    const cmsRuntimeStatusElement = document.getElementById("cms-runtime-status");
    const cmsOverviewRuntimeStatusElement = document.getElementById("cms-overview-runtime-status");
    const cmsLoadVersionsButton = document.getElementById("cms-load-versions-button");
    const cmsPublishChangeSummaryInput = document.getElementById("cms-publish-change-summary");
    const cmsPublishErrorDetailsElement = document.getElementById("cms-publish-error-details");
    const cmsPublishButton = document.getElementById("cms-publish-button");
    const cmsRestoreVersionSelect = document.getElementById("cms-restore-version-select");
    const cmsRestoreReasonInput = document.getElementById("cms-restore-reason");
    const cmsRestoreButton = document.getElementById("cms-restore-button");
    const cmsVersionsListElement = document.getElementById("cms-versions-list");
    const cmsPublishSectionElement = document.getElementById("cms-publish-section");
    const cmsLoadAuditButton = document.getElementById("cms-load-audit-button");
    const cmsAuditEntityTypeSelect = document.getElementById("cms-audit-entity-type");
    const cmsAuditStableKeyInput = document.getElementById("cms-audit-stable-key");
    const cmsAuditLimitSelect = document.getElementById("cms-audit-limit");
    const cmsAuditShowSmokeInput = document.getElementById("cms-audit-show-smoke");
    const cmsAuditLoadingElement = document.getElementById("cms-audit-loading");
    const cmsAuditErrorElement = document.getElementById("cms-audit-error");
    const cmsAuditSmokeFilterStatusElement = document.getElementById("cms-audit-smoke-filter-status");
    const cmsAuditListElement = document.getElementById("cms-audit-list");


    function focusCmsScenarioEditorSection(sectionId) {
        const section = document.getElementById(sectionId);
        if (!section) { return; }
        if (section.tagName.toLowerCase() === "details") { section.open = true; }
        section.scrollIntoView({ behavior: "smooth", block: "start" });
        window.setTimeout(() => { section.focus({ preventScroll: true }); }, 150);
    }

    function getHashParameters() {
        return new URLSearchParams(window.location.hash.replace(/^#/, ""));
    }

    function getHashActiveTab() {
        const tabId = getHashParameters().get("adminTab");
        return isKnownTab(tabId) ? tabId : Tabs.overview;
    }

    function getHashCmsSubTab() {
        const tabId = getHashParameters().get("cmsSubTab");
        return isKnownCmsSubTab(tabId) ? tabId : CmsSubTabs.overview;
    }

    function getHashValue(key) {
        return getHashParameters().get(key) || "";
    }

    function getCurrentActiveTab() {
        return tabButtons.find((button) => button.getAttribute("aria-selected") === "true")?.dataset.tabId || Tabs.overview;
    }

    function updateAdminHash(tabId, cmsSubTabId, changes = {}) {
        const selectedTabId = isKnownTab(tabId) ? tabId : Tabs.overview;
        const selectedCmsSubTabId = isKnownCmsSubTab(cmsSubTabId) ? cmsSubTabId : getHashCmsSubTab();
        const existing = getHashParameters();
        const parameters = new URLSearchParams();
        parameters.set("adminTab", selectedTabId);
        if (selectedTabId === Tabs.cmsContent || selectedCmsSubTabId !== CmsSubTabs.overview) { parameters.set("cmsSubTab", selectedCmsSubTabId); }
        ["selectedUserId", "contentPackSlug", "topicKey", "scenarioKey", "promptTemplateKey", "tutorId"].forEach((key) => {
            const value = Object.prototype.hasOwnProperty.call(changes, key) ? changes[key] : existing.get(key);
            if (value) { parameters.set(key, value); }
        });
        const nextUrl = `${window.location.pathname}${window.location.search}#${parameters.toString()}`;
        window.history.replaceState(null, "", nextUrl);
    }

    function updateHashField(key, value) {
        updateAdminHash(getCurrentActiveTab(), getHashCmsSubTab(), { [key]: value || null });
    }

    function clearAdminHash() {
        window.history.replaceState(null, "", `${window.location.pathname}${window.location.search}`);
    }

    function isKnownTab(tabId) { return Object.values(Tabs).includes(tabId); }
    function isKnownCmsSubTab(tabId) { return Object.values(CmsSubTabs).includes(tabId); }

    function getAdminHeaders(extraHeaders = {}) {
        const headers = Object.assign({}, extraHeaders);
        if (accessToken) { headers.Authorization = `Bearer ${accessToken}`; }
        return headers;
    }

    function hasAdminPermission(permissionId) {
        return adminAccessSnapshot.permissions.includes(permissionId);
    }

    function hasAnyAdminPermission(permissionIds = []) {
        return permissionIds.length === 0 || permissionIds.some(hasAdminPermission);
    }

    function hasAllAdminPermissions(permissionIds = []) {
        return permissionIds.every(hasAdminPermission);
    }

    function canAccessTab(tabId) {
        const definition = TabPermissionDefinitions[tabId];
        if (!definition) { return false; }
        if (definition.bootstrapAdminOnly && !adminAccessSnapshot.isBootstrapAdmin) { return false; }
        return hasAnyAdminPermission(definition.anyPermissions || []) && hasAllAdminPermissions(definition.allPermissions || []);
    }

    function assertCanAccessTab(tabId) {
        if (canAccessTab(tabId)) { return true; }
        setError(NotAvailableForRoleMessage);
        return false;
    }

    const UnsavedChangesMessage = "You have unsaved changes. Save draft before leaving, or discard changes.";
    const cmsScenarioStructuredInputs = [
        cmsScenarioTitleInput, cmsScenarioDescriptionInput, cmsScenarioSetupMessageInput, cmsScenarioIsActiveInput,
        cmsScenarioFirstBotMessageLinesInput, cmsScenarioContextOptionLinesInput, cmsScenarioValidContextKeywordsLinesInput, cmsScenarioCustomContextRulesLinesInput,
        cmsScenarioInvalidContextRedirectInput, cmsScenarioGoalTextInput, cmsScenarioCanDoLinesInput, cmsScenarioOpeningTextInput,
        cmsScenarioFirstUserTaskInput, cmsScenarioGuidedFollowUpLinesInput, cmsScenarioAiInstructionLinesInput,
        cmsScenarioWrapUpMessageInput, cmsScenarioFinalMessageInput, cmsScenarioHintExampleInput
    ].filter(Boolean);
    const cmsDirtyBaselines = { topic: null, scenario: null, level: null, promptTemplate: null, tutorProfile: null };
    const cmsDirtyState = { topic: false, scenario: false, level: false, promptTemplate: false, tutorProfile: false };

    function getCmsDraftSnapshot(editorKey) {
        if (editorKey === "topic") { return { title: cmsTopicTitleInput.value, description: cmsTopicDescriptionInput.value, sortOrder: cmsTopicSortOrderInput.value, isActive: cmsTopicIsActiveInput.checked }; }
        if (editorKey === "scenario") { return { title: cmsScenarioTitleInput.value, description: cmsScenarioDescriptionInput.value, setupMessage: cmsScenarioSetupMessageInput.value, structuredScenarioFields: getCmsStructuredScenarioSnapshot(), definitionJson: cmsScenarioDefinitionJsonInput.value, isActive: cmsScenarioIsActiveInput.checked }; }
        if (editorKey === "promptTemplate") { return { body: cmsPromptTemplateBodyInput.value, isActive: cmsPromptTemplateIsActiveInput.checked }; }
        if (editorKey === "level") { return { displayName: cmsLevelDisplayNameInput.value, sortOrder: cmsLevelSortOrderInput.value, wrapUpAfterUserTurn: cmsLevelWrapUpTurnInput.value, finalMessageAtUserTurn: cmsLevelFinalTurnInput.value, botLanguageComplexityGuidance: cmsLevelComplexityGuidanceInput.value, correctionGuidance: cmsLevelCorrectionGuidanceInput.value, answerLengthGuidance: cmsLevelAnswerGuidanceInput.value, adminNotes: cmsLevelAdminNotesInput.value, isActive: cmsLevelIsActiveInput.checked }; }
        if (editorKey === "tutorProfile") { return { displayName: cmsTutorProfileDisplayNameInput.value, communicationStyleJson: cmsTutorProfileCommunicationStyleJsonInput.value, safetyNotesJson: cmsTutorProfileSafetyNotesJsonInput.value, isActive: cmsTutorProfileIsActiveInput.checked }; }
        return null;
    }

    function snapshotsMatch(left, right) { return JSON.stringify(left || null) === JSON.stringify(right || null); }
    function getCmsDirtyStatusElement(editorKey) {
        if (editorKey === "topic") { return cmsTopicDirtyStatusElement; }
        if (editorKey === "scenario") { return cmsScenarioDirtyStatusElement; }
        if (editorKey === "promptTemplate") { return cmsPromptTemplateDirtyStatusElement; }
        if (editorKey === "level") { return cmsLevelDirtyStatusElement; }
        if (editorKey === "tutorProfile") { return cmsTutorProfileDirtyStatusElement; }
        return null;
    }
    function setCmsBaseline(editorKey) { cmsDirtyBaselines[editorKey] = getCmsDraftSnapshot(editorKey); updateCmsDirtyState(editorKey); }
    function clearCmsBaseline(editorKey) { cmsDirtyBaselines[editorKey] = null; updateCmsDirtyState(editorKey); }
    function updateCmsDirtyState(editorKey) {
        const baseline = cmsDirtyBaselines[editorKey];
        const isDirty = baseline !== null && !snapshotsMatch(baseline, getCmsDraftSnapshot(editorKey));
        cmsDirtyState[editorKey] = isDirty;
        const statusElement = getCmsDirtyStatusElement(editorKey);
        if (statusElement) { statusElement.classList.toggle("hidden", !isDirty); statusElement.textContent = isDirty ? "Unsaved changes" : ""; }
        return isDirty;
    }
    function updateAllCmsDirtyState() { Object.keys(cmsDirtyState).forEach(updateCmsDirtyState); }
    function hasUnsavedChanges() { updateAllCmsDirtyState(); return Object.values(cmsDirtyState).some(Boolean); }
    function confirmDiscardUnsavedChanges() { return !hasUnsavedChanges() || window.confirm(UnsavedChangesMessage); }
    function clearAllCmsDirtyState() { Object.keys(cmsDirtyState).forEach(clearCmsBaseline); }

    function activateTab(tabId) {
        const requestedTabId = isKnownTab(tabId) ? tabId : Tabs.overview;
        const selectedTabId = canAccessTab(requestedTabId) ? requestedTabId : Tabs.overview;
        updateAdminHash(selectedTabId, getHashCmsSubTab());
        tabButtons.forEach((button) => {
            const isActive = button.dataset.tabId === selectedTabId;
            button.classList.toggle("active", isActive);
            button.setAttribute("aria-selected", isActive ? "true" : "false");
        });
        tabPanels.forEach((panel) => {
            const panelTabId = panel.id.replace("tab-panel-", "");
            const isActive = panelTabId === selectedTabId;
            panel.classList.toggle("hidden", !isActive);
            panel.setAttribute("aria-hidden", isActive ? "false" : "true");
        });
    }

    function initializeTabs() {
        if (tabsInitialized) { return; }
        tabsInitialized = true;
        tabButtons.forEach((button) => button.addEventListener("click", async () => {
            const tabId = button.dataset.tabId || Tabs.overview;
            if (!assertCanAccessTab(tabId)) { return; }
            if (tabId !== getCurrentActiveTab() && !confirmDiscardUnsavedChanges()) { return; }
            activateTab(tabId);
            if (tabId === Tabs.cmsContent) {
                selectCmsSubTab(getHashCmsSubTab());
                if (!cmsHasLoadedOnce) { await loadCmsContentPacks(); }
            }
            if (tabId === Tabs.website && !websiteHasLoadedOnce) { await loadWebsiteContent(); }
            if (tabId === Tabs.system && !aiModelsHaveLoadedOnce) { await loadAiModelSettings(); }
            if (tabId === Tabs.overview) { await loadProductStatistics(); }
            if (tabId === Tabs.feedbackReports) { await loadFeedbackReports(); }
        }));
    }

    function updateSelectedUserHeader() {
        selectedUserSummaryElement.textContent = selectedUserEmail ? `Selected user: ${selectedUserEmail}` : "Selected user: -";
    }

    function updateUserRequiredEmptyStates() {
        const hasUser = Boolean(selectedUserId);
        premiumEmptyStateElement.classList.toggle("hidden", hasUser);
        premiumContentElement.classList.toggle("hidden", !hasUser);
        freeLessonEmptyStateElement.classList.toggle("hidden", hasUser);
        auditEmptyStateElement.classList.toggle("hidden", hasUser);
    }

    const setDashboardVisible = (isVisible) => { dashboard.classList.toggle("hidden", !isVisible); loginCard.classList.toggle("hidden", isVisible); };
    const setError = (message) => { loginError.textContent = message; };
    const setLookupError = (message) => { setLookupSourceError(LookupSources.userLookup, message); };
    const setAuditError = (message) => { auditErrorElement.textContent = message || ""; };
    const setActivityError = (message) => { activityErrorElement.textContent = message || ""; };
    const setGrantError = (message) => { grantErrorElement.textContent = message || ""; };
    const setGrantSuccess = (message) => { grantSuccessElement.textContent = message || ""; };


    function getTodayUtcDateString() { return new Date().toISOString().slice(0, 10); }
    function setFreeLessonResetError(message) { freeLessonResetErrorElement.textContent = message || ""; }
    function setFreeLessonResetSuccess(message) { freeLessonResetSuccessElement.textContent = message || ""; }
    function clearFreeLessonResetMessages() { setFreeLessonResetError(""); setFreeLessonResetSuccess(""); }
    function updateFreeLessonResetControlsState(isLoading) {
        const shouldDisable = isLoading || !selectedUserId;
        freeLessonResetUsageDateInput.disabled = shouldDisable;
        freeLessonResetReasonInput.disabled = shouldDisable;
        freeLessonResetButton.disabled = shouldDisable;
    }
    function setFreeLessonResetLoading(isLoading) {
        freeLessonResetLoadingElement.classList.toggle("hidden", !isLoading);
        updateFreeLessonResetControlsState(isLoading);
    }
    function renderFreeLessonResetSnapshot(payload) {
        const status = payload && payload.subscriptionStatus ? payload.subscriptionStatus : null;
        freeLessonResetUsedTodayElement.textContent = formatValue(status ? status.freeLessonUsedToday : null);
        freeLessonResetRemainingTodayElement.textContent = formatValue(status ? status.freeLessonRemainingToday : null);
        freeLessonResetEnforcementEnabledElement.textContent = formatValue(status ? status.enforcementEnabled : null);
        freeLessonResetCheckedAtUtcElement.textContent = formatValue(payload?.checkedAtUtc || status?.checkedAtUtc || null);
    }
    function clearFreeLessonResetState() {
        freeLessonResetSelectedUserEmailElement.textContent = "-";
        freeLessonResetSelectedUserIdElement.textContent = "-";
        renderFreeLessonResetSnapshot(null);
        freeLessonResetReasonInput.value = "";
        freeLessonResetUsageDateInput.value = getTodayUtcDateString();
        clearFreeLessonResetMessages();
        setFreeLessonResetLoading(false);
    }
    function setFreeLessonResetVisible(isVisible) {
        freeLessonResetCard.classList.toggle("hidden", !isVisible);
        freeLessonResetSelectedUserEmailElement.textContent = isVisible ? (selectedUserEmail || "-") : "-";
        freeLessonResetSelectedUserIdElement.textContent = isVisible ? (selectedUserId || "-") : "-";
        if (!isVisible) { clearFreeLessonResetState(); }
        else {
            renderFreeLessonResetSnapshot(selectedUserLookupPayload || null);
            if (!String(freeLessonResetUsageDateInput.value || "").trim()) { freeLessonResetUsageDateInput.value = getTodayUtcDateString(); }
            updateFreeLessonResetControlsState(false);
        }
    }
    function validateFreeLessonResetInput() {
        const usageDate = String(freeLessonResetUsageDateInput.value || "").trim();
        const reason = String(freeLessonResetReasonInput.value || "").trim();
        if (!usageDate || !/^\d{4}-\d{2}-\d{2}$/.test(usageDate) || !reason) { return { isValid: false, message: ErrorMessages.resetInvalid }; }
        return { isValid: true, usageDate, reason };
    }

    function updateGrantControlsState(isLoading) {
        const shouldDisable = isLoading || !selectedUserId;
        grantButton.disabled = shouldDisable;
        grantDurationDaysInput.disabled = shouldDisable;
        grantReasonInput.disabled = shouldDisable;
    }

    function setGrantVisible(isVisible) {
        grantCard.classList.toggle("hidden", !isVisible);
        grantSelectedUserEmailElement.textContent = isVisible ? (selectedUserEmail || "-") : "-";
        grantSelectedUserIdElement.textContent = isVisible ? (selectedUserId || "-") : "-";
        updateGrantControlsState(false);
    }


    function clearBillingCancelRenewalMessages() { billingCancelRenewalErrorElement.textContent = ""; billingCancelRenewalSuccessElement.textContent = ""; }
    function clearBillingCancelRenewalState() { billingCancelRenewalReasonInput.value = ""; clearBillingCancelRenewalMessages(); billingCancelRenewalAvailabilityElement.textContent = "Search for a user first."; }
    function getBillingCancelRenewalAvailability(payload) {
        const status = payload && payload.subscriptionStatus ? payload.subscriptionStatus : {};
        if (status.canRequestCancelRenewal === true) { return { available: true, message: "Cancellation is available for this user's paid provider subscription." }; }
        const code = status.cancellationExplanationCode || "unknown";
        const renewal = status.renewalStatus || "unknown";
        if (renewal === "cancellation_scheduled" || code === "already_scheduled") { return { available: false, message: "Cancellation is already scheduled." }; }
        if (renewal === "subscription_canceled") { return { available: false, message: "Subscription is canceled." }; }
        if (renewal === "no_paid_subscription" || code === "not_paid_provider_subscription") { return { available: false, message: "No paid provider subscription is available to cancel." }; }
        return { available: false, message: "Cancellation availability is unknown." };
    }
    function updateBillingCancelRenewalControlsState(isLoading) {
        const availability = getBillingCancelRenewalAvailability(selectedUserLookupPayload);
        billingCancelRenewalReasonInput.disabled = isLoading || !selectedUserId || !availability.available;
        billingCancelRenewalButton.disabled = isLoading || !selectedUserId || !availability.available;
    }
    function setBillingCancelRenewalLoading(isLoading) { billingCancelRenewalLoadingElement.classList.toggle("hidden", !isLoading); updateBillingCancelRenewalControlsState(isLoading); }
    function setBillingCancelRenewalVisible(isVisible) {
        billingCancelRenewalCard.classList.toggle("hidden", !isVisible);
        billingCancelRenewalSelectedUserEmailElement.textContent = isVisible ? (selectedUserEmail || "-") : "-";
        billingCancelRenewalSelectedUserIdElement.textContent = isVisible ? (selectedUserId || "-") : "-";
        const availability = getBillingCancelRenewalAvailability(selectedUserLookupPayload);
        billingCancelRenewalAvailabilityElement.textContent = availability.message;
        if (!isVisible) { clearBillingCancelRenewalState(); }
        updateBillingCancelRenewalControlsState(false);
    }
    function validateBillingCancelRenewalInput() {
        const reason = String(billingCancelRenewalReasonInput.value || "").trim();
        if (!reason) { return { isValid: false, message: ErrorMessages.billingCancelInvalid }; }
        return { isValid: true, reason };
    }


    const setRevokeError = (message) => { revokeErrorElement.textContent = message || ""; };
    const setRevokeSuccess = (message) => { revokeSuccessElement.textContent = message || ""; };

    function getRevokablePremiumEntitlements(payload) {
        const schedule = payload && Array.isArray(payload.premiumEntitlementSchedule) ? payload.premiumEntitlementSchedule : [];
        return schedule.filter((entry) => entry
            && entry.planId === "premium"
            && entry.entitlementType === "premium_access"
            && entry.status === "active");
    }

    function updateRevokeControlsState(isLoading) {
        const hasUser = Boolean(selectedUserId);
        const hasEntitlements = revokeEntitlementIdElement.options.length > 0 && revokeEntitlementIdElement.value;
        const shouldDisable = isLoading || !hasUser || !hasEntitlements;
        revokeEntitlementIdElement.disabled = isLoading || !hasUser || !hasEntitlements;
        revokeReasonInput.disabled = shouldDisable;
        revokeButton.disabled = shouldDisable;
    }

    function renderSelectedRevokeEntitlementDetails() {
        const entitlements = getRevokablePremiumEntitlements(selectedUserLookupPayload || {});
        const selected = entitlements.find((item) => String(item.entitlementId || "") === revokeEntitlementIdElement.value);
        if (!selected) { revokeEntitlementPreviewElement.value = ErrorMessages.revokeNoEntitlements; return; }
        revokeEntitlementPreviewElement.value = [
            `Entitlement ID: ${formatValue(selected.entitlementId)}`,
            `Plan: ${formatValue(selected.planId)}`,
            `Type: ${formatValue(selected.entitlementType)}`,
            `Source: ${formatValue(selected.source)}`,
            `Status: ${formatValue(selected.status)}`,
            `Starts at (UTC): ${formatValue(selected.startsAtUtc)}`,
            `Expires at (UTC): ${formatValue(selected.expiresAtUtc)}`,
            `Reason: ${formatValue(selected.reason)}`
        ].join("\n");
    }

    function renderRevokeEntitlementOptions(payload) {
        const entitlements = getRevokablePremiumEntitlements(payload);
        revokeEntitlementIdElement.textContent = "";
        if (entitlements.length === 0) {
            revokeEntitlementPreviewElement.value = ErrorMessages.revokeNoEntitlements;
            updateRevokeControlsState(false);
            return;
        }

        entitlements.forEach((entry) => {
            const option = document.createElement("option");
            const fallback = String(entry.entitlementId || "-");
            const reasonOrId = String(entry.reason || "").trim() || fallback;
            option.value = fallback;
            option.textContent = `${formatValue(entry.source)} | ${formatValue(entry.startsAtUtc)} → ${formatValue(entry.expiresAtUtc)} | ${reasonOrId}`;
            revokeEntitlementIdElement.appendChild(option);
        });

        revokeEntitlementIdElement.selectedIndex = 0;
        renderSelectedRevokeEntitlementDetails();
        updateRevokeControlsState(false);
    }

    function clearRevokeState() {
        setRevokeError("");
        setRevokeSuccess("");
        revokeReasonInput.value = "";
        setRevokeLoading(false);
    }

    function clearRevokeMessages() {
        setRevokeError("");
        setRevokeSuccess("");
    }

    function setRevokeVisible(isVisible) {
        revokeCard.classList.toggle("hidden", !isVisible);
        revokeSelectedUserEmailElement.textContent = isVisible ? (selectedUserEmail || "-") : "-";
        revokeSelectedUserIdElement.textContent = isVisible ? (selectedUserId || "-") : "-";
        if (!isVisible) {
            revokeEntitlementIdElement.textContent = "";
            revokeEntitlementPreviewElement.value = "";
            clearRevokeState();
        }
        else {
            renderRevokeEntitlementOptions(selectedUserLookupPayload || {});
        }
    }

    function setRevokeLoading(isLoading) {
        revokeLoadingElement.classList.toggle("hidden", !isLoading);
        updateRevokeControlsState(isLoading);
    }

    function validateRevokeInput() {
        const entitlementId = String(revokeEntitlementIdElement.value || "").trim();
        const reason = String(revokeReasonInput.value || "").trim();
        if (!entitlementId || !reason) { return { isValid: false, message: ErrorMessages.revokeInvalid }; }
        return { isValid: true, entitlementId, reason };
    }

    function getLookupElements(source) {
        if (source === LookupSources.premium) { return { emailInput: premiumLookupEmailInput, loadingElement: premiumLookupLoadingElement, errorElement: premiumLookupErrorElement, submitButton: premiumSearchUserButton }; }
        if (source === LookupSources.freeLesson) { return { emailInput: freeLessonLookupEmailInput, loadingElement: freeLessonLookupLoadingElement, errorElement: freeLessonLookupErrorElement, submitButton: freeLessonSearchUserButton }; }
        return { emailInput: lookupEmailInput, loadingElement: lookupLoadingElement, errorElement: lookupErrorElement, submitButton: searchUserButton };
    }

    function setLookupSourceError(source, message) {
        const elements = getLookupElements(source);
        if (elements.errorElement) { elements.errorElement.textContent = message || ""; }
    }

    function clearLookupErrors() {
        Object.values(LookupSources).forEach((source) => setLookupSourceError(source, ""));
    }

    function setLookupSourceLoading(source, isLoading) {
        const elements = getLookupElements(source);
        if (elements.loadingElement) { elements.loadingElement.classList.toggle("hidden", !isLoading); }
        if (elements.submitButton) { elements.submitButton.disabled = isLoading; }
    }

    function setLookupLoading(isLoading) { setLookupSourceLoading(LookupSources.userLookup, isLoading); }

    function setGrantLoading(isLoading) {
        grantLoadingElement.classList.toggle("hidden", !isLoading);
        updateGrantControlsState(isLoading);
    }

    function setAuditLoading(isLoading) {
        auditLoadingElement.classList.toggle("hidden", !isLoading);
        loadAuditButton.disabled = isLoading || !selectedUserId;
        auditLimitElement.disabled = isLoading || !selectedUserId;
    }

    const clearUserLookupResult = () => { lookupResultElement.textContent = ""; };

    function clearGrantState() {
        setGrantError("");
        setGrantSuccess("");
        setGrantLoading(false);
    }

    function clearAuditLog() {
        auditCardElement.classList.toggle("hidden", !selectedUserId);
        auditSelectedUserIdElement.textContent = selectedUserId || "-";
        auditResultElement.textContent = "";
        setAuditError("");
        setAuditLoading(false);
    }

    const formatValue = (value) => (value === null || value === undefined || value === "") ? "-" : (typeof value === "boolean" ? (value ? "Yes" : "No") : String(value));
    function renderKeyValueList(container, data, emptyMessage) { container.textContent = ""; if (!data || typeof data !== "object" || Object.keys(data).length === 0) { const p = document.createElement("p"); p.className = "empty-state"; p.textContent = emptyMessage; container.appendChild(p); return; } const list = document.createElement("dl"); list.className = "kv-list"; Object.keys(data).forEach((key) => { const dt = document.createElement("dt"); dt.textContent = key; const dd = document.createElement("dd"); dd.textContent = formatValue(data[key]); list.appendChild(dt); list.appendChild(dd); }); container.appendChild(list); }
    function syncAdminActivityTopScroll(topScroll, tableWrap, topScrollInner) {
        if (!topScroll || !tableWrap || !topScrollInner) { return; }
        const updateTopScrollWidth = () => { topScrollInner.style.width = `${tableWrap.scrollWidth}px`; topScroll.scrollLeft = tableWrap.scrollLeft; };
        let isSyncing = false;
        topScroll.addEventListener("scroll", () => { if (isSyncing) { return; } isSyncing = true; tableWrap.scrollLeft = topScroll.scrollLeft; isSyncing = false; });
        tableWrap.addEventListener("scroll", () => { if (isSyncing) { return; } isSyncing = true; topScroll.scrollLeft = tableWrap.scrollLeft; isSyncing = false; });
        updateTopScrollWidth();
        window.requestAnimationFrame(updateTopScrollWidth);
        if (typeof ResizeObserver === "function") {
            const resizeObserver = new ResizeObserver(updateTopScrollWidth);
            resizeObserver.observe(tableWrap);
            if (tableWrap.firstElementChild) { resizeObserver.observe(tableWrap.firstElementChild); }
        }
    }

    function renderTable(container, items, columns, emptyMessage, options = {}) { container.textContent = ""; if (!Array.isArray(items) || items.length === 0) { const p = document.createElement("p"); p.className = "empty-state"; p.textContent = emptyMessage; container.appendChild(p); return; } const normalizeColumn = (column) => typeof column === "string" ? { key: column, label: column, className: "" } : column; const columnDefs = columns.map(normalizeColumn); const wrap = document.createElement("div"); wrap.className = options.wrapClassName || "table-wrap"; const table = document.createElement("table"); table.className = "compact-table"; const thead = document.createElement("thead"); const hr = document.createElement("tr"); columnDefs.forEach((column) => { const th = document.createElement("th"); th.scope = "col"; th.textContent = column.label || column.key; if (column.className) { th.className = column.className; } hr.appendChild(th); }); thead.appendChild(hr); table.appendChild(thead); const tbody = document.createElement("tbody"); items.forEach((item) => { const row = document.createElement("tr"); columnDefs.forEach((column) => { const td = document.createElement("td"); td.textContent = formatValue(item ? item[column.key] : null); if (column.className) { td.className = column.className; } row.appendChild(td); }); tbody.appendChild(row); }); table.appendChild(tbody); let topScroll = null; let topScrollInner = null; if (options.topScroll) { topScroll = document.createElement("div"); topScroll.className = "admin-activity-top-scroll"; topScroll.setAttribute("aria-hidden", "true"); topScrollInner = document.createElement("div"); topScrollInner.className = "admin-activity-top-scroll-inner"; topScroll.appendChild(topScrollInner); container.appendChild(topScroll); } wrap.appendChild(table); container.appendChild(wrap); if (options.topScroll) { syncAdminActivityTopScroll(topScroll, wrap, topScrollInner); } }

    const createSection = (title) => { const s = document.createElement("section"); s.className = "lookup-section"; const h = document.createElement("h3"); h.textContent = title; s.appendChild(h); return s; };
    const pickFields = (source, fields) => { const result = {}; fields.forEach((field) => { result[field] = source && typeof source === "object" ? source[field] : null; }); return result; };

    function renderUserLookupResult(payload) {
        clearUserLookupResult();
        const userSection = createSection("User Summary"); const userContainer = document.createElement("div"); renderKeyValueList(userContainer, pickFields(payload.user, SummaryFields), "No user data."); userSection.appendChild(userContainer); lookupResultElement.appendChild(userSection);
        const subscriptionSection = createSection("Subscription Status"); const subscriptionContainer = document.createElement("div"); const subscription = Object.assign({}, pickFields(payload.subscriptionStatus, SubscriptionFields), { checkedAtUtc: payload.checkedAtUtc || payload.subscriptionStatus?.checkedAtUtc || null }); renderKeyValueList(subscriptionContainer, subscription, "No subscription status data."); subscriptionSection.appendChild(subscriptionContainer); lookupResultElement.appendChild(subscriptionSection);
        const profileSection = createSection("Profile"); const profileContainer = document.createElement("div"); renderKeyValueList(profileContainer, payload.profile, "No profile data."); profileSection.appendChild(profileContainer); lookupResultElement.appendChild(profileSection);
        const settingsSection = createSection("Settings"); const settingsContainer = document.createElement("div"); renderKeyValueList(settingsContainer, payload.settings, "No settings data."); settingsSection.appendChild(settingsContainer); lookupResultElement.appendChild(settingsSection);
        renderTable(premiumScheduleResultElement, payload.premiumEntitlementSchedule, EntitlementColumns, "No current or scheduled Premium entitlements.");
        renderTable(activeEntitlementsResultElement, payload.activeEntitlements, EntitlementColumns, "No active entitlements.");
        const lessonsSection = createSection("Recent Lesson Sessions"); const lessonsContainer = document.createElement("div"); renderTable(lessonsContainer, payload.recentLessonSessions, LessonSessionColumns, "No recent lesson sessions."); lessonsSection.appendChild(lessonsContainer); lookupResultElement.appendChild(lessonsSection);
        const countersSection = createSection("Daily Usage Counters"); const countersContainer = document.createElement("div"); renderTable(countersContainer, payload.dailyUsageCounters, DailyUsageColumns, "No daily usage counters."); countersSection.appendChild(countersContainer); lookupResultElement.appendChild(countersSection);
        const eventsSection = createSection("Recent Usage Events"); const eventsContainer = document.createElement("div"); renderTable(eventsContainer, payload.recentUsageEvents, UsageEventColumns, "No recent usage events."); eventsSection.appendChild(eventsContainer); lookupResultElement.appendChild(eventsSection);
    }

    const renderAuditLog = (payload) => renderTable(auditResultElement, payload && Array.isArray(payload.items) ? payload.items : [], AuditColumns, "No audit actions.");
    const renderAdminActivity = (payload) => renderTable(activityResultElement, payload && Array.isArray(payload.items) ? payload.items : [], ActivityColumns, "No Admin Activity events.", ActivityTableOptions);
    const getSelectedAuditLimit = () => [10, 25, 50, 100].includes(Number.parseInt(auditLimitElement.value, 10)) ? Number.parseInt(auditLimitElement.value, 10) : 10;




    const aiModelsFieldsElement = document.getElementById("ai-models-fields");
    const aiModelsMessageElement = document.getElementById("ai-models-message");
    const aiModelsErrorElement = document.getElementById("ai-models-error");
    const aiModelsLoadButton = document.getElementById("ai-models-load-button");
    const aiModelsSaveDraftButton = document.getElementById("ai-models-save-draft-button");
    const aiModelsValidateButton = document.getElementById("ai-models-validate-button");
    const aiModelsProviderTestButton = document.getElementById("ai-models-provider-test-button");
    const aiModelsProviderTestResultsElement = document.getElementById("ai-models-provider-test-results");
    const aiModelsResetDraftButton = document.getElementById("ai-models-reset-draft-button");
    const aiModelsPublishButton = document.getElementById("ai-models-publish-button");
    const aiModelFields = [
        ["lessonTutorChatModel", "Lesson tutor chat model"],
        ["feedbackCorrectionModel", "Feedback / correction model"],
        ["lessonHintModel", "Lesson hint model"],
        ["translationModel", "Translation model"],
        ["speechToTextModel", "Speech-to-text model"],
        ["lessonChatTextToSpeechModel", "Lesson chat text-to-speech model"],
        ["conversationModeTextToSpeechModel", "Conversation Mode text-to-speech model"],
        ["realtimeVoiceModel", "Realtime voice model"]
    ];
    let aiModelDraft = {};
    function setAiModelsMessage(message) { if (aiModelsMessageElement) { aiModelsMessageElement.textContent = message || ""; } }
    function setAiModelsError(message) { if (aiModelsErrorElement) { aiModelsErrorElement.textContent = message || ""; } }
    function renderAiModelFields() {
        if (!aiModelsFieldsElement) { return; }
        aiModelsFieldsElement.textContent = "";
        aiModelFields.forEach(([key, label]) => {
            const wrapper = document.createElement("div");
            wrapper.className = "field";
            const labelElement = document.createElement("label");
            labelElement.setAttribute("for", `ai-model-${key}`);
            labelElement.textContent = label;
            const input = document.createElement("input");
            input.id = `ai-model-${key}`;
            input.type = "text";
            input.autocomplete = "off";
            input.maxLength = 120;
            input.value = aiModelDraft[key] || "";
            input.dataset.aiModelKey = key;
            const help = document.createElement("p");
            help.className = "help-text";
            help.textContent = "Validate checks format/syntax only: letters, numbers, dot, dash, underscore, and colon. Format validation does not prove provider access.";
            wrapper.append(labelElement, input, help);
            aiModelsFieldsElement.appendChild(wrapper);
        });
    }
    function collectAiModelDraft() {
        if (!aiModelsFieldsElement) { return; }
        aiModelsFieldsElement.querySelectorAll("[data-ai-model-key]").forEach(input => { aiModelDraft[input.dataset.aiModelKey] = input.value; });
    }
    async function readAiModelsResponse(response, fallbackMessage) { if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
        if (response.status === HttpStatus.forbidden) { throw new Error(NotAvailableForRoleMessage); } if (!response.ok) { let detail = fallbackMessage; try { const body = await response.json(); detail = body.error || body.detail || detail; } catch (_) { } throw new Error(detail); } return response.json(); }
    async function loadAiModelSettings() { setAiModelsError(""); setAiModelsMessage("Loading AI model settings..."); try { const response = await fetch(ApiPaths.aiModelSettings, { method: "GET", headers: getAdminHeaders() }); const payload = await readAiModelsResponse(response, "Unable to load AI model settings."); aiModelDraft = payload.draft || payload.active || {}; renderAiModelFields(); aiModelsHaveLoadedOnce = true; setAiModelsMessage("AI model draft loaded."); } catch (error) { setAiModelsMessage(""); setAiModelsError(error instanceof Error ? error.message : "Unable to load AI model settings."); } }
    async function saveAiModelDraft() { collectAiModelDraft(); setAiModelsError(""); setAiModelsMessage("Saving AI model draft..."); try { const response = await fetch(ApiPaths.aiModelSettingsDraft, { method: "POST", headers: getAdminHeaders({ "Content-Type": "application/json" }), body: JSON.stringify(aiModelDraft) }); const payload = await readAiModelsResponse(response, "Unable to save AI model draft."); aiModelDraft = payload.draft || aiModelDraft; renderAiModelFields(); setAiModelsMessage("AI model draft saved."); return true; } catch (error) { setAiModelsMessage(""); setAiModelsError(error instanceof Error ? error.message : "Unable to save AI model draft."); return false; } }
    async function validateAiModelDraft() { collectAiModelDraft(); setAiModelsError(""); setAiModelsMessage("Validating AI model draft format/syntax only..."); try { const response = await fetch(ApiPaths.aiModelSettingsValidate, { method: "POST", headers: getAdminHeaders({ "Content-Type": "application/json" }), body: JSON.stringify(aiModelDraft) }); const payload = await readAiModelsResponse(response, "Unable to validate AI model draft."); if (!payload.isValid) { setAiModelsMessage(""); setAiModelsError((payload.errors || []).join(" ") || "AI model draft is invalid."); return false; } setAiModelsMessage((payload.warnings || []).join(" ") || "AI model draft format/syntax is valid. Format validation does not prove provider access. Use Test provider access before publishing a new model."); return true; } catch (error) { setAiModelsMessage(""); setAiModelsError(error instanceof Error ? error.message : "Unable to validate AI model draft."); return false; } }
    async function testAiModelProviderAccess() { collectAiModelDraft(); setAiModelsError(""); setAiModelsMessage("Testing AI model provider access for the current draft without publishing..."); if (aiModelsProviderTestResultsElement) { aiModelsProviderTestResultsElement.textContent = ""; } try { const response = await fetch(ApiPaths.aiModelSettingsProviderTest, { method: "POST", headers: getAdminHeaders({ "Content-Type": "application/json" }), body: JSON.stringify(aiModelDraft) }); const payload = await readAiModelsResponse(response, "Unable to test AI model provider access."); if (aiModelsProviderTestResultsElement) { aiModelsProviderTestResultsElement.textContent = JSON.stringify(payload, null, 2); } setAiModelsMessage(`Provider access test ${payload.overallStatus || "completed"}. Draft was not published.`); return payload.overallStatus === "success" || payload.overallStatus === "partial"; } catch (error) { setAiModelsMessage(""); setAiModelsError(error instanceof Error ? error.message : "Unable to test AI model provider access."); return false; } }
    async function publishAiModelDraft() { const saved = await saveAiModelDraft(); if (!saved) { return; } setAiModelsMessage("Publishing AI model settings..."); try { const response = await fetch(ApiPaths.aiModelSettingsPublish, { method: "POST", headers: getAdminHeaders({ "Content-Type": "application/json" }) }); const payload = await readAiModelsResponse(response, "Unable to publish AI model settings."); aiModelDraft = payload.draft || payload.active || aiModelDraft; renderAiModelFields(); setAiModelsMessage("AI model settings published. Run a new lesson smoke test."); } catch (error) { setAiModelsMessage(""); setAiModelsError(error instanceof Error ? error.message : "Unable to publish AI model settings."); } }
    async function resetAiModelDraft() { setAiModelsError(""); setAiModelsMessage("Resetting AI model draft from active..."); try { const response = await fetch(ApiPaths.aiModelSettingsResetDraft, { method: "POST", headers: getAdminHeaders({ "Content-Type": "application/json" }) }); const payload = await readAiModelsResponse(response, "Unable to reset AI model draft."); aiModelDraft = payload.draft || payload.active || {}; renderAiModelFields(); setAiModelsMessage("AI model draft reset from active."); } catch (error) { setAiModelsMessage(""); setAiModelsError(error instanceof Error ? error.message : "Unable to reset AI model draft."); } }

    const websiteSectionTabs = document.getElementById("website-section-tabs");
    const websiteEditorHeading = document.getElementById("website-editor-heading");
    const websiteEditorFields = document.getElementById("website-editor-fields");
    const websiteMessageElement = document.getElementById("website-message");
    const websiteErrorElement = document.getElementById("website-error");
    const websiteSaveDraftButton = document.getElementById("website-save-draft-button");
    const websitePublishButton = document.getElementById("website-publish-button");
    const websitePreviewButton = document.getElementById("website-preview-button");
    const simpleWebsitePageKeys = new Set(["download", "mobile", "pricing", "support", "terms", "privacy", "refunds", "cancellation", "seller", "aiData", "status"]);
    const websiteSections = [
        ["home", "Home page", [["logoPath","Logo image path"],["logoAltText","Logo alt text"],["fallbackLogoText","Fallback logo text"],["topHeaderText","Top header text"],["supportedLanguageLine","Supported language line"],["windowsCardBadge","Windows card badge"],["windowsCardTitle","Windows card title"],["windowsCardDescription","Windows card description"],["windowsDownloadButtonText","Windows download button text"],["mobileCardBadge","Mobile card badge"],["mobileCardTitle","Mobile card title"],["mobileCardDescription","Mobile card description"],["mobileComingSoonButtonText","Mobile coming soon button text"],["footerCopyrightText","Footer copyright text"],["footerPrivacyLabel","Footer Privacy label"],["footerTermsLabel","Footer Terms label"],["footerRefundsLabel","Footer Refund label"],["footerCancellationLabel","Footer Cancellation label"],["footerSupportLabel","Footer Support label"],["footerPricingLabel","Footer Pricing label"]]],
        ["download", "Desktop app / Download"],
        ["mobile", "Mobile app / Coming soon"],
        ["pricing", "Pricing"],
        ["support", "Support"],
        ["terms", "Legal - Terms"],
        ["privacy", "Legal - Privacy Policy"],
        ["refunds", "Legal - Refund Policy"],
        ["cancellation", "Legal - Cancellation Policy"],
        ["seller", "Legal - Seller / Company details"],
        ["aiData", "Legal - AI / Data disclosure"],
        ["status", "Legal - Platform availability / service status"],
        ["marketingSeo", "Marketing / SEO", []]
    ].map(([key, label, fields]) => ({ key, label, fields: fields || [["pageTitle", "Page title"], ["bodyMarkdown", "Body markdown"], ["seoTitle", "SEO title"], ["seoDescription", "SEO description"]], simple: simpleWebsitePageKeys.has(key) }));
    const websiteLegacyBodyFields = {
        download: [["introText"], ["currentVersionLabel", "Current version"], ["safetySupportNote", "Safety and support"]],
        mobile: [["introText"], ["androidComingSoonText", "Android"], ["iosComingSoonText", "iOS"], ["emailSupportCtaText", "Contact"]],
        pricing: [["introText"], ["freePlanText", "Free plan"], ["premiumPlanText", "Premium plan"], ["trialText", "Trial"], ["paddleLiveCheckoutDisclaimerText", "Checkout status"]],
        support: [["introText"], ["supportEmailText", "Support email"], ["responseTimeText", "Response time"], ["accountDeletionSupportText", "Accounts and deletion"], ["billingSupportText", "Billing"]],
        terms: [["effectiveDate", "Effective date"], ["intro"], ["accountUseTerms", "Accounts and use"], ["aiLearningDisclaimer", "AI and learning disclaimer"], ["billingSubscriptionTermsPlaceholder", "Billing and subscriptions"], ["contactSupportText", "Contact"]],
        privacy: [["effectiveDate", "Effective date"], ["intro"], ["dataCollected", "Data collected"], ["audioTranscriptionText", "Audio and transcription"], ["aiProcessingText", "AI processing"], ["accountPaymentDataText", "Account and payment data"], ["dataRetentionDeletionText", "Retention and deletion"], ["contactText", "Contact"]],
        refunds: [["effectiveDate", "Effective date"], ["refundEligibilityText", "Refund eligibility"], ["howToRequestRefundText", "How to request a refund"], ["paddlePaymentProviderNote", "Payment provider note"], ["contactText", "Contact"]],
        cancellation: [["effectiveDate", "Effective date"], ["howToCancelText", "How to cancel"], ["accessUntilPeriodEndText", "Access until period end"], ["supportText", "Support"]],
        seller: [["sellerNameLegalEntityPlaceholder", "Seller name / legal entity"], ["addressPlaceholder", "Address"], ["contactEmail", "Contact email"], ["taxVatCompanyRegistrationPlaceholder", "Tax, VAT, company registration"], ["paddleLiveReviewNote", "Paddle live review note"]],
        aiData: [["aiTutorDisclosureText", "AI tutor disclosure"], ["voiceTranscriptionDisclosureText", "Voice and transcription"], ["dataProcessingText", "Data processing"], ["userControlDeletionText", "User control and deletion"]],
        status: [["desktopAvailabilityText", "Desktop availability"], ["mobileComingSoonText", "Mobile"], ["serviceAvailabilityDisclaimer", "Service availability"], ["supportContactText", "Support"]]
    };
    let websiteContentDraft = { pages: {}, design: {} };
    let activeWebsiteSection = "home";
    function setWebsiteMessage(message) { websiteMessageElement.textContent = message || ""; }
    function setWebsiteError(message) { websiteErrorElement.textContent = message || ""; }
    const websiteSectionGroups = [
        { label: "Main", keys: ["home", "download", "mobile"] },
        { label: "Commercial", keys: ["pricing", "support"] },
        { label: "Legal", keys: ["terms", "privacy", "refunds", "cancellation", "seller", "aiData", "status"] },
        { label: "Marketing / SEO", keys: ["marketingSeo"] }
    ];
    function renderWebsiteTabs() {
        websiteSectionTabs.innerHTML = "";
        const sectionsByKey = new Map(websiteSections.map(section => [section.key, section]));
        websiteSectionGroups.forEach(group => {
            const groupElement = document.createElement("section");
            groupElement.className = "website-section-group";
            const groupHeading = document.createElement("h4");
            groupHeading.textContent = group.label;
            groupElement.appendChild(groupHeading);
            const groupButtons = document.createElement("div");
            groupButtons.className = "website-section-group-buttons";
            group.keys.forEach(key => {
                const section = sectionsByKey.get(key);
                if (!section) return;
                const button = document.createElement("button");
                button.type = "button";
                button.textContent = section.label;
                button.className = "website-section-tab";
                button.setAttribute("aria-selected", section.key === activeWebsiteSection ? "true" : "false");
                button.addEventListener("click", () => { collectCurrentWebsiteSection(); activeWebsiteSection = section.key; renderWebsiteEditor(); });
                groupButtons.appendChild(button);
            });
            groupElement.appendChild(groupButtons);
            websiteSectionTabs.appendChild(groupElement);
        });
    }
    function getLegacyWebsiteBodyMarkdown(pageKey, values) {
        return (websiteLegacyBodyFields[pageKey] || []).map(([key, heading]) => {
            const value = (values[key] || "").trim();
            if (!value) return "";
            return heading ? `## ${heading}\n\n${value}` : value;
        }).filter(Boolean).join("\n\n");
    }
    function createWebsiteField(key, label, value, options = {}) {
        const field = document.createElement("div");
        field.className = `field website-field${options.long ? " website-field-long" : " website-field-compact"}`;
        const labelElement = document.createElement("label");
        labelElement.htmlFor = `website-field-${key}`;
        labelElement.textContent = label;
        const input = document.createElement(options.textarea ? "textarea" : "input");
        input.id = `website-field-${key}`;
        input.dataset.websiteKey = key;
        if (options.marketing) input.dataset.websiteMarketingKey = key;
        if (options.textarea) input.rows = options.rows || 4; else input.type = options.checkbox ? "checkbox" : options.number ? "number" : "text";
        if (options.checkbox) input.checked = value === true || String(value || "").toLowerCase() === "true"; else input.value = value ?? "";
        field.append(labelElement, input);
        if (options.help) { const helper = document.createElement("p"); helper.className = "muted website-field-help"; helper.textContent = options.help; field.appendChild(helper); }
        return field;
    }
    const homeTitleStyleDefaults = { FontFamily: "inherit", MobileSizePx: "28", DesktopSizePx: "52", FontWeight: "800", LineHeight: "1.08" };
    const homeTitleFontOptions = [["inherit", "Inherit website heading font"], ["system-ui", "System UI"], ["Arial", "Arial"], ["Georgia", "Georgia"], ["Trebuchet MS", "Trebuchet MS"]];
    const homeTitleWeightOptions = ["400", "500", "600", "700", "800", "900"];
    function createHomeTitleStyleEditor(titleKey, values) {
        const section = document.createElement("section");
        section.className = "home-title-style website-field-long";
        section.dataset.titleStyleFor = titleKey;
        const heading = document.createElement("h4"); heading.textContent = "Text style";
        const helper = document.createElement("p"); helper.className = "muted"; helper.textContent = "Only this title uses these settings. Use the controlled options below; raw CSS is not supported.";
        const grid = document.createElement("div"); grid.className = "home-title-style-grid";
        const addSelect = (suffix, label, options) => { const field = document.createElement("div"); field.className = "field website-field"; const labelElement = document.createElement("label"); const id = "website-field-" + titleKey + suffix; labelElement.htmlFor = id; labelElement.textContent = label; const select = document.createElement("select"); select.id = id; select.dataset.websiteKey = titleKey + suffix; const value = String(values[titleKey + suffix] ?? homeTitleStyleDefaults[suffix]); options.forEach(([optionValue, optionLabel]) => { const option = document.createElement("option"); option.value = optionValue; option.textContent = optionLabel; option.selected = optionValue === value; select.appendChild(option); }); field.append(labelElement, select); grid.appendChild(field); };
        const addNumber = (suffix, label, min, max, step) => { const field = createWebsiteField(titleKey + suffix, label, values[titleKey + suffix] ?? homeTitleStyleDefaults[suffix], { number: true }); const input = field.querySelector("input"); input.min = String(min); input.max = String(max); input.step = String(step); input.inputMode = "decimal"; grid.appendChild(field); };
        addSelect("FontFamily", "Font family", homeTitleFontOptions);
        addNumber("MobileSizePx", "Mobile size (px)", 22, 72, 1);
        addNumber("DesktopSizePx", "Desktop size (px)", 22, 72, 1);
        addSelect("FontWeight", "Font weight", homeTitleWeightOptions.map(value => [value, value]));
        addNumber("LineHeight", "Line height", 0.9, 1.8, 0.01);
        const error = document.createElement("p"); error.className = "error home-title-style-error"; error.hidden = true;
        section.append(heading, helper, grid, error);
        return section;
    }
    function validateHomeTitleStyles() {
        if (activeWebsiteSection !== "home") return true;
        let firstError = "";
        ["windowsCardTitle", "mobileCardTitle"].forEach(titleKey => {
            const section = websiteEditorFields.querySelector(".home-title-style[data-title-style-for=\"" + titleKey + "\"]");
            const error = section?.querySelector(".home-title-style-error");
            const get = suffix => websiteEditorFields.querySelector("[data-website-key=\"" + titleKey + suffix + "\"]");
            const mobile = Number(get("MobileSizePx")?.value), desktop = Number(get("DesktopSizePx")?.value), lineHeight = Number(get("LineHeight")?.value);
            let message = "";
            if (!Number.isFinite(mobile) || mobile < 22 || mobile > 72) message = "Mobile size must be a finite number from 22 to 72 px.";
            else if (!Number.isFinite(desktop) || desktop < 22 || desktop > 72) message = "Desktop size must be a finite number from 22 to 72 px.";
            else if (mobile > desktop) message = "Mobile size must not exceed desktop size.";
            else if (!Number.isFinite(lineHeight) || lineHeight < 0.9 || lineHeight > 1.8) message = "Line height must be a finite number from 0.9 to 1.8.";
            if (error) { error.hidden = !message; error.textContent = message; }
            ["MobileSizePx", "DesktopSizePx", "LineHeight"].forEach(suffix => get(suffix)?.setCustomValidity(message));
            if (message && !firstError) firstError = message;
        });
        if (firstError) { setWebsiteError(firstError); return false; }
        return true;
    }
    function insertMarkdownAtCursor(textarea, before, after = "") {
        const start = textarea.selectionStart || 0;
        const end = textarea.selectionEnd || 0;
        const selected = textarea.value.slice(start, end);
        const insertion = `${before}${selected || "text"}${after}`;
        textarea.setRangeText(insertion, start, end, "end");
        textarea.focus();
        textarea.dispatchEvent(new Event("input", { bubbles: true }));
    }
    function createMarkdownToolbar(textarea) {
        const toolbar = document.createElement("div");
        toolbar.className = "website-markdown-toolbar";
        [["Heading", "# ", ""], ["Subheading", "## ", ""], ["Bold", "**", "**"], ["Italic", "_", "_"], ["Bullet list", "- ", ""], ["Numbered list", "1. ", ""], ["Link", "[", "](https://example.com)"], ["Quote / Note", "> ", ""], ["Horizontal rule", "\n---\n", ""]].forEach(([label, before, after]) => {
            const button = document.createElement("button");
            button.type = "button";
            button.textContent = label;
            button.addEventListener("click", () => label === "Horizontal rule" ? insertMarkdownAtCursor(textarea, before, "") : insertMarkdownAtCursor(textarea, before, after));
            toolbar.appendChild(button);
        });
        return toolbar;
    }
    const downloadFeatureCardFields = [
        ["featureCard1", "Card 1"],
        ["featureCard2", "Card 2"],
        ["featureCard3", "Card 3"],
        ["featureCard4", "Card 4"]
    ];
    const downloadFeatureCardPathHelp = "Public website asset path. Upload screenshots as WebP files to /assets/images/download/. These are public website assets, not release artifacts.";
    const expectedDownloadFeatureCardKeys = [
        "featureCard1Label", "featureCard1Title", "featureCard1Description", "featureCard1ImagePath",
        "featureCard2Label", "featureCard2Title", "featureCard2Description", "featureCard2ImagePath",
        "featureCard3Label", "featureCard3Title", "featureCard3Description", "featureCard3ImagePath",
        "featureCard4Label", "featureCard4Title", "featureCard4Description", "featureCard4ImagePath"
    ];
    const expectedDownloadScreenshotPaths = [
        "/assets/images/download/quick-start.webp",
        "/assets/images/download/topics.webp",
        "/assets/images/download/guided-lesson.webp",
        "/assets/images/download/conversation.webp"
    ];

    const defaultDownloadFeatureCardValues = {
        featureCard1Label: "Quick Start",
        featureCard1Title: "Start quickly",
        featureCard1Description: "Open the app and jump into practical language practice in a few clicks.",
        featureCard1ImagePath: "/assets/images/download/quick-start.webp",
        featureCard2Label: "Topics",
        featureCard2Title: "Choose practical topics",
        featureCard2Description: "Pick real-life situations like travel, work, daily life, and more.",
        featureCard2ImagePath: "/assets/images/download/topics.webp",
        featureCard3Label: "Guided Lesson",
        featureCard3Title: "Learn step by step",
        featureCard3Description: "Practice inside a guided lesson with clear prompts, hints, and feedback.",
        featureCard3ImagePath: "/assets/images/download/guided-lesson.webp",
        featureCard4Label: "Conversation",
        featureCard4Title: "Practice real conversation",
        featureCard4Description: "Switch to conversation mode and train natural speaking in a realistic dialogue.",
        featureCard4ImagePath: "/assets/images/download/conversation.webp"
    };
    function preserveDownloadFeatureCardFields() {
        const pages = (websiteContentDraft.pages ||= {});
        const download = (pages.download ||= {});
        expectedDownloadFeatureCardKeys.forEach(key => {
            if (download[key] === undefined || download[key] === null || String(download[key]).trim() === "") {
                download[key] = defaultDownloadFeatureCardValues[key] || "";
            }
        });
    }
    function renderDownloadFeatureCardEditor(values) {
        const section = document.createElement("section");
        section.className = "website-seo-section website-field-long";
        const heading = document.createElement("h4");
        heading.textContent = "Download feature cards";
        const helper = document.createElement("p");
        helper.className = "muted";
        helper.textContent = `Upload screenshots as WebP files to: /assets/images/download/. Expected public paths: ${expectedDownloadScreenshotPaths.join(", ")}. These are public website assets, not release artifacts. Editable CMS keys: ${expectedDownloadFeatureCardKeys.join(", ")}. Full image upload management is not part of this editor.`;
        const grid = document.createElement("div");
        grid.className = "website-seo-grid";
        downloadFeatureCardFields.forEach(([prefix, label]) => {
            const card = document.createElement("section");
            card.className = "website-field-long";
            const cardHeading = document.createElement("h5");
            cardHeading.textContent = label;
            card.append(
                cardHeading,
                createWebsiteField(`${prefix}Label`, "Card label", values[`${prefix}Label`], { help: prefix === "featureCard1" ? "Example labels: Quick Start, Topics, Guided Lesson, Conversation." : "" }),
                createWebsiteField(`${prefix}Title`, "Card title", values[`${prefix}Title`]),
                createWebsiteField(`${prefix}Description`, "Card description", values[`${prefix}Description`], { textarea: true, rows: 3, long: true }),
                createWebsiteField(`${prefix}ImagePath`, "Image path", values[`${prefix}ImagePath`], { long: true, help: downloadFeatureCardPathHelp })
            );
            grid.appendChild(card);
        });
        section.append(heading, helper, grid);
        websiteEditorFields.appendChild(section);
    }
    function renderSimpleWebsiteEditor(section, values) {
        websiteEditorFields.classList.add("website-simple-editor-fields");
        websiteEditorFields.appendChild(createWebsiteField("pageTitle", "Page title", values.pageTitle, { long: true }));
        const bodyField = createWebsiteField("bodyMarkdown", "Body markdown", values.bodyMarkdown || getLegacyWebsiteBodyMarkdown(section.key, values), { long: true, textarea: true, rows: 18 });
        const bodyTextarea = bodyField.querySelector("textarea");
        bodyField.insertBefore(createMarkdownToolbar(bodyTextarea), bodyTextarea);
        websiteEditorFields.appendChild(bodyField);
        if (section.key === "download") { renderDownloadFeatureCardEditor(values); }
        const seoSection = document.createElement("section");
        seoSection.className = "website-seo-section website-field-long";
        const heading = document.createElement("h4");
        heading.textContent = "SEO";
        const grid = document.createElement("div");
        grid.className = "website-seo-grid";
        grid.append(createWebsiteField("seoTitle", "SEO title", values.seoTitle), createWebsiteField("seoDescription", "SEO description", values.seoDescription, { textarea: true, rows: 3, long: true }));
        seoSection.append(heading, grid);
        websiteEditorFields.appendChild(seoSection);
    }

    const websiteMarketingFields = [
        ["enableConsentBanner", "Enable consent banner", "checkbox", "Shows the optional cookies banner with Accept all, Reject non-essential, and Manage choices."],
        ["enableAnalytics", "Enable analytics", "checkbox", "Loads Google Analytics only when a valid Measurement ID is also set."],
        ["googleAnalyticsMeasurementId", "Google Analytics Measurement ID", "text", "Optional public ID like G-XXXXXXXXXX. Leave empty to omit GA scripts."],
        ["enableAdsTracking", "Enable ads tracking", "checkbox", "Loads Google Ads only when a valid Ads ID is also set."],
        ["googleAdsId", "Google Ads ID", "text", "Optional public ID like AW-123456789. Leave empty to omit Ads scripts."],
        ["googleAdsDownloadConversionLabel", "Google Ads download conversion label", "text", "Optional public conversion label. Download tracking stays consent-gated."],
        ["googleSearchConsoleVerificationToken", "Google Search Console verification token", "text", "Optional public Search Console token for the meta verification tag."],
        ["enableLlmsTxt", "Enable llms.txt", "checkbox", "Publishes llms.txt for AI crawlers when enabled."]
    ];
    function renderMarketingSeoEditor() {
        websiteEditorFields.classList.add("website-simple-editor-fields");
        const section = document.createElement("section");
        section.className = "website-seo-section website-field-long";
        const heading = document.createElement("h4");
        heading.textContent = "Marketing / SEO";
        const helper = document.createElement("p");
        helper.className = "muted";
        helper.textContent = "Optional public marketing settings. Do not enter secrets; leave IDs empty to keep Google scripts out of generated pages.";
        const grid = document.createElement("div");
        grid.className = "website-seo-grid";
        const marketing = websiteContentDraft.marketing || {};
        websiteMarketingFields.forEach(([key, label, type, help]) => grid.appendChild(createWebsiteField(key, label, marketing[key], { marketing: true, checkbox: type === "checkbox", long: type !== "checkbox", help })));
        section.append(heading, helper, grid);
        websiteEditorFields.appendChild(section);
    }
    function renderWebsiteEditor() {
        const section = websiteSections.find(x => x.key === activeWebsiteSection) || websiteSections[0];
        websiteEditorHeading.textContent = section.label;
        websiteEditorFields.innerHTML = "";
        websiteEditorFields.className = "website-editor-fields";
        renderWebsiteTabs();
        if (section.key === "marketingSeo") { renderMarketingSeoEditor(); return; }
        const values = ((websiteContentDraft.pages || {})[section.key] || {});
        if (section.simple) { renderSimpleWebsiteEditor(section, values); return; }
        section.fields.forEach(([key, label]) => {
            const isLong = /description|intro|text|terms|disclaimer|collected|processing|retention|note|placeholder/i.test(key);
            websiteEditorFields.appendChild(createWebsiteField(key, label, values[key], { long: isLong, textarea: isLong, rows: 4, number: /Px|Weight/.test(key) }));
            if (section.key === "home" && (key === "windowsCardTitle" || key === "mobileCardTitle")) {
                websiteEditorFields.appendChild(createHomeTitleStyleEditor(key, values));
            }
        });
    }
    function collectCurrentWebsiteSection() { const section = websiteSections.find(x => x.key === activeWebsiteSection); if (!section) return; if (section.key === "marketingSeo") { const marketing = (websiteContentDraft.marketing ||= {}); websiteEditorFields.querySelectorAll("[data-website-marketing-key]").forEach(input => { const key = input.dataset.websiteMarketingKey; marketing[key] = input.type === "checkbox" ? String(input.checked) : input.value; }); return; } const target = ((websiteContentDraft.pages ||= {})[section.key] ||= {}); websiteEditorFields.querySelectorAll("[data-website-key]").forEach(input => { const key = input.dataset.websiteKey; target[key] = input.value; }); }
    function fillWebsiteForm(content) { websiteContentDraft = JSON.parse(JSON.stringify(content || { pages: {}, design: {}, marketing: {} })); websiteContentDraft.marketing ||= {}; renderWebsiteEditor(); }
    async function readWebsiteResponse(response, fallbackMessage) { if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
        if (response.status === HttpStatus.forbidden) { throw new Error(NotAvailableForRoleMessage); } if (!response.ok) { let detail = fallbackMessage; try { const body = await response.json(); detail = body.error || body.detail || detail; } catch (_) { } throw new Error(detail); } return response.json(); }
    async function loadWebsiteContent() { setWebsiteError(""); setWebsiteMessage("Loading Website editor..."); try { const response = await fetch(ApiPaths.websiteContent, { method: "GET", headers: getAdminHeaders() }); const payload = await readWebsiteResponse(response, "Unable to load Website content."); fillWebsiteForm(payload.draft || payload.active); websiteHasLoadedOnce = true; setWebsiteMessage("Draft loaded."); } catch (error) { setWebsiteMessage(""); setWebsiteError(error instanceof Error ? error.message : "Unable to load Website content."); } }
    async function saveWebsiteDraft() { setWebsiteError(""); if (!validateHomeTitleStyles()) return false; collectCurrentWebsiteSection(); preserveDownloadFeatureCardFields(); setWebsiteMessage("Saving draft..."); websiteSaveDraftButton.disabled = true; try { const response = await fetch(ApiPaths.websiteContentDraft, { method: "POST", headers: getAdminHeaders({ "Content-Type": "application/json" }), body: JSON.stringify(websiteContentDraft) }); const payload = await readWebsiteResponse(response, "Unable to save Website draft."); fillWebsiteForm(payload.draft); setWebsiteMessage("Draft saved."); return true; } catch (error) { setWebsiteMessage(""); setWebsiteError(error instanceof Error ? error.message : "Unable to save Website draft."); return false; } finally { websiteSaveDraftButton.disabled = false; } }
    async function previewWebsiteContent() { setWebsiteError(""); if (!validateHomeTitleStyles()) return; collectCurrentWebsiteSection(); setWebsiteMessage("Rendering Website preview..."); websitePreviewButton.disabled = true; try { const response = await fetch(ApiPaths.websiteContentPreview, { method: "POST", headers: getAdminHeaders({ "Content-Type": "application/json" }), body: JSON.stringify({ content: websiteContentDraft, pageKey: activeWebsiteSection === "marketingSeo" ? "home" : activeWebsiteSection }) }); const payload = await readWebsiteResponse(response, "Unable to preview Website content."); const previewWindow = window.open("about:blank", "_blank"); if (!previewWindow) { throw new Error("Preview popup was blocked. Allow popups for this admin site and try again."); } previewWindow.opener = null; previewWindow.document.open(); previewWindow.document.write(payload.html || ""); previewWindow.document.close(); setWebsiteMessage("Preview opened in a new tab. Nothing was saved or published."); } catch (error) { setWebsiteMessage(""); setWebsiteError(error instanceof Error ? error.message : "Unable to preview Website content."); } finally { websitePreviewButton.disabled = false; } }
    async function publishWebsiteContent() { setWebsiteError(""); if (!validateHomeTitleStyles()) return; collectCurrentWebsiteSection(); preserveDownloadFeatureCardFields(); setWebsiteMessage("Saving draft before publish..."); websitePublishButton.disabled = true; try { const saved = await saveWebsiteDraft(); if (!saved) { return; } setWebsiteMessage("Publishing saved draft to static website..."); const response = await fetch(ApiPaths.websiteContentPublish, { method: "POST", headers: getAdminHeaders({ "Content-Type": "application/json" }) }); const payload = await readWebsiteResponse(response, "Unable to publish Website content."); fillWebsiteForm(payload.active); setWebsiteMessage(`Published saved draft to ${Array.isArray(payload.publishedFiles) ? payload.publishedFiles.length : ""} static website files.`); } catch (error) { setWebsiteMessage(""); setWebsiteError(error instanceof Error ? error.message : "Unable to publish Website content."); } finally { websitePublishButton.disabled = false; } }

    function resetDashboard() {
        adminAccessSnapshot = { roles: [], permissions: [], isBootstrapAdmin: false, productionRolesAvailable: false, adminSource: "", environment: "", checkedAtUtc: "" }; adminSourceElement.textContent = "-"; environmentElement.textContent = "-"; checkedAtElement.textContent = "-"; bootstrapAdminStatusElement.textContent = "-"; adminPermissionCountElement.textContent = "-"; capabilitiesListElement.textContent = ""; renderBadges(adminRolesBadgesElement, []); renderBadges(rolesPermissionsRolesElement, []); renderPermissionList(rolesPermissionsListElement, []); workflowAvailabilityListElement.textContent = ""; systemProductionRolesAvailableElement.textContent = "false"; systemProductionRolesAvailableElement.className = "badge unavailable"; systemBillingPaddleStatusElement.textContent = "not configured"; systemBillingPaddleStatusElement.className = "badge unavailable";
        setLookupError(""); setLookupLoading(false); setLookupSourceLoading(LookupSources.premium, false); setLookupSourceLoading(LookupSources.freeLesson, false); clearLookupErrors(); clearUserLookupResult(); lookupForm.reset(); premiumLookupForm.reset(); freeLessonLookupForm.reset(); clearSelectedUserState();
        setGrantVisible(false); setRevokeVisible(false); setBillingCancelRenewalVisible(false); setFreeLessonResetVisible(false); clearGrantState(); clearRevokeState(); clearBillingCancelRenewalState(); clearFreeLessonResetState(); grantForm.reset(); revokeForm.reset(); billingCancelRenewalForm.reset(); freeLessonResetForm.reset(); clearAuditLog(); clearAllCmsDirtyState();
        clearFeedbackReportsState();
    }

    function resetSession() {
        accessToken = null;
        loginForm.reset();
        setError("");
        resetDashboard();
        setDashboardVisible(false);
    }

    function expireAdminSession(message = ErrorMessages.sessionExpired) {
        const tokenAtExpiry = accessToken;
        accessToken = null;
        fetch(ApiPaths.adminSession, {
            method: "DELETE",
            headers: tokenAtExpiry ? { Authorization: `Bearer ${tokenAtExpiry}` } : {}
        }).catch(() => { });
        resetDashboard();
        setDashboardVisible(false);
        setError(message);
    }

    function handleAuthInvalidResponse() {
        expireAdminSession();
        throw new Error(ErrorMessages.sessionExpired);
    }

    async function fetchUserByEmail(email) {
        const response = await fetch(`${ApiPaths.userLookupByEmail}?email=${encodeURIComponent(email)}`, { method: "GET", headers: getAdminHeaders() });
        return readUserLookupResponse(response, ErrorMessages.emailRequired);
    }

    async function fetchUserById(userId) {
        const endpoint = ApiPaths.userLookupByIdTemplate.replace("{userId}", encodeURIComponent(userId));
        const response = await fetch(endpoint, { method: "GET", headers: getAdminHeaders() });
        return readUserLookupResponse(response, ErrorMessages.lookupFailed);
    }

    async function readUserLookupResponse(response, badRequestMessage) {
        if (response.status === HttpStatus.badRequest) { throw new Error(badRequestMessage); }
        if (response.status === HttpStatus.notFound) { throw new Error(ErrorMessages.userNotFound); }
        if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
        if (response.status === HttpStatus.forbidden) { throw new Error(NotAvailableForRoleMessage); }
        if (!response.ok) { throw new Error(ErrorMessages.lookupFailed); }
        return response.json();
    }

    async function fetchAdminActivity() {
        const params = new URLSearchParams();
        [["actorAdminUserId", activityActorAdminUserIdInput.value], ["actorUserId", activityActorUserIdInput.value], ["targetUserId", activityTargetUserIdInput.value], ["targetAdminUserId", activityTargetAdminUserIdInput.value], ["source", activitySourceInput.value], ["result", activityResultInput.value], ["actionType", activityActionTypeInput.value], ["fromUtc", activityFromUtcInput.value ? `${activityFromUtcInput.value}Z` : ""], ["toUtc", activityToUtcInput.value ? `${activityToUtcInput.value}Z` : ""], ["limit", activityLimitInput.value]].forEach(([key, value]) => { if (String(value || "").trim()) { params.set(key, String(value).trim()); } });
        const response = await fetch(`${ApiPaths.adminActivity}?${params.toString()}`, { method: "GET", headers: getAdminHeaders() });
        if (response.status === HttpStatus.badRequest) { throw new Error(ErrorMessages.invalidActivityLimit); }
        if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
        if (response.status === HttpStatus.forbidden) { throw new Error(NotAvailableForRoleMessage); }
        if (!response.ok) { throw new Error(ErrorMessages.activityLoadFailed); }
        return response.json();
    }

    async function fetchAuditActions(userId, limit) {
        const response = await fetch(`${ApiPaths.auditActionsTemplate.replace("{userId}", encodeURIComponent(userId))}?limit=${encodeURIComponent(limit)}`, { method: "GET", headers: getAdminHeaders() });
        if (response.status === HttpStatus.badRequest) { throw new Error(ErrorMessages.invalidAuditLimit); }
        if (response.status === HttpStatus.notFound) { throw new Error(ErrorMessages.auditTargetNotFound); }
        if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
        if (response.status === HttpStatus.forbidden) { throw new Error(NotAvailableForRoleMessage); }
        if (!response.ok) { throw new Error(ErrorMessages.auditLoadFailed); }
        return response.json();
    }

    function validateGrantInput() {
        const durationDaysRaw = String(grantDurationDaysInput.value || "").trim();
        const reason = String(grantReasonInput.value || "").trim();
        const durationDays = Number.parseInt(durationDaysRaw, 10);
        if (!durationDaysRaw || !Number.isInteger(durationDays) || String(durationDays) !== durationDaysRaw || durationDays < 1 || durationDays > 365) {
            return { isValid: false, message: ErrorMessages.grantInvalid };
        }
        if (!reason) { return { isValid: false, message: ErrorMessages.grantInvalid }; }
        return { isValid: true, durationDays, reason };
    }

    function syncLookupEmailInputs(email) {
        const value = email || "";
        lookupEmailInput.value = value;
        premiumLookupEmailInput.value = value;
        freeLessonLookupEmailInput.value = value;
    }

    async function applySelectedUserPayload(payload) {
        renderUserLookupResult(payload);
        selectedUserId = payload?.user?.userId || null;
        selectedUserEmail = payload?.user?.email || null;
        selectedUserLookupPayload = payload || null;
        syncLookupEmailInputs(selectedUserEmail);
        updateSelectedUserHeader();
        updateUserRequiredEmptyStates();
        setGrantVisible(Boolean(selectedUserId) && hasAdminPermission(AdminPermissionIds.premiumGrant));
        setRevokeVisible(Boolean(selectedUserId) && hasAdminPermission(AdminPermissionIds.premiumRevoke));
        setBillingCancelRenewalVisible(Boolean(selectedUserId) && hasAdminPermission(AdminPermissionIds.billingCancelRenewal));
        setFreeLessonResetVisible(Boolean(selectedUserId) && hasAdminPermission(AdminPermissionIds.freeLessonAllowanceReset));
        clearAuditLog();
        updateHashField("selectedUserId", selectedUserId);
        if (hasAdminPermission(AdminPermissionIds.auditRead)) { await loadAuditLogForSelectedUser(); }
    }

    async function restoreSelectedUserFromHash() {
        const userId = getHashValue("selectedUserId");
        if (!userId || selectedUserId) { return; }
        try {
            await applySelectedUserPayload(await fetchUserById(userId));
        } catch (error) {
            updateHashField("selectedUserId", null);
            const message = error instanceof Error ? error.message : ErrorMessages.lookupFailed;
            setLookupError(`Selected user could not be restored: ${message}`);
            if (isAuthErrorMessage(message)) { expireAdminSession(message); }
        }
    }

    function clearSelectedUserState() {
        selectedUserId = null;
        selectedUserEmail = null;
        selectedUserLookupPayload = null;
        syncLookupEmailInputs("");
        updateSelectedUserHeader();
        updateUserRequiredEmptyStates();
        setGrantVisible(false);
        setRevokeVisible(false);
        setBillingCancelRenewalVisible(Boolean(selectedUserId) && hasAdminPermission(AdminPermissionIds.billingCancelRenewal));
        setFreeLessonResetVisible(false);
        clearGrantState();
        clearRevokeState();
        clearFreeLessonResetState();
        clearAuditLog();
        updateHashField("selectedUserId", null);
    }

    async function handleLookupSubmit(source) {
        clearLookupErrors();
        clearUserLookupResult();
        clearGrantState();
        const email = String(getLookupElements(source).emailInput?.value || "").trim();
        if (!email) {
            clearSelectedUserState();
            setLookupSourceError(source, ErrorMessages.emailRequired);
            return;
        }

        setLookupSourceLoading(source, true);
        try {
            const payload = await fetchUserByEmail(email);
            await applySelectedUserPayload(payload);
        } catch (error) {
            clearUserLookupResult();
            clearSelectedUserState();
            const message = error instanceof Error ? error.message : ErrorMessages.lookupFailed;
            setLookupSourceError(source, message);
            if (isAuthErrorMessage(message)) { expireAdminSession(message); }
        } finally {
            setLookupSourceLoading(source, false);
        }
    }

    async function refreshSelectedUserAfterMutation() {
        if (!selectedUserId) { return; }
        const payload = await fetchUserById(selectedUserId);
        await applySelectedUserPayload(payload);
    }

    async function grantPremiumForSelectedUser() {
        if (!selectedUserId || !selectedUserEmail) { return; }
        clearGrantState();
        const validation = validateGrantInput();
        if (!validation.isValid) { setGrantError(validation.message); return; }

        const confirmed = window.confirm(`Grant Premium to ${selectedUserEmail} for ${validation.durationDays} day(s)?`);
        if (!confirmed) { return; }

        setGrantLoading(true);
        try {
            const endpoint = ApiPaths.manualPremiumGrantTemplate.replace("{userId}", encodeURIComponent(selectedUserId));
            const response = await fetch(endpoint, {
                method: "POST",
                headers: getAdminHeaders({ "Content-Type": "application/json" }),
                body: JSON.stringify({ durationDays: validation.durationDays, reason: validation.reason })
            });

            if (response.status === HttpStatus.badRequest) { throw new Error(ErrorMessages.grantInvalid); }
            if (response.status === HttpStatus.notFound) { throw new Error(ErrorMessages.grantUserNotFound); }
            if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
        if (response.status === HttpStatus.forbidden) { throw new Error(NotAvailableForRoleMessage); }
            if (!(response.status === 200 || response.status === 201)) { throw new Error(ErrorMessages.grantFailed); }

            const payload = await response.json();
            setGrantSuccess(`Premium granted. Entitlement ID: ${payload.entitlementId || "-"}. Starts at: ${payload.startsAtUtc || "-"}. Expires at: ${payload.expiresAtUtc || "-"}.`);
            grantReasonInput.value = "";
            await refreshSelectedUserAfterMutation();
        } catch (error) {
            const message = error instanceof Error ? error.message : ErrorMessages.grantFailed;
            setGrantError(message);
            if (isAuthErrorMessage(message)) {
                expireAdminSession(message);
            }
        } finally { setGrantLoading(false); }
    }


    async function revokePremiumForSelectedUser() {
        if (!selectedUserId || !selectedUserEmail) { return; }
        clearRevokeMessages();
        const validation = validateRevokeInput();
        if (!validation.isValid) { setRevokeError(validation.message); return; }

        const confirmed = window.confirm(`Revoke Premium entitlement ${validation.entitlementId} for ${selectedUserEmail}?`);
        if (!confirmed) { return; }

        setRevokeLoading(true);
        try {
            const endpoint = ApiPaths.manualPremiumRevokeTemplate
                .replace("{userId}", encodeURIComponent(selectedUserId))
                .replace("{entitlementId}", encodeURIComponent(validation.entitlementId));
            const response = await fetch(endpoint, {
                method: "POST",
                headers: getAdminHeaders({ "Content-Type": "application/json" }),
                body: JSON.stringify({ reason: validation.reason })
            });

            if (response.status === HttpStatus.badRequest) { throw new Error(ErrorMessages.revokeInvalid); }
            if (response.status === HttpStatus.notFound) { throw new Error(ErrorMessages.revokeNotFound); }
            if (response.status === HttpStatus.conflict) { throw new Error(ErrorMessages.revokeConflict); }
            if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
        if (response.status === HttpStatus.forbidden) { throw new Error(NotAvailableForRoleMessage); }
            if (!response.ok) { throw new Error(ErrorMessages.revokeFailed); }

            const payload = await response.json();
            setRevokeSuccess(`Premium revoked. Entitlement ID: ${payload.entitlementId || validation.entitlementId}. Revoked at: ${payload.revokedAtUtc || "-"}.`);
            revokeReasonInput.value = "";
            await refreshSelectedUserAfterMutation();
        } catch (error) {
            const message = error instanceof Error ? error.message : ErrorMessages.revokeFailed;
            setRevokeError(message);
            if (isAuthErrorMessage(message)) {
                expireAdminSession(message);
            }
        } finally { setRevokeLoading(false); }
    }


    async function cancelPaidRenewalForSelectedUser() {
        if (!selectedUserId || !selectedUserEmail) { return; }
        clearBillingCancelRenewalMessages();
        const validation = validateBillingCancelRenewalInput();
        if (!validation.isValid) { billingCancelRenewalErrorElement.textContent = validation.message; return; }

        const confirmed = window.confirm("This cancels future renewals only. Paid Premium access remains until the current paid period ends.");
        if (!confirmed) { return; }

        setBillingCancelRenewalLoading(true);
        try {
            const endpoint = ApiPaths.billingCancelRenewalTemplate.replace("{userId}", encodeURIComponent(selectedUserId));
            const response = await fetch(endpoint, {
                method: "POST",
                headers: getAdminHeaders({ "Content-Type": "application/json" }),
                body: JSON.stringify({ reason: validation.reason })
            });

            if (response.status === HttpStatus.badRequest) { throw new Error(ErrorMessages.billingCancelInvalid); }
            if (response.status === HttpStatus.notFound) { throw new Error(ErrorMessages.billingCancelNotFound); }
            if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
        if (response.status === HttpStatus.forbidden) { throw new Error(NotAvailableForRoleMessage); }
            if (!response.ok) { throw new Error(ErrorMessages.billingCancelFailed); }

            const payload = await response.json();
            const resultCode = payload.resultCode || "unknown";
            const providerDiagnostics = [
                payload.providerErrorCode ? `Provider error code: ${payload.providerErrorCode}` : "",
                payload.providerErrorMessageSafe ? `Provider message: ${payload.providerErrorMessageSafe}` : "",
                payload.providerHttpStatusCode ? `HTTP status: ${payload.providerHttpStatusCode}` : "",
                payload.providerRequestId ? `Provider request ID: ${payload.providerRequestId}` : "",
                payload.cancellationAttemptedAtUtc ? `Attempted at: ${payload.cancellationAttemptedAtUtc}` : "",
                `Provider subscription present: ${formatValue(payload.providerSubscriptionPresent)}`,
                payload.providerSubscriptionIdLast4 ? `Provider subscription ID last4: ${payload.providerSubscriptionIdLast4}` : "",
                payload.providerSubscriptionIdHash ? `Provider subscription ID hash: ${payload.providerSubscriptionIdHash}` : ""
            ].filter(Boolean).join(". ");
            if (resultCode === "provider_error") {
                billingCancelRenewalErrorElement.textContent = `Cancellation was not confirmed by the provider and is retryable if diagnostics still show canRequestCancelRenewal = Yes. Cancel at period end: ${formatValue(payload.cancelAtPeriodEnd)}. ${providerDiagnostics}`;
            } else {
                billingCancelRenewalSuccessElement.textContent = `Cancel paid renewal result: ${resultCode}. Cancel at period end: ${formatValue(payload.cancelAtPeriodEnd)}. Effective at: ${payload.scheduledChangeEffectiveAtUtc || payload.currentPeriodEndUtc || "-"}.`;
                billingCancelRenewalReasonInput.value = "";
            }
            await refreshSelectedUserAfterMutation();
        } catch (error) {
            const message = error instanceof Error ? error.message : ErrorMessages.billingCancelFailed;
            billingCancelRenewalErrorElement.textContent = message;
            try { await refreshSelectedUserAfterMutation(); } catch (_) { }
            if (isAuthErrorMessage(message)) { expireAdminSession(message); }
        } finally { setBillingCancelRenewalLoading(false); }
    }


    async function resetFreeLessonAllowanceForSelectedUser() {
        if (!selectedUserId || !selectedUserEmail) { return; }
        clearFreeLessonResetMessages();
        const validation = validateFreeLessonResetInput();
        if (!validation.isValid) { setFreeLessonResetError(validation.message); return; }

        const confirmed = window.confirm(`Reset free lesson allowance for ${selectedUserEmail} on ${validation.usageDate}?`);
        if (!confirmed) { return; }

        setFreeLessonResetLoading(true);
        try {
            const endpoint = ApiPaths.freeLessonAllowanceResetTemplate.replace("{userId}", encodeURIComponent(selectedUserId));
            const response = await fetch(endpoint, {
                method: "POST",
                headers: getAdminHeaders({ "Content-Type": "application/json" }),
                body: JSON.stringify({ usageDate: validation.usageDate, reason: validation.reason })
            });

            if (response.status === HttpStatus.badRequest) { throw new Error(ErrorMessages.resetInvalid); }
            if (response.status === HttpStatus.notFound) { throw new Error(ErrorMessages.resetNotFound); }
            if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
        if (response.status === HttpStatus.forbidden) { throw new Error(NotAvailableForRoleMessage); }
            if (!response.ok) { throw new Error(ErrorMessages.resetFailed); }

            const payload = await response.json();
            setFreeLessonResetSuccess(`Free lesson allowance reset for ${validation.usageDate}. Removed usage ID: ${payload.removedDailyFreeLessonUsageId || "-"}.`);
            freeLessonResetReasonInput.value = "";
            await refreshSelectedUserAfterMutation();
        } catch (error) {
            const message = error instanceof Error ? error.message : ErrorMessages.resetFailed;
            setFreeLessonResetError(message);
            if (isAuthErrorMessage(message)) {
                expireAdminSession(message);
            }
        } finally { setFreeLessonResetLoading(false); }
    }


    function getSelectedCmsSlug() { return cmsContentPackSelect.value || "static-json-v1"; }
    function cmsPath(template, replacements) {
        return Object.keys(replacements).reduce((path, key) => path.replace(`{${key}}`, encodeURIComponent(replacements[key])), template);
    }
    function setCmsLoading(isLoading) { cmsLoadingElement.classList.toggle("hidden", !isLoading); }
    function setCmsError(message) { cmsErrorElement.textContent = message || ""; }
    function setCmsSuccess(message) { cmsSuccessElement.textContent = message || ""; }
    function clearCmsPublishErrorDetails() { if (cmsPublishErrorDetailsElement) { cmsPublishErrorDetailsElement.textContent = ""; cmsPublishErrorDetailsElement.classList.add("hidden"); } }
    function renderCmsPublishErrorDetails(errorOrPayload) {
        if (!cmsPublishErrorDetailsElement) { return; }
        const details = extractCmsBackendMessages(errorOrPayload);
        cmsPublishErrorDetailsElement.textContent = "";
        if (details.length === 0) { cmsPublishErrorDetailsElement.classList.add("hidden"); return; }
        const title = document.createElement("p");
        title.className = "cms-publish-error-title";
        title.textContent = "Publish failed";
        const list = document.createElement("ul");
        details.forEach((detail) => { const item = document.createElement("li"); item.textContent = detail; list.appendChild(item); });
        cmsPublishErrorDetailsElement.appendChild(title);
        cmsPublishErrorDetailsElement.appendChild(list);
        cmsPublishErrorDetailsElement.classList.remove("hidden");
    }
    function setCmsAuditError(message) { cmsAuditErrorElement.textContent = message || ""; }
    function setCmsAuditLoading(isLoading) { cmsAuditLoadingElement.classList.toggle("hidden", !isLoading); cmsLoadAuditButton.disabled = isLoading; }
    function setCmsEntityMessage(element, message, isError) { element.className = isError ? "error" : "success"; element.textContent = message || ""; }
    function hideCmsPublishDiscovery(options = {}) {
        cmsPublishDiscoveryElements.forEach((element) => {
            element.classList.add("hidden");
            if (element.hasAttribute("data-cms-scenario-draft-saved-visible")) { element.dataset.cmsScenarioDraftSavedVisible = "false"; }
        });
        if (options.clearDraftSaved) { cmsDraftSavedInSession = false; cmsDraftLikelyHasChangesInSession = false; }
    }
    function showScenarioDraftSavedPublishCallouts(draftLikelyHasChanges = true) {
        cmsDraftSavedInSession = true;
        cmsDraftLikelyHasChangesInSession = Boolean(draftLikelyHasChanges);
        [cmsScenarioStructuredPublishDiscoveryElement, cmsScenarioJsonPublishDiscoveryElement].forEach((element) => {
            if (!element) { return; }
            element.classList.remove("hidden");
            element.hidden = false;
            element.style.removeProperty("display");
            element.dataset.cmsScenarioDraftSavedVisible = "true";
        });
    }
    function showCmsPublishDiscoveryForMessage(messageElement, draftLikelyHasChanges = true) {
        cmsDraftSavedInSession = true;
        cmsDraftLikelyHasChangesInSession = Boolean(draftLikelyHasChanges);
        if (messageElement === cmsScenarioMessageElement) {
            showScenarioDraftSavedPublishCallouts(draftLikelyHasChanges);
            return;
        }
        cmsPublishDiscoveryElements.forEach((element) => {
            const isMatch = element.previousElementSibling === messageElement;
            element.classList.toggle("hidden", !isMatch);
        });
    }
    async function goToCmsPublishSection() {
        setCmsError(""); setCmsSuccess(""); clearCmsPublishErrorDetails();
        activateTab(Tabs.cmsContent);
        selectCmsSubTab(CmsSubTabs.versionsPublish, true);
        try { await loadCmsVersions(); } catch (error) { handleCmsError(error); }
        if (cmsPublishSectionElement) {
            cmsPublishSectionElement.classList.add("cms-publish-focus");
            cmsPublishSectionElement.focus({ preventScroll: true });
            cmsPublishSectionElement.scrollIntoView({ behavior: "smooth", block: "start" });
            cmsPublishChangeSummaryInput.focus({ preventScroll: true });
            window.setTimeout(() => cmsPublishSectionElement.classList.remove("cms-publish-focus"), 2400);
        }
    }
    function clearCmsResultPanel(element, message) {
        element.textContent = "";
        element.className = "cms-result-panel";
        const empty = document.createElement("p");
        empty.className = "empty-state";
        empty.textContent = message;
        element.appendChild(empty);
    }
    function getCmsResponseValue(source, camelKey, fallbackValue = undefined) {
        if (!source || typeof source !== "object") { return fallbackValue; }
        if (Object.prototype.hasOwnProperty.call(source, camelKey)) { return source[camelKey]; }
        const pascalKey = camelKey.charAt(0).toUpperCase() + camelKey.slice(1);
        if (Object.prototype.hasOwnProperty.call(source, pascalKey)) { return source[pascalKey]; }
        const expectedKey = camelKey.toLowerCase();
        const matchingKey = Object.keys(source).find((key) => key.toLowerCase() === expectedKey);
        return matchingKey ? source[matchingKey] : fallbackValue;
    }
    function getCmsResponseArray(source, camelKey) {
        const value = getCmsResponseValue(source, camelKey, []);
        return Array.isArray(value) ? value : [];
    }
    function formatCmsPreviewPublishedVersion(value) {
        return value === null || value === undefined || value === "" ? "None" : formatValue(value);
    }
    function formatCmsYesNo(value) {
        if (value === true) { return "Yes"; }
        if (value === false) { return "No"; }
        if (typeof value === "string") {
            const normalized = value.trim().toLowerCase();
            if (normalized === "true" || normalized === "yes") { return "Yes"; }
            if (normalized === "false" || normalized === "no") { return "No"; }
        }
        return formatValue(value);
    }
    function getCmsCountValue(source, counts, countKey, legacyKey) {
        const directValue = getCmsResponseValue(source, legacyKey);
        return directValue === undefined ? getCmsResponseValue(counts, countKey) : directValue;
    }
    function appendCmsDefinitionList(container, rows, className = "meta cms-preview-meta") {
        const list = document.createElement("dl");
        list.className = className;
        rows.forEach((row) => {
            const term = document.createElement("dt");
            term.textContent = row.label;
            const value = document.createElement("dd");
            value.textContent = formatValue(row.value);
            list.appendChild(term);
            list.appendChild(value);
        });
        container.appendChild(list);
    }
    function appendCmsStatusBadge(container, label, isPositive) {
        const badge = document.createElement("span");
        badge.className = `badge ${isPositive ? "available" : "unavailable"} cms-status-badge`;
        badge.textContent = label;
        badge.setAttribute("aria-label", `Validation status: ${label}`);
        container.appendChild(badge);
    }
    function appendCmsMessageList(container, headingText, messages, emptyText, isErrorList = false) {
        const section = document.createElement("section");
        section.className = "cms-message-list";
        const heading = document.createElement("h4");
        heading.textContent = headingText;
        section.appendChild(heading);
        const normalizedMessages = Array.isArray(messages) ? messages.filter((message) => String(message || "").trim()) : [];
        if (normalizedMessages.length === 0) {
            const empty = document.createElement("p");
            empty.className = "empty-state";
            empty.textContent = emptyText;
            section.appendChild(empty);
        } else {
            const list = document.createElement("ul");
            list.className = isErrorList ? "cms-error-list" : "cms-warning-list";
            normalizedMessages.forEach((message) => {
                const item = document.createElement("li");
                item.textContent = message;
                list.appendChild(item);
            });
            section.appendChild(list);
        }
        container.appendChild(section);
    }
    function appendCmsRawJsonDetails(container, summaryText, payload) {
        const details = document.createElement("details");
        details.className = "cms-raw-json-details";
        const summary = document.createElement("summary");
        summary.textContent = summaryText;
        const raw = document.createElement("pre");
        raw.className = "cms-json-output";
        raw.textContent = JSON.stringify(payload, null, 2);
        details.appendChild(summary);
        details.appendChild(raw);
        container.appendChild(details);
    }
    function appendCmsSimpleTable(container, columns, rows, emptyMessage) {
        const wrapper = document.createElement("div");
        wrapper.className = "table-wrap";
        if (!Array.isArray(rows) || rows.length === 0) {
            const empty = document.createElement("p");
            empty.className = "empty-state";
            empty.textContent = emptyMessage;
            wrapper.appendChild(empty);
            container.appendChild(wrapper);
            return;
        }
        const table = document.createElement("table");
        table.className = "compact-table cms-table";
        const thead = document.createElement("thead");
        const headRow = document.createElement("tr");
        columns.forEach((column) => {
            const th = document.createElement("th");
            th.scope = "col";
            th.textContent = column.label;
            headRow.appendChild(th);
        });
        thead.appendChild(headRow);
        table.appendChild(thead);
        const tbody = document.createElement("tbody");
        rows.forEach((row) => {
            const tr = document.createElement("tr");
            columns.forEach((column) => {
                const td = document.createElement("td");
                const value = typeof column.value === "function" ? column.value(row) : row?.[column.key];
                td.textContent = formatValue(value);
                tr.appendChild(td);
            });
            tbody.appendChild(tr);
        });
        table.appendChild(tbody);
        wrapper.appendChild(table);
        container.appendChild(wrapper);
    }
    function renderCmsValidationResult(validation) {
        cmsValidationResultElement.textContent = "";
        cmsValidationResultElement.className = "cms-result-panel cms-readable-result-panel";
        const counts = getCmsResponseValue(validation, "counts", {});
        const success = Boolean(getCmsResponseValue(validation, "success", false));
        const titleRow = document.createElement("div");
        titleRow.className = "cms-result-title-row cms-status-row";
        const title = document.createElement("h4");
        title.textContent = success ? "Validation passed" : "Validation failed";
        titleRow.appendChild(title);
        appendCmsStatusBadge(titleRow, success ? "Passed" : "Failed", success);
        cmsValidationResultElement.appendChild(titleRow);
        appendCmsDefinitionList(cmsValidationResultElement, [
            { label: "Content pack slug", value: getCmsResponseValue(validation, "contentPackSlug") },
            { label: "Checked at (UTC)", value: getCmsResponseValue(validation, "checkedAtUtc") },
            { label: "Topics", value: getCmsResponseValue(counts, "topics") },
            { label: "Scenarios", value: getCmsResponseValue(counts, "scenarios") },
            { label: "Prompt templates", value: getCmsResponseValue(counts, "promptTemplates") },
            { label: "Tutor behavior profiles", value: getCmsResponseValue(counts, "tutorBehaviorProfiles") }
        ]);
        appendCmsMessageList(cmsValidationResultElement, "Errors", getCmsResponseArray(validation, "errors"), "No errors", true);
        appendCmsMessageList(cmsValidationResultElement, "Warnings", getCmsResponseArray(validation, "warnings"), "No warnings");
        appendCmsRawJsonDetails(cmsValidationResultElement, "Show raw validation JSON", validation);
    }

    function renderCmsRuntimeStatusPanel(targetElement, status) {
        if (!targetElement) { return; }
        targetElement.textContent = "";
        targetElement.className = "cms-result-panel cms-readable-result-panel";
        const effectiveSource = String(getCmsResponseValue(status, "effectiveSource", getCmsResponseValue(status, "source", "")) || "");
        const usePublishedSnapshot = Boolean(getCmsResponseValue(status, "usePublishedSnapshotForRuntime", false));
        const readPublishedSnapshot = Boolean(getCmsResponseValue(status, "readPublishedSnapshotEnabled", false));
        const flagsEnabled = usePublishedSnapshot && readPublishedSnapshot;
        const fallbackUsed = Boolean(getCmsResponseValue(status, "fallbackUsed", false));
        const validationSuccess = Boolean(getCmsResponseValue(status, "validationSuccess", false));
        const cmsSnapshotActive = flagsEnabled && effectiveSource === "CmsPublishedSnapshot" && validationSuccess && !fallbackUsed;
        let headline = "Learner runtime needs attention";
        let statusLabel = "Needs attention";
        let positive = false;
        if (cmsSnapshotActive) { headline = "Learner runtime is using CMS published snapshot"; statusLabel = "OK"; positive = true; }
        else if (fallbackUsed || effectiveSource === "StaticJson" || effectiveSource === "StaticJsonFallback") { headline = "Learner runtime is using static JSON fallback"; statusLabel = "Fallback active"; }
        else if (usePublishedSnapshot && !readPublishedSnapshot) { headline = "CMS runtime is configured but snapshot reads are disabled"; }
        else if (flagsEnabled && !validationSuccess) { headline = "CMS published snapshot is unavailable or invalid"; }
        const titleRow = document.createElement("div");
        titleRow.className = "cms-result-title-row cms-status-row";
        const title = document.createElement("h4");
        title.textContent = headline;
        titleRow.appendChild(title);
        appendCmsStatusBadge(titleRow, statusLabel, positive);
        targetElement.appendChild(titleRow);
        const message = document.createElement("p");
        message.className = "muted";
        message.textContent = getCmsResponseValue(status, "message", "Runtime status loaded. No content bodies are displayed.");
        targetElement.appendChild(message);
        if (!cmsSnapshotActive) {
            const warning = document.createElement("p");
            warning.className = "cms-inline-warning";
            warning.textContent = "CMS published snapshot is not active/effective for learner runtime. CMS edits affect learner lessons only when the CMS published snapshot is enabled, valid, and effectively active. While static JSON fallback is active, CMS draft or published edits may not affect learner runtime.";
            targetElement.appendChild(warning);
        }
        appendCmsDefinitionList(targetElement, [
            { label: "Checked at (UTC)", value: getCmsResponseValue(status, "checkedAtUtc") },
            { label: "Content pack slug", value: getCmsResponseValue(status, "contentPackSlug") },
            { label: "Use published snapshot for runtime", value: getCmsResponseValue(status, "usePublishedSnapshotForRuntime") },
            { label: "Read published snapshot enabled", value: getCmsResponseValue(status, "readPublishedSnapshotEnabled") },
            { label: "Emergency static JSON fallback enabled", value: getCmsResponseValue(status, "fallbackToStaticJson") },
            { label: "Actual learner runtime source", value: effectiveSource },
            { label: "Published version number", value: getCmsResponseValue(status, "publishedVersionNumber") },
            { label: "Snapshot hash", value: getCmsResponseValue(status, "snapshotHash") },
            { label: "Topics", value: getCmsResponseValue(status, "topicCount") },
            { label: "Scenarios", value: getCmsResponseValue(status, "scenarioCount") },
            { label: "Prompt templates", value: getCmsResponseValue(status, "promptTemplateCount") },
            { label: "Tutor behavior profiles", value: getCmsResponseValue(status, "tutorBehaviorProfileCount") },
            { label: "Validation success", value: getCmsResponseValue(status, "validationSuccess") },
            { label: "Currently using static JSON fallback", value: fallbackUsed }
        ]);
        appendCmsMessageList(targetElement, "Errors", getCmsResponseArray(status, "errors"), "No errors", true);
        appendCmsMessageList(targetElement, "Warnings", getCmsResponseArray(status, "warnings"), "No warnings");
    }

    function renderCmsRuntimeStatus(status) {
        renderCmsRuntimeStatusPanel(cmsRuntimeStatusElement, status);
        renderCmsRuntimeStatusPanel(cmsOverviewRuntimeStatusElement, status);
    }

    function renderCmsPreviewSummary(preview) {
        cmsPreviewSummaryElement.textContent = "";
        cmsPreviewSummaryElement.className = "cms-result-panel cms-readable-result-panel";
        const titleRow = document.createElement("div");
        titleRow.className = "cms-result-title-row cms-status-row";
        const title = document.createElement("h4");
        const contentPackName = getCmsResponseValue(preview, "contentPackName");
        title.textContent = contentPackName || "Draft preview summary";
        titleRow.appendChild(title);
        cmsPreviewSummaryElement.appendChild(titleRow);
        const counts = getCmsResponseValue(preview, "counts", {});
        appendCmsDefinitionList(cmsPreviewSummaryElement, [
            { label: "Content pack slug", value: getCmsResponseValue(preview, "contentPackSlug") },
            { label: "Content pack name", value: contentPackName },
            { label: "Content pack status", value: getCmsResponseValue(preview, "contentPackStatus") },
            { label: "Current published version number", value: formatCmsPreviewPublishedVersion(getCmsResponseValue(preview, "currentPublishedVersionNumber")) },
            { label: "Topics", value: getCmsCountValue(preview, counts, "topics", "topicCount") },
            { label: "Scenarios", value: getCmsCountValue(preview, counts, "scenarios", "scenarioCount") },
            { label: "Prompt templates", value: getCmsCountValue(preview, counts, "promptTemplates", "promptTemplateCount") },
            { label: "Tutor behavior profiles", value: getCmsCountValue(preview, counts, "tutorBehaviorProfiles", "tutorBehaviorProfileCount") }
        ]);
        const topicsHeading = document.createElement("h4");
        topicsHeading.textContent = "Sample topics";
        cmsPreviewSummaryElement.appendChild(topicsHeading);
        appendCmsSimpleTable(cmsPreviewSummaryElement, [
            { key: "stableTopicKey", label: "stableTopicKey", value: (row) => getCmsResponseValue(row, "stableTopicKey") },
            { key: "title", label: "title", value: (row) => getCmsResponseValue(row, "title") },
            { key: "sortOrder", label: "sortOrder", value: (row) => getCmsResponseValue(row, "sortOrder") },
            { key: "isActive", label: "isActive", value: (row) => getCmsResponseValue(row, "isActive") }
        ], getCmsResponseArray(preview, "sampleTopics"), "No sample topics returned.");
        const scenariosHeading = document.createElement("h4");
        scenariosHeading.textContent = "Sample scenarios";
        cmsPreviewSummaryElement.appendChild(scenariosHeading);
        appendCmsSimpleTable(cmsPreviewSummaryElement, [
            { key: "stableScenarioKey", label: "stableScenarioKey", value: (row) => getCmsResponseValue(row, "stableScenarioKey") },
            { key: "topicKey", label: "topicKey", value: (row) => getCmsResponseValue(row, "topicKey") },
            { key: "title", label: "title", value: (row) => getCmsResponseValue(row, "title") },
            { key: "isActive", label: "isActive", value: (row) => getCmsResponseValue(row, "isActive") },
            { key: "definitionJsonPresent", label: "DefinitionJson present", value: (row) => formatCmsYesNo(getCmsResponseValue(row, "definitionJsonPresent")) },
            { key: "definitionJsonValid", label: "DefinitionJson valid", value: (row) => formatCmsYesNo(getCmsResponseValue(row, "definitionJsonValid")) }
        ], getCmsResponseArray(preview, "sampleScenarios"), "No sample scenarios returned.");
        appendCmsRawJsonDetails(cmsPreviewSummaryElement, "Show raw preview JSON", preview);
    }
    function tryParseCmsJson(text) { try { return { isValid: true, value: JSON.parse(text) }; } catch (error) { return { isValid: false, message: error instanceof Error ? error.message : "Invalid JSON." }; } }
    function prettyPrintCmsJson(text) { const parsed = tryParseCmsJson(text); return parsed.isValid ? { isValid: true, text: JSON.stringify(parsed.value, null, 2) } : parsed; }
    function setCmsScenarioJsonStatus(message, isError) { cmsScenarioJsonStatusElement.className = isError ? "error" : "success"; cmsScenarioJsonStatusElement.textContent = message || ""; }
    function setCmsScenarioStructuredStatus(message, isError) { cmsScenarioStructuredStatusElement.textContent = message || ""; cmsScenarioStructuredStatusElement.className = isError ? "error" : "muted"; }
    function splitCmsLines(value) { return String(value || "").split(/\r?\n/).map((line) => line.trim()).filter(Boolean); }
    function joinCmsLines(value) { return Array.isArray(value) ? value.map((item) => typeof item === "string" ? item : "").filter(Boolean).join("\n") : ""; }
    function getCmsObject(parent, key) { if (!parent[key] || typeof parent[key] !== "object" || Array.isArray(parent[key])) { parent[key] = {}; } return parent[key]; }
    function getCmsNestedObject(root, keys) { return keys.reduce((current, key) => getCmsObject(current, key), root); }
    function setCmsStringField(root, keys, value) { const parent = getCmsNestedObject(root, keys.slice(0, -1)); parent[keys[keys.length - 1]] = String(value || "").trim(); }
    function setCmsArrayField(root, keys, value) { const parent = getCmsNestedObject(root, keys.slice(0, -1)); parent[keys[keys.length - 1]] = splitCmsLines(value); }
    function getCmsStringField(root, keys) { let current = root; for (const key of keys) { if (!current || typeof current !== "object") { return ""; } current = current[key]; } return typeof current === "string" || typeof current === "number" ? String(current) : ""; }
    function getCmsArrayField(root, keys) { let current = root; for (const key of keys) { if (!current || typeof current !== "object") { return ""; } current = current[key]; } return joinCmsLines(current); }
    function getCmsStructuredScenarioSnapshot() {
        return {
            firstBotMessageShouldExplain: cmsScenarioFirstBotMessageLinesInput.value,
            contextOptions: cmsScenarioContextOptionLinesInput.value,
            validContextKeywords: cmsScenarioValidContextKeywordsLinesInput.value,
            customContextRules: cmsScenarioCustomContextRulesLinesInput.value,
            invalidContextRedirect: cmsScenarioInvalidContextRedirectInput.value,
            goal: cmsScenarioGoalTextInput.value,
            canDoStatements: cmsScenarioCanDoLinesInput.value,
            opening: cmsScenarioOpeningTextInput.value,
            firstUserTask: cmsScenarioFirstUserTaskInput.value,
            guidedFollowUps: cmsScenarioGuidedFollowUpLinesInput.value,
            aiTutorPromptInstructions: cmsScenarioAiInstructionLinesInput.value,
            wrapUpMessage: cmsScenarioWrapUpMessageInput.value,
            finalMessage: cmsScenarioFinalMessageInput.value,
            exampleHint: cmsScenarioHintExampleInput.value
        };
    }
    function getCmsScenarioDefinitionObject() {
        const parsed = tryParseCmsJson(cmsScenarioDefinitionJsonInput.value.trim());
        if (!parsed.isValid) { throw new Error(`Advanced JSON is invalid (${parsed.message}). Fix it before structured fields can be merged.`); }
        if (!parsed.value || typeof parsed.value !== "object" || Array.isArray(parsed.value)) { throw new Error("Advanced JSON root must be an object before structured fields can be merged."); }
        return parsed.value;
    }
    function fillCmsStructuredScenarioFieldsFromDefinition() {
        const parsed = tryParseCmsJson(cmsScenarioDefinitionJsonInput.value.trim());
        if (!parsed.isValid || !parsed.value || typeof parsed.value !== "object" || Array.isArray(parsed.value)) {
            setCmsScenarioStructuredStatus(parsed.isValid ? "Structured editor could not read this scenario because Advanced JSON root is not an object." : `Structured editor could not read this scenario because Advanced JSON is invalid (${parsed.message}).`, true);
            return;
        }
        const root = parsed.value;
        cmsScenarioFirstBotMessageLinesInput.value = getCmsArrayField(root, ["lessonSetup", "firstBotMessageShouldExplain"]);
        const variants = root.controlledVariation && Array.isArray(root.controlledVariation.contextVariants) ? root.controlledVariation.contextVariants : [];
        cmsScenarioContextOptionLinesInput.value = variants.map((variant) => variant && typeof variant === "object" && typeof variant.title === "string" ? variant.title : "").filter(Boolean).join("\n");
        cmsScenarioValidContextKeywordsLinesInput.value = getCmsArrayField(root, ["lessonSetup", "contextSelection", "validCustomContextKeywords"]);
        cmsScenarioCustomContextRulesLinesInput.value = getCmsArrayField(root, ["controlledVariation", "customContextRules"]);
        cmsScenarioInvalidContextRedirectInput.value = getCmsStringField(root, ["lessonSetup", "contextSelection", "invalidContextRedirect"]) || getCmsStringField(root, ["controlledVariation", "invalidContextRedirect"]);
        cmsScenarioGoalTextInput.value = getCmsStringField(root, ["learningGoal", "goal"]);
        cmsScenarioCanDoLinesInput.value = getCmsArrayField(root, ["learningGoal", "canDoStatements"]);
        cmsScenarioOpeningTextInput.value = getCmsStringField(root, ["conversationFlow", "opening"]);
        cmsScenarioFirstUserTaskInput.value = getCmsStringField(root, ["conversationFlow", "firstUserTask"]);
        cmsScenarioGuidedFollowUpLinesInput.value = getCmsArrayField(root, ["conversationFlow", "guidedPracticeFollowUpQuestions"]);
        cmsScenarioAiInstructionLinesInput.value = getCmsArrayField(root, ["aiTutorPromptInstructions"]);
        cmsScenarioWrapUpMessageInput.value = getCmsStringField(root, ["conversationFlow", "wrapUpMessage"]);
        cmsScenarioFinalMessageInput.value = getCmsStringField(root, ["conversationFlow", "finalMessage"]);
        cmsScenarioHintExampleInput.value = getCmsStringField(root, ["hintRules", "exampleHint"]);
        setCmsScenarioStructuredStatus("Structured scenario fields loaded from DefinitionJson.", false);
    }
    function mergeCmsStructuredScenarioFieldsToDefinition(options = {}) {
        const root = getCmsScenarioDefinitionObject();
        if (cmsSelectedScenario?.stableScenarioKey) { root.id = cmsSelectedScenario.stableScenarioKey; }
        setCmsStringField(root, ["metadata", "subtopic"], cmsScenarioTitleInput.value);
        setCmsStringField(root, ["lessonSetup", "setupMessage"], cmsScenarioSetupMessageInput.value);
        setCmsArrayField(root, ["lessonSetup", "firstBotMessageShouldExplain"], cmsScenarioFirstBotMessageLinesInput.value);
        const titles = splitCmsLines(cmsScenarioContextOptionLinesInput.value);
        const controlledVariation = getCmsObject(root, "controlledVariation");
        const existingVariants = Array.isArray(controlledVariation.contextVariants) ? controlledVariation.contextVariants : [];
        controlledVariation.contextVariants = titles.map((title, index) => Object.assign({}, (existingVariants[index] && typeof existingVariants[index] === "object") ? existingVariants[index] : { id: title.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "") || `option-${index + 1}` }, { title }));
        setCmsArrayField(root, ["lessonSetup", "contextSelection", "validCustomContextKeywords"], cmsScenarioValidContextKeywordsLinesInput.value);
        setCmsArrayField(root, ["controlledVariation", "customContextRules"], cmsScenarioCustomContextRulesLinesInput.value);
        setCmsStringField(root, ["lessonSetup", "contextSelection", "invalidContextRedirect"], cmsScenarioInvalidContextRedirectInput.value);
        setCmsStringField(root, ["controlledVariation", "invalidContextRedirect"], cmsScenarioInvalidContextRedirectInput.value);
        setCmsStringField(root, ["learningGoal", "goal"], cmsScenarioGoalTextInput.value);
        setCmsArrayField(root, ["learningGoal", "canDoStatements"], cmsScenarioCanDoLinesInput.value);
        setCmsStringField(root, ["conversationFlow", "opening"], cmsScenarioOpeningTextInput.value);
        setCmsStringField(root, ["conversationFlow", "firstUserTask"], cmsScenarioFirstUserTaskInput.value);
        setCmsArrayField(root, ["conversationFlow", "guidedPracticeFollowUpQuestions"], cmsScenarioGuidedFollowUpLinesInput.value);
        setCmsArrayField(root, ["aiTutorPromptInstructions"], cmsScenarioAiInstructionLinesInput.value);
        setCmsStringField(root, ["conversationFlow", "wrapUpMessage"], cmsScenarioWrapUpMessageInput.value);
        setCmsStringField(root, ["conversationFlow", "finalMessage"], cmsScenarioFinalMessageInput.value);
        setCmsStringField(root, ["hintRules", "exampleHint"], cmsScenarioHintExampleInput.value);
        const required = [["id"], ["metadata"], ["lessonSetup", "setupMessage"], ["learningGoal", "goal"], ["conversationFlow"], ["aiTutorPromptInstructions"]];
        for (const keys of required) { if (!getCmsStringField(root, keys) && keys.length !== 1) { throw new Error(`Required structured scenario field '${keys.join(".")}' is empty.`); } }
        cmsScenarioDefinitionJsonInput.value = JSON.stringify(root, null, 2);
        if (!options.silent) { setCmsScenarioStructuredStatus("Structured scenario is valid and merged into Advanced JSON. Nothing was saved or published; use Save draft to persist edits.", false); }
        return root;
    }
    function validateCmsStructuredScenarioInput() { try { mergeCmsStructuredScenarioFieldsToDefinition(); updateCmsDirtyState("scenario"); return true; } catch (error) { setCmsScenarioStructuredStatus(`Structured validation failed: ${error instanceof Error ? error.message : "Unable to assemble scenario JSON."}`, true); return false; } }
    function validateCmsScenarioJsonInput() { const text = cmsScenarioDefinitionJsonInput.value.trim(); if (!text) { setCmsScenarioJsonStatus("Validation failed: full scenario JSON is required before saving an active scenario. Nothing was saved or published.", true); return false; } const parsed = tryParseCmsJson(text); if (!parsed.isValid) { setCmsScenarioJsonStatus(`Validation failed: invalid JSON syntax (${parsed.message}). Nothing was saved or published.`, true); return false; } const root = parsed.value; if (!root || typeof root !== "object" || Array.isArray(root)) { setCmsScenarioJsonStatus("Validation failed: full scenario JSON root must be an object. Nothing was saved or published.", true); return false; } const requiredRootProperties = ["id", "metadata", "lessonSetup", "learningGoal", "targetLanguage", "levelProfiles", "conversationFlow", "controlledVariation", "offTopicHandling", "feedbackRules", "hintRules", "aiTutorPromptInstructions"]; const missing = requiredRootProperties.filter((key) => root[key] === undefined || root[key] === null || (Array.isArray(root[key]) && root[key].length === 0) || (typeof root[key] === "object" && !Array.isArray(root[key]) && Object.keys(root[key]).length === 0) || (typeof root[key] === "string" && !root[key].trim())); if (missing.length > 0) { setCmsScenarioJsonStatus(`Validation failed: missing required scenario JSON fields: ${missing.join(", ")}. Nothing was saved or published.`, true); return false; } if (typeof root.id !== "string" || !root.id.trim()) { setCmsScenarioJsonStatus("Validation failed: full scenario JSON id must be a non-empty string. Nothing was saved or published.", true); return false; } if (cmsSelectedScenario?.stableScenarioKey && root.id.trim() !== cmsSelectedScenario.stableScenarioKey) { setCmsScenarioJsonStatus(`Validation failed: full scenario JSON id must match stable scenario key '${cmsSelectedScenario.stableScenarioKey}'. Nothing was saved or published.`, true); return false; } if (!root.lessonSetup || typeof root.lessonSetup !== "object" || Array.isArray(root.lessonSetup) || typeof root.lessonSetup.setupMessage !== "string" || !root.lessonSetup.setupMessage.trim()) { setCmsScenarioJsonStatus("Validation failed: full scenario JSON is missing required lessonSetup.setupMessage. Nothing was saved or published.", true); return false; } setCmsScenarioJsonStatus("Validation passed: JSON syntax and required scenario fields are ready to save as a draft. Nothing was saved or published.", false); return true; }
    function formatCmsScenarioJsonInput() { const formatted = prettyPrintCmsJson(cmsScenarioDefinitionJsonInput.value); if (!formatted.isValid) { setCmsScenarioJsonStatus(`Format failed: invalid JSON syntax (${formatted.message}). Nothing was saved or published.`, true); return false; } cmsScenarioDefinitionJsonInput.value = formatted.text; updateCmsDirtyState("scenario"); setCmsScenarioJsonStatus("Formatted JSON for easier editing. Nothing was saved or published; use Save draft to persist edits.", false); return true; }
    function formatShortHash(hash) { const value = String(hash || ""); return value.length > 16 ? `${value.slice(0, 12)}...${value.slice(-4)}` : formatValue(value); }
    function getSelectedCmsAuditLimit() { const limit = Number.parseInt(cmsAuditLimitSelect.value, 10); return [10, 25, 50, 100].includes(limit) ? limit : 25; }
    function shouldShowCmsSmokeAuditEntries() { return Boolean(cmsAuditShowSmokeInput?.checked); }
    function isCmsSmokeAuditEntry(entry) {
        const reason = String(entry?.reason || entry?.Reason || "").toLowerCase();
        return reason.includes("smoke");
    }
    function getVisibleCmsAuditEntries(entries) {
        const filteredEntries = shouldShowCmsSmokeAuditEntries() ? entries : entries.filter((entry) => !isCmsSmokeAuditEntry(entry));
        return filteredEntries.slice(0, getSelectedCmsAuditLimit());
    }
    function updateCmsAuditSmokeFilterStatus() {
        if (!cmsAuditSmokeFilterStatusElement) { return; }
        cmsAuditSmokeFilterStatusElement.textContent = shouldShowCmsSmokeAuditEntries() ? "Smoke/test entries visible." : "Smoke/test entries hidden.";
    }

    function appendCmsBackendMessages(target, values, prefix) {
        if (!values) { return; }
        if (Array.isArray(values)) {
            values.forEach((value) => appendCmsBackendMessages(target, value, prefix));
            return;
        }
        if (typeof values === "object") {
            Object.keys(values).forEach((key) => {
                const nestedPrefix = prefix ? `${prefix} ${key}` : key;
                appendCmsBackendMessages(target, values[key], nestedPrefix);
            });
            return;
        }
        const text = String(values || "").trim();
        if (text) { target.push(prefix ? `${prefix}: ${text}` : text); }
    }
    function extractCmsBackendMessages(payload) {
        const source = payload?.cmsPayload || payload;
        const details = Array.isArray(payload?.cmsDetails) ? [...payload.cmsDetails] : [];
        if (source) {
            appendCmsBackendMessages(details, source.errors, "Error");
            appendCmsBackendMessages(details, source.warnings, "Warning");
            appendCmsBackendMessages(details, source.validation?.errors, "Validation error");
            appendCmsBackendMessages(details, source.validation?.warnings, "Validation warning");
            if (source.title) { details.push(String(source.title)); }
            if (source.detail) { details.push(String(source.detail)); }
            if (source.error) { details.push(String(source.error)); }
            if (source.message) { details.push(String(source.message)); }
        }
        return [...new Set(details.map((detail) => String(detail || "").trim()).filter(Boolean))];
    }

    function feedbackReportCategoryLabel(value) {
        return ({ suggestion: "Suggestion", app_issue: "App problem", ai_response: "AI response", account_deletion: "Account deletion request" })[value] || "-";
    }

    function feedbackReportReplyDeliveryStatusLabel(value) {
        return ({ pending: "Pending", sent: "Sent", failed: "Failed" })[value] || "Unknown";
    }

    function feedbackReportReplyFailureLabel(value) {
        return ({ email_not_configured: "Email delivery is not configured.", email_delivery_failed: "Email delivery failed." })[value] || "Delivery failed.";
    }

    function feedbackReportStatusLabel(value) {
        return ({ new: "New", reviewed: "Reviewed", needs_information: "Needs information", processing: "Processing", resolved: "Resolved", rejected: "Rejected" })[value] || "-";
    }

    function formatFeedbackReportDate(value) {
        if (!value) { return "-"; }
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? "-" : date.toLocaleString();
    }

    function formatFeedbackReportClient(platform, version) {
        const safePlatform = String(platform || "").trim();
        const safeVersion = String(version || "").trim();
        return safePlatform && safeVersion ? `${safePlatform} ${safeVersion}` : (safePlatform || safeVersion || "-");
    }

    function clearFeedbackReportDetails() {
        feedbackReportsState.selectedReportId = null;
        feedbackReportsState.selectedReport = null;
        feedbackReportsState.statusRequestPending = false;
        feedbackReportsState.replyRequestPending = false;
        feedbackReportsState.replyUnavailable = false;
        feedbackReportsState.statusPermissionDenied = false;
        feedbackReportsState.replyPermissionDenied = false;
        feedbackReportDetailsCard.classList.add("hidden");
        feedbackReportDetailsLoadingElement.classList.add("hidden");
        feedbackReportDetailsErrorElement.textContent = "";
        feedbackReportDetailsElement.textContent = "";
        feedbackReportStatusActionsElement.classList.add("hidden");
        feedbackReportReplyActionsElement.classList.add("hidden");
        feedbackReportReplyHistoryElement.classList.add("hidden");
        feedbackReportReplyHistoryContentElement.textContent = "";
        feedbackReportStatusProgressElement.classList.add("hidden");
        feedbackReportStatusErrorElement.textContent = "";
        feedbackReportStatusSuccessElement.textContent = "";
        feedbackReportReplyProgressElement.classList.add("hidden");
        feedbackReportReplyErrorElement.textContent = "";
        feedbackReportReplySuccessElement.textContent = "";
        feedbackReportReplyTextInput.value = "";
        updateFeedbackReportReplyLength();
    }

    function clearFeedbackReportsState() {
        feedbackReportsState = { page: 1, totalCount: 0, items: [], selectedReportId: null, selectedReport: null, statusRequestPending: false, replyRequestPending: false, replyUnavailable: false, statusPermissionDenied: false, replyPermissionDenied: false };
        feedbackReportsListElement.textContent = "";
        feedbackReportsErrorElement.textContent = "";
        feedbackReportsLoadingElement.classList.add("hidden");
        feedbackReportsSummaryElement.textContent = "No reports loaded.";
        feedbackReportsPreviousButton.disabled = true;
        feedbackReportsNextButton.disabled = true;
        clearFeedbackReportDetails();
    }

    function appendFeedbackReportText(container, text, className = "") {
        const element = document.createElement("p");
        if (className) { element.className = className; }
        element.textContent = text;
        container.appendChild(element);
    }

    function renderFeedbackReportsList() {
        feedbackReportsListElement.textContent = "";
        const items = Array.isArray(feedbackReportsState.items) ? feedbackReportsState.items : [];
        if (items.length === 0) {
            appendFeedbackReportText(feedbackReportsListElement, "No feedback reports match these filters.", "empty-state");
        } else {
            const wrap = document.createElement("div");
            wrap.className = "feedback-reports-table-wrap";
            const table = document.createElement("table");
            table.className = "compact-table feedback-reports-table";
            const head = document.createElement("thead");
            const headerRow = document.createElement("tr");
            ["Type", "Status", "Message preview", "User", "Created", "Client"].forEach((label) => { const cell = document.createElement("th"); cell.scope = "col"; cell.textContent = label; headerRow.appendChild(cell); });
            head.appendChild(headerRow);
            table.appendChild(head);
            const body = document.createElement("tbody");
            items.forEach((item) => {
                const row = document.createElement("tr");
                const reportId = String(item?.reportId || "");
                row.className = "feedback-report-row";
                row.tabIndex = 0;
                row.setAttribute("role", "button");
                row.setAttribute("aria-label", "Open feedback report details");
                row.setAttribute("aria-selected", String(reportId === feedbackReportsState.selectedReportId));
                row.classList.toggle("selected", reportId === feedbackReportsState.selectedReportId);
                const values = [feedbackReportCategoryLabel(item?.category), feedbackReportStatusLabel(item?.status), item?.messagePreview || "-", "", formatFeedbackReportDate(item?.createdAtUtc), formatFeedbackReportClient(item?.clientPlatform, item?.clientVersion)];
                values.forEach((value, index) => {
                    const cell = document.createElement("td");
                    if (index === 3) {
                        const displayName = String(item?.userDisplayName || "").trim();
                        if (displayName) { const name = document.createElement("div"); name.textContent = displayName; cell.appendChild(name); }
                        const email = document.createElement("div"); email.textContent = String(item?.userEmail || "-"); cell.appendChild(email);
                    } else { cell.textContent = String(value); }
                    row.appendChild(cell);
                });
                const select = () => { if (reportId) { loadFeedbackReportDetails(reportId); } };
                row.addEventListener("click", select);
                row.addEventListener("keydown", (event) => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); select(); } });
                body.appendChild(row);
            });
            table.appendChild(body);
            wrap.appendChild(table);
            feedbackReportsListElement.appendChild(wrap);
        }
        const totalCount = Number.isFinite(Number(feedbackReportsState.totalCount)) ? Number(feedbackReportsState.totalCount) : 0;
        const start = totalCount === 0 ? 0 : ((feedbackReportsState.page - 1) * FeedbackReportPageSize) + 1;
        const end = Math.min(feedbackReportsState.page * FeedbackReportPageSize, totalCount);
        feedbackReportsSummaryElement.textContent = totalCount === 0 ? "No reports found." : `${start}-${end} of ${totalCount} · Page ${feedbackReportsState.page}`;
        feedbackReportsPreviousButton.disabled = feedbackReportsState.page <= 1;
        feedbackReportsNextButton.disabled = items.length < FeedbackReportPageSize || feedbackReportsState.page * FeedbackReportPageSize >= totalCount;
    }

    async function loadFeedbackReports() {
        if (!hasAdminPermission(AdminPermissionIds.feedbackReportsRead)) { clearFeedbackReportsState(); return; }
        const status = FeedbackReportStatuses.includes(feedbackReportsStatusFilter.value) ? feedbackReportsStatusFilter.value : "";
        const category = FeedbackReportCategories.includes(feedbackReportsCategoryFilter.value) ? feedbackReportsCategoryFilter.value : "";
        const query = new URLSearchParams({ page: String(feedbackReportsState.page), pageSize: String(FeedbackReportPageSize) });
        if (status) { query.set("status", status); }
        if (category) { query.set("category", category); }
        feedbackReportsErrorElement.textContent = "";
        feedbackReportsLoadingElement.classList.remove("hidden");
        feedbackReportsPreviousButton.disabled = true;
        feedbackReportsNextButton.disabled = true;
        try {
            const payload = await adminFetch(`${ApiPaths.feedbackReports}?${query.toString()}`);
            feedbackReportsState.items = Array.isArray(payload?.items) ? payload.items : [];
            feedbackReportsState.totalCount = Number(payload?.totalCount) || 0;
            feedbackReportsState.page = Number(payload?.page) || feedbackReportsState.page;
            renderFeedbackReportsList();
        } catch (error) {
            feedbackReportsState.items = [];
            feedbackReportsState.totalCount = 0;
            if (error instanceof Error && error.message === NotAvailableForRoleMessage) { clearFeedbackReportDetails(); }
            feedbackReportsListElement.textContent = "";
            feedbackReportsSummaryElement.textContent = "No reports loaded.";
            feedbackReportsErrorElement.textContent = error instanceof Error && error.message === NotAvailableForRoleMessage ? NotAvailableForRoleMessage : "Unable to load feedback reports. Please try again.";
        } finally {
            feedbackReportsLoadingElement.classList.add("hidden");
        }
    }

    function appendFeedbackReportDetail(container, label, value, fullWidth = false) {
        const section = document.createElement("section");
        section.className = `${fullWidth ? "feedback-report-full-width " : ""}${label === "User ID" ? "feedback-report-secondary" : ""}`.trim();
        const heading = document.createElement("h3");
        heading.textContent = label;
        const content = document.createElement("p");
        content.textContent = value;
        section.append(heading, content);
        container.appendChild(section);
    }

    function appendFeedbackReportBody(container, label, value) {
        const section = document.createElement("section");
        section.className = "feedback-report-full-width";
        const heading = document.createElement("h3");
        heading.textContent = label;
        const content = document.createElement("pre");
        content.textContent = value;
        section.append(heading, content);
        container.appendChild(section);
    }

    function renderFeedbackReportReplyHistory(report) {
        const canRead = hasAdminPermission(AdminPermissionIds.feedbackReportsRead);
        feedbackReportReplyHistoryElement.classList.toggle("hidden", !canRead);
        feedbackReportReplyHistoryContentElement.textContent = "";
        if (!canRead) { return; }
        const replies = Array.isArray(report?.replies) ? report.replies : [];
        if (replies.length === 0) {
            appendFeedbackReportText(feedbackReportReplyHistoryContentElement, "No replies have been sent yet", "empty-state");
            return;
        }
        const list = document.createElement("div");
        list.className = "feedback-report-reply-history-list";
        replies.forEach((reply) => {
            const item = document.createElement("article");
            item.className = "feedback-report-reply-history-item";
            const heading = document.createElement("h4");
            heading.textContent = `Delivery status: ${feedbackReportReplyDeliveryStatusLabel(String(reply?.deliveryStatus || ""))}`;
            const created = document.createElement("p");
            created.className = "feedback-report-reply-history-meta";
            created.textContent = `Created: ${formatFeedbackReportDate(reply?.createdAtUtc)}`;
            const recipient = document.createElement("p");
            recipient.className = "feedback-report-reply-history-meta";
            recipient.textContent = `Recipient: ${String(reply?.recipientEmail || "-")}`;
            const text = document.createElement("p");
            text.className = "feedback-report-reply-history-text";
            text.textContent = String(reply?.replyText || "");
            item.append(heading, created, recipient);
            if (reply?.sentAtUtc) {
                const sent = document.createElement("p");
                sent.className = "feedback-report-reply-history-meta";
                sent.textContent = `Sent: ${formatFeedbackReportDate(reply.sentAtUtc)}`;
                item.appendChild(sent);
            }
            if (reply?.failureCode) {
                const failure = document.createElement("p");
                failure.className = "feedback-report-reply-history-failure";
                failure.textContent = feedbackReportReplyFailureLabel(String(reply.failureCode));
                item.appendChild(failure);
            }
            item.appendChild(text);
            list.appendChild(item);
        });
        feedbackReportReplyHistoryContentElement.appendChild(list);
    }

    function renderFeedbackReportDetails(report) {
        feedbackReportsState.selectedReport = report;
        feedbackReportDetailsElement.textContent = "";
        feedbackReportDetailsElement.className = "feedback-report-details";
        appendFeedbackReportDetail(feedbackReportDetailsElement, "Type", feedbackReportCategoryLabel(report?.category));
        appendFeedbackReportDetail(feedbackReportDetailsElement, "Status", feedbackReportStatusLabel(report?.status));
        appendFeedbackReportBody(feedbackReportDetailsElement, report?.category === "account_deletion" ? "Deletion reason" : "Report message", String(report?.message || "No reason provided."));
        if (String(report?.reportedAiText || "").trim()) { appendFeedbackReportBody(feedbackReportDetailsElement, "Reported AI text", String(report.reportedAiText)); }
        appendFeedbackReportDetail(feedbackReportDetailsElement, "Created", formatFeedbackReportDate(report?.createdAtUtc));
        if (report?.reviewedAtUtc) { appendFeedbackReportDetail(feedbackReportDetailsElement, "Reviewed", formatFeedbackReportDate(report.reviewedAtUtc)); }
        appendFeedbackReportDetail(feedbackReportDetailsElement, "Platform", String(report?.clientPlatform || "-"));
        appendFeedbackReportDetail(feedbackReportDetailsElement, "Client version", String(report?.clientVersion || "-"));
        appendFeedbackReportDetail(feedbackReportDetailsElement, "User email", String(report?.user?.email || "-"));
        if (String(report?.user?.displayName || "").trim()) { appendFeedbackReportDetail(feedbackReportDetailsElement, "User display name", String(report.user.displayName)); }
        if (String(report?.user?.userId || "").trim()) { appendFeedbackReportDetail(feedbackReportDetailsElement, "User ID", String(report.user.userId)); }
        renderFeedbackReportActions(report);
        renderFeedbackReportReplyHistory(report);
    }

    function updateFeedbackReportReplyLength() {
        feedbackReportReplyLengthElement.textContent = `${feedbackReportReplyTextInput.value.length} of 4000 characters`;
    }

    function renderFeedbackReportActions(report) {
        const canManageStatus = hasAdminPermission(AdminPermissionIds.feedbackReportsStatusManage);
        const canReply = hasAdminPermission(AdminPermissionIds.feedbackReportsReply);
        const status = String(report?.status || "");
        feedbackReportStatusActionsElement.classList.toggle("hidden", !canManageStatus || feedbackReportsState.statusPermissionDenied);
        feedbackReportReplyActionsElement.classList.toggle("hidden", !canReply || feedbackReportsState.replyPermissionDenied);
        if (canManageStatus) {
            feedbackReportCurrentStatusElement.textContent = `Current status: ${feedbackReportStatusLabel(status)}`;
            feedbackReportStatusButtonsElement.textContent = "";
            const actions = status === "new" ? [["reviewed", "Mark reviewed"], ["needs_information", "Needs information"], ["processing", "Mark processing"], ["resolved", "Resolve"], ["rejected", "Reject"]] : (status === "reviewed" || status === "needs_information" ? [["processing", "Mark processing"], ["resolved", "Resolve"], ["rejected", "Reject"]] : (status === "processing" ? [["resolved", "Resolve"], ["rejected", "Reject"]] : ((status === "resolved" || status === "rejected") ? [["reviewed", "Reopen as reviewed"]] : []));
            actions.forEach(([targetStatus, label]) => {
                const button = document.createElement("button");
                button.type = "button";
                button.textContent = label;
                button.disabled = feedbackReportsState.statusRequestPending;
                button.addEventListener("click", async () => { await changeFeedbackReportStatus(targetStatus); });
                feedbackReportStatusButtonsElement.appendChild(button);
            });
        }
        if (canReply) {
            const recipientEmail = String(report?.user?.email || "").trim();
            const unavailable = !recipientEmail || feedbackReportsState.replyUnavailable;
            feedbackReportReplyRecipientElement.textContent = recipientEmail ? `Recipient: ${recipientEmail}` : "No recipient email is available for this report.";
            feedbackReportReplyTextInput.disabled = unavailable || feedbackReportsState.replyRequestPending;
            feedbackReportSendReplyButton.disabled = unavailable || feedbackReportsState.replyRequestPending;
            updateFeedbackReportReplyLength();
        }
    }

    function applyFeedbackReportMutation(response, isReply = false) {
        const report = feedbackReportsState.selectedReport;
        if (!report) { return; }
        const nextStatus = String(isReply ? response?.reportStatus : response?.status || "");
        if (FeedbackReportStatuses.includes(nextStatus)) { report.status = nextStatus; }
        if (Object.prototype.hasOwnProperty.call(response || {}, "reviewedAtUtc")) { report.reviewedAtUtc = response.reviewedAtUtc; }
        const selectedId = String(feedbackReportsState.selectedReportId || "");
        feedbackReportsState.items = feedbackReportsState.items.map((item) => String(item?.reportId || "") === selectedId ? Object.assign({}, item, { status: report.status }) : item);
        renderFeedbackReportsList();
        renderFeedbackReportDetails(report);
    }

    async function refreshFeedbackReportsAfterMutation() {
        const filter = feedbackReportsStatusFilter.value;
        const selectedStatus = String(feedbackReportsState.selectedReport?.status || "");
        if (filter && filter !== selectedStatus) { feedbackReportsState.page = 1; await loadFeedbackReports(); }
    }

    async function changeFeedbackReportStatus(targetStatus) {
        const reportId = String(feedbackReportsState.selectedReportId || "");
        if (!reportId || feedbackReportsState.statusRequestPending || !hasAdminPermission(AdminPermissionIds.feedbackReportsStatusManage) || !["reviewed", "needs_information", "processing", "resolved", "rejected"].includes(targetStatus)) { return; }
        feedbackReportsState.statusRequestPending = true;
        feedbackReportStatusErrorElement.textContent = "";
        feedbackReportStatusSuccessElement.textContent = "";
        feedbackReportStatusProgressElement.textContent = "Updating status...";
        feedbackReportStatusProgressElement.classList.remove("hidden");
        renderFeedbackReportActions(feedbackReportsState.selectedReport);
        try {
            const response = await fetch(ApiPaths.feedbackReportStatusTemplate.replace("{reportId}", encodeURIComponent(reportId)), { method: "PATCH", headers: getAdminHeaders({ "Content-Type": "application/json" }), body: JSON.stringify({ status: targetStatus }) });
            if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
            if (response.status === HttpStatus.forbidden) { feedbackReportsState.statusPermissionDenied = true; feedbackReportDetailsErrorElement.textContent = "You no longer have permission to manage report status."; return; }
            if (response.status === HttpStatus.notFound) { feedbackReportStatusErrorElement.textContent = "This report is no longer available."; return; }
            if (response.status === HttpStatus.badRequest) { feedbackReportStatusErrorElement.textContent = "The requested status change is not valid."; return; }
            if (!response.ok) { feedbackReportStatusErrorElement.textContent = "Unable to update report status. Please try again."; return; }
            applyFeedbackReportMutation(await response.json());
            feedbackReportStatusSuccessElement.textContent = "Report status updated.";
            await refreshFeedbackReportsAfterMutation();
        } finally {
            feedbackReportsState.statusRequestPending = false;
            feedbackReportStatusProgressElement.classList.add("hidden");
            if (feedbackReportsState.selectedReport) { renderFeedbackReportActions(feedbackReportsState.selectedReport); }
        }
    }

    async function sendFeedbackReportReply() {
        const reportId = String(feedbackReportsState.selectedReportId || "");
        const replyText = feedbackReportReplyTextInput.value.trim();
        if (!reportId || feedbackReportsState.replyRequestPending || !hasAdminPermission(AdminPermissionIds.feedbackReportsReply)) { return; }
        if (!replyText) { feedbackReportReplyErrorElement.textContent = "Reply text is required."; return; }
        if (replyText.length > 4000) { feedbackReportReplyErrorElement.textContent = "Reply text must be 4000 characters or fewer."; return; }
        feedbackReportsState.replyRequestPending = true;
        feedbackReportReplyErrorElement.textContent = "";
        feedbackReportReplySuccessElement.textContent = "";
        feedbackReportReplyProgressElement.textContent = "Sending reply...";
        feedbackReportReplyProgressElement.classList.remove("hidden");
        renderFeedbackReportActions(feedbackReportsState.selectedReport);
        try {
            const response = await fetch(ApiPaths.feedbackReportRepliesTemplate.replace("{reportId}", encodeURIComponent(reportId)), { method: "POST", headers: getAdminHeaders({ "Content-Type": "application/json" }), body: JSON.stringify({ replyText }) });
            if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
            if (response.status === HttpStatus.forbidden) { feedbackReportsState.replyPermissionDenied = true; feedbackReportDetailsErrorElement.textContent = "You no longer have permission to send replies."; return; }
            if (response.status === HttpStatus.notFound) { feedbackReportReplyErrorElement.textContent = "This report is no longer available."; return; }
            if (response.status === HttpStatus.badRequest) { feedbackReportReplyErrorElement.textContent = "The reply is not valid. Please check the text and try again."; return; }
            const payload = await response.json().catch(() => null);
            if (response.status === HttpStatus.conflict && payload?.error === "recipient_email_unavailable") { feedbackReportsState.replyUnavailable = true; feedbackReportReplyErrorElement.textContent = "No recipient email is available for this report."; return; }
            if (response.status === HttpStatus.serviceUnavailable) { if (payload?.reportStatus) { applyFeedbackReportMutation(payload, true); } await loadFeedbackReportDetails(reportId, true); await refreshFeedbackReportsAfterMutation(); feedbackReportReplyErrorElement.textContent = "Email delivery failed. Your reply text was kept."; return; }
            if (!response.ok) { feedbackReportReplyErrorElement.textContent = "Unable to send the reply. Please try again."; return; }
            applyFeedbackReportMutation(payload, true);
            feedbackReportReplyTextInput.value = "";
            updateFeedbackReportReplyLength();
            feedbackReportReplySuccessElement.textContent = "Reply sent.";
            await loadFeedbackReportDetails(reportId);
            await refreshFeedbackReportsAfterMutation();
        } finally {
            feedbackReportsState.replyRequestPending = false;
            feedbackReportReplyProgressElement.classList.add("hidden");
            if (feedbackReportsState.selectedReport) { renderFeedbackReportActions(feedbackReportsState.selectedReport); }
        }
    }

    async function loadFeedbackReportDetails(reportId, preserveReplyDraft = false) {
        if (!hasAdminPermission(AdminPermissionIds.feedbackReportsRead)) { clearFeedbackReportDetails(); return; }
        feedbackReportsState.selectedReportId = reportId;
        feedbackReportsState.selectedReport = null;
        feedbackReportsState.replyUnavailable = false;
        feedbackReportsState.statusPermissionDenied = false;
        feedbackReportsState.replyPermissionDenied = false;
        if (!preserveReplyDraft) { feedbackReportReplyTextInput.value = ""; }
        feedbackReportDetailsCard.classList.remove("hidden");
        feedbackReportDetailsElement.textContent = "";
        feedbackReportReplyHistoryElement.classList.add("hidden");
        feedbackReportReplyHistoryContentElement.textContent = "";
        feedbackReportDetailsErrorElement.textContent = "";
        feedbackReportDetailsLoadingElement.classList.remove("hidden");
        renderFeedbackReportsList();
        try {
            const path = ApiPaths.feedbackReportTemplate.replace("{reportId}", encodeURIComponent(reportId));
            const report = await adminFetch(path);
            if (feedbackReportsState.selectedReportId === reportId) { renderFeedbackReportDetails(report); }
        } catch (error) {
            if (feedbackReportsState.selectedReportId !== reportId) { return; }
            if (error instanceof Error && error.message === NotAvailableForRoleMessage) {
                feedbackReportsState.selectedReportId = null;
                feedbackReportDetailsElement.textContent = "";
                feedbackReportDetailsLoadingElement.classList.add("hidden");
                renderFeedbackReportsList();
            }
            feedbackReportDetailsErrorElement.textContent = error instanceof Error && error.status === HttpStatus.notFound ? "This report is no longer available." : (error instanceof Error && error.message === NotAvailableForRoleMessage ? NotAvailableForRoleMessage : "Unable to load this report. Please try again.");
        } finally {
            if (feedbackReportsState.selectedReportId === reportId) { feedbackReportDetailsLoadingElement.classList.add("hidden"); }
        }
    }

    async function adminFetch(path, options = {}) {
        const headers = getAdminHeaders(options.headers || {});
        if (options.body && !headers["Content-Type"]) { headers["Content-Type"] = "application/json"; }
        const response = await fetch(path, Object.assign({}, options, { headers }));
        if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
        if (response.status === HttpStatus.forbidden) { throw new Error(NotAvailableForRoleMessage); }
        if (response.status === HttpStatus.notFound) { const error = new Error("CMS item was not found."); error.status = HttpStatus.notFound; throw error; }
        if (response.status === HttpStatus.badRequest) {
            let payload = null;
            try { payload = await response.json(); } catch (_) { }
            const details = extractCmsBackendMessages(payload);
            const message = details.length > 0 ? details.join("\n") : (payload?.error || payload?.message || "CMS request is invalid. Check draft fields and validation messages.");
            const error = new Error(message);
            error.cmsPayload = payload;
            error.cmsDetails = details;
            throw error;
        }
        if (response.status === HttpStatus.conflict) { throw new Error("CMS request conflicted with current state."); }
        if (!response.ok) { throw new Error("CMS request failed."); }
        if (response.status === 204) { return null; }
        return response.json();
    }

    function setCmsStaticJsonInitializePanelVisible(visible) { if (cmsStaticJsonInitializePanel) { cmsStaticJsonInitializePanel.classList.toggle("hidden", !visible); } }

    function clearCmsContentPackSummary(slug) {
        cmsSummarySlugElement.textContent = formatValue(slug);
        cmsSummaryNameElement.textContent = "-";
        cmsSummaryStatusElement.textContent = "Not initialized";
        cmsSummaryTopicCountElement.textContent = "-";
        cmsSummaryScenarioCountElement.textContent = "-";
        cmsSummaryPromptTemplateCountElement.textContent = "-";
        cmsSummaryTutorProfileCountElement.textContent = "-";
        cmsSummaryPublishedVersionElement.textContent = "-";
    }

    function renderCmsContentPackSummary(summary) {
        setCmsStaticJsonInitializePanelVisible(false);
        cmsSummarySlugElement.textContent = formatValue(summary?.slug);
        cmsSummaryNameElement.textContent = formatValue(summary?.name);
        cmsSummaryStatusElement.textContent = formatValue(summary?.status);
        cmsSummaryTopicCountElement.textContent = formatValue(summary?.topicCount);
        cmsSummaryScenarioCountElement.textContent = formatValue(summary?.scenarioCount);
        cmsSummaryPromptTemplateCountElement.textContent = formatValue(summary?.promptTemplateCount);
        cmsSummaryTutorProfileCountElement.textContent = formatValue(summary?.tutorBehaviorProfileCount);
        cmsSummaryPublishedVersionElement.textContent = formatValue(summary?.currentPublishedVersionNumber);
    }

    function isCmsSubTabActive(tabId) { return cmsSubTabButtons.some((button) => button.dataset.cmsSubTabId === tabId && button.getAttribute("aria-selected") === "true"); }

    function selectCmsSubTab(tabId, force = false) {
        const selectedTabId = isKnownCmsSubTab(tabId) ? tabId : CmsSubTabs.overview;
        if (!force && selectedTabId !== getHashCmsSubTab() && !confirmDiscardUnsavedChanges()) { return; }
        if (selectedTabId === CmsSubTabs.versionsPublish) { clearCmsPublishErrorDetails(); setCmsError(""); }
        if (selectedTabId === CmsSubTabs.audit) { updateCmsAuditSmokeFilterStatus(); }
        updateAdminHash(getCurrentActiveTab(), selectedTabId);
        cmsSubTabButtons.forEach((button) => {
            const isActive = button.dataset.cmsSubTabId === selectedTabId;
            button.classList.toggle("active", isActive);
            button.setAttribute("aria-selected", isActive ? "true" : "false");
        });
        cmsSubPanels.forEach((panel) => {
            panel.classList.toggle("hidden", panel.dataset.cmsSubPanel !== selectedTabId);
        });
    }

    function getNormalizedFilter(value) { return String(value || "").trim().toLowerCase(); }
    function valueContains(value, filter) { return String(value || "").toLowerCase().includes(filter); }

    function updateCmsScenarioTopicFilterOptions() {
        const selected = cmsScenarioTopicFilterSelect.value;
        const topicKeys = Array.from(new Set(cmsScenarios.map((scenario) => scenario.topicKey).filter(Boolean))).sort((left, right) => String(left).localeCompare(String(right)));
        cmsScenarioTopicFilterSelect.textContent = "";
        const allOption = document.createElement("option");
        allOption.value = "";
        allOption.textContent = "All topics";
        cmsScenarioTopicFilterSelect.appendChild(allOption);
        topicKeys.forEach((topicKey) => { const option = document.createElement("option"); option.value = topicKey; option.textContent = topicKey; cmsScenarioTopicFilterSelect.appendChild(option); });
        cmsScenarioTopicFilterSelect.value = topicKeys.includes(selected) ? selected : "";
    }

    function getFilteredCmsTopics() {
        const filter = getNormalizedFilter(cmsTopicFilterInput.value);
        if (!filter) { return cmsTopics; }
        return cmsTopics.filter((topic) => valueContains(topic.stableTopicKey, filter) || valueContains(topic.title, filter));
    }

    function getFilteredCmsScenarios() {
        const filter = getNormalizedFilter(cmsScenarioFilterInput.value);
        const topicFilter = cmsScenarioTopicFilterSelect.value;
        return cmsScenarios.filter((scenario) => {
            const matchesText = !filter || valueContains(scenario.stableScenarioKey, filter) || valueContains(scenario.topicKey, filter) || valueContains(scenario.title, filter);
            const matchesTopic = !topicFilter || scenario.topicKey === topicFilter;
            return matchesText && matchesTopic;
        });
    }

    function renderCmsTopicsTable() {
        renderCmsTable(cmsTopicsListElement, [{ key: "stableTopicKey", label: "stableTopicKey" }, { key: "title", label: "Title" }, { key: "sortOrder", label: "Sort" }, { key: "isActive", label: "Active" }], getFilteredCmsTopics(), { onSelect: selectCmsTopic, selectedId: cmsSelectedTopic?.id });
    }

    function renderCmsScenariosTable() {
        renderCmsTable(cmsScenariosListElement, [{ key: "stableScenarioKey", label: "stableScenarioKey" }, { key: "topicKey", label: "Topic key" }, { key: "title", label: "Title" }, { key: "isActive", label: "Active" }], getFilteredCmsScenarios(), { onSelect: selectCmsScenario, selectedId: cmsSelectedScenario?.id });
    }

    function renderCmsLevelsTable() {
        renderCmsTable(cmsLevelsListElement, [{ key: "stableLevelKey", label: "Level" }, { key: "displayName", label: "Name" }, { key: "wrapUpAfterUserTurn", label: "Wrap" }, { key: "finalMessageAtUserTurn", label: "Final" }, { key: "isActive", label: "Active" }], cmsLevels, { onSelect: selectCmsLevel, selectedId: cmsSelectedLevel?.stableLevelKey, emptyMessage: "No saved level profiles found. Use Initialize default levels, then Save draft and publish." });
    }

    function renderCmsPromptTemplatesTable() {
        renderCmsTable(cmsPromptTemplatesListElement, [{ key: "templateKey", label: "templateKey" }, { key: "targetStudyLanguageId", label: "Study language" }, { key: "isActive", label: "Active" }], cmsPromptTemplates, { onSelect: selectCmsPromptTemplate, selectedId: cmsSelectedPromptTemplate?.id });
    }

    function renderCmsTutorProfilesTable() {
        renderCmsTable(cmsTutorProfilesListElement, [{ key: "tutorId", label: "tutorId" }, { key: "displayName", label: "Display name" }, { key: "isActive", label: "Active" }], cmsTutorProfiles, { onSelect: selectCmsTutorProfile, selectedId: cmsSelectedTutorProfile?.id });
    }

    function renderCmsTable(container, columns, rows, options) {
        container.textContent = "";
        const config = typeof options === "function" ? { onSelect: options } : (options || {});
        if (!Array.isArray(rows) || rows.length === 0) { const empty = document.createElement("p"); empty.className = "empty-state"; empty.textContent = config.emptyMessage || "No items loaded."; container.appendChild(empty); return; }
        const onSelect = config.onSelect;
        const selectedId = config.selectedId;
        const hasAction = typeof onSelect === "function";
        const table = document.createElement("table"); table.className = "compact-table cms-table cms-selectable-table";
        const thead = document.createElement("thead"); const headRow = document.createElement("tr");
        if (hasAction) { const th = document.createElement("th"); th.className = "cms-action-column"; th.scope = "col"; th.textContent = "Select"; headRow.appendChild(th); }
        columns.forEach((column) => { const th = document.createElement("th"); th.scope = "col"; th.textContent = column.label; headRow.appendChild(th); });
        thead.appendChild(headRow); table.appendChild(thead);
        const tbody = document.createElement("tbody");
        rows.forEach((row) => {
            const tr = document.createElement("tr");
            const isSelected = selectedId !== undefined && selectedId !== null && row?.id === selectedId;
            if (hasAction) {
                tr.className = "cms-selectable-row";
                tr.tabIndex = 0;
                tr.setAttribute("aria-label", "Select CMS row");
                tr.setAttribute("aria-current", isSelected ? "true" : "false");
                tr.classList.toggle("cms-selected-row", isSelected);
                tr.addEventListener("click", (event) => {
                    if (event.target.closest("button, a, input, select, textarea, label")) { return; }
                    onSelect(row);
                });
                tr.addEventListener("keydown", (event) => {
                    if (event.key !== "Enter" && event.key !== " ") { return; }
                    if (event.target.closest("button, a, input, select, textarea, label")) { return; }
                    event.preventDefault();
                    onSelect(row);
                });
                const actionTd = document.createElement("td"); actionTd.className = "cms-action-column";
                const button = document.createElement("button"); button.type = "button"; button.className = "small-button cms-select-button"; button.textContent = isSelected ? "Selected" : "Select"; button.setAttribute("aria-pressed", isSelected ? "true" : "false");
                button.addEventListener("click", (event) => { event.stopPropagation(); onSelect(row); });
                actionTd.appendChild(button); tr.appendChild(actionTd);
            }
            columns.forEach((column) => { const td = document.createElement("td"); td.textContent = formatValue(row[column.key]); if (column.className) { td.className = column.className; } const titleKey = column.titleKey || (column.useFullValueTitle ? column.key : null); if (titleKey && row[titleKey]) { td.title = String(row[titleKey]); } tr.appendChild(td); });
            tbody.appendChild(tr);
        });
        table.appendChild(tbody); container.appendChild(table);
    }

    async function loadCmsContentPacks() {
        setCmsError(""); setCmsSuccess(""); setCmsLoading(true);
        try {
            const packs = await adminFetch(ApiPaths.cmsContentPacks);
            cmsContentPackSelect.textContent = "";
            (Array.isArray(packs) ? packs : []).forEach((pack) => { const option = document.createElement("option"); option.value = pack.slug; option.textContent = `${pack.slug} / ${pack.name || "-"}`; cmsContentPackSelect.appendChild(option); });
            if (![...cmsContentPackSelect.options].some((option) => option.value === "static-json-v1")) { const option = document.createElement("option"); option.value = "static-json-v1"; option.textContent = "static-json-v1"; cmsContentPackSelect.appendChild(option); }
            const hashSlug = getHashValue("contentPackSlug");
            cmsContentPackSelect.value = [...cmsContentPackSelect.options].some((option) => option.value === hashSlug) ? hashSlug : ([...cmsContentPackSelect.options].some((option) => option.value === "static-json-v1") ? "static-json-v1" : (cmsContentPackSelect.options[0]?.value || "static-json-v1"));
            updateHashField("contentPackSlug", cmsContentPackSelect.value);
            cmsHasLoadedOnce = true;
            await refreshCmsContentPack(true);
            setCmsSuccess("CMS content pack list loaded.");
        } catch (error) { handleCmsError(error); }
        finally { setCmsLoading(false); }
    }

    async function refreshCmsContentPack(restoreSelection = false) {
        if (!restoreSelection && !confirmDiscardUnsavedChanges()) { return false; }
        const slug = getSelectedCmsSlug();
        updateHashField("contentPackSlug", slug);
        let summary;
        try {
            summary = await adminFetch(cmsPath(ApiPaths.cmsContentPackTemplate, { slug }));
        } catch (error) {
            if (error.status === HttpStatus.notFound && slug === "static-json-v1") {
                clearCmsContentPackSummary(slug);
                setCmsStaticJsonInitializePanelVisible(true);
                setCmsError("Content pack static-json-v1 has not been initialized in CMS yet. Use Initialize from static JSON to prepare CMS draft content. Learner runtime is not changed.");
                return false;
            }
            throw error;
        }
        renderCmsContentPackSummary(summary);
        await Promise.all([loadCmsTopics(), loadCmsScenarios(), loadCmsPromptTemplates(), loadCmsTutorProfiles(), loadCmsVersions(), loadCmsAuditEntries()]);
        clearAllCmsDirtyState();
        await restoreCmsSelectionsFromHash();
        return true;
    }

    async function restoreCmsSelectionsFromHash() {
        restoringCmsSelection = true;
        try {
            await restoreCmsSelectionByKey("topicKey", cmsTopics, "stableTopicKey", selectCmsTopic, "topic");
            await restoreCmsSelectionByKey("scenarioKey", cmsScenarios, "stableScenarioKey", selectCmsScenario, "scenario");
            await restoreCmsSelectionByKey("promptTemplateKey", cmsPromptTemplates, "templateKey", selectCmsPromptTemplate, "prompt template");
            await restoreCmsSelectionByKey("tutorId", cmsTutorProfiles, "tutorId", selectCmsTutorProfile, "tutor profile");
        } finally { restoringCmsSelection = false; }
    }

    async function restoreCmsSelectionByKey(hashKey, list, rowKey, selectFunction, label) {
        const selectedKey = getHashValue(hashKey);
        if (!selectedKey) { return; }
        const row = (Array.isArray(list) ? list : []).find((item) => String(item?.[rowKey] || "") === selectedKey);
        if (!row) { setCmsError(`Previously selected CMS ${label} '${selectedKey}' no longer exists. The list remains loaded.`); updateHashField(hashKey, null); return; }
        await selectFunction(row, true);
    }

    async function loadCmsTopics() {
        const slug = getSelectedCmsSlug();
        cmsTopics = await adminFetch(cmsPath(ApiPaths.cmsTopicsTemplate, { slug }));
        cmsTopics = Array.isArray(cmsTopics) ? cmsTopics : [];
        renderCmsTopicsTable();
    }
    async function loadCmsScenarios() {
        const slug = getSelectedCmsSlug();
        cmsScenarios = await adminFetch(cmsPath(ApiPaths.cmsScenariosTemplate, { slug }));
        cmsScenarios = Array.isArray(cmsScenarios) ? cmsScenarios : [];
        updateCmsScenarioTopicFilterOptions();
        renderCmsScenariosTable();
    }
    async function loadCmsPromptTemplates() {
        const slug = getSelectedCmsSlug();
        cmsPromptTemplates = await adminFetch(cmsPath(ApiPaths.cmsPromptTemplatesTemplate, { slug }));
        cmsPromptTemplates = Array.isArray(cmsPromptTemplates) ? cmsPromptTemplates : [];
        renderCmsPromptTemplatesTable();
        loadCmsLevelsFromPromptTemplate();
    }
    async function loadCmsTutorProfiles() {
        const slug = getSelectedCmsSlug();
        cmsTutorProfiles = await adminFetch(cmsPath(ApiPaths.cmsTutorProfilesTemplate, { slug }));
        cmsTutorProfiles = Array.isArray(cmsTutorProfiles) ? cmsTutorProfiles : [];
        renderCmsTutorProfilesTable();
    }

    async function selectCmsTopic(row, force = false) { if (!force && !restoringCmsSelection && cmsSelectedTopic?.id !== row.id && !confirmDiscardUnsavedChanges()) { return; } cmsSelectedTopic = row; updateHashField("topicKey", row.stableTopicKey || null); renderCmsTopicsTable(); cmsSelectedTopic = await adminFetch(cmsPath(ApiPaths.cmsTopicTemplate, { slug: getSelectedCmsSlug(), topicId: row.id })); fillCmsTopicForm(); renderCmsTopicsTable(); }
    function fillCmsTopicForm() { const item = cmsSelectedTopic; cmsSelectedTopicIdentityElement.textContent = item ? `${item.stableTopicKey} (${item.id})` : "None selected"; cmsTopicTitleInput.value = item?.title || ""; cmsTopicDescriptionInput.value = item?.description || ""; cmsTopicSortOrderInput.value = item?.sortOrder ?? ""; cmsTopicIsActiveInput.checked = Boolean(item?.isActive); setCmsEntityMessage(cmsTopicMessageElement, "", false); hideCmsPublishDiscovery(); setCmsBaseline("topic"); }
    async function selectCmsScenario(row, force = false) { if (!force && !restoringCmsSelection && cmsSelectedScenario?.id !== row.id && !confirmDiscardUnsavedChanges()) { return; } cmsSelectedScenario = row; updateHashField("scenarioKey", row.stableScenarioKey || null); renderCmsScenariosTable(); cmsSelectedScenario = await adminFetch(cmsPath(ApiPaths.cmsScenarioTemplate, { slug: getSelectedCmsSlug(), scenarioId: row.id })); fillCmsScenarioForm(); renderCmsScenariosTable(); }
    function fillCmsScenarioForm() { const item = cmsSelectedScenario; cmsSelectedScenarioIdentityElement.textContent = item ? `${item.stableScenarioKey} (${item.id})` : "None selected"; cmsScenarioTitleInput.value = item?.title || ""; cmsScenarioDescriptionInput.value = item?.description || ""; cmsScenarioSetupMessageInput.value = item?.setupMessage || ""; cmsScenarioIsActiveInput.checked = Boolean(item?.isActive); cmsScenarioDefinitionJsonInput.value = item?.definitionJson || ""; setCmsScenarioJsonStatus(item?.isDefinitionJsonFallback ? "Showing fallback JSON built from existing draft fields; save draft to persist it as full scenario JSON." : "", Boolean(item?.isDefinitionJsonFallback)); fillCmsStructuredScenarioFieldsFromDefinition(); setCmsEntityMessage(cmsScenarioMessageElement, "", false); hideCmsPublishDiscovery(); setCmsBaseline("scenario"); }
    function mergeMissingDefaultCmsLevels(levels) {
        const merged = Array.isArray(levels) ? levels.map(level => ({ ...level })) : [];
        CmsDefaultLevelProfiles.forEach(defaultLevel => {
            if (!merged.some(level => String(level.stableLevelKey || "").toLowerCase() === defaultLevel.stableLevelKey)) {
                merged.push({ ...defaultLevel });
            }
        });
        merged.forEach(level => { level.id = level.stableLevelKey; });
        merged.sort((a, b) => (Number(a.sortOrder || 0) - Number(b.sortOrder || 0)) || String(a.stableLevelKey || "").localeCompare(String(b.stableLevelKey || "")));
        return merged;
    }

    function setCmsLevelsDraftReady(levels, message) {
        cmsLevels = mergeMissingDefaultCmsLevels(levels);
        renderCmsLevelsTable();
        if (cmsLevels.length > 0) { selectCmsLevel(cmsLevels[0], true); }
        setCmsEntityMessage(cmsLevelMessageElement, message, false);
    }

    function loadCmsLevelsFromPromptTemplate() {
        const template = cmsPromptTemplates.find(item => item.templateKey === "level_profiles");
        if (!template) { setCmsLevelsDraftReady([], "No saved level_profiles template exists for this draft. Default required levels are shown so you can Save draft, then publish through Versions & Publish."); return; }
        adminFetch(cmsPath(ApiPaths.cmsPromptTemplateTemplate, { slug: getSelectedCmsSlug(), templateId: template.id })).then(full => {
            let parsed = [];
            try { parsed = JSON.parse(full.body || "[]"); } catch { parsed = []; }
            cmsLevels = mergeMissingDefaultCmsLevels(parsed);
            renderCmsLevelsTable();
            if (!cmsSelectedLevel && cmsLevels.length > 0) selectCmsLevel(cmsLevels[0], true);
            if (cmsLevels.length !== (Array.isArray(parsed) ? parsed.length : 0)) {
                setCmsEntityMessage(cmsLevelMessageElement, "Missing required levels were added as draft-ready defaults. Save draft, then publish through Versions & Publish before runtime uses them.", false);
            }
        }).catch(handleCmsError);
    }

    function selectCmsLevel(row, force = false) {
        if (!force && cmsSelectedLevel?.stableLevelKey !== row.stableLevelKey && !confirmDiscardUnsavedChanges()) { return; }
        cmsSelectedLevel = row;
        cmsSelectedLevelIdentityElement.textContent = row ? `${row.stableLevelKey} (${row.displayName || "unnamed"})` : "None selected";
        cmsLevelDisplayNameInput.value = row?.displayName || "";
        cmsLevelSortOrderInput.value = row?.sortOrder ?? 0;
        cmsLevelWrapUpTurnInput.value = row?.wrapUpAfterUserTurn ?? "";
        cmsLevelFinalTurnInput.value = row?.finalMessageAtUserTurn ?? "";
        cmsLevelComplexityGuidanceInput.value = row?.botLanguageComplexityGuidance || "";
        cmsLevelCorrectionGuidanceInput.value = row?.correctionGuidance || "";
        cmsLevelAnswerGuidanceInput.value = row?.answerLengthGuidance || "";
        cmsLevelAdminNotesInput.value = row?.adminNotes || "";
        cmsLevelIsActiveInput.checked = Boolean(row?.isActive);
        setCmsEntityMessage(cmsLevelMessageElement, "", false);
        setCmsBaseline("level");
        renderCmsLevelsTable();
    }

    async function selectCmsPromptTemplate(row, force = false) { if (!force && !restoringCmsSelection && cmsSelectedPromptTemplate?.id !== row.id && !confirmDiscardUnsavedChanges()) { return; } cmsSelectedPromptTemplate = row; updateHashField("promptTemplateKey", row.templateKey || null); renderCmsPromptTemplatesTable(); cmsSelectedPromptTemplate = await adminFetch(cmsPath(ApiPaths.cmsPromptTemplateTemplate, { slug: getSelectedCmsSlug(), templateId: row.id })); fillCmsPromptTemplateForm(); renderCmsPromptTemplatesTable(); }
    function fillCmsPromptTemplateForm() { const item = cmsSelectedPromptTemplate; cmsSelectedPromptTemplateIdentityElement.textContent = item ? `${item.templateKey} (${item.id})` : "None selected"; cmsPromptTemplateBodyInput.value = item?.body || ""; cmsPromptTemplateIsActiveInput.checked = Boolean(item?.isActive); setCmsEntityMessage(cmsPromptTemplateMessageElement, "", false); hideCmsPublishDiscovery(); setCmsBaseline("promptTemplate"); }
    async function selectCmsTutorProfile(row, force = false) { if (!force && !restoringCmsSelection && cmsSelectedTutorProfile?.id !== row.id && !confirmDiscardUnsavedChanges()) { return; } cmsSelectedTutorProfile = row; updateHashField("tutorId", row.tutorId || null); renderCmsTutorProfilesTable(); cmsSelectedTutorProfile = await adminFetch(cmsPath(ApiPaths.cmsTutorProfileTemplate, { slug: getSelectedCmsSlug(), profileId: row.id })); fillCmsTutorProfileForm(); renderCmsTutorProfilesTable(); }
    function fillCmsTutorProfileForm() { const item = cmsSelectedTutorProfile; cmsSelectedTutorProfileIdentityElement.textContent = item ? `${item.tutorId} (${item.id})` : "None selected"; cmsTutorProfileDisplayNameInput.value = item?.displayName || ""; cmsTutorProfileCommunicationStyleJsonInput.value = item?.communicationStyleJson || ""; cmsTutorProfileSafetyNotesJsonInput.value = item?.safetyNotesJson || ""; cmsTutorProfileIsActiveInput.checked = Boolean(item?.isActive); setCmsEntityMessage(cmsTutorProfileMessageElement, "", false); hideCmsPublishDiscovery(); setCmsBaseline("tutorProfile"); }

    async function saveCmsTopicDraft() {
        if (!cmsSelectedTopic) { setCmsEntityMessage(cmsTopicMessageElement, "Select a topic first.", true); return; }
        await saveCmsDraft(ApiPaths.cmsTopicTemplate, { topicId: cmsSelectedTopic.id }, { title: cmsTopicTitleInput.value, description: cmsTopicDescriptionInput.value, sortOrder: Number(cmsTopicSortOrderInput.value || 0), isActive: cmsTopicIsActiveInput.checked, reason: "Admin CMS UI shell draft edit" }, cmsTopicMessageElement, loadCmsTopics, () => selectCmsTopic(cmsSelectedTopic));
    }
    async function saveCmsScenarioDraft(submitter = null) {
        if (!cmsSelectedScenario) { setCmsEntityMessage(cmsScenarioMessageElement, "Select a scenario first.", true); return; }
        const isAdvancedJsonSave = submitter?.id === "cms-scenario-save-button";
        if (isAdvancedJsonSave) {
            if (!validateCmsScenarioJsonInput()) { setCmsEntityMessage(cmsScenarioMessageElement, "Save draft blocked: fix Advanced JSON first.", true); return; }
        } else {
            if (!validateCmsStructuredScenarioInput()) { setCmsEntityMessage(cmsScenarioMessageElement, "Save draft blocked: fix structured scenario fields first.", true); return; }
            if (!validateCmsScenarioJsonInput()) { setCmsEntityMessage(cmsScenarioMessageElement, "Save draft blocked: fix Advanced JSON first.", true); return; }
        }
        await saveCmsDraft(ApiPaths.cmsScenarioTemplate, { scenarioId: cmsSelectedScenario.id }, { title: cmsScenarioTitleInput.value, description: cmsScenarioDescriptionInput.value, setupMessage: cmsScenarioSetupMessageInput.value, definitionJson: cmsScenarioDefinitionJsonInput.value, structuredScenarioFieldsEdited: !isAdvancedJsonSave, isActive: cmsScenarioIsActiveInput.checked, reason: isAdvancedJsonSave ? "Admin CMS UI shell advanced JSON scenario draft edit" : "Admin CMS UI shell structured scenario draft edit" }, cmsScenarioMessageElement, loadCmsScenarios, () => selectCmsScenario(cmsSelectedScenario));
        showScenarioDraftSavedPublishCallouts(true);
    }
    async function saveCmsLevelDraft() {
        if (!cmsSelectedLevel) { setCmsEntityMessage(cmsLevelMessageElement, "Select a level first.", true); return; }
        const template = cmsPromptTemplates.find(item => item.templateKey === "level_profiles");
        const updated = cmsLevels.map(level => level.stableLevelKey === cmsSelectedLevel.stableLevelKey ? { ...level, displayName: cmsLevelDisplayNameInput.value, sortOrder: Number(cmsLevelSortOrderInput.value || 0), wrapUpAfterUserTurn: Number(cmsLevelWrapUpTurnInput.value || 0), finalMessageAtUserTurn: Number(cmsLevelFinalTurnInput.value || 0), botLanguageComplexityGuidance: cmsLevelComplexityGuidanceInput.value, correctionGuidance: cmsLevelCorrectionGuidanceInput.value, answerLengthGuidance: cmsLevelAnswerGuidanceInput.value, adminNotes: cmsLevelAdminNotesInput.value, isActive: cmsLevelIsActiveInput.checked } : level);
        await saveCmsDraft(ApiPaths.cmsPromptTemplateTemplate, { templateId: template?.id || "level_profiles" }, { body: JSON.stringify(updated, null, 2), isActive: true, reason: "Admin CMS UI level profile draft edit" }, cmsLevelMessageElement, loadCmsPromptTemplates, () => { cmsLevels = updated; selectCmsLevel(updated.find(level => level.stableLevelKey === cmsSelectedLevel.stableLevelKey), true); });
    }

    function initializeDefaultCmsLevels() {
        if (!confirmDiscardUnsavedChanges()) { return; }
        setCmsLevelsDraftReady(cmsLevels, "Default required A1/A2/B1/B2 level profiles are ready. Click Save draft to persist them, then publish through Versions & Publish before runtime uses them.");
        updateCmsDirtyState("level");
    }

    async function saveCmsPromptTemplateDraft() {
        if (!cmsSelectedPromptTemplate) { setCmsEntityMessage(cmsPromptTemplateMessageElement, "Select a prompt template first.", true); return; }
        await saveCmsDraft(ApiPaths.cmsPromptTemplateTemplate, { templateId: cmsSelectedPromptTemplate.id }, { body: cmsPromptTemplateBodyInput.value, isActive: cmsPromptTemplateIsActiveInput.checked, reason: "Admin CMS UI shell draft edit" }, cmsPromptTemplateMessageElement, loadCmsPromptTemplates, () => selectCmsPromptTemplate(cmsSelectedPromptTemplate));
    }
    async function saveCmsTutorProfileDraft() {
        if (!cmsSelectedTutorProfile) { setCmsEntityMessage(cmsTutorProfileMessageElement, "Select a tutor profile first.", true); return; }
        await saveCmsDraft(ApiPaths.cmsTutorProfileTemplate, { profileId: cmsSelectedTutorProfile.id }, { displayName: cmsTutorProfileDisplayNameInput.value, communicationStyleJson: cmsTutorProfileCommunicationStyleJsonInput.value, safetyNotesJson: cmsTutorProfileSafetyNotesJsonInput.value, isActive: cmsTutorProfileIsActiveInput.checked, reason: "Admin CMS UI shell draft edit" }, cmsTutorProfileMessageElement, loadCmsTutorProfiles, () => selectCmsTutorProfile(cmsSelectedTutorProfile));
    }
    async function saveCmsDraft(template, replacements, body, messageElement, reloadList, reloadSelected) {
        setCmsError(""); setCmsSuccess(""); hideCmsPublishDiscovery(); setCmsEntityMessage(messageElement, "Saving draft...", false);
        try {
            const payload = await adminFetch(cmsPath(template, Object.assign({ slug: getSelectedCmsSlug() }, replacements)), { method: "PUT", body: JSON.stringify(body) });
            const changedFields = (payload.changedFields || []).join(", ") || "-";
            const detail = payload.noChanges ? "No draft field changes were detected." : `Changed fields: ${changedFields}.`;
            await reloadList(); await reloadSelected(); await runCmsValidation(); await loadCmsPreviewSummary();
            setCmsEntityMessage(messageElement, `Draft saved. To apply this content to runtime, publish the current draft. ${detail}`, false);
            showCmsPublishDiscoveryForMessage(messageElement, !payload.noChanges);
            if (isCmsSubTabActive(CmsSubTabs.audit)) { await loadCmsAuditEntries(); }
            else { setCmsSuccess("Open Audit to view the saved audit entry. Use Go to Publish to open Versions & Publish when you are ready to publish current draft changes."); }
        } catch (error) { const message = getCmsErrorMessage(error); setCmsEntityMessage(messageElement, message, true); if (isAuthErrorMessage(message)) { resetSession(); setError(message); } }
    }


    async function initializeStaticJsonContentPack() {
        setCmsError(""); setCmsSuccess(""); setCmsLoading(true);
        try {
            const result = await adminFetch(ApiPaths.cmsStaticJsonV1Initialize, { method: "POST" });
            const messages = Array.isArray(result?.messages) ? result.messages : [];
            setCmsSuccess(messages.length > 0 ? messages.join(" ") : "Content pack initialized. Learner runtime remains static JSON; no publish was performed.");
            cmsContentPackSelect.value = "static-json-v1";
            updateHashField("contentPackSlug", "static-json-v1");
            await refreshCmsContentPack(true);
        } catch (error) { handleCmsError(error); }
        finally { setCmsLoading(false); }
    }

    async function runCmsValidation() {
        setCmsError("");
        try {
            const validation = await adminFetch(cmsPath(ApiPaths.cmsValidateTemplate, { slug: getSelectedCmsSlug() }), { method: "POST" });
            renderCmsValidationResult(validation);
            if (!getCmsResponseValue(validation, "success", false)) {
                setCmsError(`Validation failed with ${getCmsResponseArray(validation, "errors").length} errors and ${getCmsResponseArray(validation, "warnings").length} warnings.`);
            }
            return validation;
        }
        catch (error) {
            clearCmsResultPanel(cmsValidationResultElement, "Validation could not be loaded.");
            handleCmsError(error);
            return null;
        }
    }
    async function loadCmsRuntimeStatus() {
        setCmsError("");
        try {
            const status = await adminFetch(ApiPaths.cmsRuntimeStatus);
            renderCmsRuntimeStatus(status);
            return status;
        }
        catch (error) {
            clearCmsResultPanel(cmsRuntimeStatusElement, "Runtime status could not be loaded.");
            handleCmsError(error);
            return null;
        }
    }

    async function loadCmsPreviewSummary() {
        setCmsError("");
        try {
            const preview = await adminFetch(cmsPath(ApiPaths.cmsPreviewSummaryTemplate, { slug: getSelectedCmsSlug() }));
            renderCmsPreviewSummary(preview);
            return preview;
        }
        catch (error) {
            clearCmsResultPanel(cmsPreviewSummaryElement, "Preview could not be loaded.");
            handleCmsError(error);
            return null;
        }
    }
    async function loadCmsVersions() {
        clearCmsPublishErrorDetails();
        const versionsPayload = await adminFetch(cmsPath(ApiPaths.cmsVersionsTemplate, { slug: getSelectedCmsSlug() }));
        const versions = Array.isArray(versionsPayload?.versions) ? versionsPayload.versions : [];
        renderCmsVersions(versions); return versionsPayload;
    }
    async function loadCmsAuditEntries() {
        setCmsAuditError("");
        setCmsAuditLoading(true);
        try {
            const requestedLimit = getSelectedCmsAuditLimit();
            const queryLimit = Math.max(100, requestedLimit);
            const query = new URLSearchParams({ limit: String(queryLimit) });
            const entityType = cmsAuditEntityTypeSelect.value.trim();
            const stableKey = cmsAuditStableKeyInput.value.trim();
            if (entityType) { query.set("entityType", entityType); }
            if (stableKey) { query.set("stableKey", stableKey); }
            const payload = await adminFetch(`${cmsPath(ApiPaths.cmsAuditEntriesTemplate, { slug: getSelectedCmsSlug() })}?${query.toString()}`);
            const entries = Array.isArray(payload?.entries) ? payload.entries : [];
            renderCmsAuditEntries(entries);
            return entries;
        } catch (error) {
            cmsAuditListElement.textContent = "";
            const message = getCmsErrorMessage(error);
            setCmsAuditError(`Unable to load CMS audit entries: ${message}`);
            if (isAuthErrorMessage(message)) { resetSession(); setError(message); }
            return [];
        } finally {
            setCmsAuditLoading(false);
        }
    }
    function renderCmsAuditEntries(entries) {
        updateCmsAuditSmokeFilterStatus();
        const visibleEntries = getVisibleCmsAuditEntries(entries);
        const rows = visibleEntries.map((entry) => Object.assign({}, entry, {
            actor: entry.actorEmail || entry.actorUserId || "-",
            changedFieldList: (entry.changedFields || []).join(", ") || "-",
            shortBeforeHash: formatShortHash(entry.beforeHash),
            shortAfterHash: formatShortHash(entry.afterHash),
            reasonDisplay: entry.reason || "-",
            requestIdDisplay: entry.requestId || "-"
        }));
        renderCmsTable(cmsAuditListElement, [
            { key: "createdAtUtc", label: "Timestamp UTC" },
            { key: "actor", label: "Actor" },
            { key: "contentPackSlug", label: "Content pack" },
            { key: "entityType", label: "Entity type" },
            { key: "stableKey", label: "Stable key", className: "cms-stable-key-cell", useFullValueTitle: true },
            { key: "operation", label: "Operation" },
            { key: "changedFieldList", label: "Changed fields" },
            { key: "shortBeforeHash", label: "Before hash", titleKey: "beforeHash" },
            { key: "shortAfterHash", label: "After hash", titleKey: "afterHash" },
            { key: "source", label: "Source" },
            { key: "status", label: "Status" },
            { key: "reasonDisplay", label: "Reason", titleKey: "reason" },
            { key: "requestIdDisplay", label: "Request/correlation id", titleKey: "requestId" }
        ], rows, { emptyMessage: "No CMS audit entries match the selected filters." });
    }
    function renderCmsVersions(versions) {
        renderCmsTable(cmsVersionsListElement, [{ key: "versionNumber", label: "Version" }, { key: "shortSnapshotHash", label: "Snapshot hash" }, { key: "publishStatus", label: "Status" }, { key: "publishedAtUtc", label: "Published at" }, { key: "changeSummary", label: "Change summary" }, { key: "restoredFromVersionNumber", label: "Restored from" }], versions.map((version) => Object.assign({}, version, { shortSnapshotHash: formatShortHash(version.snapshotHash) })), null);
        const existing = cmsRestoreVersionSelect.value;
        cmsRestoreVersionSelect.textContent = "";
        versions.forEach((version) => { const option = document.createElement("option"); option.value = version.versionNumber; option.textContent = `Version ${version.versionNumber} (${formatShortHash(version.snapshotHash)})`; cmsRestoreVersionSelect.appendChild(option); });
        if ([...cmsRestoreVersionSelect.options].some((option) => option.value === existing)) { cmsRestoreVersionSelect.value = existing; }
    }
    async function publishCmsDraft() {
        if (hasUnsavedChanges() && !confirm(UnsavedChangesMessage)) { return; }
        const summary = cmsPublishChangeSummaryInput.value.trim();
        clearCmsPublishErrorDetails();
        if (!summary) {
            const message = "Enter a publish change summary before publishing.";
            setCmsError(message); setCmsSuccess(""); renderCmsPublishErrorDetails({ errors: [message] });
            cmsPublishChangeSummaryInput.focus();
            return;
        }
        if (!confirm("Publish current CMS draft content? Learner runtime uses the published snapshot only when CMS runtime is enabled, valid, and effectively active; otherwise static JSON fallback may remain active.")) { return; }
        setCmsError(""); setCmsSuccess("Publishing CMS draft...");
        try {
            const payload = await adminFetch(cmsPath(ApiPaths.cmsPublishTemplate, { slug: getSelectedCmsSlug() }), { method: "POST", body: JSON.stringify({ changeSummary: summary }) });
            clearCmsPublishErrorDetails();
            hideCmsPublishDiscovery({ clearDraftSaved: true });
            setCmsSuccess(payload.noChanges ? "Publish completed with no draft changes to publish." : "Draft published. Learner runtime will use the new published snapshot when CMS runtime is enabled, valid, and effectively active.");
            await refreshCmsContentPack(); await runCmsValidation(); await loadCmsPreviewSummary();
        } catch (error) { renderCmsPublishErrorDetails(error); handleCmsError(error); }
    }
    async function restoreCmsVersion() {
        if (hasUnsavedChanges() && !confirm(UnsavedChangesMessage)) { return; }
        const versionNumber = cmsRestoreVersionSelect.value;
        if (!versionNumber) { setCmsError("Select a version to restore."); return; }
        if (!confirm(`Restore CMS content version ${versionNumber}? This creates/updates the current draft and does not mutate old versions.`)) { return; }
        setCmsError(""); setCmsSuccess(`Restoring CMS version ${versionNumber}...`);
        try {
            const payload = await adminFetch(cmsPath(ApiPaths.cmsRestoreTemplate, { slug: getSelectedCmsSlug(), versionNumber }), { method: "POST", body: JSON.stringify({ reason: cmsRestoreReasonInput.value.trim(), publishRestoredVersion: true }) });
            setCmsSuccess(payload.noChanges ? `Restore completed with no changes from version ${versionNumber}.` : `Restored version ${payload.restoredFromVersionNumber}; new version: ${payload.newVersionNumber || "draft only"}.`);
            await refreshCmsContentPack(); await runCmsValidation(); await loadCmsPreviewSummary();
        } catch (error) { handleCmsError(error); }
    }
    function getCmsErrorMessage(error) { return error instanceof Error ? error.message : "CMS request failed."; }
    function isAuthErrorMessage(message) { return message === ErrorMessages.signInAgain || message === ErrorMessages.accessDenied || message === ErrorMessages.sessionExpired; }
    function handleCmsError(error) { const message = getCmsErrorMessage(error); setCmsError(message); setCmsSuccess(""); if (isAuthErrorMessage(message)) { resetSession(); setError(message); } }

    async function loadAdminActivity() {
        setActivityError(""); activityLoadingElement.classList.remove("hidden"); loadActivityButton.disabled = true;
        try { renderAdminActivity(await fetchAdminActivity()); }
        catch (error) { activityResultElement.textContent = ""; const message = error instanceof Error ? error.message : ErrorMessages.activityLoadFailed; setActivityError(message); if (isAuthErrorMessage(message)) { expireAdminSession(message); } }
        finally { activityLoadingElement.classList.add("hidden"); loadActivityButton.disabled = false; }
    }

    async function loadAuditLogForSelectedUser() {
        if (!selectedUserId) { clearAuditLog(); return; }
        setAuditError(""); setAuditLoading(true);
        try { renderAuditLog(await fetchAuditActions(selectedUserId, getSelectedAuditLimit())); }
        catch (error) { auditResultElement.textContent = ""; const message = error instanceof Error ? error.message : ErrorMessages.auditLoadFailed; setAuditError(message); if (isAuthErrorMessage(message)) { expireAdminSession(message); } }
        finally { setAuditLoading(false); }
    }


    cmsSubTabButtons.forEach((button) => { button.addEventListener("click", () => { selectCmsSubTab(button.dataset.cmsSubTabId); }); });
    cmsGoToPublishButtons.forEach((button) => { button.addEventListener("click", async () => { await goToCmsPublishSection(); }); });
    cmsScenarioSectionNavButtons.forEach((button) => { button.addEventListener("click", () => { focusCmsScenarioEditorSection(button.dataset.cmsScenarioSectionTarget); }); });
    selectCmsSubTab(getHashCmsSubTab(), true);
    cmsLoadContentPacksButton.addEventListener("click", async () => { await loadCmsContentPacks(); });
    cmsContentPackSelect.addEventListener("change", async () => { setCmsLoading(true); try { if (await refreshCmsContentPack(false)) { setCmsSuccess("CMS content pack refreshed."); } } catch (error) { handleCmsError(error); } finally { setCmsLoading(false); } });
    cmsRefreshButton.addEventListener("click", async () => { setCmsLoading(true); try { if (await refreshCmsContentPack(false)) { setCmsSuccess("CMS content pack refreshed."); } } catch (error) { handleCmsError(error); } finally { setCmsLoading(false); } });
    cmsInitializeStaticJsonButton.addEventListener("click", async () => { await initializeStaticJsonContentPack(); });
    cmsTopicFilterInput.addEventListener("input", () => { renderCmsTopicsTable(); });
    cmsScenarioFilterInput.addEventListener("input", () => { renderCmsScenariosTable(); });
    cmsScenarioTopicFilterSelect.addEventListener("change", () => { renderCmsScenariosTable(); });
    cmsTopicForm.addEventListener("submit", async (event) => { event.preventDefault(); await saveCmsTopicDraft(); });
    cmsTopicResetButton.addEventListener("click", async () => { if (cmsSelectedTopic) { await selectCmsTopic(cmsSelectedTopic); } });
    cmsScenarioForm.addEventListener("submit", async (event) => { event.preventDefault(); await saveCmsScenarioDraft(event.submitter); });
    cmsScenarioResetButton.addEventListener("click", async () => { if (cmsSelectedScenario) { await selectCmsScenario(cmsSelectedScenario); } });
    cmsScenarioStructuredResetButton.addEventListener("click", async () => { if (cmsSelectedScenario) { await selectCmsScenario(cmsSelectedScenario); } });
    cmsScenarioFormatJsonButton.addEventListener("click", () => { formatCmsScenarioJsonInput(); });
    cmsScenarioValidateJsonButton.addEventListener("click", () => { validateCmsScenarioJsonInput(); });
    cmsLevelForm.addEventListener("submit", async (event) => { event.preventDefault(); await saveCmsLevelDraft(); });
    cmsLevelResetButton.addEventListener("click", () => { if (cmsSelectedLevel) { selectCmsLevel(cmsSelectedLevel, true); } });
    cmsLevelInitializeButton.addEventListener("click", initializeDefaultCmsLevels);
    cmsPromptTemplateForm.addEventListener("submit", async (event) => { event.preventDefault(); await saveCmsPromptTemplateDraft(); });
    cmsPromptTemplateResetButton.addEventListener("click", async () => { if (cmsSelectedPromptTemplate) { await selectCmsPromptTemplate(cmsSelectedPromptTemplate); } });
    cmsTutorProfileForm.addEventListener("submit", async (event) => { event.preventDefault(); await saveCmsTutorProfileDraft(); });
    cmsTutorProfileResetButton.addEventListener("click", async () => { if (cmsSelectedTutorProfile) { await selectCmsTutorProfile(cmsSelectedTutorProfile); } });
    cmsRunValidationButton.addEventListener("click", async () => { await runCmsValidation(); });
    cmsLoadPreviewButton.addEventListener("click", async () => { await loadCmsPreviewSummary(); });
    cmsLoadRuntimeStatusButton.addEventListener("click", async () => { await loadCmsRuntimeStatus(); });
    cmsOverviewLoadRuntimeStatusButton.addEventListener("click", async () => { await loadCmsRuntimeStatus(); });
    cmsLoadVersionsButton.addEventListener("click", async () => { try { await loadCmsVersions(); setCmsSuccess("CMS versions loaded."); } catch (error) { handleCmsError(error); } });
    cmsPublishChangeSummaryInput.addEventListener("input", () => { clearCmsPublishErrorDetails(); setCmsError(""); });
    cmsLoadAuditButton.addEventListener("click", async () => { await loadCmsAuditEntries(); });
    cmsAuditEntityTypeSelect.addEventListener("change", async () => { await loadCmsAuditEntries(); });
    cmsAuditStableKeyInput.addEventListener("keydown", async (event) => { if (event.key === "Enter") { event.preventDefault(); await loadCmsAuditEntries(); } });
    cmsAuditLimitSelect.addEventListener("change", async () => { await loadCmsAuditEntries(); });
    cmsAuditShowSmokeInput.addEventListener("change", async () => { updateCmsAuditSmokeFilterStatus(); await loadCmsAuditEntries(); });
    cmsPublishButton.addEventListener("click", async () => { await publishCmsDraft(); });
    cmsRestoreButton.addEventListener("click", async () => { await restoreCmsVersion(); });
    [cmsTopicTitleInput, cmsTopicDescriptionInput, cmsTopicSortOrderInput, cmsTopicIsActiveInput].forEach((element) => element.addEventListener("input", () => updateCmsDirtyState("topic")));
    cmsScenarioStructuredInputs.forEach((element) => element.addEventListener("input", () => { if (element !== cmsScenarioIsActiveInput) { try { mergeCmsStructuredScenarioFieldsToDefinition({ silent: true }); setCmsScenarioStructuredStatus("Structured edits are reflected in Advanced JSON. Save draft is still required.", false); } catch (error) { setCmsScenarioStructuredStatus(`Structured merge pending: ${error instanceof Error ? error.message : "Unable to assemble scenario JSON."}`, true); } } updateCmsDirtyState("scenario"); }));
    cmsScenarioDefinitionJsonInput.addEventListener("input", () => { fillCmsStructuredScenarioFieldsFromDefinition(); updateCmsDirtyState("scenario"); });
    cmsScenarioValidateStructuredButton.addEventListener("click", validateCmsStructuredScenarioInput);
    [cmsLevelDisplayNameInput, cmsLevelSortOrderInput, cmsLevelWrapUpTurnInput, cmsLevelFinalTurnInput, cmsLevelComplexityGuidanceInput, cmsLevelCorrectionGuidanceInput, cmsLevelAnswerGuidanceInput, cmsLevelAdminNotesInput, cmsLevelIsActiveInput].forEach((element) => element.addEventListener("input", () => updateCmsDirtyState("level")));
    [cmsPromptTemplateBodyInput, cmsPromptTemplateIsActiveInput].forEach((element) => element.addEventListener("input", () => updateCmsDirtyState("promptTemplate")));
    [cmsTutorProfileDisplayNameInput, cmsTutorProfileCommunicationStyleJsonInput, cmsTutorProfileSafetyNotesJsonInput, cmsTutorProfileIsActiveInput].forEach((element) => element.addEventListener("input", () => updateCmsDirtyState("tutorProfile")));
    window.addEventListener("beforeunload", (event) => { if (!hasUnsavedChanges()) { return; } event.preventDefault(); event.returnValue = UnsavedChangesMessage; });

    async function logoutAdminSession() {
        if (!confirmDiscardUnsavedChanges()) { return; }
        try {
            await fetch(ApiPaths.adminSession, { method: "DELETE", headers: getAdminHeaders() });
        } catch (_) { }
        clearAllCmsDirtyState();
        resetSession();
        clearAdminHash();
    }


    function normalizeStringList(value) {
        return Array.isArray(value) ? value.map((item) => String(item || "").trim()).filter(Boolean) : [];
    }

    function renderBadges(container, values, emptyText = "-") {
        container.textContent = "";
        if (!Array.isArray(values) || values.length === 0) { container.textContent = emptyText; return; }
        values.forEach((value) => {
            const badge = document.createElement("span");
            badge.className = "badge neutral";
            badge.textContent = value;
            container.appendChild(badge);
        });
    }

    function renderPermissionList(container, permissions) {
        container.textContent = "";
        if (!Array.isArray(permissions) || permissions.length === 0) { container.textContent = "-"; return; }
        permissions.forEach((permission) => {
            const item = document.createElement("span");
            item.className = "permission-chip";
            item.textContent = permission;
            container.appendChild(item);
        });
    }

    function renderWorkflowAvailability() {
        const permissionSet = new Set(adminAccessSnapshot.permissions);
        workflowAvailabilityListElement.textContent = "";
        WorkflowAvailabilityDefinitions.forEach((workflow) => {
            const isAvailable = workflow.anyPermissions.some((permissionId) => permissionSet.has(permissionId));
            const item = document.createElement("li");
            const label = document.createElement("span");
            label.textContent = workflow.label;
            const badge = document.createElement("span");
            badge.className = `badge ${isAvailable ? "available" : "unavailable"}`;
            badge.textContent = isAvailable ? workflow.statusWhenAvailable : "not listed";
            item.append(label, badge);
            workflowAvailabilityListElement.appendChild(item);
        });
    }

    function renderAdminAccessSnapshot() {
        adminSourceElement.textContent = adminAccessSnapshot.adminSource || "-";
        environmentElement.textContent = adminAccessSnapshot.environment || "-";
        checkedAtElement.textContent = adminAccessSnapshot.checkedAtUtc || "-";
        bootstrapAdminStatusElement.textContent = adminAccessSnapshot.isBootstrapAdmin ? "Yes" : "No";
        adminPermissionCountElement.textContent = String(adminAccessSnapshot.permissions.length);
        renderBadges(adminRolesBadgesElement, adminAccessSnapshot.roles);
        renderBadges(rolesPermissionsRolesElement, adminAccessSnapshot.roles);
        renderPermissionList(rolesPermissionsListElement, adminAccessSnapshot.permissions);
        renderWorkflowAvailability();
        systemProductionRolesAvailableElement.textContent = String(Boolean(adminAccessSnapshot.productionRolesAvailable));
        systemProductionRolesAvailableElement.className = `badge ${adminAccessSnapshot.productionRolesAvailable ? "available" : "unavailable"}`;
        tabButtons.forEach((button) => {
            const tabId = button.dataset.tabId || Tabs.overview;
            const canUseTab = canAccessTab(tabId);
            button.classList.toggle("hidden", !canUseTab);
            button.disabled = !canUseTab;
            button.setAttribute("aria-hidden", canUseTab ? "false" : "true");
        });
        if (!canAccessTab(Tabs.feedbackReports)) { clearFeedbackReportsState(); }
        if (!canAccessTab(getCurrentActiveTab())) { activateTab(Tabs.overview); }
    }

    async function loadAdminAccessSnapshot() {
        const [meResponse, capabilitiesResponse] = await Promise.all([
            fetch(ApiPaths.adminMe, { method: "GET", headers: getAdminHeaders() }),
            fetch(ApiPaths.capabilities, { method: "GET", headers: getAdminHeaders() })
        ]);
        [meResponse, capabilitiesResponse].forEach((response) => {
            if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
        if (response.status === HttpStatus.forbidden) { throw new Error(NotAvailableForRoleMessage); }
        });
        if (!meResponse.ok) { throw new Error("Unable to load admin profile."); }
        if (!capabilitiesResponse.ok) { throw new Error("Unable to load admin capabilities."); }
        const mePayload = await meResponse.json();
        const capabilitiesPayload = await capabilitiesResponse.json();
        const roles = normalizeStringList(mePayload.roles).length > 0 ? normalizeStringList(mePayload.roles) : normalizeStringList(capabilitiesPayload.roles);
        const permissions = normalizeStringList(mePayload.permissions).length > 0 ? normalizeStringList(mePayload.permissions) : normalizeStringList(capabilitiesPayload.permissions);
        adminAccessSnapshot = {
            roles,
            permissions,
            isBootstrapAdmin: Boolean(mePayload.isBootstrapAdmin),
            productionRolesAvailable: Boolean(capabilitiesPayload.productionRolesAvailable || capabilitiesPayload.capabilities?.productionRolesAvailable),
            adminSource: mePayload.adminSource || capabilitiesPayload.adminSource || "",
            environment: capabilitiesPayload.environment || "",
            checkedAtUtc: mePayload.checkedAtUtc || capabilitiesPayload.checkedAtUtc || ""
        };
        renderAdminAccessSnapshot();
        renderCapabilitiesList(capabilitiesPayload.capabilities || {});
        renderBillingPaddleStatus(capabilitiesPayload.capabilities || {});
    }

    function renderCapabilitiesList(capabilities) {
        capabilitiesListElement.textContent = "";
        Object.keys(capabilities || {}).forEach((key) => {
            const value = Boolean(capabilities[key]);
            const item = document.createElement("li");
            item.textContent = key;
            const badge = document.createElement("span");
            badge.className = `badge ${value ? "available" : "unavailable"}`;
            badge.textContent = value ? "available" : "unavailable";
            item.appendChild(badge);
            capabilitiesListElement.appendChild(item);
        });
    }

    function renderBillingPaddleStatus(capabilities) {
        const checkoutAvailable = Boolean(capabilities.paddleCheckoutAvailable);
        const webhooksAvailable = Boolean(capabilities.paddleWebhooksAvailable);
        const paymentTestComplete = Boolean(capabilities.billingLivePaymentTestComplete);
        const paidLaunchComplete = Boolean(capabilities.billingPaidLaunchReleaseComplete);

        if (checkoutAvailable && webhooksAvailable && !paymentTestComplete && !paidLaunchComplete) {
            systemBillingPaddleStatusElement.textContent = "configured / live checkout opens / live payment test pending";
            systemBillingPaddleStatusElement.className = "badge available";
            return;
        }

        if (checkoutAvailable || webhooksAvailable) {
            systemBillingPaddleStatusElement.textContent = "partially configured / live payment test pending";
            systemBillingPaddleStatusElement.className = "badge unavailable";
            return;
        }

        systemBillingPaddleStatusElement.textContent = "not configured";
        systemBillingPaddleStatusElement.className = "badge unavailable";
    }

    async function loadAdminCapabilities() {
        await loadAdminAccessSnapshot();
    }




    function getResponseValue(source, camelKey, fallbackValue = undefined) {
        if (!source || typeof source !== "object") { return fallbackValue; }
        if (Object.prototype.hasOwnProperty.call(source, camelKey)) { return source[camelKey]; }
        const pascalKey = camelKey.charAt(0).toUpperCase() + camelKey.slice(1);
        if (Object.prototype.hasOwnProperty.call(source, pascalKey)) { return source[pascalKey]; }
        const expectedKey = camelKey.toLowerCase();
        const matchingKey = Object.keys(source).find((key) => key.toLowerCase() === expectedKey);
        return matchingKey ? source[matchingKey] : fallbackValue;
    }

    function appendDefinitionRows(container, rows) {
        container.textContent = "";
        rows.forEach(([label, value]) => {
            const term = document.createElement("dt");
            term.textContent = label;
            const description = document.createElement("dd");
            description.textContent = formatValue(value);
            container.append(term, description);
        });
    }

    function setRoleManagementLoading(isLoading) {
        roleManagementLoadingElement.classList.toggle("hidden", !isLoading);
        roleManagementRefreshButton.disabled = isLoading;
        roleManagementForms.forEach((form) => Array.from(form.elements).forEach((element) => { element.disabled = isLoading || !roleManagementActorMappingFound; }));
    }

    function renderRoleManagementActor(actor) {
        roleManagementActorMappingFound = Boolean(getResponseValue(actor, "isActorMappingFound", false));
        const roleIds = getResponseValue(actor, "roleIds", []);
        appendDefinitionRows(roleManagementActorElement, [
            ["Persistent actor mapping found", roleManagementActorMappingFound ? "Yes" : "No"],
            ["Actor admin user id", getResponseValue(actor, "actorAdminUserId", "-")],
            ["Active role ids", Array.isArray(roleIds) && roleIds.length ? roleIds.join(", ") : "-"],
            ["Message", getResponseValue(actor, "message", getResponseValue(actor, "errorCode", "-"))],
            ["Generated at (UTC)", getResponseValue(actor, "generatedAtUtc", "-")]
        ]);
        roleManagementWarningElement.textContent = roleManagementActorMappingFound ? "" : "Persistent actor mapping is required for role-management mutations. Viewing may still be available through BootstrapAdmin fallback.";
    }

    function renderRoleManagementDiagnostics(diagnostics) {
        appendDefinitionRows(roleManagementDiagnosticsElement, [
            ["Total AdminUsers", getResponseValue(diagnostics, "totalAdminUsers", "-")],
            ["Active AdminUsers", getResponseValue(diagnostics, "activeAdminUsers", "-")],
            ["Disabled AdminUsers", getResponseValue(diagnostics, "disabledAdminUsers", "-")],
            ["Active role assignments", getResponseValue(diagnostics, "activeRoleAssignments", "-")],
            ["Revoked role assignments", getResponseValue(diagnostics, "revokedRoleAssignments", "-")],
            ["Roles in use", (getResponseValue(diagnostics, "rolesInUse", []) || []).join(", ") || "-"],
            ["Generated at (UTC)", getResponseValue(diagnostics, "generatedAtUtc", "-")]
        ]);
        const users = getResponseValue(diagnostics, "adminUsers", []);
        roleManagementUsersElement.textContent = "";
        appendCmsSimpleTable(roleManagementUsersElement, [
            { key: "adminUserId", label: "AdminUser ID" },
            { key: "linkedUserId", label: "Linked app user ID" },
            { key: "status", label: "Status" },
            { key: "roleIds", label: "Role IDs", value: (row) => (getResponseValue(row, "roleIds", []) || []).join(", ") || "-" },
            { key: "activeRoleCount", label: "Active roles" },
            { key: "disabledAtUtc", label: "Disabled at (UTC)" },
            { key: "createdAtUtc", label: "Created at (UTC)" }
        ], Array.isArray(users) ? users : [], "No AdminUsers are exposed by diagnostics.");
    }

    function renderRoleManagementCutoverStatus(status) {
        appendDefinitionRows(roleManagementCutoverStatusElement, [
            ["Fallback enabled", getResponseValue(status, "bootstrapAdminFallbackForAdminPermissionPoliciesEnabled", false) ? "Yes" : "No"],
            ["Default fallback enabled", getResponseValue(status, "bootstrapAdminFallbackDefaultEnabled", false) ? "Yes" : "No"],
            ["Config key", getResponseValue(status, "bootstrapAdminFallbackConfigurationKey", "-")],
            ["Config value present", getResponseValue(status, "bootstrapAdminFallbackConfigurationValuePresent", false) ? "Yes" : "No"],
            ["Persistent role authorization enabled", getResponseValue(status, "persistentRoleAuthorizationEnabled", false) ? "Yes" : "No"],
            ["Generated at (UTC)", getResponseValue(status, "generatedAtUtc", "-")]
        ]);
    }

    async function loadRoleManagementData() {
        roleManagementErrorElement.textContent = "";
        setRoleManagementLoading(true);
        try {
            const [actor, diagnostics, cutoverStatus] = await Promise.all([
                adminFetch(ApiPaths.roleAssignmentActor),
                adminFetch(ApiPaths.roleAssignmentDiagnostics),
                adminFetch(ApiPaths.rbacCutoverStatus)
            ]);
            renderRoleManagementActor(actor);
            renderRoleManagementDiagnostics(diagnostics);
            renderRoleManagementCutoverStatus(cutoverStatus);
        } catch (_) {
            roleManagementErrorElement.textContent = ErrorMessages.roleManagementLoadFailed;
        } finally {
            setRoleManagementLoading(false);
        }
    }

    function getRoleManagementPayload(form) {
        const data = new FormData(form);
        const reason = String(data.get("reason") || "").trim();
        if (!reason) { throw new Error(ErrorMessages.roleManagementReasonRequired); }
        if (data.get("confirmChange") !== "on") { throw new Error(ErrorMessages.roleManagementConfirmationRequired); }
        const payload = { reason };
        const targetAppUserId = String(data.get("targetAppUserId") || "").trim();
        const targetAdminUserId = String(data.get("targetAdminUserId") || "").trim();
        const roleId = String(data.get("roleId") || "").trim();
        const safeMetadataJson = String(data.get("safeMetadataJson") || "").trim();
        if (targetAppUserId) { payload.targetAppUserId = targetAppUserId; }
        if (targetAdminUserId) { payload.targetAdminUserId = targetAdminUserId; }
        if (roleId) { payload.roleId = roleId; }
        if (safeMetadataJson) { payload.safeMetadataJson = safeMetadataJson; }
        return payload;
    }

    function getRoleManagementMutationPath(action) {
        if (action === "provision-admin-user") { return ApiPaths.roleAssignmentProvisionAdminUser; }
        if (action === "assign") { return ApiPaths.roleAssignmentAssign; }
        if (action === "revoke") { return ApiPaths.roleAssignmentRevoke; }
        if (action === "disable-admin") { return ApiPaths.roleAssignmentDisableAdmin; }
        if (action === "enable-admin") { return ApiPaths.roleAssignmentEnableAdmin; }
        throw new Error(ErrorMessages.roleManagementMutationFailed);
    }

    async function submitRoleManagementMutation(form) {
        const messageElement = form.querySelector(".success");
        if (messageElement) { messageElement.textContent = ""; messageElement.className = "success"; }
        roleManagementErrorElement.textContent = "";
        if (!roleManagementActorMappingFound) { roleManagementErrorElement.textContent = "Persistent actor mapping is required for role-management mutations."; return; }
        try {
            const payload = getRoleManagementPayload(form);
            const result = await adminFetch(getRoleManagementMutationPath(form.dataset.roleManagementMutation), { method: "POST", body: JSON.stringify(payload) });
            if (messageElement) { messageElement.textContent = getResponseValue(result, "message", "Role management action completed."); }
            form.reset();
            await loadRoleManagementData();
        } catch (error) {
            const message = error instanceof Error ? error.message : ErrorMessages.roleManagementMutationFailed;
            if (messageElement) { messageElement.className = "error"; messageElement.textContent = message; }
            else { roleManagementErrorElement.textContent = message; }
        }
    }

    function setStatisticsLoading(isLoading) {
        statisticsLoadingElement.classList.toggle("hidden", !isLoading);
        refreshStatisticsButton.disabled = isLoading;
    }

    function renderStatisticsUnavailable(message) {
        statisticsCardsElement.textContent = message;
        studyLanguageDistributionElement.textContent = "No language data available.";
        nativeLanguageDistributionElement.textContent = "No language data available.";
        explanationLanguageDistributionElement.textContent = "No language data available.";
    }

    function renderStatisticsOverview(payload) {
        const safePayload = payload && typeof payload === "object" ? payload : {};
        const definitions = safePayload.definitions && typeof safePayload.definitions === "object" ? safePayload.definitions : {};
        const metrics = [
            ["totalInstallations", "Tracked signed-in app/device records", safePayload.totalInstallations],
            ["registeredUsersTotal", "Registered users", safePayload.registeredUsersTotal],
            ["activeTrialsNow", "Active trials now", safePayload.activeTrialsNow],
            ["activeUsersLast30Days", "Active users (30 days)", safePayload.activeUsersLast30Days],
            ["activePremiumUsersNow", "Active Premium users now", safePayload.activePremiumUsersNow],
            ["successfulPaymentsTotal", "Successful payments total", safePayload.successfulPaymentsTotal],
            ["successfulPaymentsCurrentMonth", "Successful payments current month", safePayload.successfulPaymentsCurrentMonth],
            ["activeFreeUsersLast30Days", "Active Free users (30 days)", safePayload.activeFreeUsersLast30Days]
        ];

        statisticsCardsElement.textContent = "";
        metrics.forEach(([key, label, value]) => {
            const card = document.createElement("section");
            card.className = "stat-card";
            const title = document.createElement("h3");
            title.textContent = label;
            const number = document.createElement("p");
            number.className = "stat-value";
            number.textContent = Number.isFinite(Number(value)) ? Number(value).toLocaleString() : "-";
            const definition = document.createElement("p");
            definition.className = "muted stat-definition";
            definition.textContent = definitions[key] || "Aggregate read-only product statistic.";
            card.append(title, number, definition);
            statisticsCardsElement.appendChild(card);
        });

        renderLanguageDistribution(studyLanguageDistributionElement, safePayload.selectedStudyLanguageDistribution || safePayload.studyLanguageDistribution || []);
        renderLanguageDistribution(practicedStudyLanguageDistributionElement, safePayload.practicedStudyLanguageDistributionLast30Days || []);
        renderLanguageDistribution(nativeLanguageDistributionElement, safePayload.nativeLanguageDistribution || []);
        renderLanguageDistribution(explanationLanguageDistributionElement, safePayload.explanationLanguageDistribution || []);

        statisticsCheckedAtElement.textContent = `Checked at: ${safePayload.checkedAtUtc || "-"}; window start: ${safePayload.windowStartUtc || "-"}; window days: ${safePayload.windowDays || 30}`;
    }

    function renderLanguageDistribution(container, distribution) {
        container.textContent = "";
        const items = Array.isArray(distribution) ? distribution : [];

        if (!items.length) {
            const empty = document.createElement("p");
            empty.className = "muted statistics-empty";
            empty.textContent = "No language data available.";
            container.appendChild(empty);
            return;
        }

        const table = document.createElement("table");
        table.className = "compact-table statistics-language-table";
        const thead = document.createElement("thead");
        const headerRow = document.createElement("tr");
        ["Language", "Users", "Percentage"].forEach((label) => {
            const th = document.createElement("th");
            th.textContent = label;
            headerRow.appendChild(th);
        });
        thead.appendChild(headerRow);

        const tbody = document.createElement("tbody");
        items.forEach((item) => {
            const row = document.createElement("tr");
            const languageCell = document.createElement("td");
            languageCell.textContent = item && item.language ? item.language : "Unknown";
            const userCountCell = document.createElement("td");
            userCountCell.textContent = Number.isFinite(Number(item && item.userCount)) ? Number(item.userCount).toLocaleString() : "-";
            const percentageCell = document.createElement("td");
            percentageCell.textContent = Number.isFinite(Number(item && item.percentage)) ? `${Number(item.percentage).toFixed(1)}%` : "-";
            row.append(languageCell, userCountCell, percentageCell);
            tbody.appendChild(row);
        });

        table.append(thead, tbody);
        container.appendChild(table);
    }

    async function loadProductStatistics() {
        statisticsErrorElement.textContent = "";
        setStatisticsLoading(true);
        try {
            const response = await fetch(ApiPaths.statisticsOverview, { method: "GET", headers: getAdminHeaders() });
            if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }
            if (response.status === HttpStatus.forbidden) {
                renderStatisticsUnavailable("Product statistics are not available for this role.");
                return;
            }
            if (!response.ok) { throw new Error(ErrorMessages.statisticsLoadFailed); }
            renderStatisticsOverview(await response.json());
        } catch (error) {
            statisticsErrorElement.textContent = error instanceof Error ? error.message : ErrorMessages.statisticsLoadFailed;
        } finally {
            setStatisticsLoading(false);
        }
    }

    async function showAdminShellAfterAuth(preferredTabId) {
        await loadAdminCapabilities();
        setDashboardVisible(true);
        initializeTabs();
        const selectedTabId = isKnownTab(preferredTabId) ? preferredTabId : Tabs.overview;
        activateTab(selectedTabId);
        selectCmsSubTab(getHashCmsSubTab());
        updateSelectedUserHeader();
        updateUserRequiredEmptyStates();
        await restoreSelectedUserFromHash();
        if (selectedTabId === Tabs.cmsContent && !cmsHasLoadedOnce) { await loadCmsContentPacks(); }
        if (selectedTabId === Tabs.website && adminAccessSnapshot.isBootstrapAdmin && !websiteHasLoadedOnce) { await loadWebsiteContent(); }
        if (selectedTabId === Tabs.roleManagement) { await loadRoleManagementData(); }
        if (selectedTabId === Tabs.overview) { await loadProductStatistics(); }
        if (selectedTabId === Tabs.feedbackReports) { await loadFeedbackReports(); }
    }

    async function restoreAdminSessionFromCookie() {
        signInButton.disabled = true;
        try {
            await showAdminShellAfterAuth(getHashActiveTab());
        } catch (_) {
            accessToken = null;
            if (!window.location.hash) { setError(""); }
        } finally {
            signInButton.disabled = false;
        }
    }

    loginForm.addEventListener("submit", async (event) => {
        event.preventDefault(); setError(""); signInButton.disabled = true;
        try {
            const formData = new FormData(loginForm);
            const loginResponse = await fetch(ApiPaths.login, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email: String(formData.get("email") || "").trim(), password: String(formData.get("password") || "") }) });
            if (!loginResponse.ok) { throw new Error("Login failed. Check your email and password."); }
            const loginBody = await loginResponse.json(); if (!loginBody?.accessToken) { throw new Error("Login failed. Access token is missing."); }
            accessToken = loginBody.accessToken;
            clearAdminHash();
            await showAdminShellAfterAuth(Tabs.overview);
        } catch (error) { resetSession(); setError(error instanceof Error ? error.message : "Unexpected error."); }
        finally { signInButton.disabled = false; }
    });

    lookupForm.addEventListener("submit", async (event) => { event.preventDefault(); await handleLookupSubmit(LookupSources.userLookup); });
    premiumLookupForm.addEventListener("submit", async (event) => { event.preventDefault(); await handleLookupSubmit(LookupSources.premium); });
    freeLessonLookupForm.addEventListener("submit", async (event) => { event.preventDefault(); await handleLookupSubmit(LookupSources.freeLesson); });

    grantForm.addEventListener("submit", async (event) => { event.preventDefault(); await grantPremiumForSelectedUser(); });
    revokeEntitlementIdElement.addEventListener("change", () => { renderSelectedRevokeEntitlementDetails(); updateRevokeControlsState(false); });
    revokeForm.addEventListener("submit", async (event) => { event.preventDefault(); await revokePremiumForSelectedUser(); });
    billingCancelRenewalForm.addEventListener("submit", async (event) => { event.preventDefault(); await cancelPaidRenewalForSelectedUser(); });
    freeLessonResetForm.addEventListener("submit", async (event) => { event.preventDefault(); await resetFreeLessonAllowanceForSelectedUser(); });
    loadAuditButton.addEventListener("click", async () => { await loadAuditLogForSelectedUser(); });
    loadActivityButton.addEventListener("click", async () => { await loadAdminActivity(); });
    feedbackReportsStatusFilter.addEventListener("change", async () => { feedbackReportsState.page = 1; clearFeedbackReportDetails(); await loadFeedbackReports(); });
    feedbackReportsCategoryFilter.addEventListener("change", async () => { feedbackReportsState.page = 1; clearFeedbackReportDetails(); await loadFeedbackReports(); });
    feedbackReportsPreviousButton.addEventListener("click", async () => { if (feedbackReportsState.page > 1) { feedbackReportsState.page -= 1; clearFeedbackReportDetails(); await loadFeedbackReports(); } });
    feedbackReportsNextButton.addEventListener("click", async () => { feedbackReportsState.page += 1; clearFeedbackReportDetails(); await loadFeedbackReports(); });
    feedbackReportReplyTextInput.addEventListener("input", updateFeedbackReportReplyLength);
    feedbackReportSendReplyButton.addEventListener("click", async () => { await sendFeedbackReportReply(); });
    refreshStatisticsButton.addEventListener("click", async () => { await loadProductStatistics(); });
    roleManagementRefreshButton.addEventListener("click", async () => { await loadRoleManagementData(); });
    websiteSaveDraftButton.addEventListener("click", async () => { await saveWebsiteDraft(); });
    websitePreviewButton.addEventListener("click", async () => { await previewWebsiteContent(); });
    websitePublishButton.addEventListener("click", async () => { await publishWebsiteContent(); });
    aiModelsLoadButton?.addEventListener("click", async () => { await loadAiModelSettings(); });
    aiModelsSaveDraftButton?.addEventListener("click", async () => { await saveAiModelDraft(); });
    aiModelsValidateButton?.addEventListener("click", async () => { await validateAiModelDraft(); });
    aiModelsProviderTestButton?.addEventListener("click", async () => { await testAiModelProviderAccess(); });
    aiModelsResetDraftButton?.addEventListener("click", async () => { await resetAiModelDraft(); });
    aiModelsPublishButton?.addEventListener("click", async () => { await publishAiModelDraft(); });
    roleManagementForms.forEach((form) => form.addEventListener("submit", async (event) => { event.preventDefault(); await submitRoleManagementMutation(form); }));
    logoutButton.addEventListener("click", () => { logoutAdminSession(); });
    initializeTabs();
    updateSelectedUserHeader();
    updateUserRequiredEmptyStates();
    restoreAdminSessionFromCookie();
})();
