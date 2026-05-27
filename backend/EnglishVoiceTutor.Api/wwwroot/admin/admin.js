(() => {
    const ApiPaths = {
        login: "/api/auth/login",
        capabilities: "/api/admin/capabilities",
        userLookupByEmail: "/api/admin/users/by-email"
    };

    const HttpStatus = { badRequest: 400, unauthorized: 401, forbidden: 403, notFound: 404 };
    const ErrorMessages = {
        emailRequired: "Email is required or invalid.",
        userNotFound: "User was not found.",
        signInAgain: "Please sign in again.",
        accessDenied: "Access denied. This account is not an admin.",
        lookupFailed: "Unable to load user."
    };

    const CapabilityLabels = {
        adminSelfCheck: "Admin Self Check",
        userLookupByEmail: "User Lookup by Email",
        userDiagnosticsRead: "User Diagnostics (Read)",
        auditLogRead: "Audit Log (Read)",
        manualPremiumGrant: "Manual Premium Grant",
        manualPremiumRevoke: "Manual Premium Revoke",
        freeLessonAllowanceReset: "Free Lesson Allowance Reset",
        billingPaddlePlaceholder: "Billing / Paddle",
        productionRolesPlaceholder: "Production Roles"
    };

    let accessToken = null;

    const loginCard = document.getElementById("login-card");
    const dashboard = document.getElementById("dashboard");
    const loginForm = document.getElementById("login-form");
    const loginError = document.getElementById("login-error");
    const signInButton = document.getElementById("sign-in-button");
    const logoutButton = document.getElementById("logout-button");
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

    const SummaryFields = ["userId", "email", "status", "createdAt", "lastLoginAt"];
    const SubscriptionFields = ["planId", "planName", "premiumActive", "trialActive", "trialEndsAtUtc", "subscriptionStatus", "billingProvider", "currentPeriodEndUtc", "freeLessonUsedToday", "freeLessonRemainingToday", "enforcementEnabled", "source", "checkedAtUtc"];

    const ActiveEntitlementColumns = ["entitlementId", "planId", "entitlementType", "source", "status", "startsAtUtc", "expiresAtUtc", "reason", "createdAt", "updatedAt"];
    const LessonSessionColumns = ["sessionId", "lessonContentId", "studyLanguage", "topicTitle", "subtopicTitle", "level", "modeUsed", "status", "startedAt", "finishedAt", "validTurnCount", "estimatedCost"];
    const DailyUsageColumns = ["usageDate", "studyLanguage", "lessonsStarted", "lessonsCompleted", "chatReplyCount", "hintsUsed", "feedbackRequests", "transcriptionSeconds", "ttsSeconds", "estimatedCost", "updatedAt"];
    const UsageEventColumns = ["usageEventId", "sessionId", "operation", "model", "studyLanguage", "status", "inputTokens", "outputTokens", "audioDurationMs", "inputChars", "outputBytes", "estimatedCost", "createdAt"];

    function setDashboardVisible(isVisible) {
        dashboard.classList.toggle("hidden", !isVisible);
        loginCard.classList.toggle("hidden", isVisible);
    }

    function setError(message) { loginError.textContent = message; }
    function setLookupError(message) { lookupErrorElement.textContent = message || ""; }

    function setLookupLoading(isLoading) {
        lookupLoadingElement.classList.toggle("hidden", !isLoading);
        searchUserButton.disabled = isLoading;
    }

    function clearUserLookupResult() {
        lookupResultElement.textContent = "";
    }

    function formatValue(value) {
        if (value === null || value === undefined || value === "") {
            return "-";
        }

        if (typeof value === "boolean") {
            return value ? "Yes" : "No";
        }

        return String(value);
    }

    function renderKeyValueList(container, data, emptyMessage) {
        container.textContent = "";
        if (!data || typeof data !== "object" || Object.keys(data).length === 0) {
            const empty = document.createElement("p");
            empty.className = "empty-state";
            empty.textContent = emptyMessage;
            container.appendChild(empty);
            return;
        }

        const list = document.createElement("dl");
        list.className = "kv-list";

        Object.keys(data).forEach((key) => {
            const dt = document.createElement("dt");
            dt.textContent = key;
            const dd = document.createElement("dd");
            dd.textContent = formatValue(data[key]);
            list.appendChild(dt);
            list.appendChild(dd);
        });

        container.appendChild(list);
    }

    function renderTable(container, items, columns, emptyMessage) {
        container.textContent = "";
        if (!Array.isArray(items) || items.length === 0) {
            const empty = document.createElement("p");
            empty.className = "empty-state";
            empty.textContent = emptyMessage;
            container.appendChild(empty);
            return;
        }

        const tableWrap = document.createElement("div");
        tableWrap.className = "table-wrap";

        const table = document.createElement("table");
        table.className = "compact-table";

        const thead = document.createElement("thead");
        const headRow = document.createElement("tr");
        columns.forEach((column) => {
            const th = document.createElement("th");
            th.scope = "col";
            th.textContent = column;
            headRow.appendChild(th);
        });
        thead.appendChild(headRow);
        table.appendChild(thead);

        const tbody = document.createElement("tbody");
        items.forEach((item) => {
            const row = document.createElement("tr");
            columns.forEach((column) => {
                const cell = document.createElement("td");
                cell.textContent = formatValue(item ? item[column] : null);
                row.appendChild(cell);
            });
            tbody.appendChild(row);
        });

        table.appendChild(tbody);
        tableWrap.appendChild(table);
        container.appendChild(tableWrap);
    }

    function createSection(title) {
        const section = document.createElement("section");
        section.className = "lookup-section";
        const heading = document.createElement("h3");
        heading.textContent = title;
        section.appendChild(heading);
        return section;
    }

    function pickFields(source, fields) {
        const result = {};
        fields.forEach((field) => {
            result[field] = source && typeof source === "object" ? source[field] : null;
        });
        return result;
    }

    function renderUserLookupResult(payload) {
        clearUserLookupResult();

        const userSection = createSection("User Summary");
        const userContainer = document.createElement("div");
        renderKeyValueList(userContainer, pickFields(payload.user, SummaryFields), "No user data.");
        userSection.appendChild(userContainer);
        lookupResultElement.appendChild(userSection);

        const subscriptionSection = createSection("Subscription Status");
        const subscriptionContainer = document.createElement("div");
        const subscription = Object.assign({}, pickFields(payload.subscriptionStatus, SubscriptionFields), { checkedAtUtc: payload.checkedAtUtc || payload.subscriptionStatus?.checkedAtUtc || null });
        renderKeyValueList(subscriptionContainer, subscription, "No subscription status data.");
        subscriptionSection.appendChild(subscriptionContainer);
        lookupResultElement.appendChild(subscriptionSection);

        const profileSection = createSection("Profile");
        const profileContainer = document.createElement("div");
        renderKeyValueList(profileContainer, payload.profile, "No profile data.");
        profileSection.appendChild(profileContainer);
        lookupResultElement.appendChild(profileSection);

        const settingsSection = createSection("Settings");
        const settingsContainer = document.createElement("div");
        renderKeyValueList(settingsContainer, payload.settings, "No settings data.");
        settingsSection.appendChild(settingsContainer);
        lookupResultElement.appendChild(settingsSection);

        const entitlementsSection = createSection("Active Entitlements");
        const entitlementsContainer = document.createElement("div");
        renderTable(entitlementsContainer, payload.activeEntitlements, ActiveEntitlementColumns, "No active entitlements.");
        entitlementsSection.appendChild(entitlementsContainer);
        lookupResultElement.appendChild(entitlementsSection);

        const lessonsSection = createSection("Recent Lesson Sessions");
        const lessonsContainer = document.createElement("div");
        renderTable(lessonsContainer, payload.recentLessonSessions, LessonSessionColumns, "No recent lesson sessions.");
        lessonsSection.appendChild(lessonsContainer);
        lookupResultElement.appendChild(lessonsSection);

        const countersSection = createSection("Daily Usage Counters");
        const countersContainer = document.createElement("div");
        renderTable(countersContainer, payload.dailyUsageCounters, DailyUsageColumns, "No daily usage counters.");
        countersSection.appendChild(countersContainer);
        lookupResultElement.appendChild(countersSection);

        const eventsSection = createSection("Recent Usage Events");
        const eventsContainer = document.createElement("div");
        renderTable(eventsContainer, payload.recentUsageEvents, UsageEventColumns, "No recent usage events.");
        eventsSection.appendChild(eventsContainer);
        lookupResultElement.appendChild(eventsSection);
    }

    function resetDashboard() {
        adminSourceElement.textContent = "-";
        environmentElement.textContent = "-";
        checkedAtElement.textContent = "-";
        capabilitiesListElement.innerHTML = "";
        setLookupError("");
        setLookupLoading(false);
        clearUserLookupResult();
        lookupForm.reset();
    }

    function resetSession() {
        accessToken = null;
        loginForm.reset();
        setError("");
        resetDashboard();
        setDashboardVisible(false);
    }

    function renderCapabilities(capabilities) {
        capabilitiesListElement.innerHTML = "";
        Object.keys(capabilities).forEach((key) => {
            const value = Boolean(capabilities[key]);
            const item = document.createElement("li");
            item.textContent = CapabilityLabels[key] || key;
            const badge = document.createElement("span");
            badge.className = `badge ${value ? "available" : "unavailable"}`;
            badge.textContent = value ? "available" : "unavailable";
            item.appendChild(badge);
            capabilitiesListElement.appendChild(item);
        });
    }

    async function login(email, password) {
        const response = await fetch(ApiPaths.login, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email, password }) });
        if (!response.ok) { throw new Error("Login failed. Check your email and password."); }
        const body = await response.json();
        if (!body || !body.accessToken) { throw new Error("Login failed. Access token is missing."); }
        accessToken = body.accessToken;
    }

    async function fetchCapabilities() {
        const response = await fetch(ApiPaths.capabilities, { method: "GET", headers: { Authorization: `Bearer ${accessToken}` } });
        if (response.status === HttpStatus.unauthorized) { throw new Error(ErrorMessages.signInAgain); }
        if (response.status === HttpStatus.forbidden) { throw new Error(ErrorMessages.accessDenied); }
        if (!response.ok) { throw new Error("Unable to load admin capabilities."); }
        return response.json();
    }

    async function fetchUserByEmail(email) {
        const encodedEmail = encodeURIComponent(email);
        const response = await fetch(`${ApiPaths.userLookupByEmail}?email=${encodedEmail}`, { method: "GET", headers: { Authorization: `Bearer ${accessToken}` } });

        if (response.status === HttpStatus.badRequest) {
            throw new Error(ErrorMessages.emailRequired);
        }

        if (response.status === HttpStatus.notFound) {
            throw new Error(ErrorMessages.userNotFound);
        }

        if (response.status === HttpStatus.unauthorized) {
            throw new Error(ErrorMessages.signInAgain);
        }

        if (response.status === HttpStatus.forbidden) {
            throw new Error(ErrorMessages.accessDenied);
        }

        if (!response.ok) {
            throw new Error(ErrorMessages.lookupFailed);
        }

        return response.json();
    }

    loginForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        setError("");
        signInButton.disabled = true;

        const formData = new FormData(loginForm);
        const email = String(formData.get("email") || "").trim();
        const password = String(formData.get("password") || "");

        try {
            await login(email, password);
            const capabilitiesPayload = await fetchCapabilities();
            adminSourceElement.textContent = capabilitiesPayload.adminSource || "-";
            environmentElement.textContent = capabilitiesPayload.environment || "-";
            checkedAtElement.textContent = capabilitiesPayload.checkedAtUtc || "-";
            renderCapabilities(capabilitiesPayload.capabilities || {});
            setDashboardVisible(true);
            setLookupError("");
        } catch (error) {
            resetSession();
            setError(error instanceof Error ? error.message : "Unexpected error.");
        } finally {
            signInButton.disabled = false;
        }
    });

    lookupForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        setLookupError("");
        clearUserLookupResult();

        const email = String(lookupEmailInput.value || "").trim();
        if (!email) {
            setLookupError(ErrorMessages.emailRequired);
            return;
        }

        setLookupLoading(true);

        try {
            const payload = await fetchUserByEmail(email);
            renderUserLookupResult(payload);
        } catch (error) {
            const message = error instanceof Error ? error.message : ErrorMessages.lookupFailed;
            setLookupError(message);

            if (message === ErrorMessages.signInAgain || message === ErrorMessages.accessDenied) {
                resetSession();
                setError(message);
            }
        } finally {
            setLookupLoading(false);
        }
    });

    logoutButton.addEventListener("click", () => {
        resetSession();
    });
})();
