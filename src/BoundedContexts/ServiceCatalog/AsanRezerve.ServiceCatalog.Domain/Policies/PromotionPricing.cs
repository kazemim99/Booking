using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;

namespace AsanRezerve.ServiceCatalog.Domain.Policies
{
    /// <summary>
    /// Chooses the one discount a visit gets and prices it (openspec/changes/add-discounts-and-campaigns, D5/D6).
    ///
    /// <para><b>At most one discount, the best one</b> (decision 2026-09-25). Every eligible automatic promotion is
    /// evaluated and the largest discount wins; ties go to the salon's own promotion, then to the one ending first,
    /// then to the id, so the answer never depends on load order. A code the customer typed replaces that winner only
    /// when it is strictly larger — otherwise the customer is told a better offer is already applied.</para>
    ///
    /// <para>Pure and deterministic: the clock and everything about the customer arrive in the arguments, so the quote
    /// endpoint and booking creation reach the same answer for the same inputs.</para>
    /// </summary>
    public static class PromotionPricing
    {
        /// <summary>No discount takes off more than this share of what it applies to, so no booking is free.</summary>
        public const decimal MaxDiscountShare = 0.9m;

        public const string CodeAppliedMessage = "کد تخفیف اعمال شد.";
        public const string CodeNotFoundMessage = "کد تخفیف معتبر نیست.";
        public const string BetterOfferMessage = "تخفیف بهتری روی این نوبت اعمال شده است.";
        public const string NotEnrolledMessage = "این کمپین در این سالن فعال نیست.";

        public static PriceQuote Quote(
            PromotionContext context,
            string currency,
            IEnumerable<PromotionCandidate> candidates,
            string? enteredCode)
        {
            var subtotal = context.Subtotal;
            var all = candidates.ToList();

            var bestAutomatic = all
                .Where(c => c.Promotion.Activation == PromotionActivation.Automatic && c.IsEnrolled)
                .Select(c => (c.Promotion, Evaluation: c.Promotion.Evaluate(context, c.CustomerPriorUses)))
                .Where(x => x.Evaluation.IsEligible)
                .OrderByDescending(x => x.Evaluation.Discount)
                .ThenBy(x => x.Promotion.Owner == PromotionOwner.Provider ? 0 : 1)
                .ThenBy(x => x.Promotion.EndsAt ?? DateTime.MaxValue)
                .ThenBy(x => x.Promotion.Id)
                .Select(x => ((Promotion, PromotionEvaluation)?)x)
                .FirstOrDefault();

            var code = Promotion.NormalizeCode(enteredCode);
            if (code is null)
                return Build(subtotal, currency, bestAutomatic, PromotionCodeOutcome.None, null);

            var matching = all
                .Where(c => c.Promotion.Activation == PromotionActivation.Code && c.Promotion.Code == code)
                .ToList();
            if (matching.Count == 0)
                return Build(subtotal, currency, bestAutomatic, PromotionCodeOutcome.NotFound, CodeNotFoundMessage);

            var evaluated = matching
                .Select(c => (c.Promotion, Evaluation: c.IsEnrolled
                    ? c.Promotion.Evaluate(context, c.CustomerPriorUses)
                    : PromotionEvaluation.NotEligible(NotEnrolledMessage)))
                .ToList();

            var bestCode = evaluated
                .Where(x => x.Evaluation.IsEligible)
                .OrderByDescending(x => x.Evaluation.Discount)
                .ThenBy(x => x.Promotion.Owner == PromotionOwner.Provider ? 0 : 1)
                .ThenBy(x => x.Promotion.Id)
                .Select(x => ((Promotion, PromotionEvaluation)?)x)
                .FirstOrDefault();

            if (bestCode is null)
                return Build(subtotal, currency, bestAutomatic, PromotionCodeOutcome.NotEligible,
                    evaluated[0].Evaluation.Reason);

            if (bestAutomatic is not null && bestAutomatic.Value.Item2.Discount >= bestCode.Value.Item2.Discount)
                return Build(subtotal, currency, bestAutomatic, PromotionCodeOutcome.BetterOfferApplied, BetterOfferMessage);

            return Build(subtotal, currency, bestCode, PromotionCodeOutcome.Applied, CodeAppliedMessage);
        }

        private static PriceQuote Build(
            decimal subtotal,
            string currency,
            (Promotion Promotion, PromotionEvaluation Evaluation)? winner,
            PromotionCodeOutcome outcome,
            string? message)
        {
            if (winner is null)
                return new PriceQuote(subtotal, 0m, subtotal, currency, null, outcome, message);

            var (promotion, evaluation) = winner.Value;
            var applied = new AppliedDiscount(
                promotion.Id, promotion.Title, promotion.Code, promotion.Owner, evaluation.Discount);
            return new PriceQuote(subtotal, evaluation.Discount, subtotal - evaluation.Discount, currency, applied,
                outcome, message);
        }
    }

    /// <summary>A promotion that may apply to a visit, with what is known about this customer and this salon.</summary>
    /// <param name="CustomerPriorUses">Bookings this customer currently holds with this promotion.</param>
    /// <param name="IsEnrolled">For a platform campaign: whether the salon joined it. Always true for a salon's own.</param>
    public sealed record PromotionCandidate(Promotion Promotion, int CustomerPriorUses, bool IsEnrolled = true);

    public sealed record PriceQuote(
        decimal Subtotal,
        decimal Discount,
        decimal Total,
        string Currency,
        AppliedDiscount? Applied,
        PromotionCodeOutcome CodeOutcome,
        string? CodeMessage);
}
