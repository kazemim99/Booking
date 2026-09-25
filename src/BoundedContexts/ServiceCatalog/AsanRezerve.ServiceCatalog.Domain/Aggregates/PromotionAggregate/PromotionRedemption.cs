using AsanRezerve.Core.Domain.Exceptions;

namespace AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate
{
    /// <summary>
    /// One booking holding one promotion's discount. The per-customer limit counts these, the stats sum them.
    ///
    /// <para>A booking holds at most one (unique index on <see cref="BookingId"/>). Cancelling the booking releases it
    /// and the use returns to the promotion (decision 2026-09-25); a no-show or a completed visit keeps it. A reschedule
    /// moves it to the successor booking, which keeps the same price.</para>
    /// </summary>
    public sealed class PromotionRedemption : AggregateRoot<Guid>
    {
        public Guid PromotionId { get; private set; }
        public Guid BookingId { get; private set; }
        public Guid CustomerId { get; private set; }
        public Guid ProviderId { get; private set; }
        public decimal Amount { get; private set; }
        public string Currency { get; private set; } = PlatformCurrency.Code;
        public PromotionRedemptionStatus Status { get; private set; }
        public DateTime RedeemedAt { get; private set; }
        public DateTime? ReleasedAt { get; private set; }

        private PromotionRedemption()
        {
        }

        public static PromotionRedemption Apply(
            Promotion promotion,
            Guid bookingId,
            Guid customerId,
            Guid providerId,
            decimal amount,
            string currency,
            DateTime nowUtc)
        {
            if (amount <= 0)
                throw new DomainValidationException(nameof(Amount), "مبلغ تخفیف باید بیشتر از صفر باشد.");

            return new PromotionRedemption
            {
                Id = Guid.NewGuid(),
                PromotionId = promotion.Id,
                BookingId = bookingId,
                CustomerId = customerId,
                ProviderId = providerId,
                Amount = amount,
                Currency = currency,
                Status = PromotionRedemptionStatus.Applied,
                RedeemedAt = nowUtc,
            };
        }

        /// <summary>Returns the use to the promotion. True only the first time.</summary>
        public bool Release(DateTime nowUtc)
        {
            if (Status == PromotionRedemptionStatus.Released)
                return false;

            Status = PromotionRedemptionStatus.Released;
            ReleasedAt = nowUtc;
            return true;
        }

        public void TransferTo(Guid successorBookingId) => BookingId = successorBookingId;
    }
}
