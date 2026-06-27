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
        websiteCmsSectionOverview: "/api/admin/website-cms/sections/overview",
        websiteCmsSectionDetailTemplate: "/api/admin/website-cms/sections/{sectionKey}",
        websiteCmsSectionDraftTemplate: "/api/admin/website-cms/sections/{sectionKey}/draft",
        websiteCmsSectionDraftValidateTemplate: "/api/admin/website-cms/sections/{sectionKey}/draft/validate",
        websiteCmsSectionDraftPreviewTemplate: "/api/admin/website-cms/sections/{sectionKey}/draft/preview",
        websiteCmsSectionReviewStatusTemplate: "/api/admin/website-cms/sections/{sectionKey}/review-status",
        websiteCmsSectionPublishTemplate: "/api/admin/website-cms/sections/{sectionKey}/publish",
        websiteCmsSectionUnpublishTemplate: "/api/admin/website-cms/sections/{sectionKey}/unpublish",
        websiteCmsInitializeMissing: "/api/admin/website-cms/sections/initialize-missing"
    };

    const HttpStatus = { badRequest: 400, unauthorized: 401, forbidden: 403, notFound: 404, conflict: 409 };
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
        grantInvalid: "Grant request is invalid. Check duration and reason.",
        grantUserNotFound: "Selected user was not found.",
        grantFailed: "Unable to grant Premium.",
        revokeInvalid: "Revoke request is invalid. Reason is required.",
        revokeNotFound: "Selected user or entitlement was not found.",
        revokeConflict: "This entitlement cannot be revoked.",
        revokeFailed: "Unable to revoke Premium.",
        revokeNoEntitlements: "No revokable manual Premium entitlements.",
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
        roleManagementConfirmationRequired: "Confirm that this action changes persistent admin access.",
        websiteCmsLoadFailed: "Unable to load Website CMS metadata."
    };

    const SummaryFields = ["userId", "email", "status", "createdAt", "lastLoginAt"];
    const SubscriptionFields = ["planId", "planName", "premiumActive", "trialActive", "trialEndsAtUtc", "subscriptionStatus", "billingProvider", "renewalStatus", "nextRenewalState", "cancelAtPeriodEnd", "scheduledChangeAction", "scheduledChangeEffectiveAtUtc", "currentPeriodEndUtc", "paidAccessUntilUtc", "hasActivePaidProviderSubscription", "providerSubscriptionPresent", "canRequestCancelRenewal", "cancellationExplanationCode", "lastProviderEventId", "lastProviderEventType", "lastProviderEventOccurredAtUtc", "freeLessonUsedToday", "freeLessonRemainingToday", "enforcementEnabled", "source", "checkedAtUtc"];
    const EntitlementColumns = ["entitlementId", "planId", "entitlementType", "source", "status", "startsAtUtc", "expiresAtUtc", "reason", "createdAt", "updatedAt"];
    const LessonSessionColumns = ["sessionId", "lessonContentId", "studyLanguage", "topicTitle", "subtopicTitle", "level", "modeUsed", "status", "startedAt", "finishedAt", "validTurnCount", "estimatedCost"];
    const DailyUsageColumns = ["usageDate", "studyLanguage", "lessonsStarted", "lessonsCompleted", "chatReplyCount", "hintsUsed", "feedbackRequests", "transcriptionSeconds", "ttsSeconds", "estimatedCost", "updatedAt"];
    const UsageEventColumns = ["usageEventId", "sessionId", "operation", "model", "studyLanguage", "status", "inputTokens", "outputTokens", "audioDurationMs", "inputChars", "outputBytes", "estimatedCost", "createdAt"];
    const AuditColumns = ["createdAtUtc", "actionType", "reason", "adminUserId", "adminActionId", "safeMetadataJson"];
    const Tabs = Object.freeze({ overview: "overview", userLookup: "user-lookup", premium: "premium", freeLesson: "free-lesson", auditLog: "audit-log", cmsContent: "cms-content", website: "website", roleManagement: "role-management", system: "system" });
    const AdminPermissionIds = Object.freeze({
        usersRead: "users.read",
        usersDiagnosticsRead: "users.diagnostics.read",
        premiumGrant: "premium.grant",
        premiumRevoke: "premium.revoke",
        freeLessonAllowanceReset: "free_lesson_allowance.reset",
        auditRead: "audit.read",
        cmsContentRead: "cms.content.read",
        cmsContentWriteDraft: "cms.content.write_draft",
        cmsContentPublish: "cms.content.publish",
        cmsContentRestore: "cms.content.restore",
        cmsRuntimeStatusRead: "cms.runtime_status.read",
        productStatisticsRead: "product_statistics.read",
        adminRolesManage: "admin.roles.manage"
    });
    const WorkflowAvailabilityDefinitions = Object.freeze([
        { label: "User Lookup", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.usersRead, AdminPermissionIds.usersDiagnosticsRead] },
        { label: "Premium Grant", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.premiumGrant] },
        { label: "Premium Revoke", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.premiumRevoke] },
        { label: "Free Lesson Reset", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.freeLessonAllowanceReset] },
        { label: "Audit Log", statusWhenAvailable: "read-only / available", anyPermissions: [AdminPermissionIds.auditRead] },
        { label: "CMS Content", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.cmsContentRead] },
        { label: "CMS Draft Editing", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.cmsContentWriteDraft] },
        { label: "CMS Publish", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.cmsContentPublish] },
        { label: "CMS Restore", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.cmsContentRestore] },
        { label: "Runtime Status", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.cmsRuntimeStatusRead] },
        { label: "Product Statistics", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.productStatisticsRead] },
        { label: "Persistent Admin Roles", statusWhenAvailable: "available", anyPermissions: [AdminPermissionIds.adminRolesManage] }
    ]);

    const CmsSubTabs = Object.freeze({ overview: "overview", topics: "topics", scenarios: "scenarios", levels: "levels", prompts: "prompts", tutors: "tutors", validationPreview: "validation-preview", versionsPublish: "versions-publish", audit: "audit" });
    const LookupSources = Object.freeze({ userLookup: "user-lookup", premium: "premium", freeLesson: "free-lesson" });
    let accessToken = null;
    let selectedUserId = null;
    let selectedUserEmail = null;
    let selectedUserLookupPayload = null;
    let cmsHasLoadedOnce = false;
    let websiteCmsHasLoadedOnce = false;
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
    const websiteCmsLoadingElement = document.getElementById("website-cms-loading");
    const websiteCmsErrorElement = document.getElementById("website-cms-error");
    const websiteCmsSectionOverviewElement = document.getElementById("website-cms-section-overview");
    const websiteCmsCheckedAtElement = document.getElementById("website-cms-checked-at");
    const websiteCmsInitializeMissingButton = document.getElementById("website-cms-initialize-missing-button");
    const websiteCmsInitializeResultElement = document.getElementById("website-cms-initialize-result");
    const websiteCmsDetailElement = document.getElementById("website-cms-section-detail");
    const websiteCmsSaveResultElement = document.getElementById("website-cms-save-result");
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
        if (selectedTabId === Tabs.cmsContent) { parameters.set("cmsSubTab", selectedCmsSubTabId); }
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
        const selectedTabId = isKnownTab(tabId) ? tabId : Tabs.overview;
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
            if (tabId !== getCurrentActiveTab() && !confirmDiscardUnsavedChanges()) { return; }
            activateTab(tabId);
            if (tabId === Tabs.cmsContent) {
                selectCmsSubTab(getHashCmsSubTab());
                if (!cmsHasLoadedOnce) { await loadCmsContentPacks(); }
            }
            if (tabId === Tabs.overview) { await loadProductStatistics(); }
            if (tabId === Tabs.website && !websiteCmsHasLoadedOnce) { await loadWebsiteCmsSectionOverview(); }
        }));
    }

    function setWebsiteCmsLoading(isLoading) { if (websiteCmsLoadingElement) { websiteCmsLoadingElement.classList.toggle("hidden", !isLoading); } }
    function formatWebsiteCmsValue(value) { return value === null || value === undefined || value === "" ? "-" : String(value); }
    function escapeHtml(value) { return String(value ?? "").replace(/[&<>"']/g, (character) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[character])); }
    function renderWebsiteCmsSectionOverview(payload) {
        if (!websiteCmsSectionOverviewElement) { return; }
        const sections = Array.isArray(payload?.sections) ? payload.sections : [];
        if (sections.length === 0) {
            websiteCmsSectionOverviewElement.innerHTML = '<p class="muted">No Website CMS section metadata was returned.</p>';
            return;
        }
        const rows = sections.map((section) => `<tr><td><button type="button" class="link-button" data-website-cms-section-key="${escapeHtml(section.sectionKey)}"><code>${escapeHtml(section.sectionKey)}</code></button></td><td>${escapeHtml(section.displayName)}</td><td>${escapeHtml(section.reviewStatus || "Not stored")}</td><td>${section.storedRowExists ? "Stored" : "Not stored"}</td><td>${section.draftBodyExists ? "Yes" : "No"}</td><td>${section.publishedBodyExists ? "Yes" : "No"}</td><td>${escapeHtml(formatWebsiteCmsValue(section.effectiveDate))}</td><td>${escapeHtml(formatWebsiteCmsValue(section.updatedAtUtc))}</td><td>${escapeHtml(formatWebsiteCmsValue(section.publishedAtUtc))}</td></tr>`).join("");
        websiteCmsSectionOverviewElement.innerHTML = `<table><thead><tr><th>Section key</th><th>Display name</th><th>Review status</th><th>Stored</th><th>Draft exists</th><th>Published exists</th><th>Effective date</th><th>Updated</th><th>Published</th></tr></thead><tbody>${rows}</tbody></table>`;
        if (websiteCmsCheckedAtElement) { websiteCmsCheckedAtElement.textContent = payload?.checkedAtUtc ? `Metadata checked at ${payload.checkedAtUtc}.` : ""; }
    }
    function websiteCmsPath(template, sectionKey) { return template.replace("{sectionKey}", encodeURIComponent(sectionKey)); }
    function renderWebsiteCmsDetail(detail) {
        if (!websiteCmsDetailElement) { return; }
        const statuses = ["not_started", "draft", "owner_review_needed", "legal_review_needed", "owner_approved", "legal_approved"];
        const options = statuses.map((status) => `<option value="${status}" ${detail.reviewStatus === status ? "selected" : ""}>${status}</option>`).join("");
        websiteCmsDetailElement.innerHTML = `<h4>Draft detail: <code>${escapeHtml(detail.sectionKey)}</code></h4><p class="muted">${escapeHtml(detail.displayName)} — ${escapeHtml(detail.description)}</p><p class="cms-inline-warning">Admin-only draft storage. Saving here does not publish, does not update public website rendering, and does not modify <code>site/public</code>. Draft copy is not final legal advice or legal approval. Validate, preview, and review status changes do not publish or update the public site.</p><div class="cms-button-row"><button id="website-cms-validate-draft-button" type="button">Validate draft</button><button id="website-cms-preview-draft-button" type="button">Preview draft</button></div><div id="website-cms-validation-result" class="muted" role="status"></div><div id="website-cms-preview-panel" class="cms-readonly-notice hidden" role="status"></div><form id="website-cms-draft-form"><input type="hidden" id="website-cms-detail-key" value="${escapeHtml(detail.sectionKey)}" /><div class="field"><label for="website-cms-draft-body">DraftBody</label><textarea id="website-cms-draft-body" rows="12">${escapeHtml(detail.draftBody || "")}</textarea></div><div class="field"><label for="website-cms-internal-notes">InternalNotes</label><textarea id="website-cms-internal-notes" rows="4">${escapeHtml(detail.internalNotes || "")}</textarea></div><div class="field"><label for="website-cms-effective-date">EffectiveDate</label><input id="website-cms-effective-date" type="date" value="${escapeHtml(detail.effectiveDate || "")}" /></div><div class="field"><label for="website-cms-review-status">ReviewStatus</label><select id="website-cms-review-status">${options}</select></div><div class="field"><label for="website-cms-change-reason">ChangeReason (required)</label><input id="website-cms-change-reason" type="text" required autocomplete="off" placeholder="Why is this draft changing?" /></div><p class="muted">Published body exists: ${detail.publishedBodyExists ? "Yes" : "No"}. Published timestamp: ${escapeHtml(formatWebsiteCmsValue(detail.publishedAtUtc))}. Saving a draft does not change either value.</p><div class="cms-button-row"><button id="website-cms-save-draft-button" type="submit">Save draft</button><button id="website-cms-review-status-button" type="button">Change review status only</button><button id="website-cms-publish-button" type="button">Publish section to Website CMS only</button><button id="website-cms-unpublish-button" type="button">Unpublish from Website CMS only</button></div><p class="cms-inline-warning">Admin-only Website CMS publish copies DraftBody to PublishedBody only. Unpublish from Website CMS only clears internal PublishedBody / PublishedAtUtc only. Unpublish does not update public site rendering, does not modify site/public, and does not enable live Paddle. ChangeReason is required.</p><p class="muted">owner_approved/legal_approved are internal review markers only; they are not automatic publish and are not final legal advice by themselves. Validate and preview remain available before publish.</p></form>`;
        document.getElementById("website-cms-draft-form")?.addEventListener("submit", saveWebsiteCmsDraft);
        document.getElementById("website-cms-validate-draft-button")?.addEventListener("click", validateWebsiteCmsDraft);
        document.getElementById("website-cms-preview-draft-button")?.addEventListener("click", previewWebsiteCmsDraft);
        document.getElementById("website-cms-review-status-button")?.addEventListener("click", updateWebsiteCmsReviewStatus);
        document.getElementById("website-cms-publish-button")?.addEventListener("click", publishWebsiteCmsSection);
        document.getElementById("website-cms-unpublish-button")?.addEventListener("click", unpublishWebsiteCmsSection);
    }
    async function loadWebsiteCmsSectionDetail(sectionKey) {
        if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = ""; }
        const detail = await adminFetch(websiteCmsPath(ApiPaths.websiteCmsSectionDetailTemplate, sectionKey), { method: "GET" });
        renderWebsiteCmsDetail(detail);
    }
    async function saveWebsiteCmsDraft(event) {
        event.preventDefault();
        const sectionKey = document.getElementById("website-cms-detail-key")?.value || "";
        const payload = {
            draftBody: document.getElementById("website-cms-draft-body")?.value || "",
            internalNotes: document.getElementById("website-cms-internal-notes")?.value || "",
            effectiveDate: document.getElementById("website-cms-effective-date")?.value || null,
            reviewStatus: document.getElementById("website-cms-review-status")?.value || "draft",
            changeReason: document.getElementById("website-cms-change-reason")?.value || ""
        };
        if (!payload.changeReason.trim()) { if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = "ChangeReason is required."; } return; }
        try {
            const detail = await adminFetch(websiteCmsPath(ApiPaths.websiteCmsSectionDraftTemplate, sectionKey), { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) });
            if (websiteCmsSaveResultElement) { websiteCmsSaveResultElement.textContent = `Draft saved for ${detail.sectionKey}. This did not publish or update public rendering.`; }
            renderWebsiteCmsDetail(detail);
            await loadWebsiteCmsSectionOverview();
        } catch (error) {
            if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = error instanceof Error ? error.message : "Unable to save Website CMS draft."; }
        }
    }

    async function validateWebsiteCmsDraft() {
        const sectionKey = document.getElementById("website-cms-detail-key")?.value || "";
        const resultElement = document.getElementById("website-cms-validation-result");
        try {
            const result = await adminFetch(websiteCmsPath(ApiPaths.websiteCmsSectionDraftValidateTemplate, sectionKey), { method: "POST" });
            const errors = (result.errors || []).map((item) => `<li>${escapeHtml(item)}</li>`).join("");
            const warnings = (result.warnings || []).map((item) => `<li>${escapeHtml(item)}</li>`).join("");
            if (resultElement) { resultElement.innerHTML = `<strong>Validation ${escapeHtml(result.status)}</strong> at ${escapeHtml(result.checkedAtUtc)}. This did not publish or update public rendering.${errors ? `<h5>Errors</h5><ul>${errors}</ul>` : ""}${warnings ? `<h5>Warnings</h5><ul>${warnings}</ul>` : ""}`; }
        } catch (error) {
            if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = error instanceof Error ? error.message : "Unable to validate Website CMS draft."; }
        }
    }

    async function previewWebsiteCmsDraft() {
        const sectionKey = document.getElementById("website-cms-detail-key")?.value || "";
        const panel = document.getElementById("website-cms-preview-panel");
        try {
            const preview = await adminFetch(websiteCmsPath(ApiPaths.websiteCmsSectionDraftPreviewTemplate, sectionKey), { method: "GET" });
            const paragraphs = String(preview.draftBody || "").split(/\n{2,}/).map((line) => line.trim()).filter(Boolean).map((line) => `<p>${escapeHtml(line).replace(/\n/g, "<br>")}</p>`).join("") || '<p class="muted">Empty draft.</p>';
            const warnings = (preview.warnings || []).map((item) => `<li>${escapeHtml(item)}</li>`).join("");
            if (panel) { panel.classList.remove("hidden"); panel.innerHTML = `<h4>Admin-only draft preview: ${escapeHtml(preview.displayName)}</h4><p class="muted">${escapeHtml(preview.description)}</p><p><strong>Review status:</strong> ${escapeHtml(preview.reviewStatus)} | <strong>Effective date:</strong> ${escapeHtml(formatWebsiteCmsValue(preview.effectiveDate))}</p><p class="cms-inline-warning">Admin-only preview. This is simple safe text display, not public rendering, and it does not publish or update the public site.</p>${warnings ? `<ul>${warnings}</ul>` : ""}<div>${paragraphs}</div>${preview.adminOnlyInternalNotes ? `<details><summary>Admin-only internal notes</summary><p>${escapeHtml(preview.adminOnlyInternalNotes)}</p></details>` : ""}<p class="muted">Checked at ${escapeHtml(preview.checkedAtUtc)}.</p>`; }
        } catch (error) {
            if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = error instanceof Error ? error.message : "Unable to preview Website CMS draft."; }
        }
    }

    async function updateWebsiteCmsReviewStatus() {
        const sectionKey = document.getElementById("website-cms-detail-key")?.value || "";
        const payload = { reviewStatus: document.getElementById("website-cms-review-status")?.value || "draft", changeReason: document.getElementById("website-cms-change-reason")?.value || "" };
        if (!payload.changeReason.trim()) { if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = "ChangeReason is required for review status changes."; } return; }
        try {
            const detail = await adminFetch(websiteCmsPath(ApiPaths.websiteCmsSectionReviewStatusTemplate, sectionKey), { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) });
            if (websiteCmsSaveResultElement) { websiteCmsSaveResultElement.textContent = `Review status changed to ${detail.reviewStatus}. This did not publish or update public rendering.`; }
            renderWebsiteCmsDetail(detail);
            await loadWebsiteCmsSectionOverview();
        } catch (error) {
            if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = error instanceof Error ? error.message : "Unable to update Website CMS review status."; }
        }
    }


    async function publishWebsiteCmsSection() {
        const sectionKey = document.getElementById("website-cms-detail-key")?.value || "";
        const payload = { changeReason: document.getElementById("website-cms-change-reason")?.value || "" };
        if (!payload.changeReason.trim()) { if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = "ChangeReason is required for admin-only Website CMS publish."; } return; }
        try {
            const result = await adminFetch(websiteCmsPath(ApiPaths.websiteCmsSectionPublishTemplate, sectionKey), { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) });
            if (websiteCmsSaveResultElement) { websiteCmsSaveResultElement.textContent = `${result.message} Section ${result.sectionKey} published at ${result.publishedAtUtc}.`; }
            await loadWebsiteCmsSectionOverview();
            await loadWebsiteCmsSectionDetail(sectionKey);
        } catch (error) {
            if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = error instanceof Error ? error.message : "Unable to publish Website CMS section."; }
        }
    }

    async function unpublishWebsiteCmsSection() {
        const sectionKey = document.getElementById("website-cms-detail-key")?.value || "";
        const payload = { changeReason: document.getElementById("website-cms-change-reason")?.value || "" };
        if (!payload.changeReason.trim()) { if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = "ChangeReason is required for admin-only Website CMS unpublish."; } return; }
        try {
            const result = await adminFetch(websiteCmsPath(ApiPaths.websiteCmsSectionUnpublishTemplate, sectionKey), { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) });
            if (websiteCmsSaveResultElement) { websiteCmsSaveResultElement.textContent = `${result.message} Section ${result.sectionKey} unpublished at ${result.unpublishedCheckedAtUtc}.`; }
            await loadWebsiteCmsSectionOverview();
            await loadWebsiteCmsSectionDetail(sectionKey);
        } catch (error) {
            if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = error instanceof Error ? error.message : "Unable to unpublish Website CMS section."; }
        }
    }

    async function initializeMissingWebsiteCmsSections() {
        if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = ""; }
        if (websiteCmsInitializeResultElement) { websiteCmsInitializeResultElement.textContent = "Initializing missing Website CMS metadata rows..."; }
        if (websiteCmsInitializeMissingButton) { websiteCmsInitializeMissingButton.disabled = true; }
        try {
            const payload = await adminFetch(ApiPaths.websiteCmsInitializeMissing, { method: "POST" });
            if (websiteCmsInitializeResultElement) { websiteCmsInitializeResultElement.textContent = `Initialization complete: ${payload.createdCount} created, ${payload.existingCount} existing, ${payload.totalExpectedCount} expected. Empty metadata rows only; no editing, publish, or public rendering was added.`; }
            await loadWebsiteCmsSectionOverview();
        } catch (error) {
            if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = error instanceof Error ? error.message : "Unable to initialize Website CMS metadata."; }
            if (websiteCmsInitializeResultElement) { websiteCmsInitializeResultElement.textContent = ""; }
        } finally {
            if (websiteCmsInitializeMissingButton) { websiteCmsInitializeMissingButton.disabled = false; }
        }
    }

    async function loadWebsiteCmsSectionOverview() {
        if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = ""; }
        setWebsiteCmsLoading(true);
        try {
            const payload = await adminFetch(ApiPaths.websiteCmsSectionOverview, { method: "GET" });
            renderWebsiteCmsSectionOverview(payload);
            websiteCmsHasLoadedOnce = true;
        } catch (error) {
            if (websiteCmsErrorElement) { websiteCmsErrorElement.textContent = error instanceof Error ? error.message : ErrorMessages.websiteCmsLoadFailed; }
        } finally {
            setWebsiteCmsLoading(false);
        }
    }

    if (websiteCmsInitializeMissingButton) { websiteCmsInitializeMissingButton.addEventListener("click", initializeMissingWebsiteCmsSections); }
    if (websiteCmsSectionOverviewElement) { websiteCmsSectionOverviewElement.addEventListener("click", (event) => { const button = event.target.closest("[data-website-cms-section-key]"); if (button) { loadWebsiteCmsSectionDetail(button.dataset.websiteCmsSectionKey); } }); }

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
            && entry.source === "manual_admin"
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
            option.textContent = `${formatValue(entry.startsAtUtc)} → ${formatValue(entry.expiresAtUtc)} | ${reasonOrId}`;
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
    function renderTable(container, items, columns, emptyMessage) { container.textContent = ""; if (!Array.isArray(items) || items.length === 0) { const p = document.createElement("p"); p.className = "empty-state"; p.textContent = emptyMessage; container.appendChild(p); return; } const wrap = document.createElement("div"); wrap.className = "table-wrap"; const table = document.createElement("table"); table.className = "compact-table"; const thead = document.createElement("thead"); const hr = document.createElement("tr"); columns.forEach((column) => { const th = document.createElement("th"); th.scope = "col"; th.textContent = column; hr.appendChild(th); }); thead.appendChild(hr); table.appendChild(thead); const tbody = document.createElement("tbody"); items.forEach((item) => { const row = document.createElement("tr"); columns.forEach((column) => { const td = document.createElement("td"); td.textContent = formatValue(item ? item[column] : null); row.appendChild(td); }); tbody.appendChild(row); }); table.appendChild(tbody); wrap.appendChild(table); container.appendChild(wrap); }

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
    const getSelectedAuditLimit = () => [10, 25, 50, 100].includes(Number.parseInt(auditLimitElement.value, 10)) ? Number.parseInt(auditLimitElement.value, 10) : 10;

    function resetDashboard() {
        adminAccessSnapshot = { roles: [], permissions: [], isBootstrapAdmin: false, productionRolesAvailable: false, adminSource: "", environment: "", checkedAtUtc: "" }; adminSourceElement.textContent = "-"; environmentElement.textContent = "-"; checkedAtElement.textContent = "-"; bootstrapAdminStatusElement.textContent = "-"; adminPermissionCountElement.textContent = "-"; capabilitiesListElement.textContent = ""; renderBadges(adminRolesBadgesElement, []); renderBadges(rolesPermissionsRolesElement, []); renderPermissionList(rolesPermissionsListElement, []); workflowAvailabilityListElement.textContent = ""; systemProductionRolesAvailableElement.textContent = "false"; systemProductionRolesAvailableElement.className = "badge unavailable";
        setLookupError(""); setLookupLoading(false); setLookupSourceLoading(LookupSources.premium, false); setLookupSourceLoading(LookupSources.freeLesson, false); clearLookupErrors(); clearUserLookupResult(); lookupForm.reset(); premiumLookupForm.reset(); freeLessonLookupForm.reset(); clearSelectedUserState();
        setGrantVisible(false); setRevokeVisible(false); setBillingCancelRenewalVisible(false); setFreeLessonResetVisible(false); clearGrantState(); clearRevokeState(); clearBillingCancelRenewalState(); clearFreeLessonResetState(); grantForm.reset(); revokeForm.reset(); billingCancelRenewalForm.reset(); freeLessonResetForm.reset(); clearAuditLog(); clearAllCmsDirtyState();
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
        if (response.status === HttpStatus.unauthorized || response.status === HttpStatus.forbidden) { handleAuthInvalidResponse(); }
        if (!response.ok) { throw new Error(ErrorMessages.lookupFailed); }
        return response.json();
    }

    async function fetchAuditActions(userId, limit) {
        const response = await fetch(`${ApiPaths.auditActionsTemplate.replace("{userId}", encodeURIComponent(userId))}?limit=${encodeURIComponent(limit)}`, { method: "GET", headers: getAdminHeaders() });
        if (response.status === HttpStatus.badRequest) { throw new Error(ErrorMessages.invalidAuditLimit); }
        if (response.status === HttpStatus.notFound) { throw new Error(ErrorMessages.auditTargetNotFound); }
        if (response.status === HttpStatus.unauthorized || response.status === HttpStatus.forbidden) { handleAuthInvalidResponse(); }
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
        setGrantVisible(Boolean(selectedUserId));
        setRevokeVisible(Boolean(selectedUserId));
        setBillingCancelRenewalVisible(Boolean(selectedUserId));
        setFreeLessonResetVisible(Boolean(selectedUserId));
        clearAuditLog();
        updateHashField("selectedUserId", selectedUserId);
        await loadAuditLogForSelectedUser();
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
        setBillingCancelRenewalVisible(false);
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
            if (response.status === HttpStatus.unauthorized || response.status === HttpStatus.forbidden) { handleAuthInvalidResponse(); }
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
            if (response.status === HttpStatus.unauthorized || response.status === HttpStatus.forbidden) { handleAuthInvalidResponse(); }
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
            if (response.status === HttpStatus.unauthorized || response.status === HttpStatus.forbidden) { handleAuthInvalidResponse(); }
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
            if (response.status === HttpStatus.unauthorized || response.status === HttpStatus.forbidden) { handleAuthInvalidResponse(); }
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

    async function adminFetch(path, options = {}) {
        const headers = getAdminHeaders(options.headers || {});
        if (options.body && !headers["Content-Type"]) { headers["Content-Type"] = "application/json"; }
        const response = await fetch(path, Object.assign({}, options, { headers }));
        if (response.status === HttpStatus.unauthorized || response.status === HttpStatus.forbidden) { handleAuthInvalidResponse(); }
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
    }

    async function loadAdminAccessSnapshot() {
        const [meResponse, capabilitiesResponse] = await Promise.all([
            fetch(ApiPaths.adminMe, { method: "GET", headers: getAdminHeaders() }),
            fetch(ApiPaths.capabilities, { method: "GET", headers: getAdminHeaders() })
        ]);
        [meResponse, capabilitiesResponse].forEach((response) => {
            if (response.status === HttpStatus.unauthorized || response.status === HttpStatus.forbidden) { handleAuthInvalidResponse(); }
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
            if (response.status === HttpStatus.unauthorized || response.status === HttpStatus.forbidden) { handleAuthInvalidResponse(); }
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
        if (selectedTabId === Tabs.roleManagement) { await loadRoleManagementData(); }
        if (selectedTabId === Tabs.overview) { await loadProductStatistics(); }
        if (selectedTabId === Tabs.website && !websiteCmsHasLoadedOnce) { await loadWebsiteCmsSectionOverview(); }
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
    refreshStatisticsButton.addEventListener("click", async () => { await loadProductStatistics(); });
    roleManagementRefreshButton.addEventListener("click", async () => { await loadRoleManagementData(); });
    roleManagementForms.forEach((form) => form.addEventListener("submit", async (event) => { event.preventDefault(); await submitRoleManagementMutation(form); }));
    logoutButton.addEventListener("click", () => { logoutAdminSession(); });
    initializeTabs();
    updateSelectedUserHeader();
    updateUserRequiredEmptyStates();
    restoreAdminSessionFromCookie();
})();
