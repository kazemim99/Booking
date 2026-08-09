// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/BookingPolicy.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// Represents the booking and cancellation policy for appointments
    /// </summary>
    public sealed class BookingPolicy : ValueObject
    {
        public BookingPolicy() { }

        /// <summary>
        /// Minimum hours in advance a booking can be made
        /// </summary>
        public int MinAdvanceBookingHours { get; private set; }

        /// <summary>
        /// Maximum days in advance a booking can be made
        /// </summary>
        public int MaxAdvanceBookingDays { get; private set; }

        /// <summary>
        /// Minimum hours before appointment when cancellation is allowed without penalty
        /// </summary>
        public int CancellationWindowHours { get; private set; }

        /// <summary>
        /// Percentage of deposit to forfeit if cancelled outside window (0-100)
        /// </summary>
        public decimal CancellationFeePercentage { get; private set; }

        /// <summary>
        /// Whether rescheduling is allowed
        /// </summary>
        public bool AllowRescheduling { get; private set; }

        /// <summary>
        /// Minimum hours before appointment when rescheduling is allowed
        /// </summary>
        public int RescheduleWindowHours { get; private set; }

        /// <summary>
        /// Whether deposit is required at booking time
        /// </summary>
        public bool RequireDeposit { get; private set; }

        /// <summary>
        /// Deposit percentage required at booking (0-100). Meaningful when <see cref="DepositType"/> is
        /// <see cref="Enums.DepositType.Percentage"/>.
        /// </summary>
        public decimal DepositPercentage { get; private set; }

        /// <summary>
        /// How the deposit is calculated — a percentage of the total, or a flat amount.
        /// Defaults to <see cref="Enums.DepositType.Percentage"/> so policies persisted before fixed-amount
        /// deposits existed keep their original meaning.
        /// </summary>
        public DepositType DepositType { get; private set; }

        /// <summary>
        /// Flat deposit amount required at booking. Meaningful when <see cref="DepositType"/> is
        /// <see cref="Enums.DepositType.FixedAmount"/>.
        /// </summary>
        public decimal DepositFixedAmount { get; private set; }

        private BookingPolicy(
            int minAdvanceBookingHours,
            int maxAdvanceBookingDays,
            int cancellationWindowHours,
            decimal cancellationFeePercentage,
            bool allowRescheduling,
            int rescheduleWindowHours,
            bool requireDeposit,
            decimal depositPercentage,
            DepositType depositType,
            decimal depositFixedAmount)
        {
            if (minAdvanceBookingHours < 0)
                throw new ArgumentException("Minimum advance booking hours cannot be negative", nameof(minAdvanceBookingHours));

            if (maxAdvanceBookingDays < 1)
                throw new ArgumentException("Maximum advance booking days must be at least 1", nameof(maxAdvanceBookingDays));

            if (cancellationWindowHours < 0)
                throw new ArgumentException("Cancellation window hours cannot be negative", nameof(cancellationWindowHours));

            if (cancellationFeePercentage < 0 || cancellationFeePercentage > 100)
                throw new ArgumentException("Cancellation fee percentage must be between 0 and 100", nameof(cancellationFeePercentage));

            if (rescheduleWindowHours < 0)
                throw new ArgumentException("Reschedule window hours cannot be negative", nameof(rescheduleWindowHours));

            if (depositPercentage < 0 || depositPercentage > 100)
                throw new ArgumentException("Deposit percentage must be between 0 and 100", nameof(depositPercentage));

            if (depositFixedAmount < 0)
                throw new ArgumentException("Deposit fixed amount cannot be negative", nameof(depositFixedAmount));

            // A required deposit must actually be collectable: the selected mode has to carry a usable value.
            // Rejecting the invalid combination here prevents a provider from "requiring" a deposit of zero, which
            // would leave bookings permanently unconfirmable while appearing to be configured.
            if (requireDeposit)
            {
                if (depositType == DepositType.Percentage && depositPercentage <= 0)
                    throw new ArgumentException(
                        "A percentage deposit requires a deposit percentage greater than 0", nameof(depositPercentage));

                if (depositType == DepositType.FixedAmount && depositFixedAmount <= 0)
                    throw new ArgumentException(
                        "A fixed-amount deposit requires a deposit amount greater than 0", nameof(depositFixedAmount));
            }

            MinAdvanceBookingHours = minAdvanceBookingHours;
            MaxAdvanceBookingDays = maxAdvanceBookingDays;
            CancellationWindowHours = cancellationWindowHours;
            CancellationFeePercentage = cancellationFeePercentage;
            AllowRescheduling = allowRescheduling;
            RescheduleWindowHours = rescheduleWindowHours;
            RequireDeposit = requireDeposit;
            DepositPercentage = depositPercentage;
            DepositType = depositType;
            DepositFixedAmount = depositFixedAmount;
        }

        /// <summary>
        /// Copies <paramref name="source"/>'s values into this instance.
        ///
        /// <para><b>Why this exists (EF Core owned-entity semantics).</b> This value object is persisted with
        /// <c>OwnsOne</c>, and EF tracks an owned reference by its <i>parent's</i> key. Assigning a brand-new
        /// instance to the navigation therefore does not update the already-tracked owned entry: EF keeps the entry
        /// it is tracking (with the old column values) and the replacement object is ignored, so the change is
        /// silently dropped on save. Mutating the tracked instance instead produces ordinary property
        /// modifications, which EF detects reliably.</para>
        ///
        /// <para>The value object stays immutable to callers — setters are private and this method is
        /// <c>internal</c>, reserved for the owning aggregate. <c>ComplexProperty</c> would be the cleaner mapping
        /// (true value semantics, replacement just works) but EF Core 9 does not support <b>nullable</b> complex
        /// properties, and an unconfigured policy must be null.</para>
        /// </summary>
        internal void CopyFrom(BookingPolicy source)
        {
            ArgumentNullException.ThrowIfNull(source);

            MinAdvanceBookingHours = source.MinAdvanceBookingHours;
            MaxAdvanceBookingDays = source.MaxAdvanceBookingDays;
            CancellationWindowHours = source.CancellationWindowHours;
            CancellationFeePercentage = source.CancellationFeePercentage;
            AllowRescheduling = source.AllowRescheduling;
            RescheduleWindowHours = source.RescheduleWindowHours;
            RequireDeposit = source.RequireDeposit;
            DepositPercentage = source.DepositPercentage;
            DepositType = source.DepositType;
            DepositFixedAmount = source.DepositFixedAmount;
        }

        public static BookingPolicy Create(
            int minAdvanceBookingHours,
            int maxAdvanceBookingDays,
            int cancellationWindowHours,
            decimal cancellationFeePercentage,
            bool allowRescheduling,
            int rescheduleWindowHours,
            bool requireDeposit,
            decimal depositPercentage,
            DepositType depositType = DepositType.Percentage,
            decimal depositFixedAmount = 0)
        {
            return new BookingPolicy(
                minAdvanceBookingHours,
                maxAdvanceBookingDays,
                cancellationWindowHours,
                cancellationFeePercentage,
                allowRescheduling,
                rescheduleWindowHours,
                requireDeposit,
                depositPercentage,
                depositType,
                depositFixedAmount);
        }

        /// <summary>
        /// Default policy: 2 hours minimum advance, 90 days max, 24 hours cancellation window, 50% fee
        /// </summary>
        public static BookingPolicy Default => Create(
            minAdvanceBookingHours: 2,
            maxAdvanceBookingDays: 90,
            cancellationWindowHours: 24,
            cancellationFeePercentage: 50,
            allowRescheduling: true,
            rescheduleWindowHours: 24,
            // No payment flow exists in the product yet: a deposit-requiring
            // default made every default-policy booking permanently
            // unconfirmable (BOOKING_DEPOSIT_NOT_PAID). Services that
            // explicitly configure a deposit policy still enforce it.
            requireDeposit: false,
            depositPercentage: 0);

        /// <summary>
        /// Flexible policy: 1 hour minimum, 60 days max, 12 hours cancellation, 25% fee, no deposit
        /// </summary>
        public static BookingPolicy Flexible => Create(
            minAdvanceBookingHours: 1,
            maxAdvanceBookingDays: 60,
            cancellationWindowHours: 12,
            cancellationFeePercentage: 25,
            allowRescheduling: true,
            rescheduleWindowHours: 12,
            requireDeposit: false,
            depositPercentage: 0);

        /// <summary>
        /// Strict policy: 24 hours minimum, 180 days max, 72 hours cancellation, 100% fee
        /// </summary>
        public static BookingPolicy Strict => Create(
            minAdvanceBookingHours: 24,
            maxAdvanceBookingDays: 180,
            cancellationWindowHours: 72,
            cancellationFeePercentage: 100,
            allowRescheduling: true,
            rescheduleWindowHours: 72,
            requireDeposit: true,
            depositPercentage: 50);

        /// <summary>
        /// Calculates the deposit due for a booking of <paramref name="totalPrice"/>.
        ///
        /// <para>Percentage mode takes the configured share of the total; fixed mode takes the flat amount. In both
        /// cases the result is <b>capped at the booking total</b> — a deposit can never exceed what is being booked,
        /// which matters most for a flat amount configured against a cheaper service.</para>
        /// </summary>
        public Money CalculateDepositAmount(Money totalPrice)
        {
            if (!RequireDeposit)
                return Money.Create(0, totalPrice.Currency);

            var depositAmount = DepositType == DepositType.FixedAmount
                ? DepositFixedAmount
                : totalPrice.Amount * (DepositPercentage / 100m);

            // Never ask for more than the booking is worth.
            if (depositAmount > totalPrice.Amount)
                depositAmount = totalPrice.Amount;

            return Money.Create(depositAmount, totalPrice.Currency);
        }

        /// <summary>
        /// Calculates the cancellation fee based on the policy and total price
        /// </summary>
        public Money CalculateCancellationFee(Money totalPrice)
        {
            var feeAmount = totalPrice.Amount * (CancellationFeePercentage / 100m);
            return Money.Create(feeAmount, totalPrice.Currency);
        }

        /// <summary>
        /// Checks if cancellation is allowed at a given time before the booking
        /// </summary>
        public bool CanCancelWithoutFee(DateTime bookingStartTime, DateTime currentTime)
        {
            var hoursUntilBooking = (bookingStartTime - currentTime).TotalHours;
            return hoursUntilBooking >= CancellationWindowHours;
        }

        /// <summary>
        /// Checks if rescheduling is allowed at a given time before the booking
        /// </summary>
        public bool CanReschedule(DateTime bookingStartTime, DateTime currentTime)
        {
            if (!AllowRescheduling)
                return false;

            var hoursUntilBooking = (bookingStartTime - currentTime).TotalHours;
            return hoursUntilBooking >= RescheduleWindowHours;
        }

        /// <summary>
        /// Checks if a booking can be made at a given time in the future
        /// </summary>
        public bool IsWithinBookingWindow(DateTime bookingStartTime, DateTime currentTime)
        {
            var hoursUntilBooking = (bookingStartTime - currentTime).TotalHours;
            var daysUntilBooking = (bookingStartTime - currentTime).TotalDays;

            return hoursUntilBooking >= MinAdvanceBookingHours && daysUntilBooking <= MaxAdvanceBookingDays;
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return MinAdvanceBookingHours;
            yield return MaxAdvanceBookingDays;
            yield return CancellationWindowHours;
            yield return CancellationFeePercentage;
            yield return AllowRescheduling;
            yield return RescheduleWindowHours;
            yield return RequireDeposit;
            yield return DepositPercentage;
            yield return DepositType;
            yield return DepositFixedAmount;
        }
    }
}
