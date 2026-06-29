# Windows Store / MSIX channel and update behavior plan

Review date: 2026-06-29.

Scope: channel/update behavior audit plus packaging notes for the first local Microsoft Store/MSIX prototype. This document does not change backend code, does not add migrations, does not change deployment or installer scripts, does not publish Website CMS/static site content, does not upload installers, and does not deploy anything.

## Executive recommendation

For the first local MSIX prototype, use the implemented explicit **Store** channel flag controlled by build configuration/MSBuild property. The Store channel should be a hard boundary around update behavior:

- Direct builds keep the current Inno installer and public `latest.json` update flow.
- Store builds must not call the direct `latest.json` manifest, download `.exe` installers, cache direct update installers, or launch direct installer helpers.
- Store builds should display safe version/build information and explain that updates are managed by Microsoft Store.
- Release builds, including Store/MSIX builds, remain locked to the production backend URL.

Do not add package identity detection as the only first guard. It can be added later as a defense-in-depth assertion after MSIX identity is known.

## Files inspected for current behavior

### Release/debug and backend URL behavior

- `EnglishVoiceTutor.Desktop.csproj` defines `DesktopBackendBaseUrl` as localhost for Debug by default and production for non-Debug, then fails non-Debug builds that try to use any backend URL other than `https://api.languagevoicetutor.com`.
- `Constants/BackendConstants.cs` defines the production backend constant and the Debug-only developer backend default.
- `Services/BackendEndpointBuilder.cs` normalizes Debug backend URLs but ignores saved/custom backend URLs in non-Debug builds and returns the production backend URL.
- `ViewModels/SettingsViewModel.cs` stores a `BackendBaseUrl` property internally, but resolves saved backend URLs for the current build before using them.
- `Views/SettingsView.xaml.cs` disables the diagnostics tab outside Debug.
- `Views/SettingsView.xaml` does not expose a release Settings backend URL editor.

### Current direct update behavior

- `Services/Updates/UpdateManifestClient.cs` defines the direct manifest URL as `https://languagevoicetutor.com/releases/windows/direct/latest.json`, sends a no-cache JSON request, validates product/app/platform/architecture identity, validates version and installer metadata, validates SHA-256 metadata shape, and resolves the installer URI.
- `Services/Updates/DesktopStartupUpdateCheckService.cs` runs a delayed startup check once the UI is ready, compares the current app version to the manifest version, prompts before download, blocks installer launch during an active lesson, prompts again before starting the installer, and contains failures so startup is not blocked.
- `MainWindow.xaml.cs` triggers the startup update check from `OnContentRendered` using `StartOnceWhenUiIsReady(this, IsLessonActive)`.
- `ViewModels/SettingsViewModel.cs` implements the manual Settings **Check for updates** command using the same manifest and download services.
- `Views/SettingsView.xaml` shows the **Check for updates** button and installed version text.
- `Services/Updates/UpdateDownloadService.cs` downloads the installer over HTTPS, requires an `.exe` filename, caches downloads under `%LOCALAPPDATA%\LanguageVoiceTutor\Updates`, verifies SHA-256, starts a detached delayed installer launcher, and requests app shutdown.
- `Services/Updates/DesktopAppVersionProvider.cs` reads `release-version.txt` first, then assembly informational version, then assembly version, then a local fallback.

### Existing release scripts and direct manifest behavior

- `scripts/package-windows-inno-release.ps1` is the direct Inno package script. It locks release packaging to the production backend URL, writes `release-version.txt`, builds the installer, and writes `latest.json` with direct-channel metadata.
- `scripts/validate-windows-direct-release.ps1` validates direct release artifacts including `latest.json`, direct update mode, production backend URL, installer hash, and installer existence.
- `scripts/upload-windows-direct-release.ps1` uploads the direct release artifacts and validates `latest.json` installer references.
- `installer/windows/LanguageVoiceTutor.iss` is the current direct Inno installer script and should remain untouched by Store/MSIX planning.
- `site/public/releases/windows/direct/latest.json` is the checked-in direct manifest snapshot; live direct update truth is the public HTTPS manifest.

## Current direct update behavior summary

The direct Windows channel currently uses a website-hosted manifest and Inno installer:

1. App startup waits briefly after the main window renders.
2. Startup update check loads the direct `latest.json` manifest.
3. If the manifest version is newer than the installed version, the app prompts before downloading.
4. The app downloads the referenced direct `.exe` installer over HTTPS.
5. The app verifies SHA-256 before offering to start the installer.
6. The app prompts again before installer launch.
7. If a lesson is active, startup flow asks the user to finish the lesson first.
8. Manual Settings update checks use the same manifest/download/installer path and show tester diagnostics on update-check failure.

This flow is appropriate for the current direct tester/Inno channel and must remain available there.

## Current direct latest.json usage

The direct manifest URL is a desktop constant in `UpdateManifestClient`. The manifest is expected to describe this Windows desktop app with:

- product name `Language Voice Tutor`;
- app id `LanguageVoiceTutor.Desktop`;
- platform `windows`;
- architecture `win-x64`;
- installer filename and relative URL;
- installer SHA-256 and size;
- direct update mode generated by the direct release packaging script.

The Store/MSIX build must not use this manifest because it describes the direct Inno installer channel, not Store package updates.

## Current direct installer download and launch behavior

The direct updater downloads a verified `.exe` installer into `%LOCALAPPDATA%\LanguageVoiceTutor\Updates`. When the user confirms launch, it starts a detached command-shell helper that waits briefly, launches the installer, and requests application shutdown.

That behavior is intentionally direct-channel behavior. It should never be reachable from the Store/MSIX channel because Store packages should be updated by Microsoft Store rather than by a website-hosted Inno installer.

## Current backend release URL lock behavior

Release/non-Debug desktop builds are locked to `https://api.languagevoicetutor.com` in two places:

- build-time validation in `EnglishVoiceTutor.Desktop.csproj` rejects non-Debug backend URL overrides;
- runtime normalization in `BackendEndpointBuilder` returns the production backend URL in non-Debug builds even if stale saved settings contain a different value.

Store/MSIX release builds should keep this production lock. A Store channel flag should not introduce custom backend URL selection, and Store diagnostics must not expose secrets or tokens.

## Why Store/MSIX must not use direct latest.json or direct installer updates

- Microsoft Store/MSIX updates should be owned by Microsoft Store infrastructure.
- The direct manifest points to direct Inno installer artifacts, not Store package artifacts.
- Launching an external direct installer from a Store build would blur support, rollback, and compliance boundaries.
- Direct update cache files are direct-channel artifacts and are not meaningful for Store package updates.
- The direct tester release flow and future Store flow need separate policy checks so one channel cannot accidentally publish or update through the other.
- Store builds must not create a path that downloads or executes direct installer `.exe` files from the public manifest.

## Recommended Store channel behavior

For Store/MSIX builds:

- Do not run the startup direct manifest update check.
- Do not show a direct **download and install** update prompt.
- Do not download, cache, or launch direct Inno installers.
- Do not call `https://languagevoicetutor.com/releases/windows/direct/latest.json`.
- If update controls remain visible, the message should say updates are managed by Microsoft Store.
- Continue showing safe app version/build information for support.
- Keep release backend URL locked to `https://api.languagevoicetutor.com`.
- Keep all OpenAI, Paddle, database connection string, JWT, refresh/access token, and other secret values out of desktop docs, logs, tests, examples, diagnostics, and output.
- Continue treating backend as the source of truth for account, subscription, entitlement, usage, limits, and AI provider access.
- Do not implement payment changes as part of channel/update behavior.

## Proposed app channel model

Use a small explicit channel model in a later implementation task:

- **Direct**: current direct Windows/Inno channel. Keeps direct `latest.json`, direct installer download, direct installer launch, and direct release validation.
- **Store**: future MSIX/Microsoft Store channel. Uses Store-managed updates and blocks direct updater services from executing.
- **Development/Debug**: existing developer/debug behavior if needed. May use developer backend URL behavior under Debug only, but should not be confused with Store release behavior.

Suggested naming for code in the follow-up task: `DesktopDistributionChannel.Direct`, `DesktopDistributionChannel.Store`, and optionally `DesktopDistributionChannel.Development` if the existing Debug path needs a named value.

## Implementation options for a later task

### Option A: compile-time constant / MSBuild property

Define a build property such as `DesktopDistributionChannel=Direct|Store` and emit it as an assembly metadata value, generated constant, or conditional compilation symbol.

Pros:

- Simple and explicit for the first prototype.
- Easy to audit in project files and release commands.
- Can fail the build on invalid values.
- Can keep Direct as the default so current Inno packaging remains unaffected.
- Lets policy tests assert that Store builds cannot reach direct update services.

Cons/risks:

- A wrong build command could choose the wrong channel unless validation is strict.
- Does not prove the app is actually packaged as MSIX.
- Needs clear release documentation so Store channel is not used accidentally for direct testers.

### Option B: appsettings/resource config embedded at build time

Generate or include an embedded resource/appsettings file with a channel value during packaging.

Pros:

- Keeps channel metadata data-driven and visible in build outputs.
- Can include additional future Store metadata without adding many MSBuild properties.
- Works without relying on package identity APIs.

Cons/risks:

- More moving parts than a simple first prototype needs.
- Resource inclusion mistakes could silently fall back to Direct unless guarded.
- Tests need to inspect both project configuration and embedded output.

### Option C: runtime package identity detection

At runtime, detect whether the process has MSIX package identity and infer Store behavior.

Pros:

- Can detect actual packaged identity, not only intended build configuration.
- Useful defense-in-depth after MSIX packaging exists.
- Helps identify accidental unpackaged Store-channel test runs.

Cons/risks:

- Not available to validate before an MSIX prototype exists.
- Package identity does not necessarily tell whether the package came from Microsoft Store, sideloading, or local prototype.
- Using this as the only guard could make behavior ambiguous in local prototype and CI scenarios.

### Option D: combination of build flag + package identity detection

Use an explicit build channel as the source of truth, then optionally assert/log safe package identity information in Store builds after MSIX exists.

Pros:

- Best long-term safety model.
- Keeps first behavior deterministic while allowing future runtime validation.
- Can fail safe if Store channel is built but package identity is missing in a scenario where identity is required.

Cons/risks:

- Slightly more complex than Option A.
- Requires careful wording so sideloaded local MSIX prototypes are not mistaken for public Store availability.
- Needs tests for mismatch cases.

## Recommended first implementation for MSIX prototype

Use **Option A first**: an explicit Store channel flag controlled by MSBuild/build configuration, with Direct as the default. The local MSIX prototype project passes `DesktopDistributionChannel=Store` through its project reference. Add strict validation so only known channel values are accepted. Gate update behavior through a single channel-aware service or policy method so Store builds cannot call `UpdateManifestClient.LoadLatestAsync`, `UpdateDownloadService.DownloadAndVerifyAsync`, or `TryStartVerifiedInstallerAfterAppShutdown`.

After the first MSIX package identity is known, consider **Option D** by adding package identity detection as a secondary assertion/diagnostic only. Do not rely on package identity detection alone for the first prototype.

## Keeping the direct Inno installer flow unaffected

- Leave `installer/windows/LanguageVoiceTutor.iss` unchanged.
- Leave `scripts/package-windows-inno-release.ps1` defaulting to Direct behavior.
- Leave `scripts/validate-windows-direct-release.ps1` focused on direct artifacts.
- Leave `scripts/upload-windows-direct-release.ps1` focused on direct release upload only.
- Do not change the direct manifest URL or direct `latest.json` schema for Store work.
- Do not change direct release `updateMode=manual-confirmation` as part of Store planning.
- Add Store-specific tests in a follow-up task instead of weakening current direct-channel tests.

## Tests and policy checks to add in the follow-up implementation task

- Project/build policy: valid channel values only; non-Debug backend URL remains production-locked for Direct and Store.
- Store updater block policy: Store channel code path does not call direct `LatestManifestUrl`, `LoadLatestAsync`, `DownloadAndVerifyAsync`, or installer launch helper.
- Direct updater preservation policy: Direct channel still uses public direct `latest.json`, prompts before download/install, verifies SHA-256, and uses existing active-lesson guard.
- Settings UI policy: Store build update text says updates are managed by Microsoft Store; Direct build retains **Check for updates** wording.
- Startup policy: Store channel does not perform direct startup update check; Direct channel retains the startup check.
- Version display policy: Store and Direct builds both show safe app version/build information.
- Secret hygiene policy: no tokens, API keys, connection strings, JWT secrets, Paddle secrets, OpenAI keys, or private user data in docs/tests/logs/examples.
- Local-data/channel policy: Store channel does not use direct update cache paths or direct updater artifacts.
- Optional later MSIX identity policy: if package identity detection is added, tests cover packaged Store prototype, unpackaged Direct, and mismatch behavior.

## Manual test plan: Direct build update behavior

Use only a controlled test machine/account and do not upload new artifacts unless intentionally publishing a direct tester release.

1. Install or run a Direct build.
2. Confirm the Settings screen shows the direct **Check for updates** control.
3. Confirm the app displays a safe version string.
4. Trigger manual update check.
5. Confirm the direct `latest.json` flow is used.
6. If a newer direct test manifest is available, confirm the app prompts before download.
7. Confirm the downloaded installer is SHA-256 verified before launch is offered.
8. Confirm installer launch requires the second explicit confirmation.
9. Start a lesson, then confirm startup update flow does not launch the installer during the active lesson.
10. Confirm backend requests use the production backend in release builds.

## Manual test plan: Store/MSIX prototype update behavior

Do not claim Microsoft Store availability during local prototype testing.

1. Build the future Store-channel MSIX prototype with the explicit Store channel flag.
2. Install on a clean Windows VM/test account.
3. Launch the app and sign in fresh if local data is isolated.
4. Confirm the app displays safe app version/build information.
5. Confirm Settings/About update wording says updates are managed by Microsoft Store.
6. Confirm startup does not call the direct `latest.json` URL.
7. Confirm manual update controls do not call the direct `latest.json` URL.
8. Confirm no `.exe` installer is downloaded to the direct update cache.
9. Confirm no direct installer launch helper runs.
10. Confirm backend requests use the production backend in release builds.
11. Confirm account, subscription, entitlement, usage, limits, and lesson history after sign-in come from backend-owned APIs.
12. Confirm no OpenAI/Paddle/backend secrets or auth tokens are printed in diagnostics, logs, screenshots, or docs.

## Link to local-data Option A

The first Store/MSIX prototype should follow `docs/WINDOWS_STORE_LOCAL_DATA_AUDIT.md`: use isolated Store local data, accept fresh login, do not manually copy refresh/access tokens, and rely on backend-owned account/subscription/entitlement/usage/limit state after sign-in. The Store channel update boundary supports that decision by preventing direct update cache and direct installer metadata from becoming shared Store state.

## Open decisions before implementation

- Final MSBuild property name and allowed values.
- Whether Direct remains the default for all existing package scripts, with Store requiring an explicit property.
- Exact Store Settings/About wording and localization strategy.
- Whether to hide the update button in Store builds or keep it and show Store-managed update guidance.
- Whether to add package identity detection in the same implementation task or defer it until after MSIX packaging exists.
- Final MSIX package identity and side-by-side behavior with direct installs.
- Store version mapping policy for numeric MSIX versions versus human product versions.
- Partner Center disclosure wording for Paddle/web checkout.
- WACK command/process after a real MSIX prototype exists.

## Deployment impact classification

- Documentation-only: **yes; no deploy needed**.
- Backend runtime code changed: no; no backend package/upload/restart or post-deploy health checks required.
- Desktop runtime code changed: no; no desktop build/release gate required for this documentation-only task.
- Database schema changed: no; no EF migration review/apply process required.
- Deployment scripts changed: no; no deployment policy dry-run required.
- Installer scripts changed: no; no direct installer packaging/validation checks required.
- Store/MSIX packaging added: no.
- Website CMS/static site publish changed: no.
- Uploads/deployments performed: no.
- Secrets changed or documented: no.

## Recommended follow-up task

Implement the Store channel flag/update behavior boundary without adding MSIX packaging yet. The follow-up should add the channel model, block direct updater execution for Store builds, preserve Direct updater behavior, and add policy tests before any MSIX packaging prototype is introduced.

## Implemented first channel flag/update boundary (2026-06-29)

The first Store channel runtime guard is now implemented without adding MSIX packaging.

- Build property: `DesktopDistributionChannel`.
- Default: `Direct` when the property is omitted.
- Store selection example: `dotnet build -c Release -p:DesktopDistributionChannel=Store`.
- Valid values: `Direct` and `Store`.
- Invalid values fail the MSBuild validation target and are also guarded by runtime channel parsing.

Implemented behavior:

- Direct remains the default and continues using `https://languagevoicetutor.com/releases/windows/direct/latest.json` for startup and manual update checks.
- Store builds skip the startup direct manifest update check before `UpdateManifestClient.LoadLatestAsync()` can be reached.
- Store builds make the manual **Check for updates** command show `Updates are managed by Microsoft Store.` instead of loading direct `latest.json`.
- Store builds are blocked from direct installer download and direct installer launch by `DesktopUpdatePolicy` checks in `UpdateDownloadService`.
- Non-Debug/release builds remain locked to `https://api.languagevoicetutor.com`.
- No MSIX packaging, Store submission, direct Inno installer changes, payment changes, backend changes, database migrations, deployments, uploads, or Website CMS publish were added by this implementation.

Future defense-in-depth can add MSIX package identity detection after package identity details exist, but it is not the first source of truth for this implementation.


## Local MSIX prototype packaging note (2026-06-29)

The local packaging scaffold is `packaging/windows-msix/LanguageVoiceTutor.StorePrototype.wapproj`; it passes `DesktopDistributionChannel=Store` to `EnglishVoiceTutor.Desktop.csproj`. The prototype is documented in `docs/WINDOWS_STORE_MSIX_PROTOTYPE.md`. It is not a Store submission, WACK has not passed, and direct Inno update behavior remains the current public/tester channel.
