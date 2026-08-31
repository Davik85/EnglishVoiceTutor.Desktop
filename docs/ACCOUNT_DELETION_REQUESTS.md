# Account-deletion requests

> **Deployment and verification status (2026-07-23).** At that historical deployment, production was `0.1.35-backend.133`, with `0.1.35-backend.132` as the previous rollback release. Commit `80396ce` (retained Admin audit history correction), migration `20260723045852_AddAccountAnonymizationExecution`, and the complete account-deletion backend/Admin workflow are deployed. Production later advanced to `.134`. A controlled production test created and processed a request, completed anonymization, automatically resolved and redacted the request, replaced the original email with a unique `@deleted.invalid` address, prevented original-email lookup and new login, and prevented repeat execution. Retained `AdminAction` target history no longer blocks deletion; real Admin/CMS dependencies and active paid access still do. Paddle, subscriptions, payments, and financial history remain unchanged.

## Current deployed contract

Backend `0.1.35-backend.127` introduced `POST /api/me/account-deletion-requests`; it remains available in current production backend `0.1.35-backend.134`. The endpoint requires an authenticated learner, derives the account identity only from that authenticated user, requires confirmation with the current password, and accepts an optional deletion reason. The password is used only for confirmation: it is never stored, returned, emailed, or displayed in Admin CMS.

Only one unresolved request is allowed per user. The partial unique index `IX_user_feedback_reports_ActiveAccountDeletionRequest_UserId`, applied by migration `20260721120000_AddActiveAccountDeletionRequestConstraint`, enforces that boundary. A duplicate submission safely returns the existing active request identifier and status instead of creating another unresolved request. Once a prior request reaches a terminal status, a later request may create a new support ticket by design.

## Support workflow

Account-deletion requests reuse the existing feedback/support queue with category `account_deletion`. Authorized administrators can filter and open them, change their support status, and reply through the existing email reply mechanism. Replies may acknowledge receipt, request more information, give a processing update, reject the request with an explanation, or confirm completion after the real data operation.

Submitting, replying to, or changing the status of a request does **not** automatically:

- delete or deactivate the account;
- anonymize or otherwise alter user data;
- cancel a subscription;
- revoke all authentication token families; or
- perform the actual deletion/anonymization procedure.

Actual deletion/anonymization is performed through the deployed Super-Admin workflow after a fresh preflight, rather than by ticket submission or an arbitrary support-status change. The completed account remains only as a non-login anonymized shell; refresh tokens are removed. The local, disabled Restore Credentials foundation also removes the user's restore public-credential and registration-ceremony records during anonymization; this does not claim that the pending migration has been applied. An already-issued access token may remain usable until its normal configured expiry. That expiry window is accepted for the current product scale: after expiry, refresh cannot succeed and the client must clear the invalid session. No further backend authentication change is currently planned for this behavior. A request must not be marked `resolved` or otherwise completed merely to close the ticket: that status must reflect completed anonymization. The password must never be copied into support correspondence or Admin CMS.

The repository-grounded design and complete data inventory are in [Account anonymization procedure and data inventory](ACCOUNT_ANONYMIZATION_PROCEDURE.md). That document is a design/runbook draft only: it does not implement deletion/anonymization, authorize a production operation, or change the request-intake behavior described here.

## Client status

Mobile Settings integration is implemented and manually verified. **Settings → Request account deletion** requires the current password and displays the returned request ID and status for both a newly created request and an existing active request. An incorrect password neither creates a request nor logs the learner out. Mobile wording describes a **request**, not immediate deletion; submitting or resolving the support ticket still does not perform real deletion or anonymization.

Future client design only: the entry is intended to move to **Settings → App → Contact & Support → Request account deletion**, subject to the actual Mobile Settings structure. Before password entry and submission it must show a neutral warning that saved learner information will no longer be available after irreversible deletion/anonymization, the reason remains optional, and submitting creates a support-managed request rather than immediate deletion. This future placement and warning are not implemented by the current Mobile flow.
