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
        public int MinAdvanceBookingHours { get; }

        /// <summary>
        /// Maximum days in advance a booking can be made
        /// </summary>
        public int MaxAdvanceBookingDays { get; }

        /// <summary>
        /// Minimum hours before appointment when cancellation is allowed without penalty
        /// </summary>
        public int CancellationWindowHours { get; }

        /// <summary>
        /// Percentage of deposit to forfeit if cancelled outside window (0-100)
        /// </summary>
        public decimal CancellationFeePercentage { get; }

        /// <summary>
        /// Whether rescheduling is allowed
        /// </summary>
        public bool AllowRescheduling { get; }

        /// <summary>
        /// Minimum hours before appointment when rescheduling is allowed
        /// </summary>
        public int RescheduleWindowHours { get; }

        /// <summary>
        /// Whether deposit is required at booking time
        /// </summary>
        public bool RequireDeposit { get; }

        /// <summary>
        /// Deposit percentage required at booking (0-100)
        /// </summary>
        public decimal DepositPercentage { get; }

        private BookingPolicy(
            int minAdvanceBookingHours,
            int maxAdvanceBookingDays,
            int cancellationWindowHours,
            decimal cancellationFeePercentage,
            bool allowRescheduling,
            int rescheduleWindowHours,
            bool requireDeposit,
            decimal depositPercentage)
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

            MinAdvanceBookingHours = minAdvanceBookingHours;
            MaxAdvanceBookingDays = maxAdvanceBookingDays;
            CancellationWindowHours = cancellationWindowHours;
            CancellationFeePercentage = cancellationFeePercentage;
            AllowRescheduling = allowRescheduling;
            RescheduleWindowHours = rescheduleWindowHours;
            RequireDeposit = requireDeposit;
            DepositPercentage = depositPercentage;
        }

        public static BookingPolicy Create(
            int minAdvanceBookingHours,
            int maxAdvanceBookingDays,
            int cancellationWindowHours,
            decimal cancellationFeePercentage,
            bool allowRescheduling,
            int rescheduleWindowHours,
            bool requireDeposit,
            decimal depositPercentage)
        {
            return new BookingPolicy(
                minAdvanceBookingHours,
                maxAdvanceBookingDays,
                cancellationWindowHours,
                cancellationFeePercentage,
                allowRescheduling,
                rescheduleWindowHours,
                requireDeposit,
                depositPercentage);
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
            requireDeposit: true,
            depositPercentage: 20);

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
        /// Calculates the deposit amount based on the policy and total price
        /// </summary>
        public Money CalculateDepositAmount(Money totalPrice)
        {
            if (!RequireDeposit)
                return Money.Create(0, totalPrice.Currency);

            var depositAmount = totalPrice.Amount * (DepositPercentage / 100m);
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
        }
    }
}
