// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetCustomerBookings/GetCustomerBookingsQuery.cs
// ========================================
using Booksy.Core.Application.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetCustomerBookings
{
    /// <summary>
    /// Query to get paginated bookings for a specific customer with optional filters
    /// </summary>
    public sealed record GetCustomerBookingsQuery(
        Guid CustomerId,
        string? Status = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null) : PaginatedQueryBase<CustomerBookingDto>;
}
