// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetBookingDetails/GetBookingDetailsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingDetails
{
    /// <param name="BookingId">The booking.</param>
    /// <param name="CallerId">
    /// Who is asking, when known. Decides <see cref="BookingDetailsViewModel.IsForCaller"/> and the review fields,
    /// which describe the booking for the person it is for and are empty for anyone else (the salon, an admin).
    /// </param>
    public sealed record GetBookingDetailsQuery(Guid BookingId, Guid? CallerId = null) : IQuery<BookingDetailsViewModel?>;
}
