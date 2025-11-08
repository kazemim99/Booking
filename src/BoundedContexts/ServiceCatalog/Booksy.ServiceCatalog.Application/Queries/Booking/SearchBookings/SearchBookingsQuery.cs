// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/SearchBookings/SearchBookingsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.SearchBookings
{
    public sealed record SearchBookingsQuery(
        Guid? ProviderId = null,
        Guid? CustomerId = null,
        Guid? ServiceId = null,
        Guid? StaffId = null,
        BookingStatus? Status = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        int PageNumber = 1,
        int PageSize = 20) : IQuery<SearchBookingsResult>;
}
