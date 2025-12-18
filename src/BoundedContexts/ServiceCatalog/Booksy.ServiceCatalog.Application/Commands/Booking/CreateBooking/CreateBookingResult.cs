// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CreateBooking/CreateBookingResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Booking.CreateBooking
{
    /// <summary>
    /// Result of creating a booking
    /// </summary>
    public sealed record CreateBookingResult(
        Guid BookingId,
        Guid CustomerId,
        Guid ProviderId,
        Guid ServiceId,
        Guid StaffProviderId,
        DateTime StartTime,
        DateTime EndTime,
        decimal TotalPrice,
        decimal DepositAmount,
        bool RequiresDeposit,
        string Status,
        DateTime RequestedAt)
    {
        public int DurationMinutes { get; set; }
        public string Currency { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
