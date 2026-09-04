const fallbackMarketing = { gaMeasurementId: '', googleAdsId: '', downloadConversionLabel: '' };
if (!window.lvtMarketing) { window.lvtMarketing = fallbackMarketing; }
const consentKey = "lvt_marketing_consent_v1";
const deniedConsent = { analytics_storage: "denied", ad_storage: "denied", ad_user_data: "denied", ad_personalization: "denied" };
function hasGtag() { return typeof window.gtag === "function"; }
function readConsent() { try { return JSON.parse(localStorage.getItem(consentKey) || "null"); } catch { return null; } }
function writeConsent(choice) { localStorage.setItem(consentKey, JSON.stringify({ ...choice, savedAt: new Date().toISOString() })); }
function ensureDataLayer() { window.dataLayer = window.dataLayer || []; if (!hasGtag()) { window.gtag = function(){ window.dataLayer.push(arguments); }; } }
function consentUpdate(choice) {
    const update = {
        analytics_storage: choice?.analytics ? "granted" : "denied",
        ad_storage: choice?.advertising ? "granted" : "denied",
        ad_user_data: choice?.advertising ? "granted" : "denied",
        ad_personalization: choice?.advertising ? "granted" : "denied"
    };
    ensureDataLayer(); window.gtag("consent", "update", update);
}
function loadGoogleTags(choice) {
    const config = window.lvtMarketing || {};
    const tagId = choice?.analytics && config.gaMeasurementId ? config.gaMeasurementId : choice?.advertising && config.googleAdsId ? config.googleAdsId : "";
    if (!tagId || document.querySelector("script[data-lvt-google-tag]")) return;
    ensureDataLayer(); window.gtag('consent', 'default', deniedConsent); window.gtag("js", new Date());
    if (choice?.analytics && config.gaMeasurementId) { window.gtag("config", config.gaMeasurementId); }
    if (choice?.advertising && config.googleAdsId) { window.gtag("config", config.googleAdsId); }
    const script = document.createElement("script"); script.async = true; script.src = `https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(tagId)}`; script.dataset.lvtGoogleTag = "true"; document.head.appendChild(script);
}
function applyConsent(choice) { consentUpdate(choice); loadGoogleTags(choice); }
function trackDownloadClick() {
    const config = window.lvtMarketing || {};
    const choice = readConsent();
    if (hasGtag() && config.gaMeasurementId && choice?.analytics) {
        window.gtag("event", "download_windows_click", { platform: "windows", transport_type: "beacon" });
    }
    if (hasGtag() && config.googleAdsId && config.downloadConversionLabel && choice?.advertising) {
        window.gtag("event", "conversion", { send_to: `${config.googleAdsId}/${config.downloadConversionLabel}`, transport_type: "beacon" });
    }
}
ensureDataLayer(); window.gtag('consent', 'default', deniedConsent);
window.addEventListener("DOMContentLoaded", () => {
    const existing = readConsent();
    if (existing) { applyConsent(existing); }
    document.getElementById("download-button")?.addEventListener("click", trackDownloadClick);
    const banner = document.getElementById("consent-banner");
    if (!banner || existing) return;
    const manage = document.getElementById("consent-manage");
    const save = document.getElementById("consent-save");
    const analytics = document.getElementById("consent-analytics");
    const advertising = document.getElementById("consent-advertising");
    banner.hidden = false;
    banner.addEventListener("click", event => {
        const action = event.target?.closest("button")?.dataset?.consentAction;
        if (!action) return;
        if (action === "manage") { manage.hidden = false; save.hidden = false; return; }
        const choice = action === "accept" ? { analytics: true, advertising: true } : action === "save" ? { analytics: !!analytics.checked, advertising: !!advertising.checked } : { analytics: false, advertising: false };
        writeConsent(choice); applyConsent(choice); banner.hidden = true;
    });
});
