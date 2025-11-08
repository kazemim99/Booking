namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingHistory;

/// <summary>
/// DTO representing a booking state snapshot in the audit trail.
/// </summary>
public sealed record BookingHistorySnapshotDto(
    Guid Id,
    Guid StateId,
    string StateName,
    string StateJson,
    DateTime CreatedAt,
    string? TriggeredBy,
    string? Description);
