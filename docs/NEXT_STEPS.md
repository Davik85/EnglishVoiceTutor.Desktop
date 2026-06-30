# Next Steps

Review date: 2026-06-29.

## Source of truth for current versions

These docs are a release-readiness handoff snapshot. Always verify live/public state before announcing versions.

Check the public Windows direct tester release from the live website manifest:

```powershell
Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json
```

Check the production backend release from the server `current` symlink:

```powershell
ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"
```

Generated local files under `artifacts/` are not proof that a version is live on the public site. A locally built installer becomes public only after the Windows direct release files are uploaded to the website release folder and `latest.json` is verified over HTTPS. Generated artifacts must not be committed.

Release/tester installed builds are server-only and use `https://api.languagevoicetutor.com`. Local backend URLs are DEBUG/developer-only. Diagnostics and Backend URL editing are not part of user/release Settings. The packaged desktop path covers registration/login/lesson/history/progress/update through backend APIs rather than direct provider calls. Direct builds keep the simple user-facing **Check for updates** button backed by `latest.json`, SHA-256 verification, and a flow that does not silently auto-update; clean-machine smoke remains required before tester handoff.

## Windows distribution direction

Microsoft Store/MSIX was evaluated with a local prototype and is discontinued for now. Do not run MSIX prototype commands, do not recreate `packaging/windows-msix/`, do not submit to Partner Center, and do not claim Store availability.

Current Windows distribution remains the Direct EXE/Inno installer channel with the direct `latest.json` update flow. Future Windows trust/signing work should focus on buying and integrating a proper code signing certificate for the direct EXE/Inno installer.

Backend deploy, Website CMS/static site publish, Windows direct installer upload, and database migrations remain separate processes. No backend deploy, Website CMS publish, Store submission, or Windows installer upload is implied by this cleanup.

## Release-readiness status

- Backend: production healthy at `https://api.languagevoicetutor.com`, current release `0.1.35-backend.82`.
- Website: generated public pages and Paddle-review polish are completed for `https://languagevoicetutor.com`.
- Download: current Windows tester release is visible without JavaScript and manifest-driven with JavaScript.
- Windows installer: current public tester release is `0.1.36-tester.31`, installer `LanguageVoiceTutorSetup-0.1.36-tester.31.exe`.
- Billing: Paddle live is not enabled yet; Production/live Paddle readiness remains deferred.
- Legal: legal/support/seller/AI/status/download pages are ready for owner/legal final review as drafts, not final legal advice.

Do not state that the product is fully public production-ready. This remains a controlled tester/direct Windows release, not a broad public production launch, and not broad public production readiness.

## Remaining release steps

1. Final manual website review in incognito.
2. Final owner/legal text review.
3. Final Windows installer smoke.
4. Paddle live readiness checklist.
5. Only after approval: production Paddle environment, token, webhook, and price setup.
6. Microsoft Store/MSIX discontinued for now; do not claim Microsoft Store, Android, or iOS availability as currently available.

## Backend next-step guardrails

Backend deployment uses `scripts/package-backend-linux-release.ps1` and `scripts/upload-backend-linux-release.ps1`. The upload flow uses `deploy-backend-release.sh` and `ssh -tt` for sudo restart/status when needed. Backend deploy is separate from Windows installer upload, static website publish, and database migrations. Backend upload/package scripts do not apply EF migrations automatically; database migrations remain a separate reviewed SQL process only when schema changes exist.

Current health checks:

```powershell
Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
```

Phase 3 rate limiting / abuse protection is completed and production-verified with `RateLimiting__Enabled=true`. Admin permission fallback remains disabled with `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`. Phase 4 is complete for the current release-readiness level: Phase 4A backup/readability/separate-drill-restore completed, Phase 4B local PostgreSQL backup scheduling active, Phase 4C migration rollback/remediation dry-run rehearsal completed, and Phase 4D permission-fidelity restore drill completed. Off-server encrypted backups remain optional future infrastructure hardening.

## Website/CMS next-step guardrails

The Website CMS is under Admin Shell → Website. It is Super Admin / Bootstrap Admin protected, intentionally simple, informational only, JSON/file-based, and not a full CMS. Content lives in `site/content/website-content.json` with active and draft content; public output lives in `site/public`.

Normal flow: load draft/active → Save draft → Preview selected page without publishing → Publish / Make active to promote content and render static pages. Publish creates `index.html`, `download.html`, `mobile.html`, `pricing.html`, `support.html`, `terms.html`, `privacy.html`, `refunds.html`, `cancellation.html`, `seller.html`, `ai-data.html`, and `status.html`.

Normal pages expose Page title, Body markdown, SEO title, and SEO description. Home remains structured for landing cards/assets. Design is not a normal Super Admin editing page. Markdown supports headings, emphasis, lists, markdown links, safe URL/email/domain autolinks such as `Paddle.com`, and must continue to reject/escape unsafe schemes including `javascript:`, `data:`, and `vbscript:`.

Admin Website CMS endpoints remain authenticated/authorized but no longer consume the normal admin read/write rate limit because legal text editing previously caused `RateLimitExceeded`.

## Website CMS Marketing / SEO and public crawler readiness

The Website CMS now includes a visible **Marketing / SEO** section with consent-banner, analytics, ads, Search Console verification, and `llms.txt` controls. These values are stored through the existing JSON/file-based Website CMS model; no database table, schema change, migration, backend secret, env value, or committed example JSON value is required for Google setup. Real Google IDs, conversion labels, and Search Console tokens must be entered only in Admin Website CMS when available, never in code, docs, env files, or committed JSON examples.

Current safe CMS values before real Google setup: Enable consent banner ON, Enable `llms.txt` ON, Enable analytics OFF with an empty GA4 Measurement ID, Enable ads tracking OFF with empty Ads ID and conversion label, and an empty Search Console verification token until property verification begins.

Operator field guide: GA4 Measurement ID comes from Google Analytics → Admin → Data streams → Web stream for `languagevoicetutor.com` and has format `G-XXXXXXXXXX`; Google Ads ID comes from Google Ads conversion tag setup and has format `AW-123456789`; the download conversion label comes from the same conversion action setup; Search Console token comes from HTML tag verification for `https://languagevoicetutor.com/` and only the `content="..."` value should be copied. Do not paste placeholders, whole script snippets, or GTM container IDs into these fields unless GTM support is explicitly added later.

Website Publish now emits or maintains public HTML pages, `robots.txt`, `sitemap.xml`, `llms.txt` when enabled, and `marketing-consent.js`. Generated pages include canonical URLs, meta descriptions, Open Graph/Twitter tags, JSON-LD where appropriate, and SoftwareApplication JSON-LD for Windows desktop only. They must not claim Android/iOS, Microsoft Store, Google Play, or App Store availability.

Consent mode defaults to denied before user choice for `analytics_storage`, `ad_storage`, `ad_user_data`, and `ad_personalization`. The banner supports Accept all, Reject non-essential, Manage choices, and a Privacy Policy link. Privacy Policy includes optional analytics, advertising, and cookie consent disclosure. The website remains usable when non-essential cookies are rejected, and GA/Ads scripts must not be emitted when IDs are empty or tracking is disabled.

Final verification should confirm public pages do not contain placeholder IDs such as `G-XXXXXXXXXX` or `AW-123456789`, do not include `googletagmanager.com/gtag/js` while IDs are empty, `download.html` shows current Windows installer details from `latest.json` when static release details are available, and `robots.txt`, `sitemap.xml`, `llms.txt`, and `marketing-consent.js` return `200`.

## Website/public review checklist

- Home shows logo, study language flags, Windows desktop app card, and “Android and iOS apps are planned but are not currently available.”
- Home does not say “Mobile version coming soon” and does not claim mobile apps are currently available.
- Footer has primary links: Privacy Policy, Terms of Use, Refund Policy, Cancellation, Support, Pricing.
- Footer has secondary links: Seller / Company Details, AI & Data Disclosure, Service Status.
- `seller.html`, `ai-data.html`, and `status.html` exist and are linked from the footer.
- Download page statically shows current release details when the manifest is available and remains supported by `download.js` and `/releases/windows/direct/latest.json`.
- Privacy Policy default/static content now includes optional analytics/advertising cookie disclosure. The polished consent banner is controlled by Website CMS Marketing / SEO, and Google Analytics/Ads IDs are optional public configuration values that must be left empty unless intentionally configured; never commit real Google IDs or secrets.
- Download non-JS fallback text remains: “Current Windows tester release is available through the Download for Windows button.” and “If release details do not load automatically, please contact [support@languagevoicetutor.com](mailto:support@languagevoicetutor.com).”

## Windows direct release next-step guardrails

Current manifest: `https://languagevoicetutor.com/releases/windows/direct/latest.json`.

Expected current values:

- `version`: `0.1.36-tester.31`
- `installerFileName`: `LanguageVoiceTutorSetup-0.1.36-tester.31.exe`
- `backendBaseUrl`: `https://api.languagevoicetutor.com`
- `updateMode`: `manual-confirmation`
- `minimumSupportedVersion`: `0.1.36-tester.31`

`0.1.36-tester.31` has already been built, uploaded, and verified for controlled direct testers. The user confirmed the newly uploaded build works and that the update flow works on other devices. Do not re-upload or repackage it as a next step unless a new release is intentionally prepared.

Desktop release polish already included Contacts in Settings, Contacts localization for all release-ready UI languages, safe `https`/`mailto` contact links, fixed runtime Contacts localization refresh after interface-language changes, wrapping for long localized situation/subtopic and scenario card text, and the unfinished active-lesson Back confirmation guard matching Finish/End lesson behavior.

Use:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 -Version 0.1.36-tester.31
```

Do not manually `scp` installer files if the upload script exists. After upload, verify `latest.json`, installer filename, backend base URL, installer hash, and that the download page button downloads the same installer.

Code signing remains deferred. CMS published-snapshot runtime is active for controlled tester lessons. Backend deployment, database migrations, the download website, and update UI remain separate work.

## Paddle/live billing next-step guardrails

Production billing/Paddle/subscription payment lifecycle remains deferred. Live Paddle is not enabled yet. Do not change production Paddle environment values, add live checkout links, or commit secrets. Paddle stays behind the backend/provider adapter. Desktop does not call Paddle directly and does not decide Premium directly.

Backend remains source of truth for plan, subscription, entitlement, usage, and limits. Entitlement is the source of Premium access; `PaymentEntity` is diagnostic payment history only. Desktop and future mobile clients share one backend account, one backend database, one subscription/entitlement state, and one lesson history/progress source. Paddle may be the first web/desktop provider, but Apple/Google must remain possible later for mobile. Do not add YooKassa or Russia-only billing assumptions.

## AI model CMS operations

AI model IDs are editable by Super Admin / Bootstrap Admin in **Admin → System → AI Models** through JSON/file-based CMS settings. API keys remain environment/server secrets and are not CMS content. Backend runtime remains the source of truth for AI model selection; model changes should require only CMS publish for backend runtime to use them on new AI requests. No desktop release is required because the desktop does not decide model IDs or call OpenAI directly. No DB migration was added.

The production persistent AI Models file is now verified at `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`. It was seeded from the current release file only to correct missing persistent data/config, then confirmed to exist, contain `gpt-5.5` plus `gpt-5.2`, match the current release file by SHA-256 `94f84fc07551d821bfa9dc0682bb4ee60108d11d74987b84ebb39fce96f825f1`, and survive a backend service restart with `/health` and `/api/health/database` still healthy. Treat this file as server data/config, not a release artifact; future deploys must not use release-folder AI Models JSON as the source of truth.

Current known-good model configuration: lesson tutor chat `gpt-5.5`; feedback/correction `gpt-5.2`; lesson hint `gpt-5.2`; translation `gpt-5.2`; speech-to-text `gpt-4o-mini-transcribe`; lesson chat TTS `tts-1`; Conversation Mode TTS `gpt-4o-mini-tts`; Realtime voice `gpt-realtime`.

Operational workflow before changing production models: Load AI Models → Edit draft → Save draft → Validate format → Test provider access → Review compatibility diagnostics → Publish / Make active only if relevant runtime diagnostics pass → run a small real lesson. Validate format checks syntax only and does not prove provider access. Test provider access performs provider-level checks using draft settings, does not publish, and uses safe dummy input rather than real lesson/user text. Audio and realtime roles may be marked `not_tested` if not covered by lightweight checks.

The `gpt-5.5` lesson tutor chat root cause was unsupported `temperature`, not provider unavailability. Minimal Responses API text passed, minimal structured output passed, and lesson runtime shape without user content passed after `temperature` was omitted. Keep the backend rule that `gpt-5.5` lesson tutor chat requests omit `temperature`; keep existing `gpt-5.2` behavior with `temperature: 0.3` where configured. Do not assume every newer model accepts parameters accepted by older models.

Compatibility diagnostics should be read as a matrix: `minimal_responses_text` checks model availability / Responses API access; `current_provider_test_shape` checks the older provider-test shape including `temperature` if present; `minimal_structured_output` checks strict structured output; and `lesson_chat_runtime_shape_without_user_content` checks lesson runtime request options/schema with safe dummy input. If a new model breaks lessons, inspect safe backend logs for operation, model role, configured model ID, provider status/category, and safe provider error type/code/param/message, then restore a previous known-good model if needed. Logs and Admin UI must not expose secrets, raw provider bodies, raw request bodies, full prompts, private user lesson text, env values, or connection strings.

## Discontinued Microsoft Store / MSIX channel

The Microsoft Store/MSIX prototype path is discontinued for now. The repository should not contain active Store/MSIX packaging, Store-channel runtime behavior, Store submission commands, or MSIX local packaging tests. Keep Windows release work focused on the Direct EXE/Inno installer, the direct `latest.json` update manifest, and a future code signing certificate for that direct installer path.

## 2026-06-30 recommended release path after Store/MSIX discontinuation

### Current Active Release Strategy

Windows release work stays on the Direct EXE/Inno installer. Updates continue through the direct `latest.json` manifest. Future Windows trust work is code signing for the direct installer. Backend deployment, Website CMS/static-site publish, Windows direct installer upload, DB migrations, and Paddle live account/provider work remain separate operations. Microsoft Store/MSIX is discontinued for now and must not be treated as an active next step.

### Top remaining tasks in recommended order

1. Complete final clean-machine and update-over-existing-install smoke for the current Direct EXE/Inno installer.
2. Purchase/select a Windows code signing certificate and plan integration for the direct Inno installer.
3. Prepare a signed direct installer release candidate only after signing is approved; validate, upload, and verify via the existing direct-release helper flow.
4. Complete owner/legal review of website, pricing, subscription, terms, privacy, refunds, cancellation, support, seller/company, AI/data, and status pages; publish through Website CMS/static-site flow only.
5. Complete Paddle live readiness and provider/account setup before paid public launch; do not enable live billing as part of docs-only work.
6. Collect controlled tester feedback, triage severity, and make an explicit release decision before broader public distribution.
7. Before any tester handoff, re-verify the live Windows direct manifest still points to `0.1.36-tester.31`, production backend URL, and `manual-confirmation`.
8. Keep the backend release discrepancy resolved in docs: the live symlink verification command `ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"` confirmed `/opt/languagevoicetutor/backend/releases/0.1.35-backend.82`; `/health` and `/api/health/database` were verified healthy. No backend deploy was performed for this documentation update.
9. Keep the AI Models persistence risk closed: preserve `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json` as persistent server data/config, do not package release-folder JSON as the production source of truth, and verify it after future backend deploys.
10. Keep Store/MSIX removed/discontinued; do not recreate `packaging/windows-msix`, Store channel logic, Store update messaging, WACK commands, or Partner Center planning.
11. Run backend deploy only for an approved backend runtime/configuration change; do not deploy backend for Website CMS publish, Windows installer upload, AI Models persistence correction, or docs-only work.
12. Run DB migration only for a reviewed schema/data change with backups, SQL review, privilege checks, and rollback/remediation plan.

### Task classification

- Requires backend deploy: only approved backend runtime/configuration changes.
- Requires Windows installer build/upload: signed or intentionally new direct installer releases.
- Requires Website CMS publish: public website/legal/support/pricing content changes.
- Requires DB migration: only reviewed schema/data changes.
- Docs-only: release-readiness documentation, command/runbook clarification, stale Store/MSIX wording cleanup.
- Manual account/provider/admin work: code signing purchase, Paddle live configuration, owner/legal review, tester management, AI Models publish only if model settings intentionally change; persistence verification is already complete for the current production state.
