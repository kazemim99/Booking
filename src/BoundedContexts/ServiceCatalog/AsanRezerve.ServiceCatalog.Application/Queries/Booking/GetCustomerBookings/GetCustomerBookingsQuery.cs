// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Booking/GetCustomerBookings/GetCustomerBookingsQuery.cs
// ========================================
using AsanRezerve.Core.Application.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Booking.GetCustomerBookings
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
