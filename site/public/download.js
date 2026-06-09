const manifestUrl = "/releases/windows/direct/latest.json";
const fallbackInstallerUrl = "/releases/windows/direct/LanguageVoiceTutorSetup-0.1.13-tester.1.exe";

const elements = {
    currentVersion: document.getElementById("current-version"),
    downloadButton: document.getElementById("download-button"),
    manifestStatus: document.getElementById("manifest-status"),
    detailVersion: document.getElementById("detail-version"),
    detailChannel: document.getElementById("detail-channel"),
    detailSize: document.getElementById("detail-size"),
    detailSha: document.getElementById("detail-sha"),
    fallbackDownload: document.getElementById("fallback-download"),
};

function setText(element, value) {
    if (element) {
        element.textContent = value || "Unavailable";
    }
}

function formatBytes(bytes) {
    if (!Number.isFinite(bytes) || bytes <= 0) {
        return "Unavailable";
    }

    const units = ["bytes", "KB", "MB", "GB"];
    let value = bytes;
    let unitIndex = 0;

    while (value >= 1024 && unitIndex < units.length - 1) {
        value /= 1024;
        unitIndex += 1;
    }

    if (unitIndex === 0) {
        return `${value} ${units[unitIndex]}`;
    }

    return `${value.toFixed(1)} ${units[unitIndex]}`;
}

function getSafeInstallerUrl(installerRelativeUrl) {
    if (typeof installerRelativeUrl !== "string") {
        return fallbackInstallerUrl;
    }

    const trimmedUrl = installerRelativeUrl.trim();
    if (!trimmedUrl.startsWith("/releases/windows/direct/") || !trimmedUrl.endsWith(".exe")) {
        return fallbackInstallerUrl;
    }

    return trimmedUrl;
}

function applyManifest(manifest) {
    const installerUrl = getSafeInstallerUrl(manifest.installerRelativeUrl);
    const version = manifest.version || "Unavailable";
    const channel = manifest.channel || "Unavailable";
    const installerSize = Number(manifest.installerSizeBytes);
    const sha256 = manifest.installerSha256 || "Unavailable";

    elements.downloadButton.href = installerUrl;
    setText(elements.currentVersion, version);
    setText(elements.detailVersion, version);
    setText(elements.detailChannel, channel);
    setText(elements.detailSize, formatBytes(installerSize));
    setText(elements.detailSha, sha256);
    setText(elements.manifestStatus, "Release manifest loaded.");
}

function applyManifestFailure() {
    elements.downloadButton.href = fallbackInstallerUrl;
    setText(elements.currentVersion, "Unavailable");
    setText(elements.manifestStatus, "Release manifest could not be loaded. The fallback download link is available below.");

    if (elements.fallbackDownload) {
        elements.fallbackDownload.hidden = false;
    }
}

async function loadManifest() {
    try {
        const response = await fetch(manifestUrl, { cache: "no-store" });
        if (!response.ok) {
            throw new Error(`Manifest request failed with ${response.status}`);
        }

        const manifest = await response.json();
        applyManifest(manifest);
    }
    catch {
        applyManifestFailure();
    }
}

loadManifest();
