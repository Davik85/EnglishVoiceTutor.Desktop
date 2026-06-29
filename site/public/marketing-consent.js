const consentKey = "lvt_marketing_consent_v1";
const deniedConsent = { analytics_storage: "denied", ad_storage: "denied", ad_user_data: "denied", ad_personalization: "denied" };

function hasGtag() { return typeof window.gtag === "function"; }
function readConsent() { try { return JSON.parse(localStorage.getItem(consentKey) || "null"); } catch { return null; } }
function writeConsent(choice) { localStorage.setItem(consentKey, JSON.stringify({ ...choice, savedAt: new Date().toISOString() })); }
function consentUpdate(choice) {
    const update = {
        analytics_storage: choice.analytics ? "granted" : "denied",
        ad_storage: choice.advertising ? "granted" : "denied",
        ad_user_data: choice.advertising ? "granted" : "denied",
        ad_personalization: choice.advertising ? "granted" : "denied",
    };
    if (hasGtag()) { window.gtag("consent", "update", update); }
}
function currentConfig() { return window.lvtMarketing || {}; }
function sendDownloadClick() {
    const choice = readConsent();
    const config = currentConfig();
    if (choice?.analytics && config.gaMeasurementId && hasGtag()) {
        window.gtag("event", "download_windows_click", { app_version: document.getElementById("detail-version")?.textContent || "unknown", channel: document.getElementById("detail-channel")?.textContent || "direct", platform: "windows" });
    }
    if (choice?.advertising && config.googleAdsId && config.downloadConversionLabel && hasGtag()) {
        window.gtag("event", "conversion", { send_to: `${config.googleAdsId}/${config.downloadConversionLabel}` });
    }
}
function setupBanner() {
    const banner = document.getElementById("consent-banner");
    const manage = document.getElementById("consent-manage");
    const save = document.getElementById("consent-save");
    const analytics = document.getElementById("consent-analytics");
    const advertising = document.getElementById("consent-advertising");
    const existing = readConsent();
    if (existing) { consentUpdate(existing); return; }
    if (!banner) { return; }
    banner.hidden = false;
    banner.addEventListener("click", (event) => {
        const action = event.target?.dataset?.consentAction;
        if (!action) { return; }
        if (action === "manage") { manage.hidden = false; save.hidden = false; return; }
        const choice = action === "accept" ? { analytics: true, advertising: true } : action === "reject" ? { analytics: false, advertising: false } : { analytics: Boolean(analytics?.checked), advertising: Boolean(advertising?.checked) };
        writeConsent(choice); consentUpdate(choice); banner.hidden = true;
    });
}
if (hasGtag()) { window.gtag("consent", "default", deniedConsent); }
document.addEventListener("DOMContentLoaded", () => {
    setupBanner();
    document.getElementById("download-button")?.addEventListener("click", sendDownloadClick);
});
