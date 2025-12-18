// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetQualifiedStaff/GetQualifiedStaffQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Service.GetQualifiedStaff
{
    public sealed record GetQualifiedStaffQuery(
        Guid ProviderId,
        Guid ServiceId) : IQuery<GetQualifiedStaffResult>;
}
