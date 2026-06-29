# Windows Store / MSIX local packaging prototype

Review date: 2026-06-29.

## Scope and status

This is a **local MSIX packaging prototype only** for the existing WPF desktop app. It does not submit anything to Microsoft Store, does not upload any package, does not replace the direct Windows/Inno installer flow, and does not claim Microsoft Store availability.

Deployment impact classification:

- Documentation-only: no.
- Desktop runtime code changed: no.
- Packaging project/files changed: yes.
- Backend runtime code changed: no.
- Database schema changed: no.
- Installer scripts changed: no.
- Deployment scripts changed: no.
- Store/MSIX packaging added: prototype only, no submission.
- Backend deploy needed: no.
- Windows direct installer upload needed: no.
- Website CMS publish needed: no.
- Store submission needed: no.

## What was added

- Packaging project: `packaging/windows-msix/LanguageVoiceTutor.StorePrototype.wapproj`.
- Package manifest: `packaging/windows-msix/Package.appxmanifest`.
- Local placeholder MSIX visual asset generator: `scripts/generate-store-msix-placeholder-assets.ps1`.
- Generated local placeholder MSIX visual assets output to `packaging/windows-msix/Assets/*.png` and are intentionally ignored by git.
- Static policy check: `scripts/test-store-msix-prototype-policy.ps1`.

The packaging project is separate from `EnglishVoiceTutor.Desktop.csproj`. It packages the existing WPF/Win32 desktop app through Desktop Bridge style MSIX packaging and does not convert the app to UWP, WinUI, MAUI, or Windows App SDK.

## Store channel build property

The packaging project references the desktop app project with:

```xml
<ProjectReference Include="..\..\EnglishVoiceTutor.Desktop.csproj" AdditionalProperties="DesktopDistributionChannel=Store;RuntimeIdentifier=win-x64" />
```

This keeps `Direct` as the default for normal desktop builds while making this MSIX prototype a Store-channel build. The `RuntimeIdentifier=win-x64` project-reference property is intentionally scoped to the MSIX prototype so restore creates the required `net10.0-windows/win-x64` assets for Desktop Bridge packaging without changing normal Direct builds. The direct Inno release command remains separate and unchanged.

Manual pre-package behavior check:

```powershell
dotnet build .\EnglishVoiceTutor.Desktop.csproj -c Release -p:DesktopDistributionChannel=Store
```

## Direct latest.json/update installer behavior

Store channel behavior is selected by `DesktopDistributionChannel=Store`. Store builds must not use the direct `https://languagevoicetutor.com/releases/windows/direct/latest.json` update manifest, must not download the direct Inno `.exe` installer, and must not launch the direct installer helper. Microsoft Store/MSIX updates are expected to be managed by Store/package infrastructure rather than the direct website manifest.

The direct Inno installer flow remains the current public/tester channel and remains separate from this prototype.

## Local data behavior

The first prototype uses local-data Option A from the Store local data audit: Store/MSIX local data is isolated from direct local data. A fresh login in the Store prototype is acceptable. Do not copy refresh tokens, access tokens, local auth files, or other private user data manually between direct and MSIX installs. The backend remains the source of truth for account, subscription, entitlement, usage, and limits.

## Temporary local package identity

Current local prototype identity in `Package.appxmanifest`:

- `Identity Name`: `LanguageVoiceTutor.Desktop.StorePrototype`
- `Publisher`: `CN=LanguageVoiceTutorStorePrototypeLocal`
- `Version`: `0.1.36.0`
- `DisplayName`: `Language Voice Tutor Store Prototype`
- `PublisherDisplayName`: `Language Voice Tutor Local Prototype`

These values are **not** final Partner Center values. Before any Partner Center submission, replace at least:

- package identity name;
- publisher subject;
- publisher display name;
- public display name if the Store listing should not include “Store Prototype”;
- package version sequencing policy;
- Store listing metadata and assets.

## Package version mapping

MSIX package versions must be four numeric components. Direct tester versions can include labels such as `0.1.36-tester.31`, but MSIX cannot use that string directly as the package identity version. For this prototype, the direct product line `0.1.36-tester.31` maps to the numeric MSIX prototype version `0.1.36.0`.

Before Store submission, approve a stable mapping rule. Examples to review include `major.minor.patch.0` for the first Store package from a direct build line, or a Store-only fourth-component revision counter. This does not replace the direct tester versioning scheme.

## Signing and certificates

No generated MSIX PNG output, signing private key, `.pfx`, `.pvk`, `.snk`, local `.cer`, password, token, API key, refresh/access token, DB connection string, JWT secret, Paddle key, OpenAI key, or certificate with private key may be committed.

The repository ignores local MSIX signing artifacts, generated placeholder PNG assets, and generated package outputs. For local sideload testing, create a local test certificate outside git and reference it only in local Visual Studio/MSBuild settings or temporary untracked files. If a public `.cer` export is needed for trusting a local test package, keep it outside the repository unless there is a reviewed reason to document or commit a non-secret public certificate.

## Local Windows packaging commands

These commands require Windows with Visual Studio/MSBuild components for Windows Application Packaging Projects and the Windows SDK. They were not fully executed in this Linux Codex environment because Desktop Bridge packaging targets are Windows/Visual Studio tooling.

Recommended local Windows verification:

```powershell
# 1. Verify the desktop Store-channel build behavior.
dotnet build .\EnglishVoiceTutor.Desktop.csproj -c Release -p:DesktopDistributionChannel=Store

# 2. Generate local placeholder visual assets. These PNG outputs are ignored by git.
powershell -ExecutionPolicy Bypass -File .\scripts\generate-store-msix-placeholder-assets.ps1

# 3. Run static repository policy checks.
powershell -ExecutionPolicy Bypass -File .\scripts\test-store-msix-prototype-policy.ps1

# 4. Build the local MSIX prototype project from a Visual Studio Developer PowerShell.
msbuild .\packaging\windows-msix\LanguageVoiceTutor.StorePrototype.wapproj /restore /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64 /p:AppxPackageSigningEnabled=false
```

If local sideload installation requires signing, create/use a local test certificate outside git, then build with local certificate properties or sign the generated package with `signtool` using the local certificate. Do not commit the certificate private key or generated package.

## Current packaging warnings to review later

NuGet compatibility warnings such as `NU1701` or `NU1702` may still appear during local packaging restore/build. Treat those as follow-up compatibility review items unless they become blocking errors after the `net10.0-windows/win-x64` restore target is present. They are not the primary failure addressed by this prototype fix.

## Install/uninstall test plan

1. Use a clean Windows test account or VM.
2. Confirm the direct Inno app, if installed, remains installed and functional before MSIX testing.
3. Install/sideload the locally built MSIX prototype using Windows package tooling appropriate for the signed package.
4. Launch `Language Voice Tutor Store Prototype`.
5. Sign in fresh; do not copy direct-channel auth files or tokens.
6. Confirm lessons, account state, subscription/entitlement display, usage, and limits come from the backend.
7. Confirm Store-channel update UI/behavior does not use direct `latest.json` or direct Inno installer launch.
8. Uninstall the MSIX prototype from Windows Settings or PowerShell.
9. Confirm uninstalling the MSIX prototype does not remove or modify the direct Inno installation.

## Manual smoke test plan

- Launch app from Start menu and verify branding is prototype/local.
- Register/login through backend APIs.
- Start and finish a short lesson.
- Verify lesson history/progress syncs from backend.
- Open Settings and verify no release backend URL editing is exposed.
- Verify no generated MSIX PNG output is tracked and no OpenAI API key, Paddle key, DB connection string, JWT secret, or token appears in UI, logs, docs output, or generated artifacts.
- Verify update behavior is Store-channel safe and does not call/download/launch the direct Inno update path.

## WACK follow-up

Windows App Certification Kit verification remains a follow-up step. Do not state that WACK has passed until it is run against a concrete locally built package and the results are reviewed.

## Missing before Partner Center submission

- Final reserved Partner Center package identity.
- Final publisher identity and signing path.
- Final Store-compatible version sequencing policy.
- Production-quality Store assets and screenshots.
- Store listing copy, privacy/support/legal review, and age-rating answers.
- Partner Center disclosure/policy review for Paddle/web checkout.
- WACK run and remediation.
- Final confirmation that Store channel cannot use direct `latest.json`, direct installer download, or direct installer launch.
- Final submission checklist and owner approval.
