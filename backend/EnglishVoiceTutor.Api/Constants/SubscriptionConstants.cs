namespace EnglishVoiceTutor.Api.Constants;

public static class SubscriptionConstants
{
    public static class Plans
    {
        public const string FreePlanId = "free";
        public const string PremiumPlanId = "premium";
        public const string TrialPlanId = "trial";
        public const string FreePlanName = "Free";
        public const string PremiumPlanName = "Premium";
        public const string TrialPlanName = "Trial";
        public const string FreeTier = "free";
        public const string PremiumTier = "premium";
    }

    public static class SubscriptionStatuses
    {
        public const string None = "none";
        public const string Active = "active";
        public const string Trialing = "trialing";
        public const string PastDue = "past_due";
        public const string Canceled = "canceled";
        public const string Expired = "expired";
        public const string Paused = "paused";
        public const string Unknown = "unknown";
    }


    public static class RenewalStatuses
    {
        public const string NoPaidSubscription = "no_paid_subscription";
        public const string RenewalActive = "renewal_active";
        public const string CancellationScheduled = "cancellation_scheduled";
        public const string SubscriptionCanceled = "subscription_canceled";
        public const string SubscriptionPaused = "subscription_paused";
        public const string PastDue = "past_due";
        public const string Unknown = "unknown";
    }

    public static class NextRenewalStates
    {
        public const string NotApplicable = "not_applicable";
        public const string RenewalExpected = "renewal_expected";
        public const string NoRenewalScheduled = "no_renewal_scheduled";
        public const string Unknown = "unknown";
    }

    public static class CancellationExplanationCodes
    {
        public const string None = "none";
        public const string NoPaidProviderSubscription = "no_paid_provider_subscription";
        public const string AlreadyScheduled = "already_scheduled";
        public const string ProviderSubscriptionMissing = "provider_subscription_missing";
        public const string Canceled = "canceled";
        public const string Paused = "paused";
        public const string PastDue = "past_due";
        public const string Unknown = "unknown";
    }

    public static class BillingProviders
    {
        public const string None = "none";
        public const string Paddle = "paddle";
        public const string AppleAppStore = "apple_app_store";
        public const string GooglePlay = "google_play";
        public const string Manual = "manual";
        public const string InternalTrial = "internal_trial";
    }

    public static class Entitlements
    {
        public const string PremiumAccessType = "premium_access";
        public const string SourceSubscription = "subscription";
        public const string SourceTrial = "trial";
        public const string SourceManualAdmin = "manual_admin";
        public const string SourceProviderEvent = "provider_event";
        public const string StatusActive = "active";
        public const string StatusInactive = "inactive";
        public const string StatusRevoked = "revoked";
        public const string StatusExpired = "expired";
    }

    public static class BillingEventStatuses
    {
        public const string Received = "received";
        public const string Normalized = "normalized";
        public const string Processed = "processed";
        public const string Failed = "failed";
        public const string NormalizationFailed = "normalization_failed";
        public const string Ignored = "ignored";
        public const string ReconciliationPending = "reconciliation_pending";
        public const string ReconciliationBlocked = "reconciliation_blocked";
    }

    public static class BillingEventTypes
    {
        public const string TransactionCompleted = "transaction.completed";
        public const string TransactionPaymentFailed = "transaction.payment_failed";
        public const string SubscriptionCreated = "subscription.created";
        public const string SubscriptionUpdated = "subscription.updated";
        public const string SubscriptionPastDue = "subscription.past_due";
        public const string SubscriptionCanceled = "subscription.canceled";
        public const string SubscriptionPaused = "subscription.paused";
        public const string SubscriptionResumed = "subscription.resumed";
        public const string SubscriptionActivated = "subscription.activated";
    }

    public static class ScheduledChangeActions
    {
        public const string Cancel = "cancel";
    }

    public static class PaymentStatuses
    {
        public const string Pending = "pending";
        public const string Billed = "billed";
        public const string Paid = "paid";
        public const string Completed = "completed";
        public const string Failed = "failed";
        public const string PastDue = "past_due";
        public const string Canceled = "canceled";
        public const string Refunded = "refunded";
        public const string Unknown = "unknown";
    }

    public static class PaymentPersistence
    {
        public const string MissingInternalUserIdMessage = "Missing internal user id for payment persistence.";
        public const string InvalidInternalUserIdMessage = "Invalid internal user id for payment persistence.";
        public const string UserNotFoundMessage = "User not found for payment persistence.";
        public const string UnsupportedPlanIdMessage = "Unsupported plan id for payment persistence.";
        public const string MissingProviderTransactionIdMessage = "Missing provider transaction id for payment persistence.";
        public const string InvalidBillingEventMetadataMessage = "Invalid billing event metadata for payment persistence.";
        public const string UnsupportedBillingEventTypeMessage = "Unsupported billing event type for payment persistence.";
    }

    public static class BillingEventReconciliation
    {
        public const string UnsupportedBillingEventTypeMessage = "Unsupported billing event type for entitlement reconciliation.";
        public const string MissingInternalUserIdMessage = "Missing internal user id for entitlement reconciliation.";
        public const string UnsupportedPlanIdMessage = "Unsupported plan id for entitlement reconciliation.";
        public const string UnsupportedPriceIdMessage = "Unsupported Paddle price id for entitlement reconciliation.";
        public const string UnsupportedProductIdMessage = "Unsupported Paddle product id for entitlement reconciliation.";
        public const string UnsupportedCustomDataMessage = "Unsupported Paddle custom_data marker for entitlement reconciliation.";
        public const string InvalidBillingEventMetadataMessage = "Invalid billing event metadata.";
    }

    public static class BillingEventActivation
    {
        public const string MissingBillingPeriodEndMessage = "Missing billing period end for entitlement activation.";
        public const string BillingPeriodEndNotFutureMessage = "Billing period end is not in the future.";
        public const string UserNotFoundMessage = "User not found for entitlement activation.";
        public const string UnsupportedPlanIdMessage = "Unsupported plan id for entitlement activation.";
        public const string ActivatedReason = "Entitlement activated from provider billing event.";
        public const string InvalidBillingEventMetadataMessage = "Invalid billing event metadata for entitlement activation.";
        public const string MissingInternalUserIdMessage = "Missing internal user id for entitlement activation.";
        public const string InvalidInternalUserIdMessage = "Invalid internal user id for entitlement activation.";
        public const string UnexpectedProcessingErrorMessage = "Unexpected entitlement activation processing error.";
    }


    public static class SubscriptionLifecycleSnapshot
    {
        public const string MissingInternalUserIdMessage = "Missing internal user id for subscription lifecycle snapshot.";
        public const string InvalidInternalUserIdMessage = "Invalid internal user id for subscription lifecycle snapshot.";
        public const string UserNotFoundMessage = "User not found for subscription lifecycle snapshot.";
        public const string MissingProviderSubscriptionIdMessage = "Missing provider subscription id for subscription lifecycle snapshot.";
        public const string InvalidBillingEventMetadataMessage = "Invalid billing event metadata for subscription lifecycle snapshot.";
        public const string OlderProviderEventIgnoredMessage = "Older subscription lifecycle provider event ignored.";
        public const string UnexpectedProcessingErrorMessage = "Unexpected subscription lifecycle snapshot processing error.";
        public const string ExpiredProviderEventEntitlementReason = "Provider-event Premium entitlement expired by subscription lifecycle event.";
    }

    public static class Billing
    {
        public const string BillingProviderNotConfiguredCode = "billing_provider_not_configured";
        public const string InvalidBillingCheckoutRequestCode = "invalid_billing_checkout_request";
        public const string BillingCheckoutDisabledMessage = "Billing checkout is not configured yet.";
        public const string PaddleCheckoutNotConfiguredCode = "paddle_checkout_not_configured";
        public const string PaddleCheckoutNotConfiguredMessage = "Paddle checkout is not configured yet.";
        public const string PaddleCheckoutAdapterDisabledMessage = "Paddle checkout adapter is disabled.";
        public const string PaddleCheckoutFailedCode = "paddle_checkout_failed";
        public const string PaddleCheckoutFailedMessage = "Unable to create Paddle checkout session.";
        public const string PaddleCheckoutUrlMissingCode = "paddle_checkout_url_missing";
        public const string PaddleCheckoutUrlMissingMessage = "Paddle checkout URL was not returned.";
        public const string PaddleCheckoutUrlUnavailableCode = "paddle_checkout_url_unavailable";
        public const string PaddleCheckoutUrlUnavailableMessage = "Paddle checkout is temporarily unavailable.";
        public const string PaddleCheckoutCreatedMessage = "Checkout session created.";
        public const string PaddleCheckoutClientSideTokenMissingMessage = "Paddle client-side checkout token is not configured.";
        public const string PaddleApiVersionHeaderName = "Paddle-Version";
        public const string PaddleApiVersion = "1";
        public const string PaddleCheckoutSourceDesktopBackend = "desktop_backend";
        public const string PaddleLegacyCheckoutTransactionIdParameterName = "_ptxn";
        public const string PaddleCheckoutLaunchTransactionIdParameterName = "transactionId";
        public const string DefaultPaddleEnvironment = "sandbox";
        public const string LivePaddleEnvironment = "live";
        public const string PlanIdRequiredMessage = "PlanId is required.";
        public const string UnsupportedPlanIdMessage = "Unsupported plan id.";
        public const string DefaultCheckoutCurrency = "USD";
        public const string DefaultPremiumPlanId = Plans.PremiumPlanId;
        public const string CheckoutModeSubscription = "subscription";
    }

    public static class AdminActionTypes
    {
        public const string GrantPremium = "grant_premium";
        public const string RevokePremium = "revoke_premium";
        public const string ResetFreeLesson = "reset_free_lesson";
        public const string ForcePlanRefresh = "force_plan_refresh";
    }


    public static class Diagnostics
    {
        public const string ScenarioReset = "reset";
        public const string ScenarioActivePremiumEntitlement = "active_premium_entitlement";
        public const string ScenarioActiveTrialGrant = "active_trial_grant";
        public const string ScenarioExpiredPremiumEntitlement = "expired_premium_entitlement";
        public const string ScenarioExpiredTrialGrant = "expired_trial_grant";
        public const string ScenarioDailyFreeLessonConsumed = "daily_free_lesson_consumed";

        public const string Reason = "Development subscription diagnostics scenario.";
        public const string SourcePlatform = "development_diagnostics";

        public const string LessonContentId = "dev_subscription_diagnostics_lesson";
        public const string TopicId = "dev_subscription_diagnostics_topic";
        public const string TopicTitle = "Subscription Diagnostics";
        public const string SubtopicId = "dev_subscription_diagnostics_subtopic";
        public const string SubtopicTitle = "Daily Free Lesson Consumed";
        public const string Level = "A1";
        public const string ContextId = "dev_subscription_diagnostics_context";
        public const string ContextTitle = "Development Diagnostics";
        public const string Mode = "chat";
        public const string SessionStatus = "active";

        public const int ActivePremiumStartOffsetMinutes = 5;
        public const int ActivePremiumDurationDays = 30;
        public const int ExpiredPremiumStartOffsetDays = 40;
        public const int ExpiredOffsetDays = 1;
        public const int ExpiredTrialGrantOffsetDays = 10;
    }


    public static class LessonAccessSources
    {
        public const string Authenticated = "authenticated";
        public const string Development = "development";
    }

    public static class LessonAccessDecisions
    {
        public const string AllowedPremium = "allowed_premium";
        public const string AllowedTrial = "allowed_trial";
        public const string AllowedFreeRemaining = "allowed_free_remaining";
        public const string AllowedEnforcementDisabled = "allowed_enforcement_disabled";
        public const string BlockedFreeLimitUsed = "blocked_free_limit_used";
        public const string LessonAccessDeniedError = "lesson_access_denied";
    }

    public static class LessonAccessReasons
    {
        public const string PremiumActive = "Premium access is active.";
        public const string TrialActive = "Trial access is active.";
        public const string FreeLessonRemaining = "A free lesson is still available today.";
        public const string FreeLimitUsedButEnforcementDisabled = "Free lesson has already been used today, but enforcement is disabled.";
        public const string FreeLimitUsed = "Free lesson has already been used today.";
    }

    public const string AccountTrialClaimSource = "account_trial_claim";
    public const string AccountRegistrationTrialSource = "account_registration";
    public const string TrialClaimedSuccessMessage = "Trial claimed successfully.";
    public const string TrialAlreadyClaimedMessage = "Trial has already been claimed for this account.";
    public const string DevelopmentTestAccountPremiumSource = "development_test_account";
    public const string DevelopmentTestAccountPremiumReason = "Development test account unlimited Premium access.";

    public const int PremiumTrialDays = 7;
    public const int FreeLessonsPerDay = 1;
    public const int FreeLessonConsumptionMessageThreshold = 3;
    public const bool EnforcementEnabled = false;
    public const string FreeLessonConsumptionRule = "A free lesson is consumed after a lesson session is started and the learner sends at least 3 user messages in that lesson session.";
}
