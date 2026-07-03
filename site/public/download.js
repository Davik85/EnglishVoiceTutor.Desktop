const manifestUrl = "/releases/windows/direct/latest.json";
const releaseBaseUrl = "/releases/windows/direct/";
const fallbackInstallerFileName = "LanguageVoiceTutorSetup-1.0.exe";
const fallbackInstallerUrl = `${releaseBaseUrl}${fallbackInstallerFileName}`;

const elements = {
    currentVersion: document.getElementById("current-version"),
    downloadButton: document.getElementById("download-button"),
    manifestStatus: document.getElementById("manifest-status"),
    installerSize: document.getElementById("installer-size"),
    detailVersion: document.getElementById("detail-version"),
    detailChannel: document.getElementById("detail-channel"),
    detailInstaller: document.getElementById("detail-installer"),
    detailBackendBaseUrl: document.getElementById("detail-backend-base-url"),
    detailMinimumSupportedVersion: document.getElementById("detail-minimum-supported-version"),
    detailUpdateMode: document.getElementById("detail-update-mode"),
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
        backendBaseUrl: manifest.backendBaseUrl || "Unavailable",
        minimumSupportedVersion: manifest.minimumSupportedVersion || "Unavailable",
        updateMode: manifest.updateMode || "Unavailable",
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
    setText(elements.installerSize, formatBytes(release.installerSizeBytes));
    setText(elements.detailVersion, release.version);
    setText(elements.detailChannel, release.channel);
    setText(elements.detailInstaller, release.installerFileName);
    setText(elements.detailBackendBaseUrl, release.backendBaseUrl);
    setText(elements.detailMinimumSupportedVersion, release.minimumSupportedVersion);
    setText(elements.detailUpdateMode, release.updateMode);
    setText(elements.detailSize, formatBytes(release.installerSizeBytes));
    setText(elements.detailSha, release.installerSha256);
    setText(elements.manifestStatus, "Ready to download. Latest Windows version is shown above.");
}

function applyManifestFailure(message) {
    setDownloadEnabled(true, fallbackInstallerUrl, fallbackInstallerFileName);
    setText(elements.currentVersion, "available from the public release manifest");
    setText(elements.installerSize, "Unavailable");
    setText(elements.detailVersion, "Unavailable");
    setText(elements.detailChannel, "Unavailable");
    setText(elements.detailInstaller, "Unavailable");
    setText(elements.detailBackendBaseUrl, "Unavailable");
    setText(elements.detailMinimumSupportedVersion, "Unavailable");
    setText(elements.detailUpdateMode, "Unavailable");
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
                ? "Release details are temporarily unavailable. Please try again later."
                : "Could not load the latest Windows download. Please try again later."
        );
    }
}

elements.downloadButton?.addEventListener("click", (event) => {
    if (elements.downloadButton.getAttribute("aria-disabled") === "true" || !elements.downloadButton.href) {
        event.preventDefault();
    }
});

function createScreenshotLightbox() {
    const overlay = document.createElement("div");
    overlay.className = "download-lightbox";
    overlay.hidden = true;
    overlay.innerHTML = `
        <div class="download-lightbox__backdrop" data-download-lightbox-close="true"></div>
        <div class="download-lightbox__dialog" role="dialog" aria-modal="true" aria-label="Screenshot preview">
            <button class="download-lightbox__close" type="button" aria-label="Close screenshot preview" data-download-lightbox-close="true">×</button>
            <img class="download-lightbox__image" alt="">
        </div>`;
    document.body.appendChild(overlay);
    return overlay;
}

const screenshotLightbox = createScreenshotLightbox();
const screenshotLightboxImage = screenshotLightbox.querySelector(".download-lightbox__image");
const screenshotLightboxClose = screenshotLightbox.querySelector(".download-lightbox__close");
screenshotLightboxImage.addEventListener("error", closeScreenshotLightbox);
let screenshotLightboxReturnFocus = null;

function closeScreenshotLightbox() {
    if (screenshotLightbox.hidden) {
        return;
    }

    screenshotLightbox.hidden = true;
    document.body.classList.remove("download-lightbox-open");
    screenshotLightboxImage.removeAttribute("src");
    screenshotLightboxReturnFocus?.focus?.();
    screenshotLightboxReturnFocus = null;
}

function openScreenshotLightbox(trigger) {
    const imageSrc = trigger?.dataset?.downloadLightboxSrc;
    if (!imageSrc || trigger.dataset.downloadLightboxUnavailable === "true") {
        return;
    }

    screenshotLightboxReturnFocus = trigger;
    screenshotLightboxImage.src = imageSrc;
    screenshotLightboxImage.alt = trigger.dataset.downloadLightboxAlt || "Download page screenshot";
    screenshotLightbox.hidden = false;
    document.body.classList.add("download-lightbox-open");
    screenshotLightboxClose.focus();
}

document.querySelectorAll("[data-download-lightbox-src]").forEach((trigger) => {
    const image = new Image();
    image.addEventListener("error", () => {
        trigger.dataset.downloadLightboxUnavailable = "true";
        trigger.removeAttribute("role");
        trigger.removeAttribute("tabindex");
        trigger.removeAttribute("aria-label");
    }, { once: true });
    image.src = trigger.dataset.downloadLightboxSrc;

    trigger.addEventListener("click", () => openScreenshotLightbox(trigger));
    trigger.addEventListener("keydown", (event) => {
        if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            openScreenshotLightbox(trigger);
        }
    });
});

screenshotLightbox.addEventListener("click", (event) => {
    if (event.target?.dataset?.downloadLightboxClose === "true") {
        closeScreenshotLightbox();
    }
});

document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
        closeScreenshotLightbox();
    }
});

loadManifest();
