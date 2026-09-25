// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Service/GetServiceById/GetServiceByIdQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Service.GetServiceById
{
    public sealed record GetServiceByIdQuery(
        Guid ServiceId,
        bool IncludeProvider = false,
        bool IncludeOptions = false,
        bool IncludePriceTiers = false) : IQuery<ServiceDetailsViewModel?>;
}