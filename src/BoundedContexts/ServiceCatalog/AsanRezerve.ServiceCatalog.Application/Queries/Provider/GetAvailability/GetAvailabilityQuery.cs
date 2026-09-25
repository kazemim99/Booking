// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Provider/GetAvailability/GetAvailabilityQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetAvailability;

public sealed record GetAvailabilityQuery(
    Guid ProviderId,
    DateOnly Date) : IQuery<AvailabilityViewModel>;

public sealed record AvailabilityViewModel(
    string Date,
    bool IsAvailable,
    string? Reason,
    List<TimeSlotViewModel> Slots);

public sealed record TimeSlotViewModel(
    string StartTime,
    string EndTime);
