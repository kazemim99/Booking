// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/ConfirmBooking/ConfirmBookingCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.ConfirmBooking
{
    /// <summary>
    /// Command to confirm a booking after payment
    /// </summary>
    public sealed record ConfirmBookingCommand(
        Guid BookingId,
        string? PaymentIntentId = null,
        Guid? IdempotencyKey = null) : ICommand<ConfirmBookingResult>;
}
