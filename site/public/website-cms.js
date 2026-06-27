(() => {
    const endpoint = "https://api.languagevoicetutor.com/api/website/texts";

    function appendPlainText(target, text) {
        const lines = String(text || "").trim().split(/\r?\n/);
        lines.forEach((line, index) => {
            if (index > 0) { target.appendChild(document.createElement("br")); }
            target.appendChild(document.createTextNode(line));
        });
    }

    function renderPlainText(target, text) {
        const value = String(text || "").trim();
        if (!target || !value) { return; }

        if (target.matches("main.legal-page")) {
            target.querySelectorAll(":scope > section:not(.legal-nav)").forEach((section) => { section.classList.add("hidden"); });
            let cmsSection = target.querySelector(":scope > section[data-website-cms-rendered]");
            if (!cmsSection) {
                cmsSection = document.createElement("section");
                cmsSection.className = "details-card legal-section";
                cmsSection.setAttribute("data-website-cms-rendered", "true");
                const nav = target.querySelector(":scope > .legal-nav");
                target.insertBefore(cmsSection, nav || null);
            }
            cmsSection.textContent = "";
            appendPlainText(cmsSection, value);
            return;
        }

        target.textContent = "";
        appendPlainText(target, value);
    }

    async function loadWebsiteCmsText() {
        const targets = Array.from(document.querySelectorAll("[data-website-cms-section]"));
        if (targets.length === 0) { return; }

        try {
            const response = await fetch(endpoint, { method: "GET", credentials: "omit", cache: "no-store" });
            if (!response.ok) { return; }
            const payload = await response.json();
            const texts = payload && typeof payload.texts === "object" && payload.texts !== null ? payload.texts : {};
            targets.forEach((target) => {
                const key = target.getAttribute("data-website-cms-section");
                const text = typeof texts[key] === "string" ? texts[key] : "";
                renderPlainText(target, text);
            });
        } catch {
            // Static HTML remains visible when the CMS backend is unavailable.
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", loadWebsiteCmsText, { once: true });
    } else {
        loadWebsiteCmsText();
    }
})();
