# Next Steps

Review date: 2026-06-28.

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

## Release-readiness status

- Backend: production healthy at `https://api.languagevoicetutor.com`, current release `0.1.35-backend.74`.
- Website: generated public pages and Paddle-review polish are completed for `https://languagevoicetutor.com`.
- Download: current Windows tester release is visible without JavaScript and manifest-driven with JavaScript.
- Windows installer: current public tester release is `0.1.36-tester.30`, installer `LanguageVoiceTutorSetup-0.1.36-tester.30.exe`.
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

## Website/public review checklist

- Home shows logo, study language flags, Windows desktop app card, and “Android and iOS apps are planned but are not currently available.”
- Home does not say “Mobile version coming soon” and does not claim mobile apps are currently available.
- Footer has primary links: Privacy Policy, Terms of Use, Refund Policy, Cancellation, Support, Pricing.
- Footer has secondary links: Seller / Company Details, AI & Data Disclosure, Service Status.
- `seller.html`, `ai-data.html`, and `status.html` exist and are linked from the footer.
- Download page statically shows current release details when the manifest is available and remains supported by `download.js` and `/releases/windows/direct/latest.json`.
- Download non-JS fallback text remains: “Current Windows tester release is available through the Download for Windows button.” and “If release details do not load automatically, please contact [support@languagevoicetutor.com](mailto:support@languagevoicetutor.com).”

## Windows direct release next-step guardrails

Current manifest: `https://languagevoicetutor.com/releases/windows/direct/latest.json`.

Expected current values:

- `version`: `0.1.36-tester.30`
- `installerFileName`: `LanguageVoiceTutorSetup-0.1.36-tester.30.exe`
- `backendBaseUrl`: `https://api.languagevoicetutor.com`
- `updateMode`: `manual-confirmation`

Use:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 -Version 0.1.36-tester.30
```

Do not manually `scp` installer files if the upload script exists. After upload, verify `latest.json`, installer filename, backend base URL, installer hash, and that the download page button downloads the same installer.

Code signing remains deferred. CMS published-snapshot runtime is active for controlled tester lessons. Backend deployment, database migrations, the download website, and update UI remain separate work.

## Paddle/live billing next-step guardrails

Production billing/Paddle/subscription payment lifecycle remains deferred. Live Paddle is not enabled yet. Do not change production Paddle environment values, add live checkout links, or commit secrets. Paddle stays behind the backend/provider adapter. Desktop does not call Paddle directly and does not decide Premium directly.

Backend remains source of truth for plan, subscription, entitlement, usage, and limits. Entitlement is the source of Premium access; `PaymentEntity` is diagnostic payment history only. Desktop and future mobile clients share one backend account, one backend database, one subscription/entitlement state, and one lesson history/progress source. Paddle may be the first web/desktop provider, but Apple/Google must remain possible later for mobile. Do not add YooKassa or Russia-only billing assumptions.
