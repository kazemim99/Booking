using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingAuditTrail;

/// <summary>
/// Query to get detailed audit trail for a booking with state change details.
/// Provides human-readable information about what changed between states.
/// </summary>
public sealed record GetBookingAuditTrailQuery(Guid BookingId) : IQuery<IReadOnlyList<BookingAuditTrailEntryDto>>;
