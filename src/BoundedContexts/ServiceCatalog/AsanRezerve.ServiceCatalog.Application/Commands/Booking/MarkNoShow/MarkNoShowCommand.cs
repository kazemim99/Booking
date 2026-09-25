// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Booking/MarkNoShow/MarkNoShowCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.MarkNoShow
{
    public sealed record MarkNoShowCommand(
        Guid BookingId,
        string? Notes = null,
        Guid? IdempotencyKey = null) : ICommand<MarkNoShowResult>;
}
