# Next Steps

Review date: 2026-07-23.

## Mobile v1 planning next step

The completed account-deletion rollout is not a current deployment or retest step. The next planning work is Mobile-only and must begin by inspecting the current Mobile UI, dependencies, Android configuration, shared backend contracts, and existing Settings placeholders. Then select small isolated slices; do not combine the following tracks in one code task:

1. **Notifications:** planned, not implemented. The first review must decide whether V1 needs local scheduled reminders, remote push notifications, or both. It must not assume Firebase Cloud Messaging, scheduling, content, cadence, or permission timing, and must not request unnecessary permissions or use background microphone access.
2. **Premium purchase entry points and purchase flow:** separately decide whether the first slice is CTA/navigation only or a complete Google Play Billing flow. Premium remains backend-owned and backend-verified; a local button press or unverified store result must never unlock it. Plan purchase restoration and backend verification before claiming billing completion. No Paddle change is part of Mobile Google Play work.
3. **Eight-language interface localization:** separately decide the exact eight interface languages before implementation. It is distinct from the six study languages; planned Flutter localization resources such as ARB and `flutter_localizations` are not yet implemented. Interface localization does not translate backend-generated tutor replies, CMS canonical IDs, lesson runtime metadata, or user content. Once approved, new Notifications and Premium UI must use localization-ready strings.

Consider whether a minimal localization foundation should precede new Notifications and Premium UI so those screens do not need to be rebuilt. Mobile remains the same backend account/product, with backend-owned Premium, no client-side OpenAI, no Mobile secrets or database, no unapproved endpoint, and no provider or production action during planning.

## Source of truth for current versions

These docs are a release-readiness handoff snapshot. Always verify live/public state before announcing versions.

Check the public Windows direct release from the live website manifest:

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

## Authenticated mobile lesson completion status

Authenticated mobile Finish + ready Summary is production-verified as of 2026-07-11 against backend `0.1.35-backend.112` using the existing authenticated production routes: `PUT /api/me/lesson-sessions/{sessionId}/finish` and `GET /api/me/lesson-sessions/{sessionId}/summary`. Mobile must send only completion facts such as `validTurnCount`, must not generate summaries locally or upload summary fields, and must handle both `ready` and `unavailable` summary status. Finish triggers backend-owned generation; GET only reads the already persisted learner-safe result and does not regenerate a missing summary. No new backend endpoint is required for this step; desktop and mobile must keep using the same backend session, completion, history, progress, and summary source of truth. Development `/api/dev/.../summary` routes remain diagnostics only and are not mobile contracts. Authenticated recent History is separately complete in backend `0.1.35-backend.123`; Mobile History UI and a future aggregate Progress contract remain separate concerns. This verification does not complete mobile UI, aggregate Progress, voice, translation, hints, feedback, TTS, Conversation mode, billing, store publication, or broad public production readiness.

## Lesson History and future Progress

The authenticated backend History prerequisite is complete and production-deployed in `0.1.35-backend.123`; it is no longer a pending backend route gap. Mobile client work should use `GET /api/me/lesson-history` and `GET /api/me/lesson-history/{sessionId:guid}` and must not use Desktop-local JSON or `/api/dev/...` history routes. See [Lesson History Endpoints](LESSON_HISTORY_ENDPOINTS.md).

Future backend work is limited to separately approved needs: a backend-owned aggregate Progress contract for official totals, streaks, and long-term statistics; pagination only if product requirements later need more than recent maximum-50 History; and account deletion for future store requirements where that planning item remains approved. Clients must not treat recent History as all-time Progress or invent official Progress locally.

## Voice scenario follow-up

Authenticated backend voice scenario semantic resolution was originally deployed in backend `0.1.35-backend.113` from source commit `c850f4b`; the endpoint contract is documented in `docs/LESSON_SESSIONS_ENDPOINTS.md`. Backend `0.1.35-backend.115` historically fixed a structured-output validation mismatch that could make `POST /api/me/lesson-sessions/{sessionId}/voice-scenario-resolution` return HTTP 502. The public route and Mobile request/response contract did not change, `free_context` remains supported, and runtime candidate IDs are still validated against the current CMS candidates. No Desktop client source changed and no Desktop usage of this endpoint is claimed. A physical Android retest is still required before marking the first clean voice scenario selection, or the complete Mobile voice flow, fully stabilized.

## Mobile Settings -> Learning learner level next step

Backend `0.1.35-backend.116` completes the backend prerequisite for learner level settings: the existing settings API returns and accepts optional `CurrentLevel`. The next bounded Mobile step is for Mobile Settings -> Learning to read and save `CurrentLevel` through the existing settings API before removing Choose Level from lesson start. Do not claim the complete Mobile level-flow change is implemented until Mobile consumes the field, the Choose Level start screen is removed, and physical Mobile validation passes.

## Release-readiness status

Public release boundary: the current product remains a public Windows direct release, not a full broad production-readiness claim.

- Backend: production healthy at `https://api.languagevoicetutor.com` on `0.1.35-backend.133`, with `.132` as rollback; the execution migration and complete account-deletion workflow are production-deployed and production-verified; CMS/Admin login security and persistent role management are production-verified.
- Website: generated public pages and Paddle-review polish are completed for `https://languagevoicetutor.com`.
- Download: current Windows direct public release is visible without JavaScript and manifest-driven with JavaScript.
- Windows installer: current Windows direct public release is `1.1`, installer `LanguageVoiceTutorSetup-1.1.exe`.
- Billing: controlled Paddle live payment/webhook/Premium activation and desktop cancel-renewal validation are completed for the 2026-07-02 owner-led test; full-refund Premium revocation is production-verified; chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred; broad public paid launch remains pending final release-readiness review.
- Legal: legal/support/seller/AI/status/download pages are ready for owner/legal final review as drafts, not final legal advice.

Do not state that the product is fully public production-ready. This remains a public Windows direct release, not a full broad production-readiness claim, and not broad public production readiness.

Pre-mobile planning should start from [`docs/PRE_MOBILE_READINESS.md`](PRE_MOBILE_READINESS.md) so stale Windows `1.0`, backend `.99`, tester-era, or pre-live Paddle facts are not reused as current mobile planning inputs. Mobile v1 must be treated as another client for the same Language Voice Tutor product: same backend account, database, Premium entitlement, usage/limits, lesson history/progress, lesson model, and backend-verified billing source of truth.

## Remaining release steps

1. Final manual website review in incognito.
2. Final owner/legal text review.
3. Final Windows installer smoke and code signing for the Direct installer.
4. Admin auth audit follow-up: first production slice is complete for `admin_login_success`, `admin_logout`, `admin_login_failed`, and `disabled_admin_login_denied`; keep session expiration audit persistence pending until a later approved implementation.
5. Monitoring/logging/privacy hardening for remaining Admin operations and paid-launch evidence.
6. Backup/restore/rollback drill currency check before broader launch.
7. Controlled Paddle live payment/webhook/Premium activation, failed-payment non-activation, desktop cancel-renewal, and full-refund Premium revocation are completed for the 2026-07-02 owner-led test; chargeback remains implemented/test-covered but not live-chargeback-tested, expanded customer portal/subscription management is deferred, and Direct installer code signing/final smoke plus broad release decision remain pending.
8. Microsoft Store/MSIX discontinued for now; do not claim Microsoft Store, Android, or iOS availability as currently available.

## Backend next-step guardrails

Backend deployment uses `scripts/package-backend-linux-release.ps1` and `scripts/upload-backend-linux-release.ps1`. The upload flow uses `deploy-backend-release.sh` and `ssh -tt` for sudo restart/status when needed. Backend deploy is separate from Windows installer upload, static website publish, and database migrations. Backend upload/package scripts do not apply EF migrations automatically; database migrations remain a separate reviewed SQL process only when schema changes exist.

Current health checks:

```powershell
Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
```

Selected tutor persistence is completed and production-deployed in backend `0.1.35-backend.109`: `/api/me/settings` returns and persists `selectedTutorId`, `GET /api/tutor-options` remains the tutor option source, omitted or `null` selected tutor values preserve existing state, invalid tutor IDs are rejected, `speechVoice` remains independent, no database migration was needed, and production health/database health are `200 Healthy`.

Phase 3 rate limiting / abuse protection is completed and production-verified with `RateLimiting__Enabled=true`. Production Admin RBAC / persistent role management is completed after backend `0.1.35-backend.108`; Admin permission fallback remains disabled with `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`. Phase 4 is complete for the current release-readiness level: Phase 4A backup/readability/separate-drill-restore completed, Phase 4B local PostgreSQL backup scheduling active, Phase 4C migration rollback/remediation dry-run rehearsal completed, and Phase 4D permission-fidelity restore drill completed. Off-server encrypted backups remain optional future infrastructure hardening.

## Website/CMS next-step guardrails

The Website CMS is under Admin Shell → Website. It is Super Admin / Bootstrap Admin protected, intentionally simple, informational only, JSON/file-based, and not a full CMS. Content lives in `site/content/website-content.json` with active and draft content; public output lives in `site/public`.

Normal flow: load draft/active → Save draft → Preview selected page without publishing → Publish / Make active to promote content and render static pages. Publish creates `index.html`, `download.html`, `mobile.html`, `pricing.html`, `support.html`, `terms.html`, `privacy.html`, `refunds.html`, `cancellation.html`, `seller.html`, `ai-data.html`, and `status.html`.

Normal pages expose Page title, Body markdown, SEO title, and SEO description. Home remains structured for landing cards/assets. Design is not a normal Super Admin editing page. Markdown supports headings, emphasis, lists, markdown links, safe URL/email/domain autolinks such as `Paddle.com`, and must continue to reject/escape unsafe schemes including `javascript:`, `data:`, and `vbscript:`.

Download is also structured for the Desktop app release page. In addition to Page title, Body markdown, SEO title, and SEO description, CMS Save draft and Publish preserve/fill four feature cards using `featureCard1Label` / `featureCard1Title` / `featureCard1Description` / `featureCard1ImagePath` through the matching `featureCard4*` keys. Default image paths are `/assets/images/download/quick-start.webp`, `/assets/images/download/topics.webp`, `/assets/images/download/guided-lesson.webp`, and `/assets/images/download/conversation.webp`. Blank or missing Download image paths normalize back to these defaults.

Website CMS publish must write only managed static website files, preserve public assets, preserve `releases/windows/direct`, avoid deleting or recreating the full static website root, avoid removing manually uploaded or repository-tracked website assets, and never touch `latest.json` or installer files. The fixed production issue was that CMS Publish generated `download.html` without `/assets/images/download/...` paths even though image files still existed; the root cause was partial Download page payload / `featureCard*` field preservation. Save draft and Publish now preserve/fill those fields.

Admin Website CMS endpoints remain authenticated/authorized but no longer consume the normal admin read/write rate limit because legal text editing previously caused `RateLimitExceeded`.

## Website CMS Marketing / SEO and public crawler readiness

The Website CMS now includes a visible **Marketing / SEO** section with consent-banner, analytics, ads, Search Console verification, and `llms.txt` controls. These values are stored through the existing JSON/file-based Website CMS model; no database table, schema change, migration, backend secret, env value, or committed example JSON value is required for Google setup. Real Google IDs, conversion labels, and Search Console tokens must be entered only in Admin Website CMS when available, never in code, docs, env files, or committed JSON examples.

Current safe CMS values before real Google setup: Enable consent banner ON, Enable `llms.txt` ON, Enable analytics OFF with an empty GA4 Measurement ID, Enable ads tracking OFF with empty Ads ID and conversion label, and an empty Search Console verification token until property verification begins.

Operator field guide: GA4 Measurement ID comes from Google Analytics → Admin → Data streams → Web stream for `languagevoicetutor.com` and has format `G-XXXXXXXXXX`; Google Ads ID comes from Google Ads conversion tag setup and has format `AW-123456789`; the download conversion label comes from the same conversion action setup; Search Console token comes from HTML tag verification for `https://languagevoicetutor.com/` and only the `content="..."` value should be copied. Do not paste placeholders, whole script snippets, or GTM container IDs into these fields unless GTM support is explicitly added later.

Website Publish now emits or maintains public HTML pages, `robots.txt`, `sitemap.xml`, `llms.txt` when enabled, and `marketing-consent.js`. Generated pages include canonical URLs, meta descriptions, Open Graph/Twitter tags, JSON-LD where appropriate, and SoftwareApplication JSON-LD for Windows desktop only. They must not claim Android/iOS, Microsoft Store, Google Play, or App Store availability.

Consent mode defaults to denied before user choice for `analytics_storage`, `ad_storage`, `ad_user_data`, and `ad_personalization`. The banner supports Accept all, Reject non-essential, Manage choices, and a Privacy Policy link. Privacy Policy includes optional analytics, advertising, and cookie consent disclosure. The website remains usable when non-essential cookies are rejected, and GA/Ads scripts must not be emitted when IDs are empty or tracking is disabled.

Static upload warning: analytics IDs are CMS/config controlled. Real GA/Ads IDs, conversion labels, and Search Console tokens must not be committed into static HTML, docs, or examples. A raw upload of committed `site/public` files can overwrite public pages with blank analytics configuration if those files were not generated from the current CMS/config values. After any static upload, operators must verify analytics/ads config on the public site or publish through the intended Website CMS/static workflow. This is an operations warning only, not a script or code change.

Final verification should confirm public pages do not contain placeholder IDs such as `G-XXXXXXXXXX` or `AW-123456789`, do not include `googletagmanager.com/gtag/js` while IDs are empty, `download.html` shows current Windows installer details from `latest.json` when static release details are available, and `robots.txt`, `sitemap.xml`, `llms.txt`, and `marketing-consent.js` return `200`.

## Website/public review checklist

- Home shows logo, study language flags, Windows desktop app card, and “Android and iOS apps are planned but are not currently available.”
- Home does not say “Mobile version coming soon” and does not claim mobile apps are currently available.
- Footer has primary links: Privacy Policy, Terms of Use, Refund Policy, Cancellation, Support, Pricing.
- Footer has secondary links: Seller / Company Details, AI & Data Disclosure, Service Status.
- `seller.html`, `ai-data.html`, and `status.html` exist and are linked from the footer.
- Download page statically shows current release details when the manifest is available and remains supported by `download.js` and `/releases/windows/direct/latest.json`; its safe non-JavaScript fallback href is `/releases/windows/direct/LanguageVoiceTutorSetup-1.1.exe`, never the broken relative `LanguageVoiceTutorSetup-1.1.exe`.
- Privacy Policy default/static content now includes optional analytics/advertising cookie disclosure. The polished consent banner is controlled by Website CMS Marketing / SEO, and Google Analytics/Ads IDs are optional public configuration values that must be left empty unless intentionally configured; never commit real Google IDs or secrets.
- Download non-JS fallback text remains: “Current Windows direct release is available through the Download for Windows button.” and “If release details do not load automatically, please contact [support@languagevoicetutor.com](mailto:support@languagevoicetutor.com).”

## Windows direct release next-step guardrails

Current manifest: `https://languagevoicetutor.com/releases/windows/direct/latest.json`.

Expected current values:

- `version`: `1.1`
- `installerFileName`: `LanguageVoiceTutorSetup-1.1.exe`
- `backendBaseUrl`: `https://api.languagevoicetutor.com`
- `updateMode`: `manual-confirmation`
- `minimumSupportedVersion`: `1.1`

`1.1` has already been built, uploaded, verified, and confirmed installed; the desktop displays version `1.1`. Do not re-upload or repackage it as a next step unless a new release is intentionally prepared. `minimumSupportedVersion` is intentionally `1.1` because `1.1` contains the desktop auth/session stability fix: expired access token plus valid refresh token should refresh/retry and persist the replacement session instead of logging the user out.

Historical `1.0` desktop release polish already included Contacts in Settings, Contacts localization for all release-ready UI languages, safe `https`/`mailto` contact links, fixed runtime Contacts localization refresh after interface-language changes, wrapping for long localized situation/subtopic and scenario card text, and the unfinished active-lesson Back confirmation guard matching Finish/End lesson behavior.

Use:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 -Version 1.1
```

Do not manually `scp` installer files if the upload script exists. After upload, verify `latest.json`, installer filename, backend base URL, installer hash, and that the download page button downloads the same installer.

Code signing remains deferred. CMS published-snapshot runtime is active for published Windows direct lessons. Backend deployment, database migrations, the download website, and update UI remain separate work.

## Paddle/live billing next-step guardrails

Controlled live Paddle validation is completed for payment/webhook/Premium activation, failed-payment non-activation, cancel-renewal, and full-refund Premium revoke. Broad public paid launch remains pending final readiness, legal, support, and operations review. Do not broaden production Paddle environment values, add new live checkout links, or commit secrets as part of docs-only work. Paddle stays behind the backend/provider adapter. Desktop does not call Paddle directly and does not decide Premium directly.

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
5. Keep Admin auth audit first-slice verification documented for `admin_login_success`, `admin_logout`, `admin_login_failed`, and `disabled_admin_login_denied`; keep session expiration audit persistence pending until a later approved implementation.
6. Complete monitoring/logging/privacy hardening for remaining Admin operations and paid-launch evidence.
7. Controlled Paddle live payment, webhook delivery, Premium activation, failed-payment non-activation, desktop cancel-renewal, and full-refund Premium revocation are documented as completed for the 2026-07-02 owner-led test; do not claim paid public launch until final release-readiness review, remaining release smoke/signing, and owner release decision are complete.
8. Collect controlled tester feedback, triage severity, and make an explicit release decision before broader public distribution.
9. Before any tester handoff, re-verify the live Windows direct manifest still points to `1.1`, production backend URL, and `manual-confirmation`.
10. Keep backend current-state docs aligned with the 2026-07-11 verification: the live symlink resolves to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.112`, rollback is `0.1.35-backend.111`, `/health` and `/api/health/database` are `200 Healthy`, no EF migration was run, and Windows installer/release files are unchanged.
11. Keep the AI Models persistence risk closed: preserve `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json` as persistent server data/config, do not package release-folder JSON as the production source of truth, and verify it after future backend deploys.
12. Keep Store/MSIX removed/discontinued; do not recreate `packaging/windows-msix`, Store channel logic, Store update messaging, WACK commands, or Partner Center planning.
13. Run backend deploy only for an approved backend runtime/configuration change; do not deploy backend for Website CMS publish, Windows installer upload, AI Models persistence correction, or docs-only work.
14. Run DB migration only for a reviewed schema/data change with backups, SQL review, privilege checks, and rollback/remediation plan.

### Task classification

- Requires backend deploy: only approved backend runtime/configuration changes.
- Requires Windows installer build/upload: signed or intentionally new direct installer releases.
- Requires Website CMS publish: public website/legal/support/pricing content changes.
- Requires DB migration: only reviewed schema/data changes.
- Docs-only: release-readiness documentation, command/runbook clarification, stale Store/MSIX wording cleanup.
- Manual account/provider/admin work: code signing purchase, Paddle live configuration, owner/legal review, tester management, AI Models publish only if model settings intentionally change; persistence verification is already complete for the current production state.

## Paddle live checkout manual next steps

1. Merge the live checkout preparation code.
2. Run backend and static-site tests before deployment.
3. Deploy backend only after tests pass.
4. Publish/upload static website files including `/pay.html` and a generated `/paddle.public.json` containing only the public Paddle client-side token.
5. Add live server env values for Paddle API key, webhook secret, live price id, live product id, checkout URL, expected custom_data markers, and live environment mode.
6. Restart backend and verify `/health` plus `/api/health/database`.
7. Run a controlled live checkout test only after explicit approval.

Rollback: disable live env or return `PaddleBilling__Environment`/provider settings to sandbox/disabled, confirm mismatched webhooks do not grant Premium, and do not upload Windows installers or reintroduce Store/MSIX.

## 2026-06-30 Paddle live checkout/Admin readiness update

Current production facts after backend `0.1.35-backend.108` and the 2026-07-02 controlled live payment/cancel-renewal validation:

- Backend health and database health are `200 Healthy`.
- Backend server-side Paddle configuration is in the existing env file `/etc/languagevoicetutor/backend.env`; do not invent a second env file and do not create Paddle live systemd drop-ins for this configuration.
- Backend current symlink is `/opt/languagevoicetutor/backend/current`; backend releases are under `/opt/languagevoicetutor/backend/releases/<version>`.
- AI Models persistent server data remains `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`; known-good models remain `gpt-5.5` for lesson tutor chat and `gpt-5.2` for feedback correction, lesson hints, and translation.
- Static website nginx root is `/var/www/languagevoicetutor/site`. The parent `/var/www/languagevoicetutor` is not the nginx static-site root and must not be used as the static website upload target.
- Public Paddle config is `/var/www/languagevoicetutor/site/paddle.public.json`; public Paddle checkout page is `/var/www/languagevoicetutor/site/pay.html`.
- Direct Windows release files are separate at `/var/www/languagevoicetutor/releases/windows/direct` and are not touched by static website upload.
- Active Windows delivery remains Direct EXE/Inno. Store/MSIX is discontinued and must not be reintroduced. Current direct public release is `1.1`; direct `latest.json` remains active with manual-confirmation update mode.
- Paddle website review is approved, `/pay.html` and `/paddle.public.json` are deployed/reachable, backend live Paddle env is configured, and a real transaction URL opened Paddle checkout with `Language Voice Tutor Pro`, `Pro Monthly`, `14.99 EUR`.
- 2026-07-02 controlled validation completed: real live payment Complete for Language Voice Tutor Pro at 14.99 EUR via Google Pay; live checkout transaction creation, `subscription.created`, `subscription.activated`, `transaction.completed`, payment persistence, subscription snapshot processing, reconciliation, entitlement activation (`ActivatedCount=1`, `BlockedCount=0`, `FailedCount=0`), and desktop Premium visibility were verified without exposing raw provider payloads or secrets. Earlier failed payment attempts were processed without Premium activation (`ActivatedCount=0` / `AlreadySkippedCount=1`). One PostgreSQL serialization conflict during subscription snapshot processing retried successfully and ended with `FailedCount=0`. Desktop cancel-renewal was verified: auto-renewal became inactive while Premium remained active until `8/2/2026`. Full-refund Premium revocation is production-verified; chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred and not a current blocker.
- Controlled live payment, webhook delivery, payment persistence, subscription snapshot processing, entitlement activation, desktop Premium visibility, and desktop cancel-renewal behavior were completed and documented on 2026-07-02. Paddle full-refund Premium revocation is production-verified on backend `0.1.35-backend.108` using the already stored live `adjustment.updated` event; automatic future handling should use delivered `adjustment.created` / `adjustment.updated` notifications, with the operator reprocess command reserved for already-stored/legacy events only. Chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred and not a current blocker; broad public paid launch remains pending final release-readiness review and remaining release blockers.

Static website upload command must target the real nginx root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\upload-static-site.ps1 `
  -ServerHost "lvt-server" `
  -ServerUser "deploy" `
  -RemotePath "/var/www/languagevoicetutor/site"
```

Safe backend env verification must redact secrets and must use the existing env file, for example:

```bash
sudo awk -F= '/^(Billing__|PaddleBilling__|PaddleWebhook__)/ { v=$2; if ($1 ~ /(ApiKey|SecretKey|Token)/) v=(length($2)>0 ? "SET" : "EMPTY"); print $1 "=" v }' /etc/languagevoicetutor/backend.env
```

Admin capabilities should distinguish completed controlled validation from launch completion: live checkout/webhooks, the 2026-07-02 live payment/Premium activation/cancel-renewal path, failed-payment non-activation, and full-refund Premium revocation can be reported as completed; chargeback remains implemented/test-covered but not live-chargeback-tested, expanded customer portal/subscription management is deferred, and `billingPaidLaunchReleaseComplete=false` remains until final release-readiness review and remaining blockers are closed.

Admin RBAC note: Production Admin RBAC / persistent role management is completed. `productionRolesAvailable` means persistent Admin role authorization is active with an explicit fallback cutover (`AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`). It is not a broad public-launch flag and does not override remaining paid-launch blockers. Production diagnostics show two active `super_admin` AdminUsers and fallback disabled; if this flag is false, check the explicit fallback configuration and cutover status before changing role assignments.

## Admin Activity follow-ups

- Admin Activity first production slice is completed and visible for `admin_actions`, `admin_role_assignment_events`, `cms_content_audit_logs`, and the production-applied `admin_auth_audit_events` source; keep it read-only unless an explicit schema change is approved.
- Manual Premium Grant, Manual Premium Revoke, role assignment/revocation/admin disable/enable, stored Admin note/reason visibility, and Admin auth audit events for login/logout/failure/disabled-denied are no longer active blockers for the first slice.
- Keep session expiration audit persistence pending until a future approved implementation slice.
- Review Website/AI publish audit coverage and add explicit persistence only through an approved safe audit design where existing audit tables do not already cover the event.
- Monitoring/logging/privacy hardening, backup/restore/rollback drill currency, Direct installer code signing, final installer smoke, final legal/support/site text check, and broad release decision remain pending. Controlled Paddle live payment/webhook/Premium activation, failed-payment non-activation, desktop cancel-renewal, and full-refund Premium revocation were completed on 2026-07-02; chargeback remains implemented/test-covered but not live-chargeback-tested.

## 2026-07-01 Admin login/logout/failure audit persistence design

Inspection result: do **not** persist admin login/logout/failure events into the existing visible Admin Activity source tables without an approved schema change.

Current admin login/session flow:

- App-user credential login happens in `AuthEndpoints.LoginAsync` through `IAuthService.LoginAsync` after the request email/password presence check. Missing credentials return `400`; invalid credentials return `401` and only write an application log message without a persistent audit row.
- The Admin shell cookie is created in `AuthEndpoints.LoginAsync` only after app login succeeds and either bootstrap admin access is detected or persistent Admin RBAC grants `admin.self.read`. The cookie uses the `AdminShellCookie` scheme, is non-persistent, cannot refresh, and expires at the app auth response expiry.
- Persistent Admin shell access is checked first by linked app-user id and then, if the linked AdminUser is not disabled and the email is present, by normalized email. Disabled AdminUsers are rejected by the role-assignment read service when `DisabledAtUtc` is set or status is not `active`; that returns no Admin shell access and `AuthEndpoints.LoginAsync` signs out the Admin shell cookie instead of creating one.
- Admin logout/session deletion happens in `AdminEndpoints.DeleteAdminSessionAsync`, which signs out the `AdminShellCookie` scheme and returns `204`. The Admin UI calls `DELETE /api/admin/session` from `logoutAdminSession` and then clears local Admin UI state.
- Session expiration is currently cookie/auth-ticket expiration only; it is not a persistent event, and the Admin UI treats invalid/expired session responses as local session reset.

Current persistent logging coverage:

| Event | Persisted in Admin Activity today? | Current behavior |
| --- | --- | --- |
| successful admin login | Yes, production-verified | Durable `admin_login_success` row in `admin_auth_audit_events`; Admin Activity source/filtering is visible. |
| failed app credential login | Yes, production-verified | Durable `admin_login_failed` row in `admin_auth_audit_events`; no password/body/token data is stored. |
| disabled AdminUser login attempt | Yes, production-verified | Durable `disabled_admin_login_denied` row in `admin_auth_audit_events`. |
| explicit admin logout | Yes, production-verified | Durable `admin_logout` row in `admin_auth_audit_events`. |
| session expiration | No | Cookie expiry / invalid-session handling only; no durable audit row completion is claimed. |

Existing table fit:

- `admin_actions` is not a safe fit. It is target-app-user/action audit with required `TargetUserId`, required non-empty `Reason`, and foreign keys to app users. Login failures can involve no known user/admin identity, and logout is actor/session activity rather than an action taken against a target user.
- `admin_role_assignment_events` is not a safe fit. It is role-management audit with required `TargetAdminUserId`, role-change fields, and role-assignment semantics. Login/logout/failure events are not role assignment events; forcing them here would pollute RBAC audit and still would not represent unknown failed attempts safely.
- `cms_content_audit_logs` is not a safe fit. It is CMS content audit with entity/content fields and CMS action/status semantics, not authentication/session audit.

Migration decision completed for the first production slice: the dedicated authentication/session audit table `admin_auth_audit_events` was approved, migration `20260701000000_AddAdminAuthAuditEvents` was applied before backend `0.1.35-backend.108`, and Admin Activity now shows production-verified `admin_login_success`, `admin_logout`, `admin_login_failed`, and `disabled_admin_login_denied` events. Session expiration audit persistence remains pending. The safe schema uses only bounded fields:

- `id` GUID primary key.
- `occurred_at_utc` timestamp.
- `event_type` string: `admin_login_success`, `admin_logout`, `admin_login_failed`, `disabled_admin_login_denied`.
- `result` string: `succeeded`, `failed`, or `denied`.
- Nullable `actor_user_id` for the linked app user when app authentication succeeded.
- Nullable `actor_admin_user_id` for the persistent AdminUser when resolved safely.
- Nullable normalized `actor_email` or `attempted_normalized_email`, stored only as the email string submitted/known for the login attempt after trimming/normalization; do not store passwords or raw request bodies.
- Nullable safe role context such as `role_ids_json` only after app authentication succeeds and roles are resolved from existing Admin RBAC data; do not store cookies, JWTs, authorization headers, raw claims, or full request bodies.
- Nullable `safe_metadata_json` for bounded non-secret context such as `admin_shell_cookie_issued`, `denial_reason`, or `auth_stage`; never store cookies, JWTs, Authorization headers, Paddle secrets, OpenAI keys, raw provider payloads, raw request bodies, or full provider payloads.

First safe implementation slice status:

1. Dedicated table and EF entity/configuration/migration: complete for `20260701000000_AddAdminAuthAuditEvents`.
2. `admin_login_success` persistence and production Admin Activity verification: complete.
3. `disabled_admin_login_denied` persistence and production Admin Activity verification: complete.
4. `admin_login_failed` persistence and production Admin Activity verification: complete.
5. `admin_logout` persistence and production Admin Activity verification: complete.
6. Read-only Admin Activity source/filtering for `admin_auth_audit_events`: production-visible and verified.
7. Session expiration audit persistence: pending future approved slice; do not mark complete.

Keep Admin Activity read-only over the approved source tables and do not expose password/cookie/JWT/Authorization/request-body/provider secrets.

## Admin auth audit persistence deployment follow-up

- Migration `20260701000000_AddAdminAuthAuditEvents` is already applied in production.
- Admin Activity shows and filters the `admin_auth_audit_events` source for `admin_login_success`, `admin_login_failed`, `disabled_admin_login_denied`, and `admin_logout`.
- Session expiration persistence remains pending unless a future slice identifies a clean, low-noise place to write expiration events.
- Controlled Paddle live payment/webhook/Premium activation, failed-payment non-activation, desktop cancel-renewal, and full-refund Premium revocation were completed on 2026-07-02 and remain separate from Admin auth audit persistence. Chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred; broad paid launch remains pending final release-readiness review.

## 2026-07-01 CMS production verification update

Backend `0.1.35-backend.108` fixed `cmsUiAvailable`; System → Capabilities Check now shows it as AVAILABLE. CMS Content opens in Admin Shell and the CMS Content workspace loads. Learner runtime is using an active and valid `CmsPublishedSnapshot` for `static-json-v1`, published version `46`, with 6 topics, 26 scenarios, 4 prompt templates, 3 tutor behavior profiles, validation success `Yes`, and static JSON fallback currently `No`. No CMS content was saved, published, restored, initialized, imported, or mutated during this verification.

## 2026-07-02 refund and chargeback Premium protection

In production backend `0.1.35-backend.108`, full Paddle refunds are treated as access-control events after `adjustment.created` or `adjustment.updated` webhook processing: the backend preserves Paddle/payment/subscription history, maps the adjustment back to the internal user by safe metadata or existing payment/subscription records, and expires active provider-event Premium entitlements with reason `paddle_full_refund`. Chargebacks are implemented as stronger refund evidence and are covered by tests/fake paths, but no real live chargeback was performed.

Normal cancel-renewal behavior is unchanged: scheduled cancellation keeps Premium through the paid period end. Partial refunds are conservative in this slice: the event is safely recorded/processed for review and Premium is left unchanged unless the adjustment is full or a chargeback. Provider history is preserved; payment and subscription records are not deleted, and refund processing does not fake Paddle webhook events or expose raw provider payloads, webhook signatures, tokens, cookies, secrets, API keys, or full card/payment data in Admin Activity evidence.

Full-refund Premium revocation is production-verified on current production backend `0.1.35-backend.108`: the operator reprocess of stored provider event `evt_01kwhgmvh1v9k8ve70gvnfeskm` returned `Result=Revoked`, `RevokedCount=1`, and `BlockReason=(null)`; Admin User Lookup confirmed Free/no Premium/no Trial; Admin Activity showed `paddle_full_refund_premium_revoke` succeeded for the refunded user. Broad public paid launch is no longer blocked by full-refund revoke, but remains pending final release-readiness review and non-billing blockers. Expanded customer portal/subscription management is deferred and is not a current blocker. Direct installer code signing remains pending.

## Paddle full-refund recovery next step

Backend `.97` received a duplicate replay of the production Paddle `adjustment.updated` notification `evt_01kwhgmvh1v9k8ve70gvnfeskm`, but replay was idempotent and did not reprocess the already-normalized/skipped event that had been blocked while `.96` was active. Premium revocation is now production-verified after backend `.99` operator reprocess returned `Result=Revoked` for that existing provider event id.

Backend `.98` operator reprocess was run for existing provider event id `evt_01kwhgmvh1v9k8ve70gvnfeskm` and returned `Result=Blocked` / `BlockReason=reconciliation_blocked`; root cause was reprocess still depending on the old blocked reconciliation state. Backend `.99` operator reprocess returned `Result=Revoked`; do not create a new payment, do not create a new refund, and do not perform another Paddle replay or live billing test. Broad public paid launch is no longer blocked by full-refund revoke, but remains pending final release-readiness review and remaining blockers; expanded customer portal/subscription management is deferred and Direct installer code signing remains pending.
