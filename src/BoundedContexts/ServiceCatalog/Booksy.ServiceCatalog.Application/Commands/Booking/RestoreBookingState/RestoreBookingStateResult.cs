// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/RestoreBookingState/RestoreBookingStateResult.cs
// ========================================

namespace Booksy.ServiceCatalog.Application.Commands.Booking.RestoreBookingState;

/// <summary>
/// Result of restoring a booking to a previous state.
/// </summary>
public sealed record RestoreBookingStateResult(
    Guid BookingId,
    Guid RestoredStateId,
    string PreviousStatus,
    string RestoredStatus,
    DateTime RestoredAt,
    bool Success,
    string? Message = null);
