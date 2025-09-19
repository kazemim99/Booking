using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServiceAvailability
{
    public sealed record GetServiceAvailabilityQuery(
        Guid ServiceId,
        DateTime StartDate,
        DateTime EndDate,
        Guid? StaffId = null) : IQuery<ServiceAvailabilityViewModel>;
}
