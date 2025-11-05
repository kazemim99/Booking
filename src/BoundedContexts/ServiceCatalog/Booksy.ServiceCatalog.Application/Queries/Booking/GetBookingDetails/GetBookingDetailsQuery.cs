// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetBookingDetails/GetBookingDetailsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingDetails
{
    public sealed record GetBookingDetailsQuery(Guid BookingId) : IQuery<BookingDetailsViewModel?>;
}
