const manifestUrl = "/releases/windows/direct/latest.json";
const releaseBaseUrl = "/releases/windows/direct/";

const elements = {
    currentVersion: document.getElementById("current-version"),
    downloadButton: document.getElementById("download-button"),
    manifestStatus: document.getElementById("manifest-status"),
    detailVersion: document.getElementById("detail-version"),
    detailChannel: document.getElementById("detail-channel"),
    detailInstaller: document.getElementById("detail-installer"),
    detailSize: document.getElementById("detail-size"),
    detailSha: document.getElementById("detail-sha"),
};

function setText(element, value) {
    if (element) {
        element.textContent = value || "Unavailable";
    }
}

function setDownloadEnabled(isEnabled, installerUrl, installerFileName) {
    if (!elements.downloadButton) {
        return;
    }

    if (isEnabled) {
        elements.downloadButton.href = installerUrl;
        elements.downloadButton.download = installerFileName;
        elements.downloadButton.setAttribute("aria-disabled", "false");
        elements.downloadButton.classList.remove("is-disabled");
        return;
    }

    elements.downloadButton.removeAttribute("href");
    elements.downloadButton.removeAttribute("download");
    elements.downloadButton.setAttribute("aria-disabled", "true");
    elements.downloadButton.classList.add("is-disabled");
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

function normalizeInstallerRelativeUrl(installerRelativeUrl) {
    if (typeof installerRelativeUrl !== "string") {
        throw new Error("Manifest is missing installerRelativeUrl.");
    }

    const trimmedUrl = installerRelativeUrl.trim();
    if (!/^LanguageVoiceTutorSetup-[A-Za-z0-9._-]+\.exe$/.test(trimmedUrl)) {
        throw new Error("Manifest installerRelativeUrl is not a safe installer filename.");
    }

    return trimmedUrl;
}

function validateManifest(manifest) {
    if (!manifest || typeof manifest !== "object") {
        throw new Error("Manifest is not valid JSON data.");
    }

    if (typeof manifest.version !== "string" || manifest.version.trim() === "") {
        throw new Error("Manifest is missing version.");
    }

    const installerRelativeUrl = normalizeInstallerRelativeUrl(manifest.installerRelativeUrl);
    const installerFileName = typeof manifest.installerFileName === "string" && manifest.installerFileName.trim() !== ""
        ? manifest.installerFileName.trim()
        : installerRelativeUrl;

    if (installerFileName !== installerRelativeUrl) {
        throw new Error("Manifest installer filename does not match installerRelativeUrl.");
    }

    if (!installerFileName.includes(manifest.version.trim())) {
        throw new Error("Manifest installer filename does not match the displayed version.");
    }

    return {
        version: manifest.version.trim(),
        channel: manifest.channel || "Unavailable",
        installerFileName,
        installerUrl: `${releaseBaseUrl}${installerRelativeUrl}`,
        installerSizeBytes: Number(manifest.installerSizeBytes),
        installerSha256: manifest.installerSha256 || "Unavailable",
    };
}

function applyManifest(manifest) {
    const release = validateManifest(manifest);

    setDownloadEnabled(true, release.installerUrl, release.installerFileName);
    setText(elements.currentVersion, release.version);
    setText(elements.detailVersion, release.version);
    setText(elements.detailChannel, release.channel);
    setText(elements.detailInstaller, release.installerFileName);
    setText(elements.detailSize, formatBytes(release.installerSizeBytes));
    setText(elements.detailSha, release.installerSha256);
    setText(elements.manifestStatus, "Release manifest loaded. The download link matches the current version shown above.");
}

function applyManifestFailure(message) {
    setDownloadEnabled(false);
    setText(elements.currentVersion, "Unavailable");
    setText(elements.detailVersion, "Unavailable");
    setText(elements.detailChannel, "Unavailable");
    setText(elements.detailInstaller, "Unavailable");
    setText(elements.detailSize, "Unavailable");
    setText(elements.detailSha, "Unavailable");
    setText(elements.manifestStatus, message);
}

async function loadManifest() {
    setDownloadEnabled(false);

    try {
        const response = await fetch(`${manifestUrl}?t=${Date.now()}`, { cache: "no-store" });
        if (!response.ok) {
            throw new Error(`Manifest request failed with ${response.status}`);
        }

        const manifest = await response.json();
        applyManifest(manifest);
    }
    catch (error) {
        const isValidationError = error instanceof Error
            && error.message.startsWith("Manifest")
            && !error.message.includes("request failed");
        applyManifestFailure(
            isValidationError
                ? "Release manifest is invalid. Please try again later."
                : "Could not load the latest release manifest. Please try again later."
        );
    }
}

elements.downloadButton?.addEventListener("click", (event) => {
    if (elements.downloadButton.getAttribute("aria-disabled") === "true" || !elements.downloadButton.href) {
        event.preventDefault();
    }
});

loadManifest();
