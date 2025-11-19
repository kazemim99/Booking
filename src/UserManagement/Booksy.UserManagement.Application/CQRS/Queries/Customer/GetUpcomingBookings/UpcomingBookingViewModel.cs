// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetUpcomingBookings/UpcomingBookingViewModel.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetUpcomingBookings
{
    /// <summary>
    /// View model for an upcoming booking
    /// </summary>
    public sealed record UpcomingBookingViewModel
    {
        public Guid BookingId { get; init; }
        public Guid ProviderId { get; init; }
        public string ProviderName { get; init; } = string.Empty;
        public string ServiceName { get; init; } = string.Empty;
        public DateTime StartTime { get; init; }
        public string Status { get; init; } = string.Empty;
        public decimal? TotalPrice { get; init; }
    }
}
