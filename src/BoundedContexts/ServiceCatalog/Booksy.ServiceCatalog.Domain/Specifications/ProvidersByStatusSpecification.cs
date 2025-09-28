//===========================================
// Queries/Provider/GetProvidersByLocation/GetProvidersByLocationQueryHandler.cs
//===========================================

namespace Booksy.ServiceCatalog.Domain.Specifications.Provider
{
    public sealed class ProvidersByStatusSpecification : BaseSpecification<Aggregates.Provider>
    {
        public ProvidersByStatusSpecification(ProviderStatus status)
        {
            AddCriteria(provider => provider.Status == status);
            AddOrderBy(provider => provider.Profile.BusinessName);
        }
    }
}

