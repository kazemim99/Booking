// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Booking/GetBookingDetails/GetBookingDetailsQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Booking.GetBookingDetails
{
    public sealed record GetBookingDetailsQuery(Guid BookingId) : IQuery<BookingDetailsViewModel?>;
}
