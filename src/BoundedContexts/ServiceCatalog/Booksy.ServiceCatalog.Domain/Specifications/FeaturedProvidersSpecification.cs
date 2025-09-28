//===========================================
// Queries/Provider/GetProvidersByLocation/GetProvidersByLocationQueryHandler.cs
//===========================================
namespace Booksy.ServiceCatalog.Domain.Specifications.Provider
{
    public sealed class FeaturedProvidersSpecification : BaseSpecification<Aggregates.Provider>
    {
        public FeaturedProvidersSpecification(string? categoryFilter = null)
        {
            // Only active and verified providers
            AddCriteria(provider => provider.Status == ProviderStatus.Active);
            AddCriteria(provider => provider.Status == ProviderStatus.Verified);

            // Optional category filter
            if (!string.IsNullOrWhiteSpace(categoryFilter))
            {
                var categoryLower = categoryFilter.Trim().ToLower();
                //AddCriteria(provider => provider.Tags.Any(tag => tag.ToLower().Contains(categoryLower)));
            }

            // Order by featured criteria (high rating, many reviews, etc.)
            AddOrderByDescending(provider => provider.AverageRating);
            AddThenBy(provider => provider.Profile.BusinessName);
        }
    }
}