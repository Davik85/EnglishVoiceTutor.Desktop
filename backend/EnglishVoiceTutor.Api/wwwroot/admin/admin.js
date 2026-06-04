(() => {
    const ApiPaths = {
        login: "/api/auth/login",
        adminSession: "/api/admin/session",
        capabilities: "/api/admin/capabilities",
        userLookupByEmail: "/api/admin/users/by-email",
        userLookupByIdTemplate: "/api/admin/users/{userId}",
        auditActionsTemplate: "/api/admin/users/{userId}/audit-actions",
        manualPremiumGrantTemplate: "/api/admin/users/{userId}/premium-grants",
        manualPremiumRevokeTemplate: "/api/admin/users/{userId}/premium-grants/{entitlementId}/revoke",
        freeLessonAllowanceResetTemplate: "/api/admin/users/{userId}/free-lesson-allowance/reset",
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
        cmsAuditEntriesTemplate: "/api/admin/dev/cms/content-packs/{slug}/audit-entries"
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
        resetFailed: "Unable to reset free lesson allowance."
    };

    const SummaryFields = ["userId", "email", "status", "createdAt", "lastLoginAt"];
    const SubscriptionFields = ["planId", "planName", "premiumActive", "trialActive", "trialEndsAtUtc", "subscriptionStatus", "billingProvider", "currentPeriodEndUtc", "freeLessonUsedToday", "freeLessonRemainingToday", "enforcementEnabled", "source", "checkedAtUtc"];
    const EntitlementColumns = ["entitlementId", "planId", "entitlementType", "source", "status", "startsAtUtc", "expiresAtUtc", "reason", "createdAt", "updatedAt"];
    const LessonSessionColumns = ["sessionId", "lessonContentId", "studyLanguage", "topicTitle", "subtopicTitle", "level", "modeUsed", "status", "startedAt", "finishedAt", "validTurnCount", "estimatedCost"];
    const DailyUsageColumns = ["usageDate", "studyLanguage", "lessonsStarted", "lessonsCompleted", "chatReplyCount", "hintsUsed", "feedbackRequests", "transcriptionSeconds", "ttsSeconds", "estimatedCost", "updatedAt"];
    const UsageEventColumns = ["usageEventId", "sessionId", "operation", "model", "studyLanguage", "status", "inputTokens", "outputTokens", "audioDurationMs", "inputChars", "outputBytes", "estimatedCost", "createdAt"];
    const AuditColumns = ["createdAtUtc", "actionType", "reason", "adminUserId", "adminActionId", "safeMetadataJson"];
    const Tabs = Object.freeze({ overview: "overview", userLookup: "user-lookup", premium: "premium", freeLesson: "free-lesson", auditLog: "audit-log", cmsContent: "cms-content", system: "system" });
    const CmsSubTabs = Object.freeze({ overview: "overview", topics: "topics", scenarios: "scenarios", prompts: "prompts", tutors: "tutors", validationPreview: "validation-preview", versionsPublish: "versions-publish", audit: "audit" });
    const LookupSources = Object.freeze({ userLookup: "user-lookup", premium: "premium", freeLesson: "free-lesson" });
    let accessToken = null;
    let selectedUserId = null;
    let selectedUserEmail = null;
    let selectedUserLookupPayload = null;
    let cmsHasLoadedOnce = false;
    let cmsSelectedTopic = null;
    let cmsSelectedScenario = null;
    let cmsSelectedPromptTemplate = null;
    let cmsSelectedTutorProfile = null;
    let cmsTopics = [];
    let cmsScenarios = [];
    let cmsPromptTemplates = [];
    let cmsTutorProfiles = [];
    let tabsInitialized = false;
    let restoringCmsSelection = false;

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
    const freeLessonResetLoadingElement = document.getElementById("free-lesson-reset-loading");
    const freeLessonResetErrorElement = document.getElementById("free-lesson-reset-error");
    const freeLessonResetSuccessElement = document.getElementById("free-lesson-reset-success");


    const cmsSubTabButtons = Array.from(document.querySelectorAll(".cms-sub-tab-button"));
    const cmsSubPanels = Array.from(document.querySelectorAll(".cms-sub-panel"));
    const cmsLoadContentPacksButton = document.getElementById("cms-load-content-packs-button");
    const cmsContentPackSelect = document.getElementById("cms-content-pack-select");
    const cmsRefreshButton = document.getElementById("cms-refresh-button");
    const cmsLoadingElement = document.getElementById("cms-loading");
    const cmsErrorElement = document.getElementById("cms-error");
    const cmsSuccessElement = document.getElementById("cms-success");
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
    const cmsScenarioFormatJsonButton = document.getElementById("cms-scenario-format-json-button");
    const cmsScenarioValidateJsonButton = document.getElementById("cms-scenario-validate-json-button");
    const cmsScenarioJsonStatusElement = document.getElementById("cms-scenario-json-status");
    const cmsSelectedScenarioIdentityElement = document.getElementById("cms-selected-scenario-identity");
    const cmsScenarioResetButton = document.getElementById("cms-scenario-reset-button");
    const cmsScenarioMessageElement = document.getElementById("cms-scenario-message");
    const cmsScenarioDirtyStatusElement = document.getElementById("cms-scenario-dirty-status");
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
    const cmsLoadVersionsButton = document.getElementById("cms-load-versions-button");
    const cmsPublishChangeSummaryInput = document.getElementById("cms-publish-change-summary");
    const cmsPublishButton = document.getElementById("cms-publish-button");
    const cmsRestoreVersionSelect = document.getElementById("cms-restore-version-select");
    const cmsRestoreReasonInput = document.getElementById("cms-restore-reason");
    const cmsRestoreButton = document.getElementById("cms-restore-button");
    const cmsVersionsListElement = document.getElementById("cms-versions-list");
    const cmsLoadAuditButton = document.getElementById("cms-load-audit-button");
    const cmsAuditLimitSelect = document.getElementById("cms-audit-limit");
    const cmsAuditListElement = document.getElementById("cms-audit-list");

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

    const UnsavedChangesMessage = "You have unsaved changes. Save draft before leaving, or discard changes.";
    const cmsDirtyBaselines = { topic: null, scenario: null, promptTemplate: null, tutorProfile: null };
    const cmsDirtyState = { topic: false, scenario: false, promptTemplate: false, tutorProfile: false };

    function getCmsDraftSnapshot(editorKey) {
        if (editorKey === "topic") { return { title: cmsTopicTitleInput.value, description: cmsTopicDescriptionInput.value, sortOrder: cmsTopicSortOrderInput.value, isActive: cmsTopicIsActiveInput.checked }; }
        if (editorKey === "scenario") { return { title: cmsScenarioTitleInput.value, description: cmsScenarioDescriptionInput.value, setupMessage: cmsScenarioSetupMessageInput.value, definitionJson: cmsScenarioDefinitionJsonInput.value, isActive: cmsScenarioIsActiveInput.checked }; }
        if (editorKey === "promptTemplate") { return { body: cmsPromptTemplateBodyInput.value, isActive: cmsPromptTemplateIsActiveInput.checked }; }
        if (editorKey === "tutorProfile") { return { displayName: cmsTutorProfileDisplayNameInput.value, communicationStyleJson: cmsTutorProfileCommunicationStyleJsonInput.value, safetyNotesJson: cmsTutorProfileSafetyNotesJsonInput.value, isActive: cmsTutorProfileIsActiveInput.checked }; }
        return null;
    }

    function snapshotsMatch(left, right) { return JSON.stringify(left || null) === JSON.stringify(right || null); }
    function getCmsDirtyStatusElement(editorKey) {
        if (editorKey === "topic") { return cmsTopicDirtyStatusElement; }
        if (editorKey === "scenario") { return cmsScenarioDirtyStatusElement; }
        if (editorKey === "promptTemplate") { return cmsPromptTemplateDirtyStatusElement; }
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
        adminSourceElement.textContent = "-"; environmentElement.textContent = "-"; checkedAtElement.textContent = "-"; capabilitiesListElement.textContent = "";
        setLookupError(""); setLookupLoading(false); setLookupSourceLoading(LookupSources.premium, false); setLookupSourceLoading(LookupSources.freeLesson, false); clearLookupErrors(); clearUserLookupResult(); lookupForm.reset(); premiumLookupForm.reset(); freeLessonLookupForm.reset(); clearSelectedUserState();
        setGrantVisible(false); setRevokeVisible(false); setFreeLessonResetVisible(false); clearGrantState(); clearRevokeState(); clearFreeLessonResetState(); grantForm.reset(); revokeForm.reset(); freeLessonResetForm.reset(); clearAuditLog(); clearAllCmsDirtyState();
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
    function setCmsEntityMessage(element, message, isError) { element.className = isError ? "error" : "success"; element.textContent = message || ""; }
    function renderJsonOutput(element, payload) { element.textContent = JSON.stringify(payload, null, 2); }
    function tryParseCmsJson(text) { try { return { isValid: true, value: JSON.parse(text) }; } catch (error) { return { isValid: false, message: error instanceof Error ? error.message : "Invalid JSON." }; } }
    function prettyPrintCmsJson(text) { const parsed = tryParseCmsJson(text); return parsed.isValid ? { isValid: true, text: JSON.stringify(parsed.value, null, 2) } : parsed; }
    function setCmsScenarioJsonStatus(message, isError) { cmsScenarioJsonStatusElement.className = isError ? "error" : "success"; cmsScenarioJsonStatusElement.textContent = message || ""; }
    function validateCmsScenarioJsonInput() { const text = cmsScenarioDefinitionJsonInput.value.trim(); if (!text) { setCmsScenarioJsonStatus("Validation failed: full scenario JSON is required before saving an active scenario. Nothing was saved or published.", true); return false; } const parsed = tryParseCmsJson(text); if (!parsed.isValid) { setCmsScenarioJsonStatus(`Validation failed: invalid JSON syntax (${parsed.message}). Nothing was saved or published.`, true); return false; } if (!parsed.value || typeof parsed.value !== "object" || Array.isArray(parsed.value)) { setCmsScenarioJsonStatus("Validation failed: full scenario JSON root must be an object. Nothing was saved or published.", true); return false; } setCmsScenarioJsonStatus("Validation passed: JSON syntax and required scenario fields are ready to save as a draft. Nothing was saved or published.", false); return true; }
    function formatCmsScenarioJsonInput() { const formatted = prettyPrintCmsJson(cmsScenarioDefinitionJsonInput.value); if (!formatted.isValid) { setCmsScenarioJsonStatus(`Format failed: invalid JSON syntax (${formatted.message}). Nothing was saved or published.`, true); return false; } cmsScenarioDefinitionJsonInput.value = formatted.text; updateCmsDirtyState("scenario"); setCmsScenarioJsonStatus("Formatted JSON for easier editing. Nothing was saved or published; use Save draft to persist edits.", false); return true; }
    function formatShortHash(hash) { const value = String(hash || ""); return value.length > 16 ? `${value.slice(0, 12)}...${value.slice(-4)}` : formatValue(value); }

    async function adminFetch(path, options = {}) {
        const headers = getAdminHeaders(options.headers || {});
        if (options.body && !headers["Content-Type"]) { headers["Content-Type"] = "application/json"; }
        const response = await fetch(path, Object.assign({}, options, { headers }));
        if (response.status === HttpStatus.unauthorized || response.status === HttpStatus.forbidden) { handleAuthInvalidResponse(); }
        if (response.status === HttpStatus.notFound) { throw new Error("CMS item was not found."); }
        if (response.status === HttpStatus.badRequest) {
            let message = "CMS request is invalid. Check draft fields and validation messages.";
            try { const payload = await response.json(); message = payload?.error || payload?.message || message; } catch (_) { }
            throw new Error(message);
        }
        if (response.status === HttpStatus.conflict) { throw new Error("CMS request conflicted with current state."); }
        if (!response.ok) { throw new Error("CMS request failed."); }
        if (response.status === 204) { return null; }
        return response.json();
    }

    function renderCmsContentPackSummary(summary) {
        cmsSummarySlugElement.textContent = formatValue(summary?.slug);
        cmsSummaryNameElement.textContent = formatValue(summary?.name);
        cmsSummaryStatusElement.textContent = formatValue(summary?.status);
        cmsSummaryTopicCountElement.textContent = formatValue(summary?.topicCount);
        cmsSummaryScenarioCountElement.textContent = formatValue(summary?.scenarioCount);
        cmsSummaryPromptTemplateCountElement.textContent = formatValue(summary?.promptTemplateCount);
        cmsSummaryTutorProfileCountElement.textContent = formatValue(summary?.tutorBehaviorProfileCount);
        cmsSummaryPublishedVersionElement.textContent = formatValue(summary?.currentPublishedVersionNumber);
    }

    function selectCmsSubTab(tabId, force = false) {
        const selectedTabId = isKnownCmsSubTab(tabId) ? tabId : CmsSubTabs.overview;
        if (!force && selectedTabId !== getHashCmsSubTab() && !confirmDiscardUnsavedChanges()) { return; }
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

    function renderCmsPromptTemplatesTable() {
        renderCmsTable(cmsPromptTemplatesListElement, [{ key: "templateKey", label: "templateKey" }, { key: "targetStudyLanguageId", label: "Study language" }, { key: "isActive", label: "Active" }], cmsPromptTemplates, { onSelect: selectCmsPromptTemplate, selectedId: cmsSelectedPromptTemplate?.id });
    }

    function renderCmsTutorProfilesTable() {
        renderCmsTable(cmsTutorProfilesListElement, [{ key: "tutorId", label: "tutorId" }, { key: "displayName", label: "Display name" }, { key: "isActive", label: "Active" }], cmsTutorProfiles, { onSelect: selectCmsTutorProfile, selectedId: cmsSelectedTutorProfile?.id });
    }

    function renderCmsTable(container, columns, rows, options) {
        container.textContent = "";
        if (!Array.isArray(rows) || rows.length === 0) { const empty = document.createElement("p"); empty.className = "empty-state"; empty.textContent = "No items loaded."; container.appendChild(empty); return; }
        const config = typeof options === "function" ? { onSelect: options } : (options || {});
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
            columns.forEach((column) => { const td = document.createElement("td"); td.textContent = formatValue(row[column.key]); tr.appendChild(td); });
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
        const summary = await adminFetch(cmsPath(ApiPaths.cmsContentPackTemplate, { slug }));
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
    }
    async function loadCmsTutorProfiles() {
        const slug = getSelectedCmsSlug();
        cmsTutorProfiles = await adminFetch(cmsPath(ApiPaths.cmsTutorProfilesTemplate, { slug }));
        cmsTutorProfiles = Array.isArray(cmsTutorProfiles) ? cmsTutorProfiles : [];
        renderCmsTutorProfilesTable();
    }

    async function selectCmsTopic(row, force = false) { if (!force && !restoringCmsSelection && cmsSelectedTopic?.id !== row.id && !confirmDiscardUnsavedChanges()) { return; } cmsSelectedTopic = row; updateHashField("topicKey", row.stableTopicKey || null); renderCmsTopicsTable(); cmsSelectedTopic = await adminFetch(cmsPath(ApiPaths.cmsTopicTemplate, { slug: getSelectedCmsSlug(), topicId: row.id })); fillCmsTopicForm(); renderCmsTopicsTable(); }
    function fillCmsTopicForm() { const item = cmsSelectedTopic; cmsSelectedTopicIdentityElement.textContent = item ? `${item.stableTopicKey} (${item.id})` : "None selected"; cmsTopicTitleInput.value = item?.title || ""; cmsTopicDescriptionInput.value = item?.description || ""; cmsTopicSortOrderInput.value = item?.sortOrder ?? ""; cmsTopicIsActiveInput.checked = Boolean(item?.isActive); setCmsEntityMessage(cmsTopicMessageElement, "", false); setCmsBaseline("topic"); }
    async function selectCmsScenario(row, force = false) { if (!force && !restoringCmsSelection && cmsSelectedScenario?.id !== row.id && !confirmDiscardUnsavedChanges()) { return; } cmsSelectedScenario = row; updateHashField("scenarioKey", row.stableScenarioKey || null); renderCmsScenariosTable(); cmsSelectedScenario = await adminFetch(cmsPath(ApiPaths.cmsScenarioTemplate, { slug: getSelectedCmsSlug(), scenarioId: row.id })); fillCmsScenarioForm(); renderCmsScenariosTable(); }
    function fillCmsScenarioForm() { const item = cmsSelectedScenario; cmsSelectedScenarioIdentityElement.textContent = item ? `${item.stableScenarioKey} (${item.id})` : "None selected"; cmsScenarioTitleInput.value = item?.title || ""; cmsScenarioDescriptionInput.value = item?.description || ""; cmsScenarioSetupMessageInput.value = item?.setupMessage || ""; cmsScenarioIsActiveInput.checked = Boolean(item?.isActive); cmsScenarioDefinitionJsonInput.value = item?.definitionJson || ""; setCmsScenarioJsonStatus(item?.isDefinitionJsonFallback ? "Showing fallback JSON built from existing draft fields; save draft to persist it as full scenario JSON." : "", Boolean(item?.isDefinitionJsonFallback)); setCmsEntityMessage(cmsScenarioMessageElement, "", false); setCmsBaseline("scenario"); }
    async function selectCmsPromptTemplate(row, force = false) { if (!force && !restoringCmsSelection && cmsSelectedPromptTemplate?.id !== row.id && !confirmDiscardUnsavedChanges()) { return; } cmsSelectedPromptTemplate = row; updateHashField("promptTemplateKey", row.templateKey || null); renderCmsPromptTemplatesTable(); cmsSelectedPromptTemplate = await adminFetch(cmsPath(ApiPaths.cmsPromptTemplateTemplate, { slug: getSelectedCmsSlug(), templateId: row.id })); fillCmsPromptTemplateForm(); renderCmsPromptTemplatesTable(); }
    function fillCmsPromptTemplateForm() { const item = cmsSelectedPromptTemplate; cmsSelectedPromptTemplateIdentityElement.textContent = item ? `${item.templateKey} (${item.id})` : "None selected"; cmsPromptTemplateBodyInput.value = item?.body || ""; cmsPromptTemplateIsActiveInput.checked = Boolean(item?.isActive); setCmsEntityMessage(cmsPromptTemplateMessageElement, "", false); setCmsBaseline("promptTemplate"); }
    async function selectCmsTutorProfile(row, force = false) { if (!force && !restoringCmsSelection && cmsSelectedTutorProfile?.id !== row.id && !confirmDiscardUnsavedChanges()) { return; } cmsSelectedTutorProfile = row; updateHashField("tutorId", row.tutorId || null); renderCmsTutorProfilesTable(); cmsSelectedTutorProfile = await adminFetch(cmsPath(ApiPaths.cmsTutorProfileTemplate, { slug: getSelectedCmsSlug(), profileId: row.id })); fillCmsTutorProfileForm(); renderCmsTutorProfilesTable(); }
    function fillCmsTutorProfileForm() { const item = cmsSelectedTutorProfile; cmsSelectedTutorProfileIdentityElement.textContent = item ? `${item.tutorId} (${item.id})` : "None selected"; cmsTutorProfileDisplayNameInput.value = item?.displayName || ""; cmsTutorProfileCommunicationStyleJsonInput.value = item?.communicationStyleJson || ""; cmsTutorProfileSafetyNotesJsonInput.value = item?.safetyNotesJson || ""; cmsTutorProfileIsActiveInput.checked = Boolean(item?.isActive); setCmsEntityMessage(cmsTutorProfileMessageElement, "", false); setCmsBaseline("tutorProfile"); }

    async function saveCmsTopicDraft() {
        if (!cmsSelectedTopic) { setCmsEntityMessage(cmsTopicMessageElement, "Select a topic first.", true); return; }
        await saveCmsDraft(ApiPaths.cmsTopicTemplate, { topicId: cmsSelectedTopic.id }, { title: cmsTopicTitleInput.value, description: cmsTopicDescriptionInput.value, sortOrder: Number(cmsTopicSortOrderInput.value || 0), isActive: cmsTopicIsActiveInput.checked, reason: "Admin CMS UI shell draft edit" }, cmsTopicMessageElement, loadCmsTopics, () => selectCmsTopic(cmsSelectedTopic));
    }
    async function saveCmsScenarioDraft() {
        if (!cmsSelectedScenario) { setCmsEntityMessage(cmsScenarioMessageElement, "Select a scenario first.", true); return; }
        if (!validateCmsScenarioJsonInput()) { setCmsEntityMessage(cmsScenarioMessageElement, "Save draft blocked: fix full scenario JSON first.", true); return; }
        await saveCmsDraft(ApiPaths.cmsScenarioTemplate, { scenarioId: cmsSelectedScenario.id }, { title: cmsScenarioTitleInput.value, description: cmsScenarioDescriptionInput.value, setupMessage: cmsScenarioSetupMessageInput.value, definitionJson: cmsScenarioDefinitionJsonInput.value, isActive: cmsScenarioIsActiveInput.checked, reason: "Admin CMS UI shell draft edit" }, cmsScenarioMessageElement, loadCmsScenarios, () => selectCmsScenario(cmsSelectedScenario));
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
        setCmsError(""); setCmsSuccess(""); setCmsEntityMessage(messageElement, "Saving draft...", false);
        try {
            const payload = await adminFetch(cmsPath(template, Object.assign({ slug: getSelectedCmsSlug() }, replacements)), { method: "PUT", body: JSON.stringify(body) });
            setCmsEntityMessage(messageElement, payload.noChanges ? "Saved: no changes detected." : `Saved draft. Changed fields: ${(payload.changedFields || []).join(", ") || "-"}.`, false);
            await reloadList(); await reloadSelected(); await runCmsValidation(); await loadCmsPreviewSummary(); await loadCmsAuditEntries();
        } catch (error) { const message = getCmsErrorMessage(error); setCmsEntityMessage(messageElement, message, true); if (isAuthErrorMessage(message)) { resetSession(); setError(message); } }
    }

    async function runCmsValidation() {
        setCmsError("");
        try { const validation = await adminFetch(cmsPath(ApiPaths.cmsValidateTemplate, { slug: getSelectedCmsSlug() }), { method: "POST" }); renderJsonOutput(cmsValidationResultElement, validation); if (!validation.success) { setCmsError(`Validation failed with ${(validation.errors || []).length} errors and ${(validation.warnings || []).length} warnings.`); } return validation; }
        catch (error) { handleCmsError(error); return null; }
    }
    async function loadCmsPreviewSummary() {
        setCmsError("");
        try { const preview = await adminFetch(cmsPath(ApiPaths.cmsPreviewSummaryTemplate, { slug: getSelectedCmsSlug() })); renderJsonOutput(cmsPreviewSummaryElement, preview); return preview; }
        catch (error) { handleCmsError(error); return null; }
    }
    async function loadCmsVersions() {
        const versionsPayload = await adminFetch(cmsPath(ApiPaths.cmsVersionsTemplate, { slug: getSelectedCmsSlug() }));
        const versions = Array.isArray(versionsPayload?.versions) ? versionsPayload.versions : [];
        renderCmsVersions(versions); return versionsPayload;
    }
    async function loadCmsAuditEntries() {
        const limit = Number(cmsAuditLimitSelect.value || 25);
        const query = new URLSearchParams({ limit: String(Number.isFinite(limit) ? limit : 25) });
        const payload = await adminFetch(`${cmsPath(ApiPaths.cmsAuditEntriesTemplate, { slug: getSelectedCmsSlug() })}?${query.toString()}`);
        const entries = Array.isArray(payload?.entries) ? payload.entries : [];
        renderCmsAuditEntries(entries);
        return entries;
    }
    function renderCmsAuditEntries(entries) {
        const rows = entries.map((entry) => Object.assign({}, entry, { actor: entry.actorEmail || entry.actorUserId || "-", changedFieldList: (entry.changedFields || []).join(", ") || "-", shortBeforeHash: formatShortHash(entry.beforeHash), shortAfterHash: formatShortHash(entry.afterHash) }));
        renderCmsTable(cmsAuditListElement, [{ key: "createdAtUtc", label: "Timestamp UTC" }, { key: "actor", label: "Actor" }, { key: "entityType", label: "Entity type" }, { key: "stableKey", label: "Stable key" }, { key: "changedFieldList", label: "Changed fields" }, { key: "shortBeforeHash", label: "Before hash" }, { key: "shortAfterHash", label: "After hash" }], rows, null);
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
        if (!confirm("Publish current CMS draft content? Runtime learner behavior still remains static JSON by default.")) { return; }
        setCmsError(""); setCmsSuccess("Publishing CMS draft...");
        try {
            const payload = await adminFetch(cmsPath(ApiPaths.cmsPublishTemplate, { slug: getSelectedCmsSlug() }), { method: "POST", body: JSON.stringify({ changeSummary: summary }) });
            setCmsSuccess(payload.noChanges ? "Publish completed with no draft changes to publish." : `Published CMS draft as version ${payload.versionNumber || "-"}.`);
            await refreshCmsContentPack(); await runCmsValidation(); await loadCmsPreviewSummary();
        } catch (error) { handleCmsError(error); }
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
    selectCmsSubTab(getHashCmsSubTab(), true);
    cmsLoadContentPacksButton.addEventListener("click", async () => { await loadCmsContentPacks(); });
    cmsContentPackSelect.addEventListener("change", async () => { setCmsLoading(true); try { if (await refreshCmsContentPack(false)) { setCmsSuccess("CMS content pack refreshed."); } } catch (error) { handleCmsError(error); } finally { setCmsLoading(false); } });
    cmsRefreshButton.addEventListener("click", async () => { setCmsLoading(true); try { if (await refreshCmsContentPack(false)) { setCmsSuccess("CMS content pack refreshed."); } } catch (error) { handleCmsError(error); } finally { setCmsLoading(false); } });
    cmsTopicFilterInput.addEventListener("input", () => { renderCmsTopicsTable(); });
    cmsScenarioFilterInput.addEventListener("input", () => { renderCmsScenariosTable(); });
    cmsScenarioTopicFilterSelect.addEventListener("change", () => { renderCmsScenariosTable(); });
    cmsTopicForm.addEventListener("submit", async (event) => { event.preventDefault(); await saveCmsTopicDraft(); });
    cmsTopicResetButton.addEventListener("click", async () => { if (cmsSelectedTopic) { await selectCmsTopic(cmsSelectedTopic); } });
    cmsScenarioForm.addEventListener("submit", async (event) => { event.preventDefault(); await saveCmsScenarioDraft(); });
    cmsScenarioResetButton.addEventListener("click", async () => { if (cmsSelectedScenario) { await selectCmsScenario(cmsSelectedScenario); } });
    cmsScenarioFormatJsonButton.addEventListener("click", () => { formatCmsScenarioJsonInput(); });
    cmsScenarioValidateJsonButton.addEventListener("click", () => { validateCmsScenarioJsonInput(); });
    cmsPromptTemplateForm.addEventListener("submit", async (event) => { event.preventDefault(); await saveCmsPromptTemplateDraft(); });
    cmsPromptTemplateResetButton.addEventListener("click", async () => { if (cmsSelectedPromptTemplate) { await selectCmsPromptTemplate(cmsSelectedPromptTemplate); } });
    cmsTutorProfileForm.addEventListener("submit", async (event) => { event.preventDefault(); await saveCmsTutorProfileDraft(); });
    cmsTutorProfileResetButton.addEventListener("click", async () => { if (cmsSelectedTutorProfile) { await selectCmsTutorProfile(cmsSelectedTutorProfile); } });
    cmsRunValidationButton.addEventListener("click", async () => { await runCmsValidation(); });
    cmsLoadPreviewButton.addEventListener("click", async () => { await loadCmsPreviewSummary(); });
    cmsLoadVersionsButton.addEventListener("click", async () => { try { await loadCmsVersions(); setCmsSuccess("CMS versions loaded."); } catch (error) { handleCmsError(error); } });
    cmsLoadAuditButton.addEventListener("click", async () => { try { await loadCmsAuditEntries(); setCmsSuccess("CMS audit entries loaded."); } catch (error) { handleCmsError(error); } });
    cmsPublishButton.addEventListener("click", async () => { await publishCmsDraft(); });
    cmsRestoreButton.addEventListener("click", async () => { await restoreCmsVersion(); });
    [cmsTopicTitleInput, cmsTopicDescriptionInput, cmsTopicSortOrderInput, cmsTopicIsActiveInput].forEach((element) => element.addEventListener("input", () => updateCmsDirtyState("topic")));
    [cmsScenarioTitleInput, cmsScenarioDescriptionInput, cmsScenarioSetupMessageInput, cmsScenarioIsActiveInput, cmsScenarioDefinitionJsonInput].forEach((element) => element.addEventListener("input", () => updateCmsDirtyState("scenario")));
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

    async function loadAdminCapabilities() {
        const capabilitiesResponse = await fetch(ApiPaths.capabilities, { method: "GET", headers: getAdminHeaders() });
        if (capabilitiesResponse.status === HttpStatus.unauthorized || capabilitiesResponse.status === HttpStatus.forbidden) { handleAuthInvalidResponse(); }
        if (!capabilitiesResponse.ok) { throw new Error("Unable to load admin capabilities."); }
        const capabilitiesPayload = await capabilitiesResponse.json();
        adminSourceElement.textContent = capabilitiesPayload.adminSource || "-";
        environmentElement.textContent = capabilitiesPayload.environment || "-";
        checkedAtElement.textContent = capabilitiesPayload.checkedAtUtc || "-";
        capabilitiesListElement.textContent = "";
        Object.keys(capabilitiesPayload.capabilities || {}).forEach((key) => {
            const value = Boolean(capabilitiesPayload.capabilities[key]);
            const item = document.createElement("li");
            item.textContent = key;
            const badge = document.createElement("span");
            badge.className = `badge ${value ? "available" : "unavailable"}`;
            badge.textContent = value ? "available" : "unavailable";
            item.appendChild(badge);
            capabilitiesListElement.appendChild(item);
        });
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
    freeLessonResetForm.addEventListener("submit", async (event) => { event.preventDefault(); await resetFreeLessonAllowanceForSelectedUser(); });
    loadAuditButton.addEventListener("click", async () => { await loadAuditLogForSelectedUser(); });
    logoutButton.addEventListener("click", () => { logoutAdminSession(); });
    initializeTabs();
    updateSelectedUserHeader();
    updateUserRequiredEmptyStates();
    restoreAdminSessionFromCookie();
})();
