# Website Paddle review readiness

Review date: 2026-06-28.

## Current public website status

Public site: `https://languagevoicetutor.com`.

The current generated public website is Paddle-review-polished and ready for final owner/legal review. This is not final legal advice and does not enable live Paddle.

Public generated pages include:

- `index.html`
- `download.html`
- `mobile.html`
- `pricing.html`
- `support.html`
- `terms.html`
- `privacy.html`
- `refunds.html`
- `cancellation.html`
- `seller.html`
- `ai-data.html`
- `status.html`

The home page shows the logo, supported study language flags, a Windows desktop app card, and safe mobile wording. It must not claim mobile apps are currently available and must not say “Mobile version coming soon”. The approved wording is: “Android and iOS apps are planned but are not currently available.”

The shared footer has two rows:

- Primary: Privacy Policy, Terms of Use, Refund Policy, Cancellation, Support, Pricing.
- Secondary: Seller / Company Details, AI & Data Disclosure, Service Status.

`seller.html`, `ai-data.html`, and `status.html` are part of the public site and are linked from the footer.

## Website CMS source and flow

The Website CMS exists in the Admin Shell under **Website**. It is intentionally simple and informational only, not a full CMS. It is Super Admin / Bootstrap Admin protected and JSON/file-based.

- Content storage: `site/content/website-content.json`
- Content model: active and draft content in the JSON document
- Public static output: `site/public`

Flow:

1. Admin Website tab loads draft/active content.
2. **Save draft** writes draft content.
3. **Preview** renders the selected page preview without publishing.
4. **Publish / Make active** promotes draft/active content and renders static HTML files.

The normal-page editor is simplified to Page title, Body markdown, SEO title, and SEO description. Home remains structured because it has landing cards/assets. Design is not treated as a normal Super Admin editing page.

Markdown rendering supports headings, bold, italic, bullet lists, numbered lists, markdown links, plain safe URLs, plain emails, and bare domains such as `Paddle.com`. Unsafe schemes such as `javascript:`, `data:`, and `vbscript:` must remain rejected or escaped.

Admin Website CMS endpoints remain authenticated/authorized but no longer consume the normal admin read/write rate limit because legal text editing previously caused `RateLimitExceeded`.

## Website CMS Marketing / SEO and public crawler readiness

The Website CMS now includes a visible **Marketing / SEO** section with consent-banner, analytics, ads, Search Console verification, and `llms.txt` controls. These values are stored through the existing JSON/file-based Website CMS model; no database table, schema change, migration, backend secret, env value, or committed example JSON value is required for Google setup. Real Google IDs, conversion labels, and Search Console tokens must be entered only in Admin Website CMS when available, never in code, docs, env files, or committed JSON examples.

Current safe CMS values before real Google setup: Enable consent banner ON, Enable `llms.txt` ON, Enable analytics OFF with an empty GA4 Measurement ID, Enable ads tracking OFF with empty Ads ID and conversion label, and an empty Search Console verification token until property verification begins.

Operator field guide: GA4 Measurement ID comes from Google Analytics → Admin → Data streams → Web stream for `languagevoicetutor.com` and has format `G-XXXXXXXXXX`; Google Ads ID comes from Google Ads conversion tag setup and has format `AW-123456789`; the download conversion label comes from the same conversion action setup; Search Console token comes from HTML tag verification for `https://languagevoicetutor.com/` and only the `content="..."` value should be copied. Do not paste placeholders, whole script snippets, or GTM container IDs into these fields unless GTM support is explicitly added later.

Website Publish now emits or maintains public HTML pages, `robots.txt`, `sitemap.xml`, `llms.txt` when enabled, and `marketing-consent.js`. Generated pages include canonical URLs, meta descriptions, Open Graph/Twitter tags, JSON-LD where appropriate, and SoftwareApplication JSON-LD for Windows desktop only. They must not claim Android/iOS, Microsoft Store, Google Play, or App Store availability.

Consent mode defaults to denied before user choice for `analytics_storage`, `ad_storage`, `ad_user_data`, and `ad_personalization`. The banner supports Accept all, Reject non-essential, Manage choices, and a Privacy Policy link. Privacy Policy includes optional analytics, advertising, and cookie consent disclosure. The website remains usable when non-essential cookies are rejected, and GA/Ads scripts must not be emitted when IDs are empty or tracking is disabled.

Final verification should confirm public pages do not contain placeholder IDs such as `G-XXXXXXXXXX` or `AW-123456789`, do not include `googletagmanager.com/gtag/js` while IDs are empty, `download.html` shows current Windows installer details from `latest.json` when static release details are available, and `robots.txt`, `sitemap.xml`, `llms.txt`, and `marketing-consent.js` return `200`.

## Download page readiness

The Windows direct release manifest is:

```text
https://languagevoicetutor.com/releases/windows/direct/latest.json
```

Current public tester release:

- `version`: `0.1.36-tester.31`
- `installerFileName`: `LanguageVoiceTutorSetup-0.1.36-tester.31.exe`
- `backendBaseUrl`: `https://api.languagevoicetutor.com`
- `updateMode`: `manual-confirmation`

The download page is manifest-driven and also useful without JavaScript. When the local/public manifest is available, the static page shows current release details instead of only showing Loading or Unavailable. It keeps `download.js` and `/releases/windows/direct/latest.json` support.

Required static fallback text:

- “Current Windows tester release is available through the Download for Windows button.”
- “If release details do not load automatically, please contact [support@languagevoicetutor.com](mailto:support@languagevoicetutor.com).”

Windows direct release upload is separate from backend deploy and static website publish. Use `scripts/upload-windows-direct-release.ps1`; do not manually `scp` installer files when the script exists. After upload, verify `latest.json`, `installerFileName`, `backendBaseUrl`, installer hash, and that the download page button downloads the same installer.

## Paddle/legal readiness

Live Paddle is not enabled yet. Do not change production Paddle environment values during website review. Do not place real Paddle API keys, price IDs, client-side tokens, webhook secrets, raw payloads, signatures, customer IDs, transaction IDs, JWT secrets, database URLs, OpenAI keys, or other secrets in docs or public pages.

Paddle remains behind the backend/provider adapter. Desktop must not directly decide Premium and must not directly integrate with Paddle. Backend remains the source of truth for plan, subscription, entitlement, usage, and limits. Entitlement remains the source of Premium access; `PaymentEntity` is diagnostic payment history only.

Website/legal pages prepared for review:

- Pricing / Subscription terms
- Terms of Use
- Privacy Policy
- Refund Policy
- Cancellation Policy
- Support
- Seller / Company Details
- AI & Data Disclosure
- Platform Availability / Service Status
- Download page

Legal texts are product/legal drafts and must not be described as final legal advice. Seller details are public business details only; do not publish passport/private personal data. `Paddle.com` bare domains are clickable via markdown/autolink rendering. Download page, footer, and legal/support pages should be considered Paddle-review-ready pending final owner/legal review.

## Release-readiness status

- Backend: production healthy at `https://api.languagevoicetutor.com`, current release `0.1.35-backend.82`.
- Website: public pages generated and Paddle-review polish completed.
- Download: current Windows tester release visible without JavaScript and manifest-driven with JavaScript.
- Windows installer: current public tester release `0.1.36-tester.31`.
- Billing: Paddle live not enabled yet.
- Legal: legal/support/seller/AI/status pages ready for owner/legal final review.

Remaining release steps:

1. Final manual website review in incognito.
2. Final owner/legal text review.
3. Final Windows installer smoke.
4. Paddle live readiness checklist.
5. Only after approval: production Paddle environment, token, webhook, and price setup.
6. Microsoft Store/MSIX is discontinued for now and must not be listed as an active next step or claimed as currently available.

Do not state that the product is fully public production-ready. The current Windows release remains a controlled tester/direct Windows release, not a broad public production launch, and not broad public production readiness. Production/live Paddle readiness remains deferred.

## 2026-06-30 approved-domain `/pay.html` checkout page

The public website now includes `/pay.html` for Paddle approved-domain checkout. It loads Paddle.js from `https://cdn.paddle.com/paddle/v2/paddle.js`, reads `_ptxn`, validates it as a Paddle transaction id, initializes Paddle with a public client-side token loaded from `/paddle.public.json`, and calls `Paddle.Checkout.open({ transactionId })`. If `_ptxn` is missing/invalid, Paddle.js fails to load, or the public token config is missing, it shows a safe support-oriented fallback without secrets.

Publishers must create `/paddle.public.json` during static-site publish from `site/public/paddle.public.example.json` and inject only the live Paddle client-side token. Do not publish server API keys, webhook secrets, `.env` files, private keys, generated installer artifacts, or backend signing material. Backend deployment and Website CMS/static publish remain separate operations.
