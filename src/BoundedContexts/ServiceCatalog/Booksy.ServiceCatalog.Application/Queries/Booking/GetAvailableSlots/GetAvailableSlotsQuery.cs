// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetAvailableSlots/GetAvailableSlotsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetAvailableSlots
{
    /// <param name="ServiceIds">All services in the visit; when provided the
    /// generated slots span their combined duration.</param>
    public sealed record GetAvailableSlotsQuery(
        Guid ProviderId,
        Guid ServiceId,
        DateTime Date,
        Guid? StaffId = null,
        IReadOnlyList<Guid>? ServiceIds = null) : IQuery<GetAvailableSlotsResult>;
}
