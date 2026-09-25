namespace AsanRezerve.ServiceCatalog.Domain.Enums
{
    /// <summary>Who runs a promotion. A platform promotion is an admin campaign salons opt into.</summary>
    public enum PromotionOwner
    {
        Platform = 0,
        Provider = 1
    }

    /// <summary>How a customer gets a promotion: on its own, or by typing its code.</summary>
    public enum PromotionActivation
    {
        Automatic = 0,
        Code = 1
    }

    public enum DiscountKind
    {
        Percentage = 0,
        FixedAmount = 1
    }

    /// <summary>What the promotion's manager decided. The state customers experience is <see cref="PromotionState"/>.</summary>
    public enum PromotionStatus
    {
        Active = 0,
        Paused = 1,
        Ended = 2
    }

    /// <summary>
    /// The state a promotion is in at a given instant, derived from <see cref="PromotionStatus"/>, its dates and its
    /// usage. Never stored: "expired" and "exhausted" happen without anyone doing anything.
    /// </summary>
    public enum PromotionState
    {
        Scheduled = 0,
        Active = 1,
        Paused = 2,
        Expired = 3,
        Exhausted = 4,
        Ended = 5
    }

    public enum PromotionRedemptionStatus
    {
        /// <summary>The booking holds this discount and it counts against the promotion's limits.</summary>
        Applied = 0,

        /// <summary>The booking was cancelled; the use went back to the promotion.</summary>
        Released = 1
    }

    /// <summary>What happened to the code a customer typed.</summary>
    public enum PromotionCodeOutcome
    {
        /// <summary>No code was entered.</summary>
        None = 0,
        Applied = 1,
        NotFound = 2,
        NotEligible = 3,

        /// <summary>The code is valid but an automatic offer gives the customer more; that one was applied.</summary>
        BetterOfferApplied = 4
    }
}
