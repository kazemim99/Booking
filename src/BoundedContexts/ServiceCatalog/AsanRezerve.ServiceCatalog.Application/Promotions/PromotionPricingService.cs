using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.Repositories;

namespace AsanRezerve.ServiceCatalog.Application.Promotions
{
    /// <summary>
    /// Prices a visit with the promotions in force at a salon and keeps each booking's redemption in step with the
    /// booking (openspec/changes/add-discounts-and-campaigns, design D4/D8).
    ///
    /// <para>The quote endpoint and booking creation both call <see cref="PriceAsync"/>, so they cannot disagree for
    /// the same inputs at the same instant. Everything here runs on the caller's unit of work: the redemption, the
    /// promotion's counter and the booking commit together or not at all.</para>
    /// </summary>
    public interface IPromotionPricingService
    {
        Task<PromotionPricingResult> PriceAsync(PricingRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records that <paramref name="bookingId"/> holds the discount <paramref name="pricing"/> chose. Returns what
        /// the booking should snapshot, or null when no promotion applied.
        /// </summary>
        Task<AppliedDiscount?> RedeemAsync(PromotionPricingResult pricing, Guid bookingId, CancellationToken cancellationToken = default);

        /// <summary>The booking was cancelled: its use goes back to the promotion. No-op without a redemption.</summary>
        Task ReleaseForBookingAsync(Guid bookingId, DateTime nowUtc, CancellationToken cancellationToken = default);

        /// <summary>The booking was rescheduled: its redemption now belongs to the successor.</summary>
        Task TransferAsync(Guid fromBookingId, Guid toBookingId, CancellationToken cancellationToken = default);
    }

    /// <param name="AppointmentStart">The salon's wall-clock start of the visit.</param>
    /// <param name="NowUtc">The instant of booking.</param>
    public sealed record PricingRequest(
        ProviderId ProviderId,
        IReadOnlyList<PricedLine> Lines,
        string Currency,
        DateTime AppointmentStart,
        Guid CustomerId,
        string? Code,
        DateTime NowUtc);

    public sealed record PromotionPricingResult(
        PricingRequest Request,
        PriceQuote Quote,
        IReadOnlyDictionary<Guid, int> CustomerPriorUses);

    /// <summary>
    /// The discount a visit was priced with went away before the booking was saved (last use taken, paused). A 409
    /// with its own code so a client can tell it from a taken slot: the time is still free, only the price changed.
    /// </summary>
    public sealed class PromotionUnavailableException : ConflictException
    {
        public override string ErrorCode => "PROMOTION_UNAVAILABLE";

        public PromotionUnavailableException(string message) : base(message) { }
    }

    public sealed class PromotionPricingService : IPromotionPricingService
    {
        private readonly IPromotionRepository _promotions;
        private readonly IPromotionRedemptionRepository _redemptions;

        public PromotionPricingService(IPromotionRepository promotions, IPromotionRedemptionRepository redemptions)
        {
            _promotions = promotions;
            _redemptions = redemptions;
        }

        public async Task<PromotionPricingResult> PriceAsync(PricingRequest request, CancellationToken cancellationToken = default)
        {
            var candidates = await _promotions.GetPricingCandidatesAsync(request.ProviderId, request.NowUtc, cancellationToken);

            var ids = candidates.Select(c => c.Promotion.Id).ToList();
            var priorUses = await _redemptions.AppliedCountsForCustomerAsync(request.CustomerId, ids, cancellationToken);

            // Only ask the database when some promotion cares.
            var isNewCustomer = !candidates.Any(c => c.Promotion.NewCustomersOnly)
                || !await _promotions.HasPriorBookingAsync(request.CustomerId, request.ProviderId, cancellationToken);

            var context = new PromotionContext(request.Lines, request.AppointmentStart, request.NowUtc, isNewCustomer);
            var quote = PromotionPricing.Quote(
                context,
                request.Currency,
                candidates.Select(c => new PromotionCandidate(
                    c.Promotion, priorUses.GetValueOrDefault(c.Promotion.Id), c.IsEnrolled)),
                request.Code);

            return new PromotionPricingResult(request, quote, priorUses);
        }

        public async Task<AppliedDiscount?> RedeemAsync(
            PromotionPricingResult pricing, Guid bookingId, CancellationToken cancellationToken = default)
        {
            var applied = pricing.Quote.Applied;
            if (applied is null)
                return null;

            // The tracked row: its RedemptionCount is the concurrency token, so a race for the last use is lost here
            // by one of the two requests rather than won by both.
            var promotion = await _promotions.GetAsync(applied.PromotionId, cancellationToken)
                ?? throw new PromotionUnavailableException("این تخفیف دیگر در دسترس نیست؛ لطفاً دوباره تلاش کنید.");

            var request = pricing.Request;
            try
            {
                promotion.RecordRedemption(pricing.CustomerPriorUses.GetValueOrDefault(promotion.Id), request.NowUtc);
            }
            catch (BusinessRuleViolationException)
            {
                // Another booking took the last use (or the salon paused it) between pricing and now. A conflict, not
                // a bad request: retried, the visit is priced without it — and the customer sees the new price first.
                throw new PromotionUnavailableException(
                    "ظرفیت این تخفیف همین حالا تکمیل شد یا دیگر فعال نیست؛ لطفاً دوباره تلاش کنید تا قیمت جدید را ببینید.");
            }

            await _redemptions.AddAsync(
                PromotionRedemption.Apply(
                    promotion, bookingId, request.CustomerId, request.ProviderId.Value, applied.Amount,
                    request.Currency, request.NowUtc),
                cancellationToken);

            return applied;
        }

        public async Task ReleaseForBookingAsync(Guid bookingId, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            var redemption = await _redemptions.GetAppliedForBookingAsync(bookingId, cancellationToken);
            if (redemption is null || !redemption.Release(nowUtc))
                return;

            var promotion = await _promotions.GetAsync(redemption.PromotionId, cancellationToken);
            promotion?.ReleaseRedemption();
        }

        public async Task TransferAsync(Guid fromBookingId, Guid toBookingId, CancellationToken cancellationToken = default)
        {
            var redemption = await _redemptions.GetAppliedForBookingAsync(fromBookingId, cancellationToken);
            redemption?.TransferTo(toBookingId);
        }
    }
}
