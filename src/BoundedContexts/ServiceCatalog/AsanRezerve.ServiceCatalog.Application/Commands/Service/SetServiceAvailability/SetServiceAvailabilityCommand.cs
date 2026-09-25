// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Service/SetServiceAvailability/SetServiceAvailabilityCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Service.SetServiceAvailability
{
    public sealed record SetServiceAvailabilityCommand(
        Guid ServiceId,
        Dictionary<DayOfWeek, ServiceAvailabilityRequest?> Availability,
        Guid? IdempotencyKey = null) : ICommand<SetServiceAvailabilityResult>;

    public sealed record ServiceAvailabilityRequest(
        TimeOnly StartTime,
        TimeOnly EndTime,
        int MaxConcurrentBookings = 1,
        List<Guid>? AvailableStaffIds = null);
}