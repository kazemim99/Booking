// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Booking/MarkNoShow/MarkNoShowResult.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.MarkNoShow
{
    public sealed record MarkNoShowResult(
        Guid BookingId,
        string Status,
        DateTime MarkedAt);
}
