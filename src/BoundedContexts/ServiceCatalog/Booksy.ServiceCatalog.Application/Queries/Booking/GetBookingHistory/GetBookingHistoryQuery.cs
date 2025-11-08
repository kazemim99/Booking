using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingHistory;

/// <summary>
/// Query to get all historical state snapshots for a booking.
/// Returns complete audit trail of state changes.
/// </summary>
public sealed record GetBookingHistoryQuery(Guid BookingId) : IQuery<IReadOnlyList<BookingHistorySnapshotDto>>;
