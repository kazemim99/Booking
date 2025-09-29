//===========================================
// Queries/Provider/GetProvidersByLocation/GetProvidersByLocationQueryHandler.cs
//===========================================
namespace Booksy.ServiceCatalog.Domain.Specifications.Provider
{
    /// <summary>
    /// Specification for finding providers by geographic location
    /// </summary>
    public sealed class ProvidersByLocationSpecification : BaseSpecification<Aggregates.Provider>
    {
        public ProvidersByLocationSpecification(
            double latitude,
            double longitude,
            double radiusKm,
            BusinessSize? type = null,
            bool? offersMobileServices = null)
        {
            // Only active providers
            AddCriteria(provider => provider.Status == ProviderStatus.Active);

            // Provider type filter
            if (type.HasValue)
            {
                AddCriteria(provider => provider.Size == type.Value);
            }

            // Mobile services filter
            if (offersMobileServices.HasValue)
            {
                AddCriteria(provider => provider.OffersMobileServices == offersMobileServices.Value);
            }

            // Geographic distance filter (this would be implemented in the repository with spatial queries)
            // For now, this is a placeholder - actual implementation would use spatial functions
            AddCriteria(provider =>
                Math.Abs((double)(provider.Address.Latitude - latitude)) <= radiusKm / 111.0 &&
                Math.Abs((double)(provider.Address.Longitude - longitude)) <= radiusKm / 111.0);

            // Order by distance (calculated in the repository)
            AddOrderBy(provider => provider.Address.Latitude); // Placeholder for distance ordering
        }
    }
}
