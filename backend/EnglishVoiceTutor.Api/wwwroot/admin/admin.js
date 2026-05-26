(() => {
    const ApiPaths = { login: "/api/auth/login", capabilities: "/api/admin/capabilities" };
    const HttpStatus = { unauthorized: 401, forbidden: 403 };
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

    function setDashboardVisible(isVisible) { dashboard.classList.toggle("hidden", !isVisible); loginCard.classList.toggle("hidden", isVisible); }
    function setError(message) { loginError.textContent = message; }
    function resetDashboard() { adminSourceElement.textContent = "-"; environmentElement.textContent = "-"; checkedAtElement.textContent = "-"; capabilitiesListElement.innerHTML = ""; }

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
        if (response.status === HttpStatus.unauthorized) { throw new Error("Please sign in again."); }
        if (response.status === HttpStatus.forbidden) { throw new Error("Access denied. This account is not an admin."); }
        if (!response.ok) { throw new Error("Unable to load admin capabilities."); }
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
        } catch (error) {
            accessToken = null;
            setDashboardVisible(false);
            resetDashboard();
            setError(error instanceof Error ? error.message : "Unexpected error.");
        } finally {
            signInButton.disabled = false;
        }
    });

    logoutButton.addEventListener("click", () => {
        accessToken = null;
        loginForm.reset();
        setError("");
        resetDashboard();
        setDashboardVisible(false);
    });
})();
