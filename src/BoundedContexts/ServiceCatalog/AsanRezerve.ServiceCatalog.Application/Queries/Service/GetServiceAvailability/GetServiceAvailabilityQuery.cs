using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Service.GetServiceAvailability
{
    public sealed record GetServiceAvailabilityQuery(
        Guid ServiceId,
        DateTime StartDate,
        DateTime EndDate,
        Guid? StaffId = null) : IQuery<ServiceAvailabilityViewModel>;
}
