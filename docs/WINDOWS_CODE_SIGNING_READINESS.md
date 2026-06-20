# Windows code signing readiness

Review date: 2026-06-20.

Scope: planning and documentation only. This page does not enable signing, change packaging behavior, change upload behavior, change backend deployment, change database migrations, change billing/Paddle behavior, or change Admin/CMS behavior.

## Current Windows direct-release flow audit

The current Windows direct-release flow is intentionally simple and remains valid for controlled tester handoff when an unsigned build is knowingly accepted.

| Area | Current state |
| --- | --- |
| Packaging script | `scripts/package-windows-inno-release.ps1` publishes the desktop app for `win-x64`, enforces the production backend URL for tester/release installed builds, runs Inno Setup 6 through `ISCC.exe`, and prepares server-ready direct-release files. |
| Installer technology | Inno Setup 6 is the primary direct-download installer path. Velopack is deprecated for this release UX, ZIP packaging is only an emergency/developer fallback, and MSIX/Microsoft Store remains deferred. |
| Installer artifact | The final installer is produced as `artifacts\installers\windows\LanguageVoiceTutorSetup-{version}.exe`. |
| Server-ready release folder | The upload-ready folder is `artifacts\releases\windows\direct`. It contains the installer plus `latest.json`, `changelog.json`, `known-issues.json`, and `checksums.sha256`. |
| Manifest generation | `package-windows-inno-release.ps1` generates `latest.json` with product identity, app id, platform, architecture, channel, version, installer filename/relative URL, SHA-256, size, non-secret `backendBaseUrl`, minimum supported version, manual-confirmation update mode, and release notes. |
| Changelog and known issues | `package-windows-inno-release.ps1` writes `changelog.json` and `known-issues.json` for the same release folder. The current known issues can include that the installer is unsigned and may trigger SmartScreen warnings. |
| Checksums | `package-windows-inno-release.ps1` computes the installer SHA-256 and writes `checksums.sha256`; `scripts/validate-windows-direct-release.ps1` recomputes the installer hash and checks it against both `latest.json` and `checksums.sha256`. |
| Local release validation | `scripts/validate-windows-direct-release.ps1` validates required files, manifest identity, backend URL, update mode, version/installer naming consistency, JSON shape, local path leakage, installer size, and SHA-256 consistency. It does not currently verify Authenticode signatures. |
| Upload | `scripts/upload-windows-direct-release.ps1` runs local validation first, then uploads only the installer and required direct-release JSON/checksum files to the configured static website release path by SSH/SCP. It does not deploy the backend, run EF migrations, or upload secrets. |
| Live verification | Operators verify `https://languagevoicetutor.com/releases/windows/direct/latest.json`, download the installer referenced by the manifest, and compare SHA-256 with `latest.json` and `checksums.sha256` before sharing links. |

Generated `artifacts/` files, installer executables, backend packages, SQL dumps, `.env` files, and secrets must not be committed. A locally built installer is not public/live until the Windows direct-release files are uploaded and live `latest.json` is verified over HTTPS.

## Why code signing is needed

Windows installer code signing provides a cryptographic publisher identity and tamper-evidence for the downloaded installer. It is especially important before a public release candidate or broad public distribution because unsigned installers create Windows SmartScreen and reputation friction, make publisher identity unclear to users, and weaken the release trust story even when SHA-256 validation is also present.

Code signing does not replace HTTPS hosting, SHA-256 manifest validation, safe release notes, or manual tester instructions. It is an additional release-hardening control.

## What signing should cover

Required future signing target:

- the final Inno Setup installer executable: `LanguageVoiceTutorSetup-{version}.exe`.

Optional later signing targets, only if the release pipeline is explicitly designed to support them safely:

- desktop application binaries produced in `artifacts\publish\win-x64-inno` before they are packed into the installer;
- helper executables or native dependencies if any are added later and ownership/trust requirements justify signing them.

Do not add binary signing until the certificate custody model, timestamping model, verification command, failure policy, and public-release gate are approved.

## Material that must never be committed

Never commit or paste signing material or signing-service access material into this repository, documentation examples, scripts, CI logs, issue comments, support notes, or release artifacts:

- certificate files;
- private keys;
- PFX files;
- passwords or passphrases;
- timestamp credentials;
- signing service tokens;
- vendor credentials;
- hardware-token PINs;
- recovery codes;
- any real secret environment values.

Documentation may describe placeholder concepts such as "signing certificate" or "signing service" but must not include real secret values or private signing material.

## Expected future signing flow

The future public-release flow should be designed as an explicit opt-in hardening step:

1. Package the installer with the existing Inno Setup flow.
2. Sign the final installer executable.
3. Verify the Authenticode signature and signer/publisher identity.
4. Validate release files, including SHA-256 consistency after signing.
5. Upload only after signing and validation pass.
6. Verify the live HTTPS manifest and downloaded installer hash before sharing public links.

Signing changes the installer bytes, so any SHA-256 values in `latest.json` and `checksums.sha256` must reflect the signed installer, not a pre-signing installer.

## Future verification command concept

A future verification step should check, at minimum:

- the installer has a valid Authenticode signature;
- the signature chain is trusted on the target Windows release environment;
- the signer/publisher information matches the approved owner name;
- timestamping is present if that is part of the approved signing policy;
- release validation fails when signing is required and the signature is missing, invalid, expired without a valid timestamp, or from an unexpected publisher.

This check belongs after installer signing and before upload. For public release candidate/public release mode, it should also be part of the release gate so an unsigned or incorrectly signed installer cannot be uploaded accidentally.

## Scripts that should eventually participate

Future signing work should keep responsibilities separated:

| Script or gate | Expected future role |
| --- | --- |
| `scripts/package-windows-inno-release.ps1` | Continue packaging. If signing is added later, either call a dedicated signing step after Inno Setup output or document that a separate signing command must run immediately after packaging. Signing should remain optional for local/dev/test packaging until explicitly enabled. |
| `scripts/validate-windows-direct-release.ps1` | Add optional signature verification, with a mode that requires a valid approved signature for public release candidate/public release validation. |
| `scripts/upload-windows-direct-release.ps1` | Continue to validate before upload. Once signing is required for a release mode, upload must stop if validation reports a missing or invalid signature. |
| `tools/run_desktop_release_gate.ps1` | Eventually include or call signature verification for public release candidate/public release mode, while keeping controlled local/dev/test gates usable when signing is not enabled. |

Do not add signtool integration, signing-service integration, certificate path variables, CI/CD signing, or upload behavior changes until the owner approves the signing option and custody model.

## Optional for local/dev/test, required for public release modes

Signing should remain optional for local, developer, and controlled tester packaging until a future release policy explicitly enables it. Controlled tester/direct releases can remain unsigned for now if the owner knowingly accepts SmartScreen and trust friction for the private tester cohort.

Before broad public distribution, the public release candidate should require a signed installer or a documented owner-approved exception. Signing verification must be added before broad public distribution so the release process fails safely when signing is required but missing.

## Certificate and signing-service options to evaluate

This is not a purchase recommendation. The final choice requires owner approval after checking current vendor terms, identity validation requirements, cost, supported timestamping, token/cloud custody model, CI compatibility, and expected Windows reputation behavior.

| Option | Setup complexity | Expected trust improvement | Operational burden | Suitability for private tester releases | Suitability for public release | Risks and unknowns |
| --- | --- | --- | --- | --- | --- | --- |
| No signing for private controlled tester builds | Lowest. Keep current flow and communicate SmartScreen warnings. | Low. SHA-256 and HTTPS still help integrity, but publisher identity is absent. | Low operational burden. | Acceptable only when testers are controlled, informed, and the owner knowingly accepts unsigned-installer warnings. | Not suitable for broad public distribution except with a documented exception. | User trust friction, SmartScreen warnings, support burden, and weaker publisher identity. |
| OV code signing certificate | Moderate. Requires organization validation and secure key/certificate handling. | Medium. Shows publisher identity after signing, but reputation may still need time/download history. | Medium. Requires renewal, secure storage, timestamp policy, and signing workstation/service process. | Usually more than needed for a small private tester cohort. | Potentially suitable if owner accepts validation/custody/reputation tradeoffs. | Vendor rules, hardware-token/cloud-key requirements, reputation warm-up, renewal and incident response. |
| EV code signing certificate | Higher. Stronger organization validation and often hardware-backed custody requirements. | Higher initial trust expectation than OV in many Windows distribution contexts, though exact SmartScreen behavior is not guaranteed. | Higher. Token/HSM custody, access control, renewal, and operational procedures are more demanding. | Usually excessive for private controlled tester builds. | Potentially suitable for public release if owner accepts cost and process burden. | Cost, procurement time, token logistics, signer availability, vendor changes, and no absolute guarantee of warning-free downloads. |
| Azure Trusted Signing / Azure Code Signing style service | Moderate to high. Requires Azure tenant/account setup, identity validation, service configuration, access policy, and release integration. | Potentially strong if supported for the app's publisher identity and target distribution model. | Medium to high. Requires cloud access governance, audit, service availability planning, and integration controls. | Usually not necessary for private controlled tester builds unless already approved and available. | Potentially suitable for public release after owner/vendor validation. | Availability by region/entity type, pricing, policy requirements, integration details, timestamp behavior, and lock-in. |

## Explicit non-impact areas

Code signing readiness must not affect:

- backend deploy scripts or release symlinks;
- EF migrations or database schema/data;
- Paddle/billing/subscription/entitlement behavior;
- Desktop or Admin direct access to Paddle, which must remain disallowed;
- Admin/CMS authorization, content workflow, Save draft, Publish, or Restore behavior;
- `latest.json` format until a future approved signing implementation explicitly requires a documented format change;
- production runtime behavior.
