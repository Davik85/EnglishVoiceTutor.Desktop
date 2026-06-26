# Website Paddle Review Readiness

Review date: 2026-06-25.

Public website: `https://languagevoicetutor.com`.
Production API reference only: `https://api.languagevoicetutor.com`.

## Scope and non-goals

This is a documentation-only audit of the public website source currently present in the repository, focused on what should be visible before requesting or relying on Paddle domain review for paid subscription use.

Non-goals for this task:

- Do not enable production/live Paddle.
- Do not change backend environment variables or deployment configuration.
- Do not add real Paddle API keys, price IDs, client-side tokens, webhook secrets, customer IDs, transaction IDs, raw payloads, signatures, OpenAI keys, JWT keys, connection strings, or other secrets.
- Do not change billing behavior, entitlement behavior, Desktop behavior, database migrations, deployment scripts, or production configuration.
- Do not provide final legal advice. Legal/policy page copy should be treated as owner/legal review draft material only.


## Static website deployment path status

Known source folder: public website source files live under `site/public/`. Do not modify `site/public` content as part of a deployment-path audit.

Known upload helper: `scripts/upload-static-site.ps1` uploads only the top-level files from `site/public` to the caller-provided `-RemotePath`. The helper validates that `-RemotePath` looks like an absolute Linux path, creates that directory, and copies files there, but it does not discover nginx configuration and does not prove that the supplied path is the public HTTPS web root.

Known Windows release path: Windows direct release files are a separate flow from the public website pages. Existing repository documentation records `/var/www/languagevoicetutor/releases/windows/direct` as the Windows direct release folder served through `/releases/windows/direct/`; do not mix installer/manifest uploads with website-page uploads.

Known repository-documented public website path: `docs/COMMAND_PLAYBOOK.md` records `/var/www/languagevoicetutor/site` as the public website nginx root and explicitly warns not to upload website files to `/var/www/languagevoicetutor/`. Treat `/var/www/languagevoicetutor` as an unsafe guessed parent path for static website uploads.

Unknown/needs verification: actual nginx web root for `languagevoicetutor.com` must be re-verified on the server before any future upload because repository notes can become stale and the upload helper accepts any syntactically valid absolute `-RemotePath`. Do not upload static site files to a guessed `-RemotePath`, including `/var/www/languagevoicetutor`, just because that directory exists or because a previous copy command succeeded.

Recommended safe next manual verification command, to run manually only by an operator with server access before any upload:

```powershell
ssh lvt-server "sudo nginx -T 2>/dev/null | sed -n '/server_name languagevoicetutor.com/,/server_name/p' | grep -E 'server_name|^[[:space:]]*root |^[[:space:]]*alias '"
```

This command is read-only and is intended to print only nginx `server_name`, `root`, and `alias` lines for the public site context; it must be reviewed before choosing any `scripts/upload-static-site.ps1 -RemotePath` value.

## Current website files/routes found in the repo

The public website appears to be maintained as static files under `site/public/`:

| Repo path | Externally expected route/path | Current purpose |
| --- | --- | --- |
| `site/public/index.html` | `/` | Landing page with Windows panel, planned mobile panel, footer links, and mail contact. |
| `site/public/download.html` | `/download.html` | Private tester Windows download page with app description, release details, installer manifest loading, SmartScreen warning, and support email. |
| `site/public/download.js` | `/download.js` | Loads `/releases/windows/direct/latest.json`, validates the installer filename/version metadata, and enables the Windows download link. |
| `site/public/styles.css` | `/styles.css` | Landing/download page styling. |
| `site/public/assets/images/landing/windows-desktop.webp` | `/assets/images/landing/windows-desktop.webp` | Landing image for Windows desktop app. |
| `site/public/assets/images/landing/mobile.webp` | `/assets/images/landing/mobile.webp` | Landing image for planned mobile apps. |
| `site/public/assets/images/landing/README.md` | not public page content | Image source/asset note. |
| `scripts/upload-static-site.ps1` | deployment helper, not a route | Uploads files from `site/public` to a static website folder. |

Related but not public marketing site source:

- `backend/EnglishVoiceTutor.Api/wwwroot/admin/` contains backend Admin UI static files, not the public Paddle review site.
- Existing Paddle/readiness planning docs include `docs/PADDLE_LIVE_READINESS_REVIEW.md` and `docs/paddle-production-readiness-checklist.md`, but those are internal planning documents rather than public website pages.

## Currently implemented public website pages/sections

### `/` landing page

Implemented sections/content:

- Product name: Language Voice Tutor.
- Windows app panel marked `Available for testers`.
- Short product description: practice real-life language lessons by text or voice on desktop.
- Mobile app panel marked `In development`, with Android/iOS planned.
- Footer copyright.
- Footer links labeled Privacy Policy and Terms of Use, but they point to in-page anchors (`#privacy-policy`, `#terms-of-use`) that do not currently exist in the page.
- Footer contact link to `support@languagevoicetutor.com`.

### `/download.html` tester download page

Implemented sections/content:

- Private tester download label.
- Product title and short Windows desktop/AI tutor description.
- Tester-only note.
- Current version/download button driven by manifest loading.
- Release details: version, channel, installer filename, size, SHA-256.
- SmartScreen warning because code signing is deferred.
- Support email link to `support@languagevoicetutor.com`.

### Release manifest/download path expected by JavaScript

`download.js` expects:

- `/releases/windows/direct/latest.json`
- installer files under `/releases/windows/direct/`

Those release files are expected externally but are not part of the static source files inspected for this documentation task.

## Current externally expected pages/sections for Paddle review

Paddle domain review commonly expects a public website to make the paid product, seller, support path, and customer terms understandable before customers pay. For this project, the public site should expose or link to owner/legal-reviewed drafts for:

- Clear product/service description.
- Pricing or subscription terms, including `<PREMIUM_PRICE_AND_BILLING_PERIOD>` once approved.
- Key features/deliverables included with purchase.
- Terms of Service / Terms and Conditions.
- Privacy Policy.
- Refund Policy.
- Cancellation policy / how to cancel.
- Support contact path, using `<SUPPORT_EMAIL>` and optionally `<SUPPORT_PHONE_OR_OWNER_DECISION>`.
- Company/legal seller information placeholder, using `<LEGAL_SELLER_NAME>`.
- Supported platforms.
- AI/data/privacy disclosures.
- Trial/free/premium explanation.

## Gaps for Paddle review

| Need | Current status | Gap |
| --- | --- | --- |
| Clear product/service description | Partial | Present as short marketing copy, but no fuller public explanation of how lessons, voice/text practice, accounts, or AI tutor behavior work. |
| Pricing/subscription terms | Missing | No public price, renewal period, billing cadence, taxes, or subscription terms. Use `<PREMIUM_PRICE_AND_BILLING_PERIOD>` until owner-approved. |
| Included features/deliverables | Partial | Windows download and AI tutor practice are described, but Premium deliverables and limitations are not listed. |
| Terms of Service / Terms and Conditions | Missing | Footer has a `Terms of Use` anchor link, but no actual terms route/section exists. |
| Privacy Policy | Missing | Footer has a `Privacy Policy` anchor link, but no actual privacy route/section exists. |
| Refund Policy | Missing | No refund terms or support process are visible. |
| Cancellation policy/how to cancel | Missing | No public explanation of subscription renewal cancellation or account/support flow. |
| Support contact path | Partial | `support@languagevoicetutor.com` is visible, but no support page or billing-specific support expectations are published. |
| Company/legal seller information | Missing | No seller/legal entity placeholder or address/owner-reviewed business information. |
| Supported platforms | Partial | Windows available for testers and mobile planned are visible; no formal supported OS/version/platform statement. |
| AI/data/privacy disclosures | Missing | The site mentions an AI tutor but does not explain AI processing, account data, voice/audio handling, retention boundaries, or third-party providers in owner/legal-reviewed terms. |
| Trial/free/premium explanation | Missing | No public explanation of private tester access, free limits, trial behavior, Premium benefits, or what changes after purchase. |
| Footer legal links | Broken/incomplete | Current `#privacy-policy` and `#terms-of-use` links do not target implemented sections. |

## Recommended minimal page map

Keep the first public review iteration small and static. Suggested minimum pages/routes:

| Route | Purpose |
| --- | --- |
| `/` | Public overview with product description, core features, supported platforms, AI disclosure summary, clear links to pricing, download, legal, refund, cancellation, and support pages. |
| `/pricing.html` | Premium subscription overview with `<PREMIUM_PRICE_AND_BILLING_PERIOD>`, renewal/cancellation summary, free/trial limits, and included deliverables. Owner/legal review required. |
| `/download.html` | Tester or public Windows download page. If still private, keep `Private tester download` wording and avoid public paid-launch claims. |
| `/terms.html` | Terms of Service / Terms and Conditions draft for owner/legal review. Include `<LEGAL_SELLER_NAME>`. |
| `/privacy.html` | Privacy Policy draft for owner/legal review, including account data, lesson data, voice/audio processing, AI provider disclosure, payment processor disclosure, support data, retention/deletion contact path, and children/minors stance. |
| `/refunds.html` | Refund Policy draft for owner/legal review, including how to request help through `<SUPPORT_EMAIL>`. |
| `/cancellation.html` | Cancellation/how-to-cancel page explaining customer cancellation path, cancellation-at-period-end behavior if that remains the product decision, and support escalation through `<SUPPORT_EMAIL>`. |
| `/support.html` | Support page with `<SUPPORT_EMAIL>`, `<SUPPORT_PHONE_OR_OWNER_DECISION>`, expected response window if owner-approved, and billing/account/download issue categories. |
| `/company.html` or footer block | Seller/legal information with `<LEGAL_SELLER_NAME>` and any owner-approved address/tax/business registration details. |

A single-page version may be acceptable for early review only if all legal/support/pricing sections are real, linkable, and visible. Separate pages are clearer and easier to review.

## Unsafe claims to avoid

Avoid publishing claims that are not yet operationally or legally approved, including:

- `Paddle live payments are enabled` or `production billing is ready` before owner-approved live configuration and legal/support review.
- `Available on iOS/Android` while mobile apps are only planned/in development.
- `Certified`, `guaranteed fluency`, `guaranteed results`, or similar outcome guarantees.
- `Unlimited` usage unless backend limits, costs, and abuse controls truly support it.
- `No data is stored`, `audio is never processed by third parties`, or `100% private` unless verified and approved against actual backend, AI, logging, analytics, payment, and support flows.
- `Refunds always granted`, `cancel anytime with immediate refund`, or other refund/cancellation promises unless owner/legal and operations approve them.
- `Secure payment handled by us` wording that confuses Paddle/payment processor responsibilities.
- Any exact pricing, tax, billing period, refund window, support SLA, legal entity, address, phone number, or compliance statement before owner approval.
- Any secret-bearing examples, transaction IDs, customer IDs, webhook payloads, signatures, API keys, OpenAI keys, JWT keys, connection strings, or production environment values.

## First implementation slices after this documentation task

1. Add static legal/support placeholder pages without enabling Paddle: `terms.html`, `privacy.html`, `refunds.html`, `cancellation.html`, `support.html`, and optionally `pricing.html`/`company.html`.
2. Fix footer links in `index.html` to point to real pages once those pages exist; mirror the legal/support links on `download.html`.
3. Draft owner/legal-review copy using only placeholders: `<LEGAL_SELLER_NAME>`, `<SUPPORT_EMAIL>`, `<SUPPORT_PHONE_OR_OWNER_DECISION>`, and `<PREMIUM_PRICE_AND_BILLING_PERIOD>`.
4. Add a concise pricing/free/trial/Premium section that does not contain live Paddle IDs or enable checkout.
5. Add an AI/data disclosure summary that accurately describes desktop-to-backend-to-AI processing at a high level without exposing internal secrets or raw payloads.
6. Add a static-site smoke check that verifies public legal/pricing/support links exist and do not point to missing anchors.
7. Run a final owner/legal/support review before any live Paddle domain submission or public paid launch claims.

## Static-site smoke test

Run `python3 tools/test_static_site_paddle_review_pages.py` before uploading the public site. The check verifies the Paddle review-readiness pages and links under `site/public/`, required owner/legal placeholders, absence of live checkout wiring, absence of obvious secret-like identifiers, and absence of paid-production/mobile-availability claims.

## Future CMS-managed website content planning

Future planning for managing public website legal, seller, support, policy, and pricing display copy through Admin CMS is documented in `docs/WEBSITE_CMS_LEGAL_CONTENT_PLAN.md`. That plan is documentation-only and should be treated as the future source for CMS-managed legal/site content planning; it does not change the current static `site/public/` review-readiness pages or enable production Paddle.


## Current status after 2026-06-25 Website CMS foundation rollout

- Static review-readiness pages exist for `pricing.html`, `terms.html`, `privacy.html`, `refunds.html`, `cancellation.html`, and `support.html` and remain the actual public website rendering source.
- Production public website root is confirmed as `/var/www/languagevoicetutor/site`. The accidental upload to `/var/www/languagevoicetutor/` was quarantined at `/var/www/languagevoicetutor/_mistaken_static_upload_20260625`.
- The Admin Website tab is a top-level read-only planning/status skeleton, not a `CMS Content` sub-tab. It does not save drafts, publish content, change static public pages, or serve public content.
- The Website CMS backend/database foundation exists and production migration `20260625090000_AddWebsiteCmsLegalContentFoundation` has been applied, but public rendering is not connected to Website CMS.
- Backend release `0.1.35-backend.52` is deployed, with `/health` and `/api/health/database` returning `200 Healthy` after deployment.
- Live Paddle remains disabled. No checkout links, checkout buttons, live Paddle identifiers, Paddle client token, webhook secret, or paid production behavior are enabled.
- Final public seller, legal, support, refund, cancellation, privacy, terms, and pricing values still require owner/legal approval.

## Secret and production-change confirmation

This audit document intentionally contains no real Paddle API keys, price IDs, client-side tokens, webhook secrets, customer IDs, transaction IDs, raw payloads, signatures, OpenAI keys, JWT keys, connection strings, or other secrets.

This task did not enable production/live Paddle, did not change backend environment variables, and did not change billing behavior, entitlement behavior, Desktop behavior, database migrations, deployment scripts, or production configuration.

## Static legal/support shell added

A minimal static website shell has been added under `site/public/` for Paddle review readiness: `pricing.html`, `terms.html`, `privacy.html`, `refunds.html`, `cancellation.html`, and `support.html`. The landing and download pages now link to these static pages.

Remaining owner/legal placeholders still require review before treating the copy as final policy or enabling paid production billing: `<LEGAL_SELLER_NAME>`, `<SUPPORT_PHONE_OR_OWNER_DECISION>`, and `<PREMIUM_PRICE_AND_BILLING_PERIOD>`. The shell does not add Paddle keys, Paddle identifiers, checkout buttons, backend configuration, deployment changes, or production billing behavior.
