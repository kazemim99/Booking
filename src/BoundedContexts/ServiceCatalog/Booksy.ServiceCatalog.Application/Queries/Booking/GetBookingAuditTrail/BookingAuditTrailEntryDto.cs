namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingAuditTrail;

/// <summary>
/// DTO representing a detailed audit trail entry for a booking state change.
/// </summary>
public sealed record BookingAuditTrailEntryDto(
    Guid StateId,
    string StateName,
    string Status,
    DateTime CreatedAt,
    string? TriggeredBy,
    string? Description,
    Dictionary<string, string> StateSnapshot);
