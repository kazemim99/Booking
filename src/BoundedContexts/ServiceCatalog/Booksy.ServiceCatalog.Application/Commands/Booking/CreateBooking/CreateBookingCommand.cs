// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CreateBooking/CreateBookingCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.CreateBooking
{
    /// <summary>
    /// Command to create a new booking request
    /// </summary>
    /// <param name="ServiceIds">All services bundled in the visit. When
    /// provided it supersedes <paramref name="ServiceId"/> (which remains for
    /// caller compatibility); duration and price are summed over the set.</param>
    public sealed record CreateBookingCommand(
        Guid CustomerId,
        Guid ProviderId,
        Guid ServiceId,
        Guid StaffProviderId,
        DateTime StartTime,
        string? CustomerNotes = null,
        Guid? IdempotencyKey = null,
        IReadOnlyList<Guid>? ServiceIds = null) : ICommand<CreateBookingResult>;
}
