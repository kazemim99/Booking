// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetServiceById/GetServiceByIdQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServiceById
{
    public sealed record GetServiceByIdQuery(
        Guid ServiceId,
        bool IncludeProvider = false,
        bool IncludeOptions = false,
        bool IncludePriceTiers = false) : IQuery<ServiceDetailsViewModel?>;
}