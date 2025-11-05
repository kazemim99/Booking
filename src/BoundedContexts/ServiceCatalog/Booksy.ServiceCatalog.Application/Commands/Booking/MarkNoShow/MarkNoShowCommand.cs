// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/MarkNoShow/MarkNoShowCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.MarkNoShow
{
    public sealed record MarkNoShowCommand(
        Guid BookingId,
        string? Notes = null,
        Guid? IdempotencyKey = null) : ICommand<MarkNoShowResult>;
}
