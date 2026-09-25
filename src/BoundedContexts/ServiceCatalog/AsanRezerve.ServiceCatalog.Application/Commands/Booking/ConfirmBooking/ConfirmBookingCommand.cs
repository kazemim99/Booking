// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Booking/ConfirmBooking/ConfirmBookingCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.ConfirmBooking
{
    /// <summary>
    /// Command to confirm a booking after payment
    /// </summary>
    public sealed record ConfirmBookingCommand(
        Guid BookingId,
        string? PaymentIntentId = null,
        Guid? IdempotencyKey = null) : ICommand<ConfirmBookingResult>;
}
