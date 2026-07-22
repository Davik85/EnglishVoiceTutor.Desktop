# Account-deletion requests

## Current deployed contract

Backend `0.1.35-backend.127` deploys `POST /api/me/account-deletion-requests`. The endpoint requires an authenticated learner, derives the account identity only from that authenticated user, requires confirmation with the current password, and accepts an optional deletion reason. The password is used only for confirmation: it is never stored, returned, emailed, or displayed in Admin CMS.

Only one unresolved request is allowed per user. The partial unique index `IX_user_feedback_reports_ActiveAccountDeletionRequest_UserId`, applied by migration `20260721120000_AddActiveAccountDeletionRequestConstraint`, enforces that boundary. A duplicate submission safely returns the existing active request identifier and status instead of creating another unresolved request.

## Support workflow

Account-deletion requests reuse the existing feedback/support queue with category `account_deletion`. Authorized administrators can filter and open them, change their support status, and reply through the existing email reply mechanism. Replies may acknowledge receipt, request more information, give a processing update, reject the request with an explanation, or confirm completion after the real data operation.

This release provides request intake and support-managed tracking only. Submitting, replying to, or changing the status of a request does **not** automatically:

- delete or deactivate the account;
- anonymize or otherwise alter user data;
- cancel a subscription;
- revoke all authentication token families; or
- perform the actual deletion/anonymization procedure.

Actual deletion or anonymization remains a manual support process. A request must not be marked `resolved` or otherwise completed merely to close the ticket: that status must reflect that the real approved deletion or anonymization work has been performed. The password must never be copied into support correspondence or Admin CMS.

## Client status

Mobile Settings integration is not implemented yet. Until the separate Mobile task is completed, the deployed backend endpoint and Admin support workflow exist without a learner-facing Mobile Settings entry. Future Mobile wording must describe a **request**, not immediate deletion.
