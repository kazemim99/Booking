// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Booking/CreateBooking/CreateBookingCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.CreateBooking
{
    /// <summary>
    /// Command to create a new booking request
    /// </summary>
    /// <param name="ServiceIds">All services bundled in the visit. When
    /// provided it supersedes <paramref name="ServiceId"/> (which remains for
    /// caller compatibility); duration and price are summed over the set.</param>
    /// <param name="ProviderCustomerId">Provider-entered bookings only: the salon's customer-book
    /// entry the booking is for. Must belong to <paramref name="ProviderId"/>.</param>
    public sealed record CreateBookingCommand(
        Guid CustomerId,
        Guid ProviderId,
        Guid ServiceId,
        Guid StaffProviderId,
        DateTime StartTime,
        string? CustomerNotes = null,
        Guid? IdempotencyKey = null,
        IReadOnlyList<Guid>? ServiceIds = null,
        Guid? ProviderCustomerId = null,
        string? WalkInFirstName = null,
        string? WalkInLastName = null,
        string? WalkInPhone = null,
        bool NotifyCustomer = true,
        string? PromotionCode = null) : ICommand<CreateBookingResult>;
}
