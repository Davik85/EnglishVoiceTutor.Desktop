# Desktop Upgrade / Paywall UI Plan

Review date: 2026-05-29.

Status: Step 4D-4 implemented; manual refresh after checkout is available and remains backend-driven.

This is Step 4D after the backend Paddle billing lifecycle foundation through Step 4B and the Step 4C production webhook setup checklist/config guard. Step 4D-2 now implements a simple Windows desktop access/paywall panel after backend lesson-start denial. Manual checkout launch and manual refresh after checkout are implemented, and this document does not change backend billing behavior.

## 1. Purpose

This plan defines how the Windows desktop app should present free-limit, trial, Premium, checkout, and blocked-access states without making local entitlement decisions.

The desktop app should guide learners clearly when they can start lessons, when they need to sign in, when their free daily lesson allowance is used, when Premium or trial access is active, and when checkout is unavailable. All of those states must be displayed from backend account, access, subscription, usage, and entitlement state.

## 2. Non-negotiable architecture boundaries

- Backend is the only source of truth for Premium/free/trial/usage/access.
- Desktop only displays backend state.
- Desktop must call backend access/status endpoints.
- Desktop must request checkout from backend.
- Desktop must open the `checkoutUrl` returned by backend.
- Desktop must not activate Premium locally.
- Desktop must not mutate local state to grant Premium after checkout.
- Desktop must refresh backend status after checkout.
- Future mobile must follow the same backend-account/backend-entitlement model.
- Paddle must remain behind the backend/provider adapter.
- Desktop should not contain Paddle business logic beyond opening the backend-provided `checkoutUrl`.
- `EntitlementEntity` remains the source of Premium access.
- `SubscriptionEntity` is a provider-agnostic snapshot only and must not grant Premium access by itself.
- `PaymentEntity` is diagnostic payment history only and must not grant Premium access.
- Do not add YooKassa, Russia-only, or provider-specific access assumptions.

## 3. Backend endpoints desktop should rely on

Desktop should rely on existing backend endpoints and avoid inventing desktop-local billing rules.

- `GET /api/me/lesson-access`
  - Primary authenticated lesson-start preflight.
  - Use before navigating into a new normal lesson.
  - Treat the backend response as authoritative for whether the new lesson may start.
- `GET /api/dev/lesson-access`
  - Local/dev fallback only where appropriate.
  - Keep this out of production access decisions.
- `POST /api/me/billing/checkout-session`
  - Authenticated checkout-session request.
  - Desktop sends the request to backend and opens only the returned `checkoutUrl` when present.
  - Desktop does not call Paddle directly.
- Existing authenticated user/status/settings endpoints already used by desktop
  - Continue using them for account, settings, and any existing user/status display.
  - Do not infer Premium locally from settings or cached account data.

Future candidate endpoints may be proposed later only if the current endpoints do not provide enough display state. Future candidates must be explicitly designed and approved before implementation.

## 4. Main UX states

### Signed out

- Cannot start a normal lesson.
- Show a sign-in/register prompt.
- Do not show checkout until the user is authenticated.
- Keep the message simple: the user needs an account so access and lesson progress can be saved by backend.

### Trial active

- Show trial active status.
- Lesson access is allowed when backend allows it.
- Optionally show trial end date if backend provides it.
- Do not calculate trial validity locally from a cached registration date.

### Premium active

- Show Premium active status.
- Lesson access is allowed when backend allows it.
- Do not show a paywall.
- Premium must appear only because backend access/status state says Premium is active.

### Free with allowance remaining

- Show remaining free lesson allowance if backend provides it.
- Lesson access is allowed when backend allows it.
- Keep the state calm and informational.

### Free allowance used

- Backend denies new lesson start when `SubscriptionEnforcement:Enabled=true` and the free allowance is consumed.
- Show a calm upgrade/paywall screen after backend denial.
- Existing lesson continuation should not be interrupted.
- Do not block current lesson continuation unless backend explicitly denies that flow.

### Past due

- Show a clear payment issue state if backend exposes that status.
- Do not decide access locally from a past-due subscription snapshot.
- Access follows backend entitlement/access state.
- Avoid alarming text; explain that access depends on backend confirmation.

### Canceled / paused

- Show inactive subscription state if backend exposes that status.
- Do not decide access locally from canceled or paused subscription snapshots.
- Access follows backend entitlement/access state.

### Checkout unavailable

- Show a friendly message that upgrade is temporarily unavailable.
- Do not crash.
- Allow the user to continue with free/trial behavior if backend allows it.
- Do not show raw provider errors, secrets, IDs, or internal configuration details.

## 5. Lesson start flow

1. User clicks **Start Lesson**.
2. Desktop calls backend lesson-access preflight.
3. If backend allows access, desktop navigates to the lesson.
4. If backend denies with `lesson_access_denied`, desktop shows the paywall/upgrade state.
5. If a network or server error occurs, desktop shows a calm retry/fallback message.
6. Desktop does not block current lesson continuation unless backend explicitly denies that flow.

Important behavior:

- The backend preflight result is authoritative.
- Desktop should not use local Premium flags to override backend denial.
- Desktop should not consume free allowance locally.
- Desktop should not assume a failed preflight means payment is required if the failure is a network/server problem.

## 6. Upgrade flow

1. User clicks **Upgrade**.
2. Desktop calls `POST /api/me/billing/checkout-session`.
3. If the response has `checkoutUrl`, desktop opens it in the browser/system webview strategy; the URL is a backend-hosted Paddle checkout launch page (`/checkout/paddle?...`), not a direct Paddle API or pay link.
4. Desktop does not assume payment success.
5. Desktop shows a waiting-for-confirmation / refresh-status state.
6. Desktop refreshes backend access/status after checkout.
7. Premium appears only after backend state changes through webhook processing and entitlement activation/extension.

Notes:

- Checkout itself does not activate Premium.
- Desktop should not contain Paddle API keys, webhook secrets, price IDs, customer IDs, transaction IDs, or secret-bearing URLs.
- Desktop should not call Paddle directly; the backend-hosted launch page loads Paddle.js and starts Paddle checkout.
- Desktop should not include Paddle-specific business access logic beyond opening the backend-provided `checkoutUrl`.

## 7. Checkout result handling

Plan:

- No local success assumption.
- No local entitlement mutation.
- Add a **Refresh status** action.
- Optional polling can be planned later, but must be bounded and backend-driven.
- If checkout is abandoned, keep the user in the previous backend state.
- If webhook delay happens, show a clear message such as: payment confirmation may take a moment; refresh status shortly.
- If checkout fails or is unavailable, show a friendly retry message and keep the user's previous backend-driven access state.

## 8. UI copy principles

- Use clear, calm, non-punitive text.
- Avoid scary payment language.
- Avoid overpromising instant activation.
- Explain that access updates after payment confirmation.
- Keep a global/international tone.
- UI language currently may remain English.
- Do not mention internal implementation details such as entitlement tables, provider event IDs, webhook secrets, or raw provider payloads.

Example copy direction, not final UI text:

- Signed out: "Sign in or create an account to start lessons and save your progress."
- Free allowance used: "You have used today's free lesson. Upgrade to continue with more lessons, or come back tomorrow."
- Waiting after checkout: "Payment confirmation can take a moment. Refresh your status when checkout is complete."
- Checkout unavailable: "Upgrade is temporarily unavailable. You can still continue with any access currently available on your account."

## 9. First implementation slices after plan approval

- Step 4D-1: completed; backend-state view model mapping for access/paywall states was added without changing UI layout much.
- Step 4D-2: completed; desktop now shows a simple backend-driven access/paywall panel after lesson-start denial.
- Step 4D-3: completed; the access panel Upgrade action calls the backend checkout-session endpoint and opens only the backend-provided `checkoutUrl` when present.
- Step 4D-4: completed; after checkout opens, the access panel shows a manual **Refresh status** action that asks backend lesson-access/subscription-status endpoints and does not activate Premium locally.
- Step 4D-5: polish copy/layout.
- Step 4D-6: optional bounded polling if needed.

Each implementation slice should keep backend access/status as the authority and should include focused build/manual verification before moving to the next slice.

## 10. Non-goals

- No full checkout UI in this task.
- No backend billing logic changes.
- No local Premium decision.
- No local Premium activation.
- No Paddle API key in desktop.
- No production Paddle secret in desktop.
- No mobile implementation.
- No refunds/chargebacks.
- No reconciliation job.
- No Admin UI changes.
- No database entity changes.
- No EF migrations.
- No smoke script changes.
- No test changes.
- No configuration default changes.

## 11. Verification checklist for future UI implementation

Future checkout/status implementation should continue to verify:

- signed-out user cannot start a normal lesson and sees the access panel;
- signed-in free user with allowance can start a new lesson;
- free allowance used shows the simple access/paywall panel only when backend denies a new lesson;
- trial user is allowed when backend allows access;
- Premium user is allowed when backend allows access;
- checkout unavailable is handled calmly;
- `checkoutUrl` opens only when backend returns it;
- Premium is not activated locally;
- refresh status uses backend;
- existing lesson continuation still works;
- desktop Debug build;
- desktop Release build;
- backend build;
- relevant backend smoke scripts.

## 12. Current status

Step 4D-4 is implemented for manual refresh only: after checkout opens, the desktop can ask backend lesson-access/subscription-status endpoints for current state and update the access panel. Premium still depends on backend state, normally after valid webhook processing; the desktop does not decide payment success, does not activate Premium locally, and does not poll automatically.
