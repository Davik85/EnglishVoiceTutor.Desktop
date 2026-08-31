# Current State

Review date: 2026-08-31.

## 2026-08-31 Restore Credentials production and Mobile Internal-testing checkpoint

Restore Credentials is implemented with Fido2 4.0.1 and is enabled in production backend `0.1.35-backend.145`. Migration `20260831080122_AddRestoreCredentialsFoundation` was reviewed and applied to production, creating only the Restore Credentials foundation tables; neither `.144` nor `.145` required an additional EF migration. The RP ID is `languagevoicetutor.com`; the configured Android origins include the verified Play application signer origin `android:apk-key-hash:5gr1ycjiv39bNQDeH1A-Fv_MciZhEEWhOaL7vdAsXpk`. The separately observed Google Play Source Stamp certificate is not an application-signing origin and is not allowed. `/.well-known/assetlinks.json` is published and live with both required relations and the verified Play signing certificate fingerprints.

Registration initially rejected credentials with `UserVerificationRequirementNotMet`. Commit `ac128f269a21d21c11b2985ff3665e53b5f3d574` added fixed privacy-safe rejection classification without changing authentication behavior; commit `6bad13ea1db31f8161710c8a054d0970c1cc9dfe` corrected the root cause by retaining `ResidentKey=Required` while explicitly setting registration `UserVerification=Discouraged`, matching the already-discouraged assertion policy. Deployment and health verification completed successfully. New registrations then produced no Restore Credentials rejection log and production contained active restore credentials (`active_total=2` in the immediate read-only check).

Mobile `0.1.0+8` / versionCode 8 is uploaded to and installed from Google Play Internal testing. It preserves ordinary device-bound secure session storage by excluding FlutterSecureStorage from Android cloud/device-transfer and legacy full backup; a fresh verified assertion creates the existing normal backend `AuthResponse` session with a new normal refresh token rather than transferring an existing refresh token. Password authentication remains available and authoritative. Explicit logout suppresses/clears automatic Restore Credentials restoration, and account anonymization removes Restore Credentials public-credential and ceremony state. A 2026-08-31 cross-device Android device-transfer test completed successfully: a Play-installed v8 source device with a registered restore credential restored to a clean target device, automatically authenticated without login/password entry, and launched working lessons. This verifies the tested Android/Google Play account-session restoration path only; it does not verify Google Play billing purchase restoration, refunds, pending purchases, other billing lifecycles, public rollout, or general Android public availability.

## 2026-08-29 / 2026-08-30 controlled Google Play validation and reconciliation checkpoint

Production backend remains `0.1.35-backend.142`, built from source commit `c199dc6064c34f3b705eb4a56aed6aa6c684fb9c`; repository `HEAD` may be newer because of documentation-only operational records. The real Internal-testing versionCode 5 completed a controlled Google Play license-test purchase: the Play purchase sheet opened, Mobile sent the purchase to the existing backend verification path, backend verified the subscription, backend-owned Premium became active, and Admin CMS showed `billingProvider=google_play` and `renewalStatus=renewal_active`. Mobile neither grants Premium locally nor owns acknowledgement.

The earlier catalog/configuration blocker is closed. During accelerated license testing, Google Play renewed approximately every five minutes but backend Premium initially expired after the first period because `GooglePlayReconciliation__Enabled=false`. On 2026-08-29 the operator backed up configuration, enabled `GooglePlayReconciliation__Enabled=true`, restarted backend `.142`, verified the service and both health endpoints at HTTP 200, and observed successful subscriptions-v2 verification logs. No reconciliation poll interval is asserted here. `GooglePlayPendingRefundReview` remains disabled unless separately verified.

After enablement, a new license-test subscription renewed across subsequent periods and backend reconciliation refreshed the same Google Play Premium entitlement from Google Play subscriptions-v2 authoritative state. CMS remained/returned to `planId=premium`, `premiumActive=Yes`, `subscriptionStatus=active`, `billingProvider=google_play`, and `renewalStatus=renewal_active`. The entitlement expiry moved forward. A few-second transient Free window was observed around accelerated renewal boundaries before reconciliation refreshed state; this is non-blocking controlled-test evidence, not a production outage or a defect reproduced under a normal monthly period. After the permitted test renewals ended, final expiry returned Google Play, backend Premium, and Mobile to non-Premium and restored new-purchase eligibility. This proves controlled purchase -> backend Premium -> renewal processing -> final expiry -> backend Free; it does not mean Google grants a brand-new subscription automatically after expiry.

Current controlled configuration is `GooglePlayBilling.Enabled=true`, `GooglePlayRtdn.Enabled=true`, and `GooglePlayReconciliation.Enabled=true`, with Internal testing (not Closed testing), normal Mobile runtime only, Product ID `premium`, active Base Plan ID `monthly`, and no Google Play free trial or introductory offer. Pending-payment, explicit pre-expiry cancellation, fresh-install restore, real-money purchase, refunds/voided purchases, chargebacks, broad public rollout, and wider provider-isolation edges remain unproven. Play Console review on 2026-08-30 confirmed `com.languagevoicetutor.mobile` Registered under ORRALEN TECHNOLOGIES LTD with three registered keys, no active Policy center issues, completed App content declarations with assigned ratings, automatic Play protection active, no Play Integrity API integration, and Target SDK 36 on the current Internal-testing Android line. This is a compliance snapshot, not legal advice or a public-rollout approval.

## 2026-08-28 backend `.142` account-wide Premium and Google Play purchase-gating foundation rollout

Production backend `0.1.35-backend.142` is active at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.142`, with `0.1.35-backend.141` retained as rollback. Deployment used source commit `c199dc6064c34f3b705eb4a56aed6aa6c684fb9c` through the standard repository package -> upload dry-run -> real upload/restart flow. `languagevoicetutor-backend.service` is active and listening normally on `127.0.0.1:5001`; public `/health` and `/api/health/database` both returned HTTP 200 after deployment.

Migration `20260827105749_AddGooglePlayTrialDeferralFoundation` was applied from starting migration `20260803052655_AddGooglePlayPendingRefundReviewFoundation` after reviewed bounded SQL (SHA-256 `168C4D9EDACABD448E51A0326EA5E7A21DC50185FEBCF55ABBAEB178483A4BDD`) and a fresh readable backup at `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260828_052137Z.dump` (8,012,168 bytes; `pg_restore --list` 315 lines). It created only `public.google_play_initial_premium_deferrals`, its primary key, the unique `GooglePlayPurchaseClaimId` index, `Status` + `NextAttemptAtUtc` and `UserId` indexes, and the EF history row. No existing application table or application data changed. The table is owned by `lvt_app`, runtime access is verified, `lvt_analytics_reader` has no access, and the table was empty immediately after migration.

The deployed backend now calculates one provider-neutral continuous Premium timeline across backend trial, `manual_admin`, Paddle, and Google Play coverage, while Paddle lifecycle/refund/chargeback mutation remains exact-Paddle-owned. The authenticated `SubscriptionStatus` Google Play new-purchase gate fails closed: active/trialing Paddle cancellation permits a future Google purchase only with matching latest processed Paddle `BillingEvent` evidence of a complete authoritative `cancel` snapshot and strictly future effective time; stale, legacy, or unproven cancellation state, past-due/paused recovery state, and multiple or ambiguous renewal owners block. These defensive backend ownership semantics do not cancel, switch, or replace providers and do not change Premium coverage calculation incorrectly.

Google Play purchase processing, RTDN, reconciliation, and pending-refund runtime remain disabled. Backend Data Protection is now enabled through production configuration, using the persistent key ring at `/var/lib/languagevoicetutor/backend/data-protection/key-ring` and active certificate at `/etc/languagevoicetutor/data-protection/certificates/active/backend-data-protection.pfx`, both outside versioned backend releases. The active PFX was opened successfully with its configured secret and has a valid private key; it is issued to and by `CN = Language Voice Tutor Backend Data Protection` and is valid from 2026-08-28 09:53:05 GMT through 2031-08-27 09:53:05 GMT. The key ring is `0700 deploy:deploy`, the active PFX is `0640 root:deploy`, and the certificate-password source is `0600 root:root`. A protected backup under `/var/backups/languagevoicetutor/data-protection` passed `sha256sum` integrity verification and an isolated restore drill; the restored PFX opened and its private key was valid, and the temporary restore directory was removed. A pre-change `backend.env` backup was made, the service restarted successfully, startup logs showed no Data Protection/certificate/PFX/key-ring initialization errors, and public `/health` plus `/api/health/database` returned HTTP 200 `Healthy` (`canConnect=true`). The persistent key-ring directory currently has no key XML because no real protected Google Play token operation was intentionally triggered merely to generate one; this is expected, not a failure. Android Publisher authentication is provisioned through the production service-account credential outside versioned release directories; an OAuth probe and a read-only Android Publisher request both succeeded. This verified provider access did not create a Google Play purchase or mutate a provider record, and it does not enable the disabled Google Play runtime. Google Cloud Pub/Sub / RTDN production configuration remains unconfigured. Product ID remains `premium`; Base Plan ID `monthly` is still a Play Console draft and is not activated. Mobile purchase-gate source is committed separately, but no Mobile store release is claimed. This is a verified backend/migration, Data Protection, and Android Publisher authentication provisioning closeout, not broad public production readiness.

## 2026-08-25 Windows Direct Release 1.6 and historical backend `.141`

Windows Direct Release `1.6` is the current verified public release on channel `direct-public`. The public manifest identifies `LanguageVoiceTutorSetup-1.6.exe`, `version: 1.6`, `minimumSupportedVersion: 1.6`, `backendBaseUrl: https://api.languagevoicetutor.com`, and `updateMode: manual-confirmation`. The published manifest records installer SHA-256 `9eaac1ffa1ead6c3590f2cf072ff6dcabb7edba912c38a6cd1d6875ad5ac1aa3` and size `188959874` bytes. The live cache-busted manifest was verified after upload; no independent second public-download SHA verification is claimed for 1.6.

At that rollout, production backend `0.1.35-backend.141` was current, with `0.1.35-backend.140` retained as rollback. Approved source commit `5c8e973a0c2ea3f24186c30bf743a77d8d776e57` (`Remove legacy free usage limits`) was already pushed to `origin/main`. The standard repository package -> dry-run -> upload flow was used. Public `/health` returned HTTP 200 `Healthy`; `/api/health/database` returned HTTP 200 `Healthy` with `canConnect=true`. No EF migration or database schema change was part of `.141`.

Release `.141` removes the obsolete legacy `free_dev` / `FreeLimits` product-limit mechanism while preserving usage-event and daily-usage persistence. Free accounts receive one free lesson per day. Once that lesson is allowed to start, there is no separate Free-plan quota for chat replies, hints, STT/transcription, TTS, feedback, or estimated-cost usage; Trial and Premium/Pro do not have those product quotas. Technical ASP.NET rate limiting remains separate infrastructure/anti-abuse protection, not a tariff or subscription limit.

Windows 1.6 includes the corresponding Desktop cleanup: obsolete learner-facing Free-limit behavior for chat, hints, transcription, and TTS is removed. A technical HTTP 429 is shown as a localized temporary too-many-requests/wait-and-retry condition rather than Free-plan exhaustion, and a valid 429 does not mark the backend unreachable. The operator successfully completed the manual-confirmation update from installed Windows 1.5 to 1.6; no broader installed-app functional smoke is claimed for that update.

No Mobile release, Website CMS publish, static website deployment, billing/Paddle/Google Play enablement, authentication change, or entitlement-architecture change occurred. Google Play remains disabled. Language Voice Tutor remains the product/application name, the stable Windows AppId remains `LanguageVoiceTutor.Desktop`, and the existing ORRALEN icon/shortcut behavior remains unchanged. Code signing remains deferred. This is a public Windows direct release and verified backend deployment, not a full broad production-readiness claim.

## Historical 2026-08-24 Windows Direct Release 1.5

Windows Direct Release `1.5` was the verified public release before 1.6 on channel `direct-public`. Its public manifest identified `LanguageVoiceTutorSetup-1.5.exe`, `version: 1.5`, `minimumSupportedVersion: 1.5`, `backendBaseUrl: https://api.languagevoicetutor.com`, and `updateMode: manual-confirmation`. The exact public installer SHA-256 was `dea33ac29414d5956db52cec0dd703ecb12778e071c1e601dcf394f1def2e10b`; the installer was `188955887` bytes, and an independent public HTTPS download produced the same SHA-256.

This was a Windows direct-release upload only. No backend deployment, EF migration, database change, Mobile release, Website CMS/public-site change, billing/authentication/lesson behavior change, or Google Play enablement occurred. Production backend remains `0.1.35-backend.140` with `0.1.35-backend.139` retained as rollback at `https://api.languagevoicetutor.com`; Google Play remains disabled.

The only product-visible change from Windows 1.4 is a refinement to the ORRALEN Windows application icon artwork/background. Language Voice Tutor remains the product/application name, the stable Windows AppId remains `LanguageVoiceTutor.Desktop`, and application functionality, accounts, local-data identity, and update continuity remain unchanged. This is not a full product rename or wider Desktop/Mobile rebrand.

The existing shortcut-icon cache mitigation remains unchanged: canonical `{app}\Assets\Branding\app-icon.ico` stays installed, Start Menu and common Desktop shortcuts use `{app}\Assets\Branding\app-icon-1.5.ico`, and upgrade cleanup removes only old `app-icon-*.ico` files under `Assets\Branding` without deleting canonical `app-icon.ico`.

Code signing remains deferred. This is a public Windows direct release, not a full broad production-readiness claim. The static/no-JavaScript fallback was not separately verified by this Windows release upload.

## Historical 2026-08-24 Windows Direct Release 1.4

Windows Direct Release `1.4` was the verified public release before 1.5. Its public manifest identified `LanguageVoiceTutorSetup-1.4.exe`, `version: 1.4`, `minimumSupportedVersion: 1.4`, `backendBaseUrl: https://api.languagevoicetutor.com`, and `updateMode: manual-confirmation`. The exact public installer SHA-256 was `d7c8dec5495bc08ba426614f14033e1b9363daa8eb1de6d1130e450071de277c`; the installer was downloaded again over HTTPS after upload and its computed SHA-256 matched `installerSha256` in the public `latest.json`.

This was a Windows direct-release upload only. No backend deployment, EF migration, database change, Mobile release, Google Play enablement, Website CMS publish, or static website publish occurred. Production backend remains `0.1.35-backend.140` with `0.1.35-backend.139` retained as rollback at `https://api.languagevoicetutor.com`; Google Play remains disabled.

Windows 1.4 introduces the ORRALEN application icon as a bounded Desktop branding slice, not a product rename. The product/application name remains Language Voice Tutor, the stable Windows AppId remains `LanguageVoiceTutor.Desktop`, and existing account and update continuity remain unchanged. Broader Desktop and Mobile product-facing ORRALEN rebranding remains future audited work.

The canonical installed icon remains `{app}\Assets\Branding\app-icon.ico`. Start Menu and common Desktop shortcuts use the version-specific `{app}\Assets\Branding\app-icon-1.4.ico`; during upgrades, cleanup is limited to old `app-icon-*.ico` files inside `Assets\Branding` and does not delete canonical `app-icon.ico`. Changing the shortcut icon path with `AppVersion` prevents Windows Explorer from reusing the prior release icon from its path-based cache after an in-place upgrade.

Code signing remains deferred. This is a public Windows direct release, not a full broad production-readiness claim. The normal manifest-driven download and public installer hash were verified, but the static/no-JavaScript fallback was not separately verified by this Windows release upload.

## Historical 2026-08-22 `.140` Website CMS ORRALEN design-contract deployment

Live verification on 2026-08-22 confirmed production backend `0.1.35-backend.140`, rollback backend `0.1.35-backend.139`, an active `languagevoicetutor-backend.service`, HTTP 200 `Healthy` from `/health`, and HTTP 200 `Healthy` with `canConnect=true` from `/api/health/database`. Backend/Desktop repository `HEAD` and freshly fetched `origin/main` both resolved to `2df4316f8c51e01af1cefb94f9349e07ef5f484a`.

Release `.140` adds the Website CMS contract and Admin support for independent `FooterTextColor`, so Header background, Header text, Footer background, Footer text, and Main text remain independently CMS-owned design values. This additive JSON/file contract expansion required no EF migration or database schema change. It did not enable Google Play Billing, RTDN, reconciliation, or pending-refund processing; did not change Paddle behavior; and did not change Desktop or Mobile runtime behavior. Backend deployment, Website CMS Publish, and static-site upload remain separate operations.

The public website now presents ORRALEN as the operating/company/master brand and identifies the operating company as ORRALEN TECHNOLOGIES LTD, while Language Voice Tutor remains the current product/application name. This website work does not mean the Desktop or Mobile clients have completed a product-facing ORRALEN rebrand. Existing accounts, backend contracts and URLs, Premium entitlement architecture, History, Progress, billing-provider architecture, package/product/subscription identifiers, signing, and update continuity remain unchanged.

## 2026-08-27 account-wide Premium stacking correction, deployed in `.142`

Source now uses one provider-neutral continuous-Premium tail for backend `TrialGrant`, `manual_admin`, Paddle `provider_event`, and Google Play `provider_event` coverage. Contiguous future Premium extends the same account-wide timeline; overlaps extend only to the later real end and are never summed twice; gaps, revoked/inactive/expired rows, other users, plans, or entitlement types do not extend the current tail. Admin grant scheduling and the existing subscription-status response/Desktop display semantics are preserved, while fixed Paddle periods now start after the complete continuous tail.

Paddle mutation is provider-specific: activation, lifecycle expiry, full-refund, and chargeback require exact Paddle `SubscriptionId` ownership. Google, `manual_admin`, trial, and other Paddle subscription coverage can contribute to the provider-neutral timeline but are not Paddle mutation targets. Legacy unscoped `provider_event` rows with `SubscriptionId == null` are never retroactively claimed; ambiguous adjustment ownership fails closed for manual review. This defensive ownership isolation is deployed in `.142`; it does not enable Paddle or Google Play production runtime.

Backend `.142` adds an authenticated `SubscriptionStatus` Google Play new-purchase gate calculated from all Premium provider subscriptions for the account, independently of the single provider snapshot selected for display. Trial, `manual_admin`, fixed non-renewing coverage, terminal provider subscriptions, and active/trialing Paddle with a future scheduled cancellation proven by its matching latest processed `BillingEvent` complete-snapshot metadata do not block a new Google purchase. Legacy cancellation fields without that authoritative event proof fail closed, as does a scheduled-cancel effective time that has already been reached while local state remains active/trialing. Paddle `subscription.updated` is persisted as a current snapshot: explicit `scheduled_change: null` clears an earlier cancel/pause/resume, while absent or invalid scheduled-change evidence is rejected and cannot clear stored state. Active renewal after that authoritative removal, payment-retry/past-due, hold, paused/resumable, or otherwise recoverable external renewal ownership blocks; past-due and paused state take precedence over cancellation metadata. Multiple apparent owners and incomplete or conflicting provider state fail closed. This gate changes neither the provider-neutral Premium timeline nor provider lifecycle mutation, and Google Play runtime remains disabled.

For a newly verified Google purchase, the immutable initial deferral is the account's existing continuous Premium tail at the provider purchase start minus that start, not only remaining trial. Positive sub-day duration still uses Google's exact 24-hour minimum. Production mutation requires exact Product ID `premium`, exact Base Plan ID `monthly`, a 28-to-31-day ordinary base-price auto-renewing period, and no Play trial, introductory offer, prepaid plan, promotion, replacement, or ambiguous shape. A separate license-test-only accelerated-period path requires both existing `TestPurchasesEnabled` and exact `AllowedTestPurchaseUserIds` authorization; it does not weaken production eligibility. The provider mutation remains acknowledgement-first, ETag-bound, exactly once per purchase claim, and authoritative only after the fresh post-defer GET confirms the stored target.

Migration `20260827105749_AddGooglePlayTrialDeferralFoundation` is applied in `.142` and created the additive `google_play_initial_premium_deferrals` table. Its row stores the original coverage start/tail, provider baseline, immutable duration/target, license-test marker, ETag, retry/outcome state, and authoritative result; the purchase-claim unique index prevents the same initial coverage from being added twice. At the `.142` rollout, Production Google Play Billing, RTDN, reconciliation, and pending-refund processing remained disabled; no credentials, Data Protection provisioning, Google Cloud/Pub/Sub setup, provider call, or runtime enablement occurred. The Product ID is `premium`; Base Plan ID `monthly` exists in Play Console only as a draft and is not activated, with no Google Play trial or introductory offer.

## Historical 2026-08-03 `.139` disabled Google Play RTDN, reconciliation, and pending-refund rollout

Implementation commits `e9c09c5c8125d2bd16b0f9ed102eb8376cfa565c` and `91bd0830b7df7cfef1c1174985583c8b821c746e` are deployed in backend `0.1.35-backend.139`, with `.138` retained as rollback. Migrations `20260802154345_AddGooglePlayRtdnPersistenceFoundation` and `20260803052655_AddGooglePlayPendingRefundReviewFoundation` are applied. The additive tables `google_play_purchase_token_secrets`, `google_play_rtdn_events`, and `google_play_pending_refund_reviews` exist, are owned by `lvt_app`, have required runtime access, and have no listed `lvt_analytics_reader` privileges; all were empty immediately after migration. The protected RTDN, reconciliation, linked-purchase, and pending-refund foundations are deployed, but Google Play Billing, RTDN, reconciliation, and pending-refund review remain disabled. No Google Cloud or Play Console configuration, Google Play record, Desktop/Mobile release, Website CMS/public-site publish, or intentional Paddle/trial/manual-Premium behavior change occurred.

Backend Data Protection certificate-rotation support is implemented in source only. Production has no certificate provisioned, no persistent key ring provisioned, no Data Protection enablement, no Google credentials, and no Google Play processing.

The standard `scripts/upload-backend-linux-release.ps1` `-PackageFirst` dry run and upload flow was used after a fresh readable PostgreSQL backup. `languagevoicetutor-backend.service` is active and running; startup was normal and listening on `127.0.0.1:5001`; public `/health` and `/api/health/database` returned HTTP 200 after both migration and deployment.

## Historical 2026-07-30 production `.138` trial/manual Premium expiry correction

For that deployment, production backend was `0.1.35-backend.138`, with `0.1.35-backend.137` retained as rollback. Source commit `fcba7a8d5a92e77da868b7857c7c5bd85d4f93bb` (`Fix trial and manual premium expiry calculation`) was deployed at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.138`; the previous release was `/opt/languagevoicetutor/backend/releases/0.1.35-backend.137`. `languagevoicetutor-backend.service` was active and running, and public `/health` plus `/api/health/database` returned HTTP 200 `Healthy` with `canConnect=true`.

This release restored the established Premium extension behavior; it did not introduce a new subscription timing system. Manual Premium grant creation now includes an active account trial stored in `TrialGrants` as well as applicable active Premium `Entitlements`. For a trial ending at `T1`, a new manual grant starts at `T1` and its duration is calculated from that delayed start. If an applicable Premium entitlement ends later than the trial, the later expiry remains the extension base. Expired, revoked, inactive, and other-user entitlements do not extend the grant.

Subscription status preserves the later final Premium coverage expiry while the trial is active. An already-overlapping manual entitlement can therefore report its later stored expiry without a status read changing `StartsAtUtc`, `ExpiresAtUtc`, or any other database value. Manual Premium remains provider-neutral: it is not presented as Paddle or Google Play and does not expose provider renewal or cancellation controls.

Production smoke used a controlled account without recording identifying values. Installed Desktop 1.3 refreshed its unchanged Account layout from the backend: current tariff remained Trial, no new trial-status line was introduced, Premium displayed active until 2026-11-14 instead of the earlier trial expiry, and Auto-renew remained inactive. Admin CMS **Premium Entitlement Schedule** showed the scheduled `manual_admin` entitlement with a future start around 2026-08-06 and final expiry around 2026-11-14. Its absence from **Active Entitlements** before `StartsAtUtc` is expected, because that view contains currently active rather than future scheduled entitlements.

The deployment required no EF migration, schema change, or production-data rewrite. No Desktop, Mobile, Windows installer, static-site, or Website CMS release occurred. Google Play verification, ownership, token fingerprinting, persistence, acknowledgement state, and Mobile purchase flow were unchanged. Paddle production behavior was unchanged.

## 2026-07-30 production `.137` Website CMS legal-text deployment

For this historical deployment, production was `0.1.35-backend.137`, with `0.1.35-backend.136` retained as rollback. Commit `fc00d5e9c482c4aed857ff16669e4677bbc2ace2` (`Expand Website CMS legal text limits`) was deployed. The service was active and running; public `/health` and `/api/health/database` returned HTTP 200 `Healthy` with `canConnect=true`.

Website CMS long-form pages `terms`, `privacy`, `refunds`, `cancellation`, `seller`, `aiData`, and `status` now preserve `bodyMarkdown` up to 64,000 characters. Oversized legal text is rejected before normalization rather than silently truncated, including Save Draft, Preview, stored-document reading, and Publish; the previously saved draft remains unchanged. Non-legal `bodyMarkdown`, including Download, remains limited to 12,000 characters; SEO and ordinary short-text limits remain 180 and 900 characters. The owner verified Admin CMS load, the live counter, full-text save/reload/preview/publish, and the published public legal page.

No EF migration or schema change ran. No Desktop, Mobile, Windows installer, or Windows Direct release changed. Backend deployment packaged the Admin CMS code but did not itself publish or replace public legal content; the authorized owner separately used Website CMS Publish.

## CMS setup-localization draft import

Historical pre-publication state: backend `0.1.35-backend.135` was active and `.134` was its rollback release. The draft-localization import notes below predate the completed publication and client integrations; they remain retained as history only.

## Historical 2026-07-28 Windows Direct Release 1.2

Windows Direct Release `1.2` was published from source commit `1f957ebd`. The full Desktop release gate, installer packaging, local direct-release validation, and upload-helper dry run passed; the real upload completed to `/var/www/languagevoicetutor/releases/windows/direct`. The live `direct-public` manifest identifies `LanguageVoiceTutorSetup-1.2.exe`, `minimumSupportedVersion: 1.2`, `backendBaseUrl: https://api.languagevoicetutor.com`, `updateMode: manual-confirmation`, `releaseDateUtc: 2026-07-28T15:17:03Z`, SHA-256 `852df1842ed24417f7a94099c0c9f5e96edf274d3305acc87a519d8ca5f84b49`, and size `188895825` bytes. The public website downloads that installer.

Updating an installed 1.1 application to 1.2 succeeded; the updated application launched and lessons worked. The manual-confirmation update flow also worked on other devices: the user chooses **Check for updates**, the app verifies the manifest and SHA-256, and installation proceeds only after confirmation. The app does not silently auto-update.

This was a Windows static-release upload only: no backend deployment or migration occurred. At that time production was `0.1.35-backend.134` with `.133` as rollback; Google Play remains disabled. Paddle, subscriptions, Website CMS publication, lesson scenarios, and backend lesson-context behavior were not changed. Generated `artifacts/` outputs remain uncommitted, code signing remains deferred, and this public Windows Direct release is not a broad production-readiness claim.

## 2026-07-28 production `.134` disabled Google Play entitlement-bridge deployment

For this historical deployment, production was `0.1.35-backend.134`, with `0.1.35-backend.133` retained as rollback. Commit `002807282cc9924cdc9eb631ae69e1343ce200d9` was deployed. Migration `20260727045935_AddGooglePlayPurchaseClaims` was applied separately after a fresh readable PostgreSQL backup (7,253,981 bytes; `pg_restore --list` returned 287 lines). Its reviewed bounded SQL added only `public.google_play_purchase_claims`, its primary key and indexes, and the EF migration-history entry; it did not modify subscriptions, entitlements, payments, billing events, Paddle data, users, or other existing billing tables. The table is owned by and available to `lvt_app`; `lvt_analytics_reader` access was explicitly revoked because this sensitive provider-ownership table is outside the approved analytics surface.

Google Play remains disabled in production. The authenticated verification route returned `503 not_configured` with `subscriptionStatusRefreshRecommended=false`; the claim table remained empty, and no Google Play claim or entitlement was created. An unauthenticated route request returned `401`. Public backend and database health returned HTTP 200 `Healthy` with `canConnect=true`, and `languagevoicetutor-backend.service` is active and running. Manual Admin CMS smoke passed for login, dashboard, user lookup, manually granted Premium display, Paddle Premium provider/period display, Feedback & reports, logout, and repeat login. No rollback was required. No credentials, package/product configuration, acknowledgement, replacement-token handling, RTDN, Mobile purchase flow, Payment projection, or BillingEvent projection was added or enabled; Paddle checkout, webhooks, entitlements, renewal/cancellation behavior, and Desktop Premium display remain unchanged.

## 2026-07-28 bounded lesson-state and context deployment boundaries

The deployed `.134` backend includes `LessonPromptBuilder` bounded full-lesson prompt history. The capacity is `min((effectiveFinalLearnerTurn * 2) + 3, 70)`, with fallback 10 and an absolute backend safety cap of 70. This backend behavior no longer has only the prior ten-message truncation. It changes no public route or JSON contract.

Current learner input remains separate from prior history; normal provider replies remain stateless and persisted lesson messages are not used to hydrate normal reply prompts. The backend deployment required no additional migration, CMS publication, authentication, billing, or dependency change.

The repository’s Desktop client-side changes covered by this bounded backend deployment were subsequently published in Windows Direct Release 1.2. Mobile release and physical-device validation remain separate and pending.

## 2026-07-27 accepted Google Play Billing foundation and subsequent `.134` disabled deployment

Main contains the accepted disabled-by-default Google Play purchase-verification foundation through commit `335ef8a0`: authenticated token verification, SHA-256 purchase-token ownership fingerprinting, a sanitized subscriptions-v2 verifier, lazy ADC-backed wiring, and internal verified ProductId/UTC period/acknowledgement/test-purchase metadata. The initial foundation was dormant; later repository changes connected `GooglePlayPurchaseVerificationService` to atomic verified-purchase persistence, which uses provider-scoped verified-period persistence internally to maintain the exact `google_play` Subscription and linked Premium entitlement. The public endpoint is `POST /api/me/billing/google-play/purchases/verify`; in deployed `.134` infrastructure it remains `503 not_configured` because Google Play is disabled. Migration `20260727045935_AddGooglePlayPurchaseClaims` is applied in production.

The authenticated Google Play verification service passes only trusted verified metadata into atomic ownership/Subscription/entitlement persistence. This disabled infrastructure is now deployed in `.134`, and migration `20260727045935_AddGooglePlayPurchaseClaims` is applied; Google Play remains disabled, so actual purchase processing has not been enabled or tested and the production claim table remains empty. Test purchases remain unsupported, no acknowledgement occurs, and no Payment or BillingEvent projection is created. Repository-only account-status multi-provider selection remains entitlement-driven and protects valid Paddle Premium; Paddle checkout, webhook processing, Premium activation/cancellation/refund behavior, and Desktop Premium-expiry display remain unchanged. Credentials, package/product configuration, controlled sandbox validation, Mobile connection, acknowledgement, replacement-token handling, RTDN, and a production purchase rollout remain pending.

## 2026-07-23 production `.133` account-deletion completion verification

At the time of this historical account-deletion deployment, production was `0.1.35-backend.133`, with `0.1.35-backend.132` as rollback. Commit `80396ce` is deployed; retained `AdminAction` target history no longer blocks execution, while real Admin/CMS dependencies and active paid access still block it. Migration `20260723045852_AddAccountAnonymizationExecution` is applied. A controlled production test created and processed an account-deletion request, completed anonymization, automatically resolved and redacted the request, replaced the original email with a unique `@deleted.invalid` address, prevented original-email lookup and new login, and prevented repeat execution. Refresh tokens are removed; an already-issued access token may remain usable until normal expiry, after which refresh fails and the client must clear the invalid session. This accepted expiry window is not an active backend defect and no further backend authentication change is planned. No Paddle/provider or financial record was modified. Public backend and database health both returned HTTP 200 `Healthy` after deployment. Account-deletion backend work is complete for the approved current scope; the active product focus returns to the Mobile client.

## 2026-07-23 production `.131` Admin confirmation report-ID correction

Production `0.1.35-backend.131` exposed a UI-only report-ID mismatch: the enabled deletion button passed a details response object whose identifier is `reportId`, while the confirmation guard read nonexistent `report.id`. The dialog therefore did not open and no execute request was sent. The correction passes the selected report ID explicitly through confirmation/execution and remains deployed in production `.133`; it changed no migration or backend execution contract.

## 2026-07-23 production `.130` account-deletion workflow correction

Production `0.1.35-backend.130` exposed a bounded workflow issue: support could manually mark an account-deletion request `resolved` while the preflight was blocked and anonymization had not run. The deployed correction rejects that manual transition until the related operation is completed and improves paid-period/Admin-CMS blocker guidance. No migration or Paddle/provider change was required.

## 2026-07-22 production account-deletion and Admin login-security release state

At the time of this 2026-07-22 account-deletion and Admin login-security release record, production was `0.1.35-backend.133`; `0.1.35-backend.132` was the previous rollback release. Account-anonymization Slice 1, both account-anonymization migrations, and the complete Admin preflight, confirmation, execution, and email-intake flow are deployed and production-verified. Email intake creates the same normal `account_deletion` support request with existing duplicate protection; it has no second-Admin approval and does not call Paddle or another provider. Active Premium blocks deletion until the paid period ends, with renewal cancellation communicated by the operator; refunds, disputes, and chargebacks remain manual support matters. Security commit `8dd301b3` (`Harden Admin login credential handling`) remains deployed. `languagevoicetutor-backend.service` is active and running; public `/health` returned HTTP 200 `Healthy`; and public `/api/health/database` returned HTTP 200 `Healthy` with `canConnect=true`. CMS/Admin production smoke passed in a private window: Admin login, dashboard and Feedback & reports loading, account-deletion report opening, logout/repeat login, Sign in/Enter submission, legacy sensitive URL cleanup, and fail-closed native fallback verification.

The `.128` Admin login form explicitly posts to `/admin/` and its email/password inputs have no `name` attributes, so a missing or unparsable `admin.js` cannot serialize or transmit credentials through native form submission. Working JavaScript still prevents native submission and sends the trimmed email plus unchanged password as JSON to `POST /api/auth/login`. At startup, `admin.js` removes legacy `email` and `password` query parameters case-insensitively without reloading, while preserving the Admin path, unrelated query parameters, and hash. Removed values are not copied into fields, storage, logs, errors, or diagnostics. No migration, static website deployment, Website CMS publish, installer upload, billing/Paddle, role, or production configuration change was part of `.128`.

Authenticated `POST /api/me/account-deletion-requests` is deployed. It requires current-password confirmation, accepts an optional reason, derives identity from the authenticated learner, never persists or exposes the password, and permits only one unresolved request per user. Migration `20260721120000_AddActiveAccountDeletionRequestConstraint` is applied and adds only partial unique index `IX_user_feedback_reports_ActiveAccountDeletionRequest_UserId`; it created no table or sequence, so no additional grants or ownership changes were required.

The request uses the existing support queue and Admin email reply workflow. It does not automatically delete, deactivate, anonymize, revoke all token families, cancel subscriptions, or alter user data. Actual deletion/anonymization is the separately executed deployed Super-Admin workflow, and a request resolves only after that operation completes. Mobile Settings integration is implemented and manually verified: it requires the current password, safely rejects an incorrect password without creating a request or logging the learner out, and shows the returned request ID and status. See [Account-deletion requests](ACCOUNT_DELETION_REQUESTS.md).

## Progress V1

Authenticated backend-owned Progress V1 is available at `GET /api/me/progress`. It aggregates only owned finished sessions with non-null `finishedAt`, uses UTC calendar rules, and is separate from the maximum-50 Lesson History response. No schema, migration, index, stored aggregate, cache, job, or backfill was added. See [Progress Endpoints](PROGRESS_ENDPOINTS.md).

## Source of truth for current versions

These docs record the release-ready handoff state, but live systems can change. Always verify the live/public state before telling a tester that a version is current.

Check the public Windows direct release from the live website manifest:

```powershell
Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json
```

If a PowerShell path reads raw manifest text and `ConvertFrom-Json` fails because a UTF-8 BOM is present at the start of `latest.json`, strip the BOM before parsing:

```powershell
($raw -replace "^\uFEFF", "") | ConvertFrom-Json
```

Check the production backend release from the server `current` symlink:

```powershell
ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"
```

Check production backend health and database health:

```powershell
Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
```

Generated local files under `artifacts/` are not proof that a version is live on the public site. A locally built installer becomes public only after the Windows direct release files are uploaded to the website release folder and `latest.json` is verified over HTTPS. Generated release outputs, including `latest.json`, `changelog.json`, `known-issues.json`, `checksums.sha256`, installers, and packages, must not be committed.

## Windows client functionality source of truth

For the current Windows desktop client feature baseline, language counts, lesson flow, settings sections, and mobile-client reuse notes, see [Windows Client Functionality Overview](WINDOWS_CLIENT_FUNCTIONALITY_OVERVIEW.md).

## Concise release-readiness status

- Backend: production is deployed and healthy at `https://api.languagevoicetutor.com` on `0.1.35-backend.145`. The prior `.142` account-wide Premium/purchase-gating foundation and its additive Google Play trial-deferral migration remain deployed; `.144` added safe Restore Credentials diagnostics and `.145` corrected Restore Credentials registration user-verification policy without a further EF migration. Production Backend Data Protection is enabled with its persistent key ring, active certificate, protected backup/restore drill, and post-change health verification complete. Controlled Google Play Internal testing is enabled; public rollout remains pending.
- Website: public pages at `https://languagevoicetutor.com` are generated and Paddle-review polish is completed for the current static site.
- Download: Windows Direct Release 1.6 is available through the manifest-driven `/releases/windows/direct/latest.json` flow; the published manifest SHA-256 and size are verified facts, without a claimed independent second public-download hash. The static/no-JavaScript fallback was not separately verified by this Windows release upload.
- Windows installer: current Windows direct public release is `1.6`, installer `LanguageVoiceTutorSetup-1.6.exe`; its update flow remains manual-confirmation and does not silently auto-update. The installed 1.5 -> 1.6 manual-confirmation update completed successfully.
- AI Models: persistent production storage at `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json` is verified, survived a backend service restart, and contains the known-good `gpt-5.5` / `gpt-5.2` production setup.
- Billing: controlled Paddle live payment/webhook/Premium activation and desktop cancel-renewal validation are completed for the 2026-07-02 owner-led test; full-refund Premium revocation is production-verified; chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred; broad public paid launch remains pending final release-readiness review.
- Legal: pricing, subscription terms, terms, privacy, refunds, cancellation, support, seller/company details, AI/data disclosure, platform availability/status, and download pages are ready for owner/legal final review as product/legal drafts, not final legal advice.

Remaining follow-ups after Windows Direct Release 1.6 publication:

1. Code signing remains deferred and accepted as a known release risk / SmartScreen warning source for this release.
2. Post-release monitoring and customer feedback triage remain ongoing.
3. Backup/restore/rollback currency checks remain ongoing operational work.
4. Final owner/legal/support/pricing review remains a follow-up for broader marketing and paid-launch expansion.
5. Logging/release-readiness checks for remaining Admin operations and paid-launch evidence remain follow-up work.
6. Admin auth audit first production slice is complete for `admin_login_success`, `admin_logout`, `admin_login_failed`, and `disabled_admin_login_denied`; session expiration audit persistence remains pending until separately implemented/verified.
7. Controlled Paddle live payment/webhook/Premium activation, failed-payment non-activation, desktop cancel-renewal validation, and full-refund Premium revocation are completed; chargeback remains implemented/test-covered but not live-chargeback-tested; partial refunds remain conservative/manual-review; expanded customer portal/subscription management is deferred and not a 1.0 blocker; broad paid launch remains pending final release-readiness review.
8. Microsoft Store/MSIX was evaluated and discontinued for now; Microsoft Store availability is not claimed.

Do not state that the product is fully public production-ready. The current Windows release remains a public Windows direct release, not a full broad production-readiness claim, and not broad public production readiness.

## Backend deployment state and boundaries

Production backend URL: `https://api.languagevoicetutor.com`.

Health endpoints:

- `https://api.languagevoicetutor.com/health`
- `https://api.languagevoicetutor.com/api/health/database`

The current backend release is `0.1.35-backend.145`. The deployed account-deletion flow includes migrations `20260722132656_AddAccountAnonymizationPreflightFoundation` and `20260723045852_AddAccountAnonymizationExecution`; Google Play foundation migrations and `20260831080122_AddRestoreCredentialsFoundation` are applied. Public backend and database health returned HTTP 200 `Healthy`; `.142` applied the additive Google Play trial-deferral foundation migration, while `.144` and `.145` required no further migration. Backend Data Protection is enabled in production with its persistent key ring and active certificate outside release directories; its protected backup and isolated restore drill were verified. The historical `.140` Website CMS contract plus `.138` trial/manual Premium expiry correction remain production-verified. Previous backend rollback reference must always be verified from `/opt/languagevoicetutor/backend/previous` before rollback.

Backend deployment uses:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 -Version 0.1.35-backend.123 -PackageFirst -DryRun
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 -Version 0.1.35-backend.123 -PackageFirst
```

The backend upload flow uses the uploaded `deploy-backend-release.sh` helper and `ssh -tt` for sudo restart/status when needed. Do not document old fragile inline bash deployment paths as the current flow.

Backend deploy is separate from Windows installer upload, static website publish, Website CMS publish, database migrations, provider/Paddle live changes, and AI Models data/config correction. Backend upload/package scripts do not apply EF migrations automatically. Database migrations remain a separate reviewed SQL/operator process only when schema changes exist. Backend deploy does not upload Windows installer files, does not publish public website HTML, does not change production billing/Paddle configuration, and must not treat release-folder AI Models JSON as the production source of truth.

Admin Product Statistics still uses the `Tracked signed-in app/device records` label for backend `DeviceEntity` records; this metric is not raw installer downloads. `Successful payments total` and `Successful payments current month` remain internal billing-event metrics and are not the source of Premium access.

Phase 3 rate limiting / abuse protection is completed and production-verified with `RateLimiting__Enabled=true`. Production Admin RBAC / persistent role management is completed for backend `0.1.35-backend.108`: persistent AdminUsers can sign in to `/admin`, admin source is reported as `persistent_role_assignment`, role-aware Admin UI works, `super_admin` can assign/revoke roles and disable AdminUsers, disabled AdminUsers lose Admin access, support and billing_support least-privilege checks passed, `403` from role-limited workflows no longer logs the admin out, and `401` still returns to login. Bootstrap Admin fallback for Admin permission policies remains disabled with `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`. The Website CMS endpoints are still authenticated/authorized but no longer consume the normal admin read/write rate limit because long legal text editing caused `RateLimitExceeded` during normal CMS work.

Phase 4 is complete for the current release-readiness level: Phase 4A backup/readability/separate-drill-restore completed, Phase 4B local PostgreSQL backup scheduling is active, Phase 4C migration rollback/remediation dry-run rehearsal completed, and Phase 4D permission-fidelity restore drill completed. Off-server encrypted backups remain optional future infrastructure hardening.


## 2026-07-19 authenticated Lesson History production state

Authenticated `GET /api/me/lesson-history` and `GET /api/me/lesson-history/{sessionId:guid}` are implemented, verified, deployed, and available in backend `0.1.35-backend.123`. Both require authentication and derive ownership exclusively from the authenticated request identity. The list returns only the learner's recent sessions, newest first, with a current maximum of 50 and summary, message-count, and valid-turn indicators. Detail returns owned session metadata plus available summary, transcript messages, and feedback; an unknown or non-owned session safely returns `404`, while unauthenticated requests return `401`. The canonical contract is [Lesson History Endpoints](LESSON_HISTORY_ENDPOINTS.md).

This completes the backend API, not Mobile History UI. Mobile must consume `/api/me/...`, never Desktop-local JSON or `/api/dev/...`. No Desktop behavior changed. The recent maximum-50 list is not an all-time Progress API: clients must not derive official totals, streaks, aggregates, or long-term progress from it. Future official Progress requires a separate backend-owned aggregate contract. No database migration or schema change was required.

## 2026-07-18 Website CMS inline Home-title typography production verification

Backend `0.1.35-backend.122` is production-deployed and healthy for the completed Website CMS inline Home-title typography feature and the Home-title font-size CSS precedence fix. The previous backend release is `0.1.35-backend.121`; `languagevoicetutor-backend.service` is active and running; public `/health` returned HTTP 200; public `/api/health/database` returned HTTP 200; no EF migrations were run; and no separate static-site upload was required for this backend release. Public page changes from title edits are applied later through Website CMS Publish.

Website CMS now edits Home page application-card title typography inline, directly below `windowsCardTitle` and `mobileCardTitle`. Windows and Mobile title styles are independent. Each title supports controlled font family, mobile size in pixels, desktop size in pixels, font weight, and line height through companion fields: `windowsCardTitleFontFamily`, `windowsCardTitleMobileSizePx`, `windowsCardTitleDesktopSizePx`, `windowsCardTitleFontWeight`, `windowsCardTitleLineHeight`, `mobileCardTitleFontFamily`, `mobileCardTitleMobileSizePx`, `mobileCardTitleDesktopSizePx`, `mobileCardTitleFontWeight`, and `mobileCardTitleLineHeight`. Safe defaults are heading-font inheritance, `28px` mobile size, `52px` desktop size, `800` weight, and `1.08` line height, so existing Website CMS JSON remains compatible and no database migration is required. Existing text, Design values, Marketing/SEO, legal pages, pricing, support, download content, and mobile-page content remain preserved.

The backend renderer generates responsive title size as `font-size: clamp(<mobileSize>px, 4vw, <desktopSize>px);`. CMS users do not edit raw CSS, `clamp()`, `vw`, selectors, or style attributes. The supported workflow is Home page title text plus inline **Text style** controls -> **Save draft** -> **Preview** -> **Publish / Make active**. There is no separate Typography tab, Typography page, global typography editor, raw CSS editor, or second Website CMS configuration system. Preview and Publish use the same backend renderer.

The first deployed implementation exposed a Preview-only symptom because existing public stylesheet selectors `.landing-page .app-panel h1` and `.landing-page .app-panel h2` had greater specificity than the generated class-only selectors, so font weight changed while font size stayed overridden. The final renderer-owned selectors are `.landing-page .app-panel h1.app-panel__title--windows` and `.landing-page .app-panel h2.app-panel__title--mobile`, which have sufficient normal CSS specificity. The fix did not introduce `!important`, inline styles, or JavaScript style assignment.

## 2026-07-17 Admin Feedback & reports production workflow

Backend `0.1.35-backend.119` is production-deployed and healthy for the complete Admin Feedback & reports workflow, with previous backend release `0.1.35-backend.118`. Repository commit `d4ddd33` (`Add admin feedback reports workflow`) was deployed through the standard `scripts/upload-backend-linux-release.ps1` flow with `PackageFirst`; a dry run completed before the real upload. The live backend `current` symlink was switched to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.119`, the `previous` symlink retained `.118`, `languagevoicetutor-backend.service` restarted successfully and was active/running, public `/health` returned HTTP 200, public `/api/health/database` returned HTTP 200, the Admin CMS loaded the deployed workflow, and no rollback was required. The deployment script did not run EF migrations.

The backend records authenticated user feedback through the existing `POST /api/me/feedback-reports` Mobile submission path. `UserId` remains backend-derived from authentication, supported categories remain `suggestion`, `app_issue`, and `ai_response`, and backend `.119` required no Mobile API contract change.

The Admin Feedback & reports API is deployed at `GET /api/admin/feedback-reports`, `GET /api/admin/feedback-reports/{reportId}`, `PATCH /api/admin/feedback-reports/{reportId}/status`, and `POST /api/admin/feedback-reports/{reportId}/replies`. The required permissions are `feedback_reports.read`, `feedback_reports.status.manage`, and `feedback_reports.reply`. Approved production roles are `super_admin` and `support`; these permissions are not granted to `content_editor`, `billing_support`, or `read_only_auditor`. Support does not gain unrelated Website CMS, legal content, billing/Premium, AI model settings, role management, secrets, or unrelated system-administration access.

Status workflow: `new` can be marked `reviewed` or `resolved`; `reviewed` can be resolved; `resolved` can be reopened as `reviewed`; manual reset to `new` is not supported; same-status updates are idempotent; and `ReviewedAtUtc` records the first review/resolution. A successful reply changes only `new` to `reviewed`, does not automatically resolve the report, and final resolution remains a deliberate Admin action.

Reply workflow: the recipient is resolved from the report user and cannot be changed by the Admin. The From address and the subject cannot be changed by the Admin; the subject is exactly `Language Voice Tutor support`. Reply attempts are persisted before delivery and use `pending`, `sent`, and `failed` states. Failed delivery does not change report status. Successful delivery changes only `new` to `reviewed`. Reply history is visible in report details, newest first, and failed attempts remain visible. No automatic retry, outbox, attachments, ticketing, reply editing/deletion, exports, bulk operations, or OpenAI processing was added.

Migration `20260717120148_AddUserFeedbackReports` previously added `user_feedback_reports`. Migration `20260717151432_AddUserFeedbackReportReplies` is now applied in production and added `user_feedback_report_replies`. The reply table exists, is owned by `lvt_app`, `lvt_app` has application access, and `lvt_analytics_reader` has no access to reply content. The migration was applied separately from backend deployment; no additional migration is required for backend `.119`. When reviewed SQL creates application objects under an operator role, ownership and grants must be verified before the migration is considered complete.

Production smoke validation confirmed an initial reply attempt failed safely because `SmtpEmail__Enabled` was absent: the reply text remained in the CMS, the failed attempt was stored in reply history, and the UI displayed a safe “Email delivery is not configured” message without exposing SMTP/provider details. Operators then added `SmtpEmail__Enabled=true` to the existing `/etc/languagevoicetutor/backend.env` without documenting SMTP host, username, password, From address, recipient address, or other secret values; after backend restart, both health endpoints remained HTTP 200. A second reply attempt was successfully delivered with the expected support subject and sender identity, the successful reply appeared in reply history, report status updated correctly, the report was subsequently resolved successfully, and reply history remained available.

The generic email sender selects the real SMTP transport only when all of these are true: `SmtpEmail__Enabled=true`, `Host` is configured, `Port` is greater than zero, and `FromAddress` is configured. Otherwise `NoOpEmailSender` is selected, `IsConfigured` is false, and no SMTP connection is attempted. Password reset and support replies use the same generic `IEmailSender` transport; only `SmtpEmailSender` contains SMTP transport logic. `PasswordResetEmailSender` remains a thin password-reset message formatter, and password-reset subject/body and external behavior remain unchanged. Safe failure logging uses fixed error categories and does not write raw provider exceptions, user IDs, token IDs, recipient emails, reset URLs, reset codes, token hashes, or SMTP details to the reviewed failure logs.

Admin CMS Feedback & reports is packaged inside the backend release, so no public static-site upload is required for this CMS change. List, filters, pagination, details, status controls, reply form, and reply history are deployed. Report and reply text are rendered as plain text. Reply drafts remain in memory only; failed sends preserve the current draft; successful sends clear it; switching reports clears the previous draft and history; and no reply data is stored in localStorage, sessionStorage, cookies, URLs, or console logs.

Final verification recorded for this workflow: backend build passed; focused Admin read tests passed; focused Admin CMS tests passed; focused email sender and safe logging tests passed; password-reset logging tests passed; Admin RBAC permission policy checks passed; Admin roles/permissions policy checks passed; the full desktop release gate with `IncludeEfChecks` passed; EF reported no pending model changes; the repository was pushed with a clean working tree before deployment; and production health, database health, email delivery, status changes, reply history, and report resolution were manually validated.

## 2026-07-13 backend 0.1.35-backend.116 production verification

Backend `0.1.35-backend.116` is production-deployed and healthy, with previous rollback release `0.1.35-backend.115`. The deployment used `scripts/upload-backend-linux-release.ps1 -Version 0.1.35-backend.116 -PackageFirst` after a completed dry run. Public `/health` returned HTTP 200 `Healthy`, public `/api/health/database` returned HTTP 200 `Healthy` with `canConnect=true`, and `languagevoicetutor-backend.service` is active and running on `127.0.0.1:5001` behind the existing production setup. No EF migration was added or executed, no database schema changed, and no Desktop UI, Mobile, CMS UI, public website, Windows installer, billing, Paddle, voice, transcription, TTS, semantic-resolution, or AI Models configuration files were deployed or changed by this release.

The backend prerequisite for moving learner level selection into Mobile Settings -> Learning is complete: existing `GET /api/me/settings` returns `CurrentLevel`, and existing `PUT /api/me/settings` accepts optional `CurrentLevel` values `A1`, `A2`, `B1`, and `B2` with canonical uppercase storage/return behavior, new-profile default `A1`, preserve-on-omitted-or-null semantics, validation rejection for blank/unsupported PUT values, repair of legacy blank/whitespace and `unknown` to `A1`, and safe `A1` responses for arbitrary unsupported stored values such as `C1` without overwriting them. `UserProfileEntity.CurrentLevel` remains the storage location; no new endpoint, table, or migration was introduced.

The saved `CurrentLevel` is only the user's selected level and does not replace or duplicate CMS level behavior. Published CMS level profiles remain the source of truth for level-dependent lesson behavior and lesson length through the unchanged chain: saved `CurrentLevel` -> Mobile selects the matching runtime level profile -> backend runtime scenario supplies CMS-published `levelProfiles` -> the active level profile supplies language complexity, correction guidance, answer length, hint behavior, wrap-up timing, and final-turn timing -> lesson requests carry the selected level and resolved profile values -> `LessonLimitHelper` and `LessonPromptBuilder` apply those values. Mobile has not yet consumed `CurrentLevel`, the Choose Level start screen has not yet been removed, and physical Mobile validation of this new settings behavior has not yet happened.

## 2026-07-13 authenticated voice scenario resolution production verification

Backend `0.1.35-backend.115` was deployed and verified for this dated voice scenario structured-output fix; `0.1.35-backend.114` was the previous backend release for that deployment. This is historical context; production has since advanced to `0.1.35-backend.142`. Backend `0.1.35-backend.114` was already active before the `.115` deployment, so it must not be described as containing the `.115` structured-output fix. The `.115` deployment completed successfully through the existing `scripts/upload-backend-linux-release.ps1` flow, `languagevoicetutor-backend.service` was active, public `/health` returned HTTP 200, and public `/api/health/database` returned HTTP 200 with `canConnect=true`. No EF migration or database schema change was required or run. Website and Windows installer files were not deployed.

Backend `0.1.35-backend.115` fixes `POST /api/me/lesson-sessions/{sessionId}/voice-scenario-resolution` returning HTTP 502 when the provider returned a structured-output shape that was permitted by the old provider schema but rejected by backend validation. The provider schema now has one explicit result shape for each decision: `published_context`, `free_context`, `clarify`, and `unsafe`. The backend converts the nested provider result back into the existing flat public endpoint response, so the public route and Mobile request/response contract did not change. `free_context` remains a first-class result, runtime candidate IDs are still validated against the current CMS candidates for the lesson, production credential validation remains unchanged, and the automated tests did not use a live OpenAI call. No scenario titles, transcript phrases, CMS scenario IDs, or language-specific production examples were added.

Verification recorded for this backend work: `OpenAiVoiceScenarioResolutionServiceTests` passed (`61 passed, 0 failed, 0 skipped`), the full backend test suite passed (`162 passed, 0 failed, 0 skipped`), the Release backend build succeeded with `0 warnings` and `0 errors`, production deployment completed through the existing upload script, production health/database-health checks passed, and no EF migrations were run. A physical Android retest is still required to confirm that the first clean voice scenario selection no longer returns HTTP 502. Initial mixed-script transcription rejection remains a separate Mobile issue; keyboard overflow and lesson-screen UI work remain separate Mobile issues; missing lesson-chat avatar assets remain a separate Mobile issue; and the complete Mobile voice flow must not be marked fully stabilized yet.

## 2026-07-11 authenticated lesson summary production verification

Backend `0.1.35-backend.112` was the current production backend for this dated verification; `0.1.35-backend.111` was the rollback release. It was packaged and uploaded with the normal Linux backend release scripts. Production verification confirmed the `current` symlink points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.112`, `languagevoicetutor-backend.service` is active, `/health` returns `200 Healthy`, and `/api/health/database` returns `200 Healthy` with `canConnect=true`. No EF migrations were run, no database schema changed, and Windows installer/release files stayed unchanged.

Backend `0.1.35-backend.111` already supported authenticated `PUT /api/me/lesson-sessions/{sessionId}/finish` and `GET /api/me/lesson-sessions/{sessionId}/summary`, but summary generation could remain unavailable because `LessonSummaryGenerationService` read only the top-level Responses API `output_text` field. Real provider responses may place the structured summary text under `output[].content[].text`; when top-level `output_text` was absent, an empty string reached JSON deserialization, causing a safely isolated `JsonException` that did not undo successful lesson completion.

Backend `0.1.35-backend.112` keeps top-level `output_text` support, adds fallback extraction from nonblank `output[].content[].text`, rejects blank provider output before JSON deserialization, and continues to isolate summary-generation failure from lesson completion. No local/client summary generation was introduced. Authenticated Finish triggers backend-owned generation; authenticated GET reads only the stored learner-safe result and does not regenerate a missing summary. Development `/api/dev/.../summary` routes remain diagnostic/development boundaries and are not the mobile production flow.

A real authenticated Flutter mobile lesson was verified in production on 2026-07-11: the session started, lesson messages persisted, `PUT /api/me/lesson-sessions/{sessionId}/finish` completed the lesson, and `GET /api/me/lesson-sessions/{sessionId}/summary` returned a ready backend-owned learner-safe summary displayed by mobile, including summary, strengths, improvements, vocabulary, grammar, and next steps. Authenticated desktop Finish uses the shared completion path, but desktop currently displays its existing local desktop summary flow; the `.112` extraction fix did not change desktop UI or Finish response contracts. Mobile is the first verified client displaying the authenticated backend-owned GET summary result.

## Production Admin RBAC / persistent roles

Production Admin RBAC / persistent role management is completed after backend release `0.1.35-backend.108`, deployed by the normal backend package/upload flow. The production backend `current` symlink was verified at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.108`; `/health` and `/api/health/database` returned `200 Healthy`. No EF migrations were added or run for this RBAC stage. Windows installer release files were not changed.

Manual production verification completed: persistent AdminUsers can sign in to `/admin`; admin source is `persistent_role_assignment`; the role-aware Admin UI works; role-limited workflows return `403` without logging the admin out; `401` still returns to login; `super_admin` can assign and revoke roles and disable AdminUsers; disabled AdminUsers lose Admin access; `support` can use allowed support workflows; `billing_support` can use Manual Premium Grant after selecting a user and providing a reason; `billing_support` cannot access `super_admin`-only areas; `support` cannot grant or revoke Premium; and role visibility/workflow availability matches the backend permission catalog.

Current final role policy:

- `support`: can sign in, use User Lookup / User Overview, read approved diagnostics and allowed audit entries, and reset free lesson allowance; cannot grant/revoke Premium, cancel paid renewal, manage roles, edit/publish CMS, edit Website, or manage System AI Models.
- `billing_support`: can sign in, use User Lookup / User Overview, read billing/subscription/Premium diagnostics, cancel paid renewal if the existing backend policy allows it, and Manual Premium Grant for verified payment recovery cases; cannot Premium Revoke unless explicitly granted later, manage roles, edit/publish CMS, edit Website, or manage System AI Models.
- `content_editor`: can use CMS content read/draft workflows according to current permissions; cannot publish/restore unless explicitly granted and cannot manage billing, Premium, Admin roles, or System AI Models.
- `read_only_auditor`: can use read-only diagnostics/audit/statistics according to current permissions; cannot mutate user, billing, Premium, CMS, Website, roles, or System AI Models.
- `super_admin`: has full Admin access, including role management, disabling AdminUsers, Premium support actions, CMS/Website/System controls according to existing backend permissions.

Admin Activity first production slice is completed: the Admin Activity tab is visible and usable, includes `admin_actions`, `admin_role_assignment_events`, and the production-applied `admin_auth_audit_events` source, displays admin-entered reasons/notes where stored, and keeps `safeMetadataJson` separate from Admin note. Production verification has shown `admin_login_success`, `admin_logout`, `admin_login_failed`, and `disabled_admin_login_denied` rows in Admin Activity from `admin_auth_audit_events`; session expiration audit persistence and any Website/AI publish audit coverage not already represented in existing audit tables remain pending until separately implemented/verified.

## Public website and Website CMS

Public site: `https://languagevoicetutor.com`.

The public website uses the ORRALEN operating/company/master brand and ORRALEN TECHNOLOGIES LTD legal identity. Language Voice Tutor remains the product/application name used by the existing Desktop and Mobile clients and by the homepage product cards. The canonical public logo path remains `site/public/assets/brand/lvt-logo.png`; its transparent canvas padding was removed without altering the artwork. Explicit newlines entered in the CMS footer copyright field are preserved in rendered public footer paragraphs. Desktop homepage sizing keeps the 88px header and compact logo allocation while allowing the normal page to fit desktop viewports without unnecessary horizontal or vertical overflow.

The approved CMS palette is Header background `#F2E8D5`, Header text `#17324D`, Footer background `#1B2A3A`, Footer text `#EDE7DC`, with static supporting colors Language names `#102A43`, separators `#8A7557`, and footer links `#FFFFFF`. Live inspection on 2026-08-22 confirmed the logo, ORRALEN TECHNOLOGIES LTD footer identity, registered-office line, approved header colors, and approved footer background. It also found that the public CMS output currently emits Footer text `#FFFFFF`, not the approved `#EDE7DC`; correcting that value requires a separate authorized Website CMS edit and Publish and was not performed during this documentation sync. See [Website Paddle review readiness](WEBSITE_PADDLE_REVIEW_READINESS.md) for the detailed Website CMS/design boundary.

The Website CMS exists in the Admin Shell under **Website**. It is intentionally simple and informational only; it is not a full CMS. Access is Super Admin / Bootstrap Admin protected. Content is JSON/file-based at `site/content/website-content.json`, and that JSON document contains both active and draft content. Public static site output is `site/public`.

Website flow:

1. Admin Website tab loads draft/active content.
2. **Save draft** writes draft content.
3. **Preview** renders the selected page preview without publishing.
4. **Publish / Make active** promotes draft/active content and renders static HTML files.

Publish generates these public pages: `index.html`, `download.html`, `mobile.html`, `pricing.html`, `support.html`, `terms.html`, `privacy.html`, `refunds.html`, `cancellation.html`, `seller.html`, `ai-data.html`, and `status.html`.

The Website CMS editor is simplified for normal pages: Page title, Body markdown, SEO title, and SEO description. Home remains structured because it has landing cards/assets. Design is not treated as a normal Super Admin editing page.

Markdown rendering supports headings, bold, italic, bullet lists, numbered lists, markdown links, plain safe URLs, plain emails, and bare domains such as `Paddle.com`. Unsafe schemes such as `javascript:`, `data:`, and `vbscript:` must remain rejected or escaped.

## Website CMS Marketing / SEO and public crawler readiness

The Website CMS now includes a visible **Marketing / SEO** section. These settings are stored through the existing JSON/file-based Website CMS model; no database table, schema change, migration, backend secret, environment variable, or committed example JSON value is required for Google setup. Google Analytics, Google Ads, and Search Console values are optional public website configuration and must be entered only in Admin Website CMS when real owner-approved values exist. Do not put real Google IDs, conversion labels, Search Console tokens, script snippets, GTM container IDs, secrets, or placeholder example values into code, docs, env files, or committed JSON examples.

Marketing / SEO fields:

- Enable consent banner
- Enable analytics
- Google Analytics Measurement ID
- Enable ads tracking
- Google Ads ID
- Google Ads download conversion label
- Google Search Console verification token
- Enable llms.txt

Current safe CMS values before real Google setup:

- Enable consent banner: ON
- Enable llms.txt: ON
- Enable analytics: OFF until a real GA4 Measurement ID is available
- Google Analytics Measurement ID: empty until available
- Enable ads tracking: OFF until real Google Ads values are available
- Google Ads ID: empty until available
- Google Ads download conversion label: empty until available
- Google Search Console verification token: empty until Search Console property verification is started

Operator field guide:

- Google Analytics Measurement ID: Google Analytics → Admin → Data streams → Web stream for `languagevoicetutor.com` → Measurement ID. Expected format: `G-XXXXXXXXXX`. Do not paste the example placeholder into CMS.
- Google Ads ID: Google Ads → Goals / Conversions → selected website conversion action → Tag setup. Expected format: `AW-123456789`. Do not paste the example placeholder into CMS.
- Google Ads download conversion label: same Google Ads conversion action setup; the label is specific to the download conversion action.
- Google Search Console verification token: Search Console → add property for `https://languagevoicetutor.com/` → HTML tag verification. Copy only the value inside `content="..."`, not the full meta tag.
- Do not paste whole Google script snippets into any of these fields.
- Do not use GTM container IDs in the GA Measurement ID field unless the website code explicitly supports GTM later.

Website Publish now emits or maintains public HTML pages, `robots.txt`, `sitemap.xml`, `llms.txt` when enabled, and `marketing-consent.js`. Public generated pages include canonical URLs, meta descriptions, Open Graph tags, Twitter card tags, JSON-LD where appropriate, and SoftwareApplication JSON-LD for the Windows desktop app only. Public pages must not claim Android/iOS availability and must not claim Microsoft Store, Google Play, or App Store availability.

Consent and privacy readiness:

- The consent banner is controlled from Website CMS.
- Consent mode defaults to denied before user choice: `analytics_storage`, `ad_storage`, `ad_user_data`, and `ad_personalization` are denied.
- The banner supports Accept all, Reject non-essential, Manage choices, and a Privacy Policy link.
- Privacy Policy includes optional analytics, advertising, and cookie consent disclosure.
- The website remains usable when non-essential cookies are rejected.
- Google Analytics / Google Ads scripts must not be emitted when IDs are empty or tracking is disabled.

Final verification caveats:

- Public pages must not contain placeholder GA IDs such as `G-XXXXXXXXXX`.
- Public pages must not contain placeholder Ads IDs such as `AW-123456789`.
- Public pages must not include `googletagmanager.com/gtag/js` while IDs are empty.
- `download.html` should show current Windows installer details from `latest.json` when static release details are available.
- `robots.txt`, `sitemap.xml`, `llms.txt`, and `marketing-consent.js` should return `200`.

## Current public website readiness

The home page shows the logo, supported study language flags, a Windows desktop app card, and safe mobile wording. Home must not claim mobile apps are currently available and must not say “Mobile version coming soon”. The approved wording is: “Android and iOS apps are planned but are not currently available.”

The generated footer is shared across pages and has two rows:

- Primary: Privacy Policy, Terms of Use, Refund Policy, Cancellation, Support, Pricing.
- Secondary: Seller / Company Details, AI & Data Disclosure, Service Status.

`seller.html`, `ai-data.html`, and `status.html` are part of the public site and are linked from the footer.

The download page is manifest-driven and also useful without JavaScript. When the local/public manifest is available, it statically shows current release details instead of only showing Loading or Unavailable. It keeps `download.js` and `/releases/windows/direct/latest.json` support. The static non-JS fallback text is:

- “Current Windows direct release is available through the Download for Windows button.”
- “If release details do not load automatically, please contact [support@languagevoicetutor.com](mailto:support@languagevoicetutor.com).”

The Download page is a structured Website CMS release page for the Desktop app, not just a generic markdown page. The CMS controls the visible CTA page title, main CTA body markdown, SEO title, SEO description, and four structured feature cards. The feature-card keys are `featureCard1Label`, `featureCard1Title`, `featureCard1Description`, `featureCard1ImagePath`, `featureCard2Label`, `featureCard2Title`, `featureCard2Description`, `featureCard2ImagePath`, `featureCard3Label`, `featureCard3Title`, `featureCard3Description`, `featureCard3ImagePath`, `featureCard4Label`, `featureCard4Title`, `featureCard4Description`, and `featureCard4ImagePath`. Default screenshot paths are `/assets/images/download/quick-start.webp`, `/assets/images/download/topics.webp`, `/assets/images/download/guided-lesson.webp`, and `/assets/images/download/conversation.webp`; these are public website assets, not Windows release artifacts.

Current public Download page layout: the existing Windows desktop app release hero remains. The left CTA card shows eyebrow `WINDOWS DESKTOP APP`, the CMS page title as the main heading, CMS body intro text, current version and installer size, the **Download for Windows** button, manifest status line, and SmartScreen/support notes. The right side shows four CMS-driven feature cards with screenshot images and accepted click-to-enlarge lightbox behavior. The footer follows the hero directly. There is no visible Technical release details block and no separate below-hero support card. `bodyMarkdown` is split visually: intro paragraphs render before version/button, SmartScreen/support-like notes render after manifest status, and obsolete “Current version details are loaded from the release manifest” text must not be shown as a public user-facing block.

`download.js` reads `/releases/windows/direct/latest.json`; version and installer size are manifest-driven. Historical verification confirmed the normal manifest-driven public download of Windows 1.2. The static/no-JavaScript fallback should point to the intended current installer, but its tracked HTML value was not changed or separately verified by that Windows release upload. The Download button must keep working if JavaScript or manifest loading fails by using the safe public installer fallback.

Accepted visual state: the Download page background is lightened to be closer to the Home page tone, cards use a readable blue-tinted translucent panel treatment, the CTA layout order is accepted, and feature-card lightbox behavior is accepted. Future visual changes should be small and scoped to Download page CSS unless explicitly requested.

## Historical Windows Direct Release 1.0 publication record

Windows Direct Release 1.0 was published on the public direct channel before the later `1.1`, `1.2`, `1.3`, `1.4`, `1.5`, and current `1.6` releases. The release was built locally with Inno Setup, validated, uploaded to `/var/www/languagevoicetutor/releases/windows/direct`, verified on the server, verified over public HTTPS, verified on the website download page, and manually checked by downloading the installer from the public Download button.

Public release manifest values verified over HTTPS:

- `version`: `1.0`
- `installerFileName`: `LanguageVoiceTutorSetup-1.0.exe`
- `channel`: `direct-public`
- `backendBaseUrl`: `https://api.languagevoicetutor.com`
- `minimumSupportedVersion`: `1.0`
- `updateMode`: `manual-confirmation`
- `sha256`: `d6be93fbcd75536a0cd149bd8872c8327fc3131ede247b1db2b2d33d673680e1`
- `installerSizeBytes`: `188751650`

Publication verification completed:

- Local installer created: `artifacts\installers\windows\LanguageVoiceTutorSetup-1.0.exe`.
- Server-ready release copy created: `artifacts\releases\windows\direct\LanguageVoiceTutorSetup-1.0.exe`.
- Direct release manifest created: `artifacts\releases\windows\direct\latest.json`.
- `scripts\validate-windows-direct-release.ps1` passed before upload.
- `latest.json`, `changelog.json`, and `known-issues.json` parsed as JSON.
- `latest.json`, `changelog.json`, `known-issues.json`, and `checksums.sha256` had no UTF-8 BOM.
- Manifest identity matched product `Language Voice Tutor`, app id `LanguageVoiceTutor.Desktop`, platform `windows`, architecture `win-x64`, backend `https://api.languagevoicetutor.com`, channel `direct-public`, and update mode `manual-confirmation`.
- Installer SHA-256 matched both `latest.json` and `checksums.sha256`.
- `changelog.json` and `known-issues.json` both referenced version `1.0`.
- Dry-run upload uploaded nothing; the real upload completed successfully for `latest.json`, `changelog.json`, `known-issues.json`, `checksums.sha256`, and `LanguageVoiceTutorSetup-1.0.exe`.
- Server command confirmed remote `latest.json` content.
- Public HTTPS `latest.json` returned version `1.0`, installer `LanguageVoiceTutorSetup-1.0.exe`, backend `https://api.languagevoicetutor.com`, minimum supported version `1.0`, and update mode `manual-confirmation`.
- Public download page showed Current version `1.0`, release details for channel `direct-public`, size `180.0 MB`, and SHA-256 `d6be93fbcd75536a0cd149bd8872c8327fc3131ede247b1db2b2d33d673680e1`.
- Manual website check confirmed the Download button downloads the `1.0` installer.

Historical scope boundary: the public release upload affected only Windows direct release files. It did not deploy backend code, run migrations, modify database state, change billing/Paddle/refund logic, upload website files, rebuild the installer, change secrets, or change installer binaries. That historical Windows release upload did not change the backend; the backend has since advanced and the current production backend is `0.1.35-backend.142`.

Code signing remains deferred and accepted as a known release risk; Windows SmartScreen warnings remain expected until a future signed installer is published. Historical Windows Direct 1.1, 1.2, 1.3, 1.4, and 1.5 followed the 1.0 record; 1.6 is the current public release.

## Windows direct release

Manifest: `https://languagevoicetutor.com/releases/windows/direct/latest.json`.

Historical public direct release values for Windows Direct 1.1:

- `channel`: `direct-public`
- `version`: `1.1`
- `installerFileName`: `LanguageVoiceTutorSetup-1.1.exe`
- `installerRelativeUrl`: `LanguageVoiceTutorSetup-1.1.exe`
- `backendBaseUrl`: `https://api.languagevoicetutor.com`
- `updateMode`: `manual-confirmation`
- `minimumSupportedVersion`: `1.1`

For this historical record, the `1.1` Windows direct release was built, uploaded, verified, and confirmed installed; the desktop displayed version `1.1`. Backend deployment was not part of the desktop `1.1` release or the later static website upload; that desktop release did not change backend deployment. Production has since advanced to backend `0.1.35-backend.142`. No database migrations were added or run for either the `.112` summary extraction fix or the `.115` voice scenario structured-output validation fix. `minimumSupportedVersion` was intentionally `1.1` because `1.1` contains the desktop auth/session stability fix described below.


### Desktop auth/session fix in Windows Direct Release 1.1

Windows Direct Release `1.1` includes the desktop auth/session refresh bypass fix. Authenticated desktop clients that previously attached stale bearer tokens directly were converted to the central refresh-aware flow. The fixed clients include `BackendSubscriptionStatusClient`, `BackendCheckoutSessionClient`, `BackendCancelSubscriptionClient`, `BackendTrialClaimClient`, the authenticated `/me/settings` flow in `BackendUserSettingsClient`, and `BackendLessonAccessDecisionClient`. Expected behavior is that an expired access token with a valid refresh token refreshes, retries, and persists the replacement session instead of logging the user out. Update/reinstall should preserve the auth session, user settings, Lesson History, and Progress.

Release-relevant desktop polish included in historical `1.0`:

- Settings now includes a Contacts tab with `support@languagevoicetutor.com` and `https://languagevoicetutor.com`.
- Contacts is localized for all release-ready UI languages: `en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, and `bg`.
- Contacts uses the selected interface language, not the study language. The stale WPF binding state after interface-language changes was fixed by notifying Contacts bindings during interface-language refresh, so Russian Contacts text appears only for Russian UI and non-Russian UIs no longer show Russian Contacts text.
- Contact links are restricted to safe `https` and `mailto` handling.
- Situation/subtopic selection allows long localized topic names to wrap instead of clipping, and scenario card title/description wrapping remains protected by policy tests.
- Back during an unfinished active lesson now uses the same confirmation guard as Finish/End lesson: Cancel keeps the user in the lesson, Confirm continues the existing exit/end/navigation flow, and the guard does not apply before a lesson starts or after a lesson is already finalized.

Recent relevant implementation commits for this handoff state: `52b5c1a` (Polish desktop release localization and lesson guard), `c704ec3` (Fix contacts localization coverage), and `d2a1202` (Fix Contacts localization refresh).

Final local validation before/around this release included clean `git status`, `git diff --check`, `dotnet restore`, Debug and Release `dotnet build`, `python .\tools\test_desktop_release_polish_policy.py`, `python .\tools\test_finish_lesson_confirmation_policy.py`, `powershell -ExecutionPolicy Bypass -File .\tools\run_desktop_release_gate.ps1`, and `powershell -ExecutionPolicy Bypass -File .\scripts\validate-windows-direct-release.ps1`. The desktop release gate passed restore, Debug build, Release build, backend build, lesson content audit, interface localization audit, desktop backend boundary audit, tutor prompt policy, lesson behavior CMS ownership policy, admin/RBAC static policy checks, and desktop release smoke gate automated checks; EF checks were skipped because there were no schema-affecting backend changes. Windows direct release validation for historical `1.0` passed release directory/file presence, no UTF-8 BOM, JSON parsing, required manifest fields, production backend URL, manual-confirmation update mode, installer presence, installer SHA-256 agreement with `latest.json` and `checksums.sha256`, and matching `1.0` changelog/known-issues versions.

Use the Windows direct-release upload helper:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 -ServerHost lvt-server -ServerUser deploy -RemotePath /var/www/languagevoicetutor/releases/windows/direct
```

Do not manually `scp` installer files when the script exists. Windows direct release upload is separate from backend deploy and static website publish. After upload, verify `latest.json`, `installerFileName`, `backendBaseUrl`, installer hash, and that the download page button downloads the same installer named by the manifest.

Code signing remains deferred. CMS published-snapshot runtime is active for published Windows direct lessons. Backend deployment, database migrations, the download website, and update UI remain separate work.

## Paddle, legal, and subscription architecture

Live Paddle is not enabled yet. Do not change production Paddle environment values as part of documentation or website review work. Do not put real Paddle API keys, price IDs, client-side tokens, webhook secrets, raw payloads, signatures, customer IDs, transaction IDs, JWT secrets, database URLs, OpenAI keys, or other secrets in docs.

Paddle remains behind the backend/provider adapter. Desktop must not directly decide Premium and must not directly integrate with Paddle. The backend remains the source of truth for plan, subscription, entitlement, usage, and limits. Entitlement remains the source of Premium access; `PaymentEntity` is diagnostic payment history only and is not the source of Premium.

Desktop now and future mobile clients share one backend account, one backend database, one subscription/entitlement state, and one lesson history/progress source. Paddle is likely the first web/desktop provider, but the architecture must allow Apple and Google later for mobile. Do not introduce YooKassa, Russia-only billing assumptions, a full Paddle state mirror, or production Paddle activation in documentation-only updates.

Website/legal pages prepared for review include Pricing / Subscription terms, Terms of Use, Privacy Policy, Refund Policy, Cancellation Policy, Support, Seller / Company Details, AI & Data Disclosure, Platform Availability / Service Status, and Download. Legal texts are product/legal drafts and must not be described as final legal advice. Seller details are public business details only; do not publish passport/private personal data. `Paddle.com` bare domains are clickable via markdown/autolink rendering. The download page, footer, and legal/support pages are Paddle-review-ready pending final owner/legal review.


## Selected tutor settings API deployment

Backend commit `268681e` (`Add selected tutor to user settings API`) is deployed in production release `0.1.35-backend.109`; the previous backend release was `0.1.35-backend.108`. The deployed package was `LanguageVoiceTutor.Backend-linux-x64-0.1.35-backend.109.zip`, uploaded/deployed with the normal PackageFirst backend flow. The deploy script did not run EF migrations because no database migration was needed: `UserProfileEntity.SelectedTutorId` already existed in the EF model and existing migrations. Production verification after deployment confirmed `/opt/languagevoicetutor/backend/current` resolved to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.109`, `languagevoicetutor-backend.service` was active/running, `https://api.languagevoicetutor.com/health` returned `200 Healthy`, and `https://api.languagevoicetutor.com/api/health/database` returned `200 Healthy`.

`selectedTutorId` is no longer a known backend API gap. `/api/me/settings` is now the persisted account-state source for the selected tutor: `GET /api/me/settings` returns `selectedTutorId`, and `PUT /api/me/settings` persists the selected tutor when a valid `selectedTutorId` is supplied. `GET /api/tutor-options` remains the source for available tutor options. The backend validates supplied tutor IDs against `TutorAvatarOptions.All`, rejects invalid values, canonicalizes valid IDs, and persists them to `UserProfileEntity.SelectedTutorId`. Omitted or `null` `selectedTutorId` values preserve the existing selected tutor for backward compatibility. The existing settings fields (`nativeLanguage`, `studyLanguage`, `explanationLanguage`, `speechVoice`, `speechSpeed`, and `conversationModeEnabled`) continue to work separately, and `speechVoice` is not changed automatically when `selectedTutorId` changes.

## AI model settings in Super Admin CMS

AI model identifiers for backend runtime are managed through the Super Admin / Bootstrap Admin controlled **Admin → System → AI Models** CMS endpoint set. Backend runtime remains the source of truth for AI model selection: the Desktop app calls backend endpoints and does not choose OpenAI model IDs. The active and draft values are stored in JSON/file-based persistent server data at `site/content/ai-model-settings.json` resolved outside versioned backend release folders (for production, under the persistent `/opt/languagevoicetutor/backend/site/content/` tree rather than `/opt/languagevoicetutor/backend/current` or `/opt/languagevoicetutor/backend/releases/<version>`). Packaged defaults are only fallback/seed data; startup must not overwrite an existing published active file. API keys are not stored in CMS, no database table is used, and no EF migration was added. OpenAI API keys remain server environment secrets, especially `OPENAI_API_KEY`.

Production verification update: `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json` now exists as persistent server data/config, was seeded from the current release only because the persistent file was missing, has mode `644`, and survived a `languagevoicetutor-backend.service` restart. `sha256sum` matched the current release copy exactly (`94f84fc07551d821bfa9dc0682bb4ee60108d11d74987b84ebb39fce96f825f1`), and post-restart checks confirmed the file still existed and contained `gpt-5.5` plus `gpt-5.2`. This resolved the AI Models persistence risk. Future backend deploys must not rely on `/opt/languagevoicetutor/backend/current/site/content/ai-model-settings.json` or any `/opt/languagevoicetutor/backend/releases/<version>/site/content/ai-model-settings.json` file as the production source of truth.

Current known-good AI model configuration: lesson tutor chat uses `gpt-5.5`; feedback/correction, lesson hints, and translation use `gpt-5.2`; speech-to-text uses `gpt-4o-mini-transcribe`; normal lesson chat TTS uses `tts-1`; Conversation Mode TTS uses `gpt-4o-mini-tts`; and Realtime voice uses `gpt-realtime`. These are model IDs only and do not include provider credentials.

Known-good production values are lesson tutor chat `gpt-5.5`, feedback/correction `gpt-5.2`, lesson hint `gpt-5.2`, and translation `gpt-5.2`. Publishing AI model settings affects new backend AI requests without a desktop release because the desktop continues to call backend endpoints and does not choose OpenAI model IDs. The Super Admin workflow is: Load AI Models → Edit draft → Save draft → Validate format → Test provider access → Review compatibility diagnostics → Publish / Make active only if relevant runtime diagnostics pass → run a small real lesson after publishing. Validate format checks syntax only and does not prove provider access. Test provider access performs provider-level checks using draft settings, does not publish settings, and uses safe dummy input rather than real lesson/user text. Audio and realtime roles may be `not_tested` when not covered by lightweight provider tests.

The `gpt-5.5` lesson tutor chat investigation found that `gpt-5.5` was available to the deployed OpenAI API key/project. The root cause was the request parameter `temperature`, not model unavailability. Safe provider diagnostics recorded `statusCode: 400`, `safeCategory: invalid_request`, `providerErrorType: invalid_request_error`, `providerErrorParam: temperature`, and `sanitizedProviderMessage: Unsupported parameter: 'temperature' is not supported with this model.` Minimal Responses API text, minimal structured output, and the lesson runtime shape without user content passed after `temperature` was omitted. Therefore `gpt-5.5` can be used for lesson tutor chat when backend runtime requests omit `temperature`.

Backend request-shape rule: for `gpt-5.5` lesson tutor chat runtime requests, omit `temperature`; for `gpt-5.2`, preserve existing behavior and still send `temperature: 0.3` where currently configured. Do not reintroduce `temperature` for `gpt-5.5` unless provider compatibility changes and is retested, and do not assume newer model families accept every parameter accepted by older models. New model families must be tested with provider access diagnostics before publish.

Compatibility diagnostics are interpreted as follows: `minimal_responses_text` verifies basic model availability and Responses API access; `current_provider_test_shape` verifies the older provider-test shape including `temperature` if present; `minimal_structured_output` verifies strict structured output support using a tiny safe schema; and `lesson_chat_runtime_shape_without_user_content` verifies lesson runtime request options/schema with safe dummy input. If the minimal text check fails, suspect project/key availability or alias usage. If minimal text passes but the current provider-test shape fails, inspect the added parameter. If structured output fails, schema/text-format compatibility is the issue. If structured output passes but lesson runtime shape fails, the lesson schema or runtime request shape is the issue. If the lesson runtime shape passes, the model is safe to try in a small real lesson.

Provider errors are mapped to safe categories. Super Admin sees only safe provider fields: `statusCode`, `safeCategory`, `providerErrorType`, `providerErrorCode`, `providerErrorParam`, and `sanitizedProviderMessage`. Logs may include safe runtime fields such as `operation`, `modelRole`, `configuredModelId`, provider status/category, and provider error type/code/param/message where available. Logs and Admin UI must not expose API keys, Authorization headers, raw provider response bodies, raw request bodies, full prompts, private user lesson text, environment values, or connection strings.

## Windows distribution channel

The active Windows distribution channel is the Direct EXE/Inno installer. The direct `latest.json` update flow remains active for update checks, installer download, verification, and installer launch.

Microsoft Store/MSIX was evaluated with a local prototype and is discontinued for now. Store/MSIX packaging is not implemented or active, no Store submission is planned, and Store-channel runtime behavior should not be reintroduced unless the product decision changes in a separate future effort. Future Windows trust work should focus on buying and integrating a code signing certificate for the direct EXE/Inno installer.

Backend deploy, Website CMS/static site publish, Windows direct installer upload, and database migrations remain separate processes.

## 2026-06-30 release-readiness audit snapshot

### Current Active Release Strategy

- Windows: Direct EXE/Inno installer.
- Updates: direct `latest.json` manifest at `site/public/releases/windows/direct/latest.json` and `https://languagevoicetutor.com/releases/windows/direct/latest.json`.
- Signing: future trust work is a code signing certificate for the direct EXE/Inno installer.
- Backend: production API is `https://api.languagevoicetutor.com`; backend deploy uses package/upload helpers plus `/health` and `/api/health/database` checks.
- Website: public site is `https://languagevoicetutor.com`; Website CMS/static-site publish is separate from backend deploy.
- Billing: Paddle/global provider-agnostic billing remains the target; controlled Paddle live validation is completed, while broad paid-launch readiness remains pending final review.
- Store/MSIX: discontinued for now and not an active release path.

### Current release point

- Windows direct release: `1.6`, verified from public `https://languagevoicetutor.com/releases/windows/direct/latest.json` with `channel=direct-public`, installer `LanguageVoiceTutorSetup-1.6.exe`, production backend URL, `minimumSupportedVersion=1.6`, and manual-confirmation update mode. The published manifest SHA-256 is `9eaac1ffa1ead6c3590f2cf072ff6dcabb7edba912c38a6cd1d6875ad5ac1aa3` and size is `188959874` bytes; no independent second public-download SHA verification is claimed for 1.6.
- Backend release in tracked release docs: current production is `0.1.35-backend.142`, with `.141` as rollback; `/health` and `/api/health/database` are verified healthy. Older backend references are historical and not current production unless a section is explicitly documenting those releases.
- AI Models persistent production file: verified at `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`; it survived backend service restart, matched the current release copy by SHA-256 `94f84fc07551d821bfa9dc0682bb4ee60108d11d74987b84ebb39fce96f825f1`, and contains lesson tutor chat `gpt-5.5`, feedback/correction `gpt-5.2`, lesson hint `gpt-5.2`, and translation `gpt-5.2`. For `gpt-5.5`, backend requests must omit `temperature`.

### What is ready, partial, and blocked

Ready for controlled tester use: direct Windows manifest/update flow, production backend health-check procedure, CMS published-snapshot runtime for lessons, verified persistent AI Models production storage, Website CMS draft/publish mechanics, and documented secret boundaries.

Partially ready: Windows public installer release because signing and wider smoke/feedback remain; website/legal pages because owner/legal final review remains; AI tutor quality because CMS content approval and tester feedback remain. Backend operations remain controlled/manual: current production is documented as `0.1.35-backend.142`, with deploys, health checks, database health checks, and migrations kept as separate operations.

Blocked before broad public paid release: code signing for the direct installer, direct installer clean-machine/update smoke, final website/legal/support/pricing approval, monitoring/privacy/release-readiness review, and explicit release decision after controlled tester feedback. Controlled Paddle live payment/Premium activation, failed-payment non-activation, cancel-renewal, and full-refund Premium revocation are completed, but they are not a broad launch decision; chargeback remains implemented/test-covered but not live-chargeback-tested, partial refund remains conservative/manual-review, and expanded customer portal/subscription management is deferred.

### Must not be touched for this docs-only state

Do not change backend runtime code, desktop runtime code, database schema/migrations, Inno installer behavior, deployment scripts, Website CMS live content, backend deployment, Windows direct upload, Store/MSIX files, Paddle/OpenAI/AI Models runtime behavior, generated artifacts, signing private keys, or secrets as part of this documentation audit.

### Do not mix these operations

- Backend deploy is not Windows installer upload.
- Website CMS publish is not backend deploy.
- DB migration is separate and must be reviewed.
- Direct Windows installer upload is not Store/MSIX.
- Paddle live account/provider changes are not code deploy unless an approved backend configuration/code change is required.

## 2026-06-30 Paddle live checkout preparation state

At that historical Paddle checkpoint, backend live checkout code was deployed in `0.1.35-backend.108`, `/pay.html` and `/paddle.public.json` were published under the real nginx root, and live server-side Paddle config was present in `/etc/languagevoicetutor/backend.env`. The controlled 2026-07-02 live payment/webhook/Premium activation path completed for the expected Language Voice Tutor Pro monthly price, and desktop cancel-renewal behavior was verified. Windows Direct 1.0 was the then-current release; AI Models persistent storage was verified and untouched. Store/MSIX remained discontinued and active Windows distribution remained Direct EXE/Inno.

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
- Controlled Paddle live payment validation completed on 2026-07-02 for Language Voice Tutor Pro. A real Paddle live payment completed for 14.99 EUR by Google Pay for customer email `11111@gmail.com`; Paddle status was Complete. Backend `0.1.35-backend.108` remained healthy afterward: production backend health returned `200 Healthy` and production database health returned `200 Healthy`. Backend logs showed live checkout transaction creation, webhook receipt for `subscription.created`, `subscription.activated`, and `transaction.completed`, successful payment persistence, reconciliation marking the completed transaction for activation, subscription snapshot processing, and entitlement activation with `ActivatedCount=1`, `BlockedCount=0`, `FailedCount=0`. Earlier `transaction.payment_failed` attempts were stored and safely processed with `ActivatedCount=0` / `AlreadySkippedCount=1`; they did not grant Premium. One transient PostgreSQL serialization failure occurred during subscription lifecycle snapshot processing; the retry policy retried it, the retry succeeded, and final snapshot processing completed with `FailedCount=0`. This is observed non-blocking retry evidence, not a failed payment flow.
- Desktop Premium visibility was confirmed after payment: Current tariff `Premium`, free lessons remaining `without limits`, Premium active until `8/2/2026`, and auto-renewal initially Active. Cancel-renewal verification also completed from the desktop flow: after cancellation, Desktop still showed Current tariff `Premium`, free lessons remaining `without limits`, Premium active until `8/2/2026`, and Auto-renewal inactive. This confirms cancellation disables future renewal while preserving paid Premium access until the paid period end when no refund exists. The later full refund removes backend Premium access.
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

Admin capabilities should distinguish completed controlled validation from launch completion: live checkout/webhooks, the 2026-07-02 live payment/Premium activation/cancel-renewal path, failed-payment non-activation, and full-refund Premium revocation can be reported as completed. Chargeback remains implemented/test-covered but not live-chargeback-tested, partial refund remains conservative/manual-review, expanded customer portal/subscription management is deferred, and `billingPaidLaunchReleaseComplete=false` continues until final release-readiness review and remaining non-billing blockers are closed.

Admin RBAC note: `productionRolesAvailable` now means persistent Admin role authorization is active with an explicit fallback cutover (`AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`). It is not a broad public-launch flag and does not override remaining paid-launch blockers. Production diagnostics show two active `super_admin` AdminUsers and fallback disabled; if this flag is false, check the explicit fallback configuration and cutover status before changing role assignments.

## Admin Activity / Audit Log first safe slice (read-only)

- Added the first actor-centric **Admin Activity** view as a read-only slice built from existing audit tables only: `admin_actions`, `admin_role_assignment_events`, and `cms_content_audit_logs`.
- Admin Activity now displays existing admin-entered reasons/notes where those values are already stored in the normalized audit rows, while keeping safe metadata in a separate column.
- The backend endpoint is `GET /api/admin/activity` and is protected by the existing audit-read policy.
- A later approved migration added the dedicated `admin_auth_audit_events` table/source for Admin auth/session events rather than overloading `admin_actions`, `admin_role_assignment_events`, or `cms_content_audit_logs`.
- On 2026-07-01, migration `20260701000000_AddAdminAuthAuditEvents` was applied to production after a fresh readable backup and SQL review. The production table exists, the owner was corrected to `lvt_app`, and `lvt_app` has table privileges.
- Production Admin Activity includes the `admin_auth_audit_events` source dropdown entry and shows verified `admin_login_success`, `admin_logout`, `admin_login_failed`, and `disabled_admin_login_denied` events. Session expiration audit persistence remains pending.
- Website/AI publish audit may still be partial when the corresponding events are not already present in the existing audit tables.
- Controlled Paddle live payment/webhook/Premium activation and desktop cancel-renewal validation were completed on 2026-07-02; failed payment attempts did not grant Premium; full-refund Premium revocation is production-verified; chargeback remains implemented/test-covered but not live-chargeback-tested, expanded customer portal/subscription management is deferred and is not a current blocker, and broad public paid launch remains pending.

## 2026-07-01 Admin Activity and emergency Premium revoke update

Historical note: the production backend for this Admin Activity update has since advanced to `0.1.35-backend.116`, with previous release `/opt/languagevoicetutor/backend/releases/0.1.35-backend.115`. No EF migration or database schema change was required for the `.115` voice scenario structured-output validation fix, and website/Windows installer files were not changed.

- Admin Activity is visible and usable in production and includes `admin_role_assignment_events` plus `admin_actions`, including `manual_premium_grant` and `manual_premium_revoke`.
- Admin Activity table usability was improved with a top horizontal scrollbar and wider Admin note column; Admin note/reason is visible where stored, and `safeMetadataJson` remains separate from Admin note.
- Admin Activity continues to be read-only and now resolves existing `admin_actions` actor app-user ids to matching persistent `admin_users` where possible, so `actorAdminUserId`, `actorUserId`, and source/action filters can find existing admin action rows such as Manual Premium Grant, Manual Premium Revoke, Free Lesson Reset, and Billing Cancel Renewal.
- Manual Premium Revoke is completed as an emergency `super_admin` backend entitlement/access-control action. It requires an admin reason, expires/revokes active Premium entitlement rows, including paid/provider-backed active Premium entitlements, and writes an `admin_actions` Admin Activity entry with safe metadata. After revoke, the selected user no longer has active Premium access.
- Emergency Premium Revoke does not mutate Paddle provider history, does not delete `PaymentEntity` records, does not fake Paddle webhook events, does not make payment history the Premium access source, and does not change Paddle webhook/payment activation rules. Cancel paid renewal remains a separate future-renewal cancellation action; paid subscription/provider state may show `cancellation_scheduled` and `cancelAtPeriodEnd=true` while backend Premium access can still be separately revoked by `super_admin` when needed.
- No EF migration was added for this update; existing entitlement and admin action fields support the emergency revoke/audit behavior.
- Controlled Paddle live payment/webhook/Premium activation, failed-payment non-activation, desktop cancel-renewal, and full-refund Premium revocation were completed on 2026-07-02. Chargeback remains implemented/test-covered but not live-chargeback-tested; partial refund remains conservative/manual-review; expanded customer portal/subscription management is deferred and not a current blocker. Session expiration audit persistence remains pending and must not be marked complete.


## Mobile lesson-session reply placeholder production verification (2026-07-08)

Production backend `0.1.35-backend.112` is deployed and verified for backend-owned authenticated lesson completion and summaries. Backend `.111` already supported authenticated Finish and Summary but could leave summaries unavailable because `LessonSummaryGenerationService` read only top-level Responses API `output_text`; `.112` also supports nested `output[].content[].text`, rejects blank provider output before JSON deserialization, and preserves successful lesson completion if summary generation fails. `PUT /api/me/lesson-sessions/{sessionId}/finish` remains backward compatible with the existing desktop payload `{ "validTurnCount": 1 }`, marks owned sessions complete idempotently, and makes a best-effort backend summary generation attempt from persisted lesson messages plus safe lesson/session metadata. `GET /api/me/lesson-sessions/{sessionId}/summary` is the authenticated learner-safe read route for an already persisted result and does not regenerate missing summaries; production clients must not generate or upload summary, strengths, improvements, vocabulary, grammar, or next steps. A real authenticated Flutter mobile lesson displayed a ready backend-owned GET summary result on 2026-07-11. Existing desktop Finish uses the shared completion path, desktop UI and Finish response contracts are unchanged, and desktop currently displays its existing local desktop summary flow. Existing `POST /api/lesson-chat/reply` and `POST /api/me/lesson-sessions/{sessionId}/messages` behavior is unchanged. Development `/api/dev/.../summary` routes remain diagnostic/development-only and are not production mobile contracts. Desktop and mobile continue to share the backend session, completion, history, progress, and summary source of truth.

Production backend `0.1.35-backend.110` is deployed and verified for the backend-only mobile lesson-session reply placeholder route. The live `/opt/languagevoicetutor/backend/current` symlink resolved to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.110`, `https://api.languagevoicetutor.com/health` returned `200 OK` / `Healthy` / `Production`, `https://api.languagevoicetutor.com/api/health/database` returned `200 OK` / `Healthy` / `canConnect true`, and `languagevoicetutor-backend.service` was active/running. No EF migrations were added or run.

The new route is `POST /api/me/lesson-sessions/{sessionId}/reply` with request body `{ "messageText": "..." }`. It authenticates the user, verifies session ownership, verifies active session state, checks existing limits where applicable, and for a valid active session intentionally returns controlled `409 Conflict` with `mobile_lesson_reply_not_implemented`. Blank `messageText` returns `400`, missing/not-owned sessions return `404`, inactive/ended sessions return the existing session-ended `409` payload, exceeded chat reply limits return the existing `429` free/rate-limit payload, and unavailable session storage returns the existing `503` storage-unavailable payload.

This historical placeholder note no longer describes the accepted Mobile lesson implementation. Mobile uses the existing authenticated `POST /api/lesson-chat/reply`, supplying the current learner message, bounded prior history, selected scenario/context, live phase, turn limits, and other existing `LessonChatRequest` fields. Mobile owns client-side live lesson state; backend owns final prompt construction, tutor-policy enforcement, provider calls, limits, and response generation. Mobile does not call OpenAI directly. Normal provider replies remain stateless, persisted lesson-session messages do not hydrate the prompt, and the placeholder route remains historical/diagnostic rather than the normal Mobile reply path.

Pre-deploy verification passed: `dotnet test backend\EnglishVoiceTutor.Api.Tests\EnglishVoiceTutor.Api.Tests.csproj` passed `89/89`; `python .\tools\test_backend_linux_deployment_policy.py`, `python .\tools\test_backend_refresh_token_migration_policy.py`, `python .\tools\test_backend_refresh_token_policy.py`, and `python .\tools\test_desktop_release_backend_lock_policy.py` passed. This was docs/backend-route-only release scope and did not change desktop code, mobile repo code, billing, OpenAI/provider configuration, deployment scripts, Website CMS/static site content, voice, TTS, realtime, analytics, history, store setup, or installer release artifacts.

## Admin auth audit persistence production verification (2026-07-01)

- Migration `20260701000000_AddAdminAuthAuditEvents` was applied before backend `0.1.35-backend.108` deployment, after fresh backup creation and SQL review.
- Fresh pre-migration backup evidence is limited to safe metadata: path `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260701_154405Z.dump`, size `6.4M`, and `pg_restore --list` line count `245`. Do not paste backup contents, SQL dumps, secrets, env files, tokens, cookies, provider payloads, or raw user data.
- Production backend `0.1.35-backend.108` is deployed successfully; `/opt/languagevoicetutor/backend/current` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.108`, `languagevoicetutor-backend.service` is active/running, `/health` returns `200 Healthy`, and `/api/health/database` returns `200 Healthy`.
- The dedicated `admin_auth_audit_events` table exists in production, its owner was corrected to `lvt_app`, and `lvt_app` has table privileges.
- Admin Activity includes `admin_auth_audit_events` as a read-only source, the source dropdown includes `admin_auth_audit_events`, and production Admin Activity shows verified `admin_login_success`, `admin_logout`, `admin_login_failed`, and `disabled_admin_login_denied` events.
- Session expiration audit persistence remains pending and is not claimed complete.
- Session expiration persistence remains pending; no low-noise expiration persistence completion is claimed.
- Controlled Paddle live payment/webhook/Premium activation and desktop cancel-renewal validation were completed on 2026-07-02. Full-refund Premium revocation is production-verified; chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred and not a current blocker.

## CMS capability and runtime production verification (2026-07-01)

Backend `0.1.35-backend.108` fixed the stale `cmsUiAvailable` capability state. **System → Capabilities Check** now shows `cmsUiAvailable` as AVAILABLE, the Admin Shell **CMS Content** tab opens, and the CMS Content workspace loads. This is production UI availability verification only; no CMS content was saved, published, restored, initialized, imported, or otherwise mutated during this verification.

Learner runtime is production-verified as using `CmsPublishedSnapshot`, with the CMS published snapshot active and valid. Runtime status currently shows content pack slug `static-json-v1`, published version number `46`, 6 topics, 26 scenarios, 4 prompt templates, 3 tutor behavior profiles, validation success `Yes`, and currently using static JSON fallback `No`. Static JSON remains available as emergency fallback, but it is not active in the verified production runtime state.

## 2026-07-02 refund and chargeback Premium protection

In production backend `0.1.35-backend.108`, full Paddle refunds are treated as access-control events after `adjustment.created` or `adjustment.updated` webhook processing: the backend preserves Paddle/payment/subscription history, maps the adjustment back to the internal user by safe metadata or existing payment/subscription records, and expires active provider-event Premium entitlements with reason `paddle_full_refund`. Chargebacks are implemented as stronger refund evidence and are covered by tests/fake paths, but no real live chargeback was performed.

Normal cancel-renewal behavior is unchanged: scheduled cancellation keeps Premium through the paid period end. Partial refunds are conservative in this slice: the event is safely recorded/processed for review and Premium is left unchanged unless the adjustment is full or a chargeback. Provider history is preserved; payment and subscription records are not deleted, and refund processing does not fake Paddle webhook events or expose raw provider payloads, webhook signatures, tokens, cookies, secrets, API keys, or full card/payment data in Admin Activity evidence.

Full-refund Premium revocation was production-verified on backend `0.1.35-backend.108` during this historical 2026-07-02 validation: the operator reprocess of stored provider event `evt_01kwhgmvh1v9k8ve70gvnfeskm` (`adjustment.updated`, transaction `txn_01kwhg9bdxhp5738wqwc7xkh3q`, subscription `sub_01kwhga8nbx7hdcqgq5fea9wc6`) returned `UserResolutionSource=payment`, `FullRefundDetected=True`, `ChargebackDetected=False`, `EntitlementCandidatesCount=1`, `RevokedCount=1`, `Result=Revoked`, and `BlockReason=(null)`. Admin User Lookup confirmed `planId=free`, `planName=Free`, `premiumActive=No`, and `trialActive=No`; Admin Activity showed `actionType=paddle_full_refund_premium_revoke`, `result=succeeded`, targeting the refunded user. Broad public paid launch is no longer blocked by full-refund revoke, but remains pending final release-readiness review and non-billing blockers. Expanded customer portal/subscription management is deferred and is not a current blocker. Direct installer code signing remains pending.

## 2026-07-02 Paddle refund replay recovery status

Production backend `0.1.35-backend.97` was deployed and verified healthy: `/opt/languagevoicetutor/backend/current` pointed to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.97`, `/health` and `/api/health/database` returned healthy, `languagevoicetutor-backend.service` was active/running, and no EF migrations were run by the upload script. After deployment, replaying the already-delivered Paddle `adjustment.updated` event `evt_01kwhgmvh1v9k8ve70gvnfeskm` was idempotent: the provider event id was a duplicate, normalization reported `AlreadyNormalizedCount=1`, payment persistence reported `AlreadyCurrentCount=1`, reconciliation did not reprocess the existing already-normalized/skipped event, and entitlement activation reported `AlreadySkippedCount=1`. Premium remained active.

Root cause: backend `.97` fixed fallback user resolution for new adjustment events, but Paddle replay keeps the same provider event id. Existing events normalized/skipped or blocked under `.96` are not automatically replayed through the `received -> reconciliation_pending -> processed` pipeline by duplicate webhook ingestion.

Backend `0.1.35-backend.98` was deployed and healthy, and the operator-only command ran correctly through `systemd-run` with the backend environment file for `evt_01kwhgmvh1v9k8ve70gvnfeskm`, but it returned `Result=Blocked` / `BlockReason=reconciliation_blocked` even though it found the stored `adjustment.updated` event, resolved the user through payment history, detected a full refund, and found one active Premium entitlement candidate. Root cause: `.98` reprocess still depended on the old reconciliation pipeline/state for an event already blocked/skipped under older code. Backend `.99` fixed the explicit operator-only recovery path. The `.99` operator reprocess returned `Result=Revoked` for the stored full-refund event, and Admin/Desktop status confirmed Premium inactive. No more live payment, refund, replay, or chargeback testing is required for this release-readiness slice. Expanded customer portal/subscription management is deferred and is not a current blocker. Direct installer code signing remains pending.
# Backend-owned localized lesson setup production content and publication gate

All 26 active canonical lesson scenarios now contain authored `setupLocalizations` for `fr`, `de`, `pt`, `es`, and `it`: 130 localized setup-message templates and 625 localized context titles. English remains canonical, stable context IDs and canonical English source fields remain unchanged, and runtime continues to expose the additive request-specific `localizedSetup` projection selected from the authenticated learner's backend study-language setting.

CMS draft editing remains permissive so incomplete localization work can be saved. Creating a new CMS publication now requires every active scenario to have complete non-English setup localizations: each required language needs a non-empty template, exact stable-context coverage with non-blank titles, and exactly the canonical setup-message placeholders. Legacy published snapshots without `setupLocalizations` remain readable for runtime and rollback. No database migration, deployment, CMS import, CMS publication, or production operation was performed for this change.

The localization rollout is complete. Production backend `0.1.35-backend.142` is active with `.141` retained for rollback; CMS published version `51` is the valid runtime source (`CmsPublishedSnapshot`, `fallbackUsed=false`) with 26 scenarios, 130 localized setup-message templates, and 625 localized context titles. Runtime responses own the additive `localizedSetup` projection. Authored `setupLocalizations` remain backend/CMS data; clients do not consume that authored field directly, and no migration was required.

The Admin CMS scenario editor exposes five full localized first-message textareas for French, German, Portuguese, Spanish, and Italian directly below the canonical English field. Each edits one complete `setupMessageTemplate` text block; messages are not split into Goal, situation, or instruction controls. Structured Save draft uses those visible fields, while Advanced JSON Save draft preserves the entered `DefinitionJson` as authoritative; the normal successful-save reload synchronizes visible fields from saved JSON. `contextVariantTitles` stay internal to Advanced JSON, draft save remains permissive, and publication validation requires complete exact coverage. The completed Desktop and Mobile clients consume only the response-owned `localizedSetup` projection.
