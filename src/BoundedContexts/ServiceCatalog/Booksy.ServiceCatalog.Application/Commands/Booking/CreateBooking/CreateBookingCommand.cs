// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CreateBooking/CreateBookingCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.CreateBooking
{
    /// <summary>
    /// Command to create a new booking request
    /// </summary>
    public sealed record CreateBookingCommand(
        Guid CustomerId,
        Guid ProviderId,
        Guid ServiceId,
        Guid StaffProviderId,
        DateTime StartTime,
        string? CustomerNotes = null,
        Guid? IdempotencyKey = null) : ICommand<CreateBookingResult>;
}
