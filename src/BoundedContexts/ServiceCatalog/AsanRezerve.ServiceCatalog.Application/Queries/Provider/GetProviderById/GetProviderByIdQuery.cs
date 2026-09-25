// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Provider/GetProviderById/GetProviderByIdQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Application.Queries.Provider.SearchProviders;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetProviderById
{
    /// <summary>
    /// Query to get provider details by ID
    /// </summary>
    public sealed record GetProviderByIdQuery(
        Guid ProviderId,
        bool IncludeServices = false,
        bool IncludeStaff = false) : IQuery<ProviderDetailsResult?>;
}