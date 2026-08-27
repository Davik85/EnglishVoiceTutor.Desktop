using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Services.Billing;

internal sealed record GooglePlayInitialPremiumDeferralEvidence(
    string InitialEtag,
    bool IsLicenseTestPurchase);

internal static class GooglePlayTrialDeferralEligibility
{
    private static readonly TimeSpan MinimumMonthlyPeriod = TimeSpan.FromDays(28);
    private static readonly TimeSpan MaximumMonthlyPeriod = TimeSpan.FromDays(31);

    public static GooglePlayInitialPremiumDeferralEvidence? Select(
        GooglePlaySubscriptionV2Snapshot snapshot,
        GooglePlaySubscriptionLineItemSnapshot selected,
        DateTimeOffset startedAtUtc,
        DateTimeOffset expiresAtUtc,
        bool explicitlyAllowedLicenseTestPurchase)
    {
        var initialPeriod = expiresAtUtc - startedAtUtc;
        var allowedLicenseTestPurchase = snapshot.IsTestPurchase && explicitlyAllowedLicenseTestPurchase;
        if (!HasOrdinaryPaidAutoRenewingShape(snapshot, selected, allowedLicenseTestPurchase)
            || (!allowedLicenseTestPurchase
                && (initialPeriod < MinimumMonthlyPeriod || initialPeriod > MaximumMonthlyPeriod)))
        {
            return null;
        }

        return new GooglePlayInitialPremiumDeferralEvidence(snapshot.Etag!, allowedLicenseTestPurchase);
    }

    public static bool HasOrdinaryPaidAutoRenewingShape(
        GooglePlaySubscriptionV2Snapshot snapshot,
        GooglePlaySubscriptionLineItemSnapshot selected,
        bool allowLicenseTestPurchase = false) =>
        string.Equals(snapshot.SubscriptionState, "SUBSCRIPTION_STATE_ACTIVE", StringComparison.Ordinal)
            && (!snapshot.IsTestPurchase || allowLicenseTestPurchase)
            && snapshot.LinkedPurchaseToken is null
            && !string.IsNullOrWhiteSpace(snapshot.Etag)
            && snapshot.LineItems.Count == 1
            && string.Equals(selected.ProductId, SubscriptionConstants.Billing.GooglePlayPremiumProductId, StringComparison.Ordinal)
            && selected.HasAutoRenewingPlan
            && selected.AutoRenewEnabled == true
            && !selected.HasPrepaidPlan
            && string.Equals(selected.BasePlanId, SubscriptionConstants.Billing.GooglePlayPremiumBasePlanId, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(selected.OfferId)
            && selected.OfferPhase == GooglePlaySubscriptionOfferPhase.BasePrice
            && !selected.HasSignupPromotion
            && !selected.HasItemReplacement
            && !selected.HasDeferredItemRemoval
            && string.IsNullOrWhiteSpace(selected.DeferredItemReplacementProductId)
            && selected.HasLatestSuccessfulOrderId;
}
