// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/RestoreBookingState/RestoreBookingStateCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.RestoreBookingState;

/// <summary>
/// Command to restore a booking to a previous state from audit history.
/// Admin-only operation for correcting errors or reverting changes.
/// </summary>
public sealed record RestoreBookingStateCommand(
    Guid BookingId,
    Guid StateId,
    string? RestoredBy = null,
    string? Reason = null) : ICommand<RestoreBookingStateResult>;
