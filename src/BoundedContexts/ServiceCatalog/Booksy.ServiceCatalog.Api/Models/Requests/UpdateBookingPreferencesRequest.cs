using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Api.Models.Requests
{
    /// <summary>
    /// Booking-policy settings for a provider, including deposit requirements.
    ///
    /// <para>Field names deliberately mirror what the existing provider-settings UI already sends
    /// (<c>requiresDeposit</c>, <c>depositType</c>, <c>depositPercentage</c>), so the built screen works without
    /// client changes. Defaults match <c>BookingPolicy.Default</c> so a partial payload cannot silently loosen an
    /// unrelated rule.</para>
    /// </summary>
    public sealed class UpdateBookingPreferencesRequest
    {
        /// <summary>Whether a deposit must be paid before a booking can be confirmed.</summary>
        public bool RequiresDeposit { get; set; }

        /// <summary>"Percentage" or "FixedAmount". Defaults to Percentage when omitted or unrecognised.</summary>
        public string? DepositType { get; set; }

        /// <summary>Deposit share of the total (0–100), used in Percentage mode.</summary>
        public decimal DepositPercentage { get; set; }

        /// <summary>Flat deposit amount, used in FixedAmount mode.</summary>
        public decimal DepositFixedAmount { get; set; }

        public int MinAdvanceBookingHours { get; set; } = 2;
        public int MaxAdvanceBookingDays { get; set; } = 90;
        public int CancellationWindowHours { get; set; } = 24;
        public decimal CancellationFeePercentage { get; set; } = 50;
        public bool AllowRescheduling { get; set; } = true;
        public int RescheduleWindowHours { get; set; } = 24;

        /// <summary>
        /// Parses <see cref="DepositType"/> tolerantly. An unknown or missing value falls back to Percentage — the
        /// historical meaning — rather than failing, so older clients keep working. Validation still rejects a
        /// required deposit whose selected mode has no usable amount.
        /// </summary>
        public DepositType ResolveDepositType() =>
            Enum.TryParse<DepositType>(DepositType, ignoreCase: true, out var parsed)
                ? parsed
                : Domain.Enums.DepositType.Percentage;
    }
}
