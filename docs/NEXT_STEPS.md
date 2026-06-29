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

## Store / MSIX sequence

Current Store/MSIX work now includes a local prototype scaffold, but remains unsubmitted and unpublished. Use this order:

1. Review `docs/WINDOWS_STORE_LOCAL_DATA_AUDIT.md`.
2. Complete/review `docs/WINDOWS_STORE_CHANNEL_UPDATE_PLAN.md`.
3. Use the implemented Store channel flag (`dotnet build -c Release -p:DesktopDistributionChannel=Store`) for pre-MSIX behavior checks; Direct remains the default.
4. Verify the local MSIX packaging scaffold in `packaging/windows-msix/` on Windows/Visual Studio without changing the direct Inno installer flow.
5. Add Windows App Certification Kit verification notes only after WACK is actually run.

Do not upload Store packages, submit to Partner Center, or claim Store availability. The current prototype commands are local-only and documented in `docs/WINDOWS_STORE_MSIX_PROTOTYPE.md`.

## Release-readiness status

- Backend: production healthy at `https://api.languagevoicetutor.com`, current release `0.1.35-backend.80`.
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
6. Microsoft Store preparation later; do not claim Microsoft Store, Android, or iOS availability as currently available.

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

Current known-good model configuration: lesson tutor chat `gpt-5.5`; feedback/correction `gpt-5.2`; lesson hint `gpt-5.2`; translation `gpt-5.2`; speech-to-text `gpt-4o-mini-transcribe`; lesson chat TTS `tts-1`; Conversation Mode TTS `gpt-4o-mini-tts`; Realtime voice `gpt-realtime`.

Operational workflow before changing production models: Load AI Models → Edit draft → Save draft → Validate format → Test provider access → Review compatibility diagnostics → Publish / Make active only if relevant runtime diagnostics pass → run a small real lesson. Validate format checks syntax only and does not prove provider access. Test provider access performs provider-level checks using draft settings, does not publish, and uses safe dummy input rather than real lesson/user text. Audio and realtime roles may be marked `not_tested` if not covered by lightweight checks.

The `gpt-5.5` lesson tutor chat root cause was unsupported `temperature`, not provider unavailability. Minimal Responses API text passed, minimal structured output passed, and lesson runtime shape without user content passed after `temperature` was omitted. Keep the backend rule that `gpt-5.5` lesson tutor chat requests omit `temperature`; keep existing `gpt-5.2` behavior with `temperature: 0.3` where configured. Do not assume every newer model accepts parameters accepted by older models.

Compatibility diagnostics should be read as a matrix: `minimal_responses_text` checks model availability / Responses API access; `current_provider_test_shape` checks the older provider-test shape including `temperature` if present; `minimal_structured_output` checks strict structured output; and `lesson_chat_runtime_shape_without_user_content` checks lesson runtime request options/schema with safe dummy input. If a new model breaks lessons, inspect safe backend logs for operation, model role, configured model ID, provider status/category, and safe provider error type/code/param/message, then restore a previous known-good model if needed. Logs and Admin UI must not expose secrets, raw provider bodies, raw request bodies, full prompts, private user lesson text, env values, or connection strings.

## Future Microsoft Store / MSIX channel next steps

This is planned work only; the current Windows direct Inno installer flow remains the working release channel and must not be changed by Store preparation.

1. Finish/review the local data audit in `docs/WINDOWS_STORE_LOCAL_DATA_AUDIT.md` and keep the migration decision pending until reviewed.
2. Plan the Store channel build flag and update behavior so Store builds cannot use direct `latest.json`, without changing runtime behavior in the planning step.
3. Then prototype MSIX packaging locally without changing `scripts/package-windows-inno-release.ps1`, `installer/windows/LanguageVoiceTutor.iss`, direct release artifacts, or `latest.json`.
4. Add Windows App Certification Kit local verification notes only after a real MSIX prototype confirms the command/process.
5. Prepare Store listing/legal/assets content and screenshots.
6. Decide final Paddle/web checkout disclosure wording for Store listing and in-app upgrade flow after Partner Center policy review.

Known gaps before submission: no final Partner Center package identity, final Store version mapping, WACK pass, production Store screenshots/assets checklist, Partner Center disclosure review, or Store submission checklist. A local prototype project and temporary identity now exist, but they do not imply Store availability. See `docs/WINDOWS_STORE_RELEASE_PLAN.md` and `docs/WINDOWS_STORE_LOCAL_DATA_AUDIT.md`.
