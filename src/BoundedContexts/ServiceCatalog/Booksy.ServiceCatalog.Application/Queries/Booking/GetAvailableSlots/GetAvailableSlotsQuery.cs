// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetAvailableSlots/GetAvailableSlotsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetAvailableSlots
{
    public sealed record GetAvailableSlotsQuery(
        Guid ProviderId,
        Guid ServiceId,
        DateTime Date,
        Guid? StaffId = null) : IQuery<GetAvailableSlotsResult>;
}
