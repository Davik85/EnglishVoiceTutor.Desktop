(() => {
    const ApiPaths = {
        login: "/api/auth/login",
        capabilities: "/api/admin/capabilities",
        userLookupByEmail: "/api/admin/users/by-email",
        auditActionsTemplate: "/api/admin/users/{userId}/audit-actions",
        manualPremiumGrantTemplate: "/api/admin/users/{userId}/premium-grants",
        manualPremiumRevokeTemplate: "/api/admin/users/{userId}/premium-grants/{entitlementId}/revoke",
        freeLessonAllowanceResetTemplate: "/api/admin/users/{userId}/free-lesson-allowance/reset"
    };

    const HttpStatus = { badRequest: 400, unauthorized: 401, forbidden: 403, notFound: 404, conflict: 409 };
    const ErrorMessages = {
        emailRequired: "Email is required or invalid.",
        userNotFound: "User was not found.",
        signInAgain: "Please sign in again.",
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
    const Tabs = Object.freeze({ overview: "overview", userLookup: "user-lookup", premium: "premium", freeLesson: "free-lesson", auditLog: "audit-log", system: "system" });

    let accessToken = null;
    let selectedUserId = null;
    let selectedUserEmail = null;
    let selectedUserLookupPayload = null;

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

    function activateTab(tabId) {
        tabButtons.forEach((button) => {
            const isActive = button.dataset.tabId === tabId;
            button.classList.toggle("active", isActive);
            button.setAttribute("aria-selected", isActive ? "true" : "false");
        });
        tabPanels.forEach((panel) => {
            const panelTabId = panel.id.replace("tab-panel-", "");
            const isActive = panelTabId === tabId;
            panel.classList.toggle("hidden", !isActive);
            panel.setAttribute("aria-hidden", isActive ? "false" : "true");
        });
    }

    function initializeTabs() {
        tabButtons.forEach((button) => button.addEventListener("click", () => activateTab(button.dataset.tabId || Tabs.overview)));
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
    const setLookupError = (message) => { lookupErrorElement.textContent = message || ""; };
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

    function setLookupLoading(isLoading) { lookupLoadingElement.classList.toggle("hidden", !isLoading); searchUserButton.disabled = isLoading; }

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
        setLookupError(""); setLookupLoading(false); clearUserLookupResult(); lookupForm.reset(); selectedUserId = null; selectedUserEmail = null; selectedUserLookupPayload = null;
        setGrantVisible(false); setRevokeVisible(false); setFreeLessonResetVisible(false); clearGrantState(); clearRevokeState(); clearFreeLessonResetState(); grantForm.reset(); revokeForm.reset(); freeLessonResetForm.reset(); clearAuditLog();
    }

    function resetSession() { accessToken = null; loginForm.reset(); setError(""); resetDashboard(); setDashboardVisible(false); }

    async function fetchUserByEmail(email) {
        const response = await fetch(`${ApiPaths.userLookupByEmail}?email=${encodeURIComponent(email)}`, { method: "GET", headers: { Authorization: `Bearer ${accessToken}` } });
        if (response.status === HttpStatus.badRequest) { throw new Error(ErrorMessages.emailRequired); }
        if (response.status === HttpStatus.notFound) { throw new Error(ErrorMessages.userNotFound); }
        if (response.status === HttpStatus.unauthorized) { throw new Error(ErrorMessages.signInAgain); }
        if (response.status === HttpStatus.forbidden) { throw new Error(ErrorMessages.accessDenied); }
        if (!response.ok) { throw new Error(ErrorMessages.lookupFailed); }
        return response.json();
    }

    async function fetchAuditActions(userId, limit) {
        const response = await fetch(`${ApiPaths.auditActionsTemplate.replace("{userId}", encodeURIComponent(userId))}?limit=${encodeURIComponent(limit)}`, { method: "GET", headers: { Authorization: `Bearer ${accessToken}` } });
        if (response.status === HttpStatus.badRequest) { throw new Error(ErrorMessages.invalidAuditLimit); }
        if (response.status === HttpStatus.notFound) { throw new Error(ErrorMessages.auditTargetNotFound); }
        if (response.status === HttpStatus.unauthorized) { throw new Error(ErrorMessages.signInAgain); }
        if (response.status === HttpStatus.forbidden) { throw new Error(ErrorMessages.accessDenied); }
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

    async function refreshSelectedUserAfterMutation() {
        if (!selectedUserEmail) { return; }
        const payload = await fetchUserByEmail(selectedUserEmail);
        renderUserLookupResult(payload);
        selectedUserId = payload?.user?.userId || null;
        selectedUserEmail = payload?.user?.email || selectedUserEmail;
        selectedUserLookupPayload = payload;
        setGrantVisible(Boolean(selectedUserId));
        setRevokeVisible(Boolean(selectedUserId));
        setFreeLessonResetVisible(Boolean(selectedUserId));
        await loadAuditLogForSelectedUser();
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
                headers: { "Content-Type": "application/json", Authorization: `Bearer ${accessToken}` },
                body: JSON.stringify({ durationDays: validation.durationDays, reason: validation.reason })
            });

            if (response.status === HttpStatus.badRequest) { throw new Error(ErrorMessages.grantInvalid); }
            if (response.status === HttpStatus.notFound) { throw new Error(ErrorMessages.grantUserNotFound); }
            if (response.status === HttpStatus.unauthorized) { throw new Error(ErrorMessages.signInAgain); }
            if (response.status === HttpStatus.forbidden) { throw new Error(ErrorMessages.accessDenied); }
            if (!(response.status === 200 || response.status === 201)) { throw new Error(ErrorMessages.grantFailed); }

            const payload = await response.json();
            setGrantSuccess(`Premium granted. Entitlement ID: ${payload.entitlementId || "-"}. Starts at: ${payload.startsAtUtc || "-"}. Expires at: ${payload.expiresAtUtc || "-"}.`);
            grantReasonInput.value = "";
            await refreshSelectedUserAfterMutation();
        } catch (error) {
            const message = error instanceof Error ? error.message : ErrorMessages.grantFailed;
            setGrantError(message);
            if (message === ErrorMessages.signInAgain || message === ErrorMessages.accessDenied) {
                resetSession();
                setError(message);
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
                headers: { "Content-Type": "application/json", Authorization: `Bearer ${accessToken}` },
                body: JSON.stringify({ reason: validation.reason })
            });

            if (response.status === HttpStatus.badRequest) { throw new Error(ErrorMessages.revokeInvalid); }
            if (response.status === HttpStatus.notFound) { throw new Error(ErrorMessages.revokeNotFound); }
            if (response.status === HttpStatus.conflict) { throw new Error(ErrorMessages.revokeConflict); }
            if (response.status === HttpStatus.unauthorized) { throw new Error(ErrorMessages.signInAgain); }
            if (response.status === HttpStatus.forbidden) { throw new Error(ErrorMessages.accessDenied); }
            if (!response.ok) { throw new Error(ErrorMessages.revokeFailed); }

            const payload = await response.json();
            setRevokeSuccess(`Premium revoked. Entitlement ID: ${payload.entitlementId || validation.entitlementId}. Revoked at: ${payload.revokedAtUtc || "-"}.`);
            revokeReasonInput.value = "";
            await refreshSelectedUserAfterMutation();
        } catch (error) {
            const message = error instanceof Error ? error.message : ErrorMessages.revokeFailed;
            setRevokeError(message);
            if (message === ErrorMessages.signInAgain || message === ErrorMessages.accessDenied) {
                resetSession();
                setError(message);
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
                headers: { "Content-Type": "application/json", Authorization: `Bearer ${accessToken}` },
                body: JSON.stringify({ usageDate: validation.usageDate, reason: validation.reason })
            });

            if (response.status === HttpStatus.badRequest) { throw new Error(ErrorMessages.resetInvalid); }
            if (response.status === HttpStatus.notFound) { throw new Error(ErrorMessages.resetNotFound); }
            if (response.status === HttpStatus.unauthorized) { throw new Error(ErrorMessages.signInAgain); }
            if (response.status === HttpStatus.forbidden) { throw new Error(ErrorMessages.accessDenied); }
            if (!response.ok) { throw new Error(ErrorMessages.resetFailed); }

            const payload = await response.json();
            setFreeLessonResetSuccess(`Free lesson allowance reset for ${validation.usageDate}. Removed usage ID: ${payload.removedDailyFreeLessonUsageId || "-"}.`);
            freeLessonResetReasonInput.value = "";
            await refreshSelectedUserAfterMutation();
        } catch (error) {
            const message = error instanceof Error ? error.message : ErrorMessages.resetFailed;
            setFreeLessonResetError(message);
            if (message === ErrorMessages.signInAgain || message === ErrorMessages.accessDenied) {
                resetSession();
                setError(message);
            }
        } finally { setFreeLessonResetLoading(false); }
    }

    async function loadAuditLogForSelectedUser() {
        if (!selectedUserId) { clearAuditLog(); return; }
        setAuditError(""); setAuditLoading(true);
        try { renderAuditLog(await fetchAuditActions(selectedUserId, getSelectedAuditLimit())); }
        catch (error) { auditResultElement.textContent = ""; const message = error instanceof Error ? error.message : ErrorMessages.auditLoadFailed; setAuditError(message); if (message === ErrorMessages.signInAgain || message === ErrorMessages.accessDenied) { resetSession(); setError(message); } }
        finally { setAuditLoading(false); }
    }

    loginForm.addEventListener("submit", async (event) => {
        event.preventDefault(); setError(""); signInButton.disabled = true;
        try {
            const formData = new FormData(loginForm);
            const loginResponse = await fetch(ApiPaths.login, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email: String(formData.get("email") || "").trim(), password: String(formData.get("password") || "") }) });
            if (!loginResponse.ok) { throw new Error("Login failed. Check your email and password."); }
            const loginBody = await loginResponse.json(); if (!loginBody?.accessToken) { throw new Error("Login failed. Access token is missing."); }
            accessToken = loginBody.accessToken;
            const capabilitiesResponse = await fetch(ApiPaths.capabilities, { method: "GET", headers: { Authorization: `Bearer ${accessToken}` } });
            if (capabilitiesResponse.status === HttpStatus.unauthorized) { throw new Error(ErrorMessages.signInAgain); }
            if (capabilitiesResponse.status === HttpStatus.forbidden) { throw new Error(ErrorMessages.accessDenied); }
            if (!capabilitiesResponse.ok) { throw new Error("Unable to load admin capabilities."); }
            const capabilitiesPayload = await capabilitiesResponse.json();
            adminSourceElement.textContent = capabilitiesPayload.adminSource || "-"; environmentElement.textContent = capabilitiesPayload.environment || "-"; checkedAtElement.textContent = capabilitiesPayload.checkedAtUtc || "-";
            capabilitiesListElement.textContent = ""; Object.keys(capabilitiesPayload.capabilities || {}).forEach((key) => { const value = Boolean(capabilitiesPayload.capabilities[key]); const item = document.createElement("li"); item.textContent = key; const badge = document.createElement("span"); badge.className = `badge ${value ? "available" : "unavailable"}`; badge.textContent = value ? "available" : "unavailable"; item.appendChild(badge); capabilitiesListElement.appendChild(item); });
            setDashboardVisible(true); initializeTabs(); activateTab(Tabs.overview); updateSelectedUserHeader(); updateUserRequiredEmptyStates();
        } catch (error) { resetSession(); setError(error instanceof Error ? error.message : "Unexpected error."); }
        finally { signInButton.disabled = false; }
    });

    lookupForm.addEventListener("submit", async (event) => {
        event.preventDefault(); setLookupError(""); clearUserLookupResult(); clearGrantState();
        const email = String(lookupEmailInput.value || "").trim();
        if (!email) { selectedUserId = null; selectedUserEmail = null; selectedUserLookupPayload = null; setGrantVisible(false); setRevokeVisible(false); setFreeLessonResetVisible(false); clearAuditLog(); setLookupError(ErrorMessages.emailRequired); return; }
        setLookupLoading(true);
        try {
            const payload = await fetchUserByEmail(email);
            renderUserLookupResult(payload);
            selectedUserId = payload?.user?.userId || null;
            selectedUserEmail = payload?.user?.email || null;
            selectedUserLookupPayload = payload;
            setGrantVisible(Boolean(selectedUserId));
            setRevokeVisible(Boolean(selectedUserId));
            setFreeLessonResetVisible(Boolean(selectedUserId));
            clearAuditLog();
            await loadAuditLogForSelectedUser();
        } catch (error) {
            selectedUserId = null; selectedUserEmail = null; selectedUserLookupPayload = null; clearUserLookupResult(); setGrantVisible(false); setRevokeVisible(false); setFreeLessonResetVisible(false); clearGrantState(); clearRevokeState(); clearFreeLessonResetState(); clearAuditLog();
            const message = error instanceof Error ? error.message : ErrorMessages.lookupFailed; setLookupError(message);
            if (message === ErrorMessages.signInAgain || message === ErrorMessages.accessDenied) { resetSession(); setError(message); }
        } finally { setLookupLoading(false); }
    });

    grantForm.addEventListener("submit", async (event) => { event.preventDefault(); await grantPremiumForSelectedUser(); });
    revokeEntitlementIdElement.addEventListener("change", () => { renderSelectedRevokeEntitlementDetails(); updateRevokeControlsState(false); });
    revokeForm.addEventListener("submit", async (event) => { event.preventDefault(); await revokePremiumForSelectedUser(); });
    freeLessonResetForm.addEventListener("submit", async (event) => { event.preventDefault(); await resetFreeLessonAllowanceForSelectedUser(); });
    loadAuditButton.addEventListener("click", async () => { await loadAuditLogForSelectedUser(); });
    logoutButton.addEventListener("click", () => { resetSession(); });
    updateSelectedUserHeader();
    updateUserRequiredEmptyStates();
})();
