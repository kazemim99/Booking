// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Service/GetQualifiedStaff/GetQualifiedStaffQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Service.GetQualifiedStaff
{
    public sealed record GetQualifiedStaffQuery(
        Guid ProviderId,
        Guid ServiceId) : IQuery<GetQualifiedStaffResult>;
}
