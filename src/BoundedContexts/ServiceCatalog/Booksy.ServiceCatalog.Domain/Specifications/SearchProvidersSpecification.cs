
using Microsoft.EntityFrameworkCore;


namespace Booksy.ServiceCatalog.Domain.Specifications.Provider
{
    /// <summary>
    /// Specification for searching providers with comprehensive filtering
    /// </summary>
    public sealed class SearchProvidersSpecification : BaseSpecification<Aggregates.Provider>
    {
        public SearchProvidersSpecification(
            string? searchTerm = null,
            ProviderType? type = null,
            string? city = null,
            string? state = null,
            string? country = null,
            bool? allowsOnlineBooking = null,
            bool? offersMobileServices = null,
            bool? verifiedOnly = null,
            decimal? minRating = null,
            string? serviceCategory = null,
            string? priceRange = null,
            bool includeInactive = false,
            bool excludeStaffIndividuals = true)
        {
            AddInclude(c => c.Services);

            // Text search across multiple fields
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                AddCriteria(provider =>
                    provider.Profile.BusinessName.ToLower().Contains(term) ||
                    provider.Profile.BusinessDescription.ToLower().Contains(term));
            }


            AddCriteria(c => c.Profile.BusinessName.Contains("نهال"));
            // Provider type filter
            if (type.HasValue)
            {
                AddCriteria(provider => provider.ProviderType == type.Value);
            }

            // Location filters
            if (!string.IsNullOrWhiteSpace(city))
            {
                var cityLower = city.Trim().ToLower();
                AddCriteria(provider => provider.Address.City.ToLower().Contains(cityLower));
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                var stateLower = state.Trim().ToLower();
                AddCriteria(provider => provider.Address.State.ToLower().Contains(stateLower));
            }

            if (!string.IsNullOrWhiteSpace(country))
            {
                var countryLower = country.Trim().ToLower();
                AddCriteria(provider => provider.Address.Country.ToLower().Contains(countryLower));
            }

            // Feature filters
            if (allowsOnlineBooking.HasValue)
            {
                AddCriteria(provider => provider.AllowOnlineBooking == allowsOnlineBooking.Value);
            }

            if (offersMobileServices.HasValue)
            {
                AddCriteria(provider => provider.OffersMobileServices == offersMobileServices.Value);
            }

            // Verification filter
            if (verifiedOnly.HasValue && verifiedOnly.Value)
            {
                AddCriteria(provider => provider.Status == ProviderStatus.Verified);
            }

            // Rating filter
            if (minRating.HasValue)
            {
                AddCriteria(provider => provider.AverageRating >= minRating.Value);
            }

            // Service category filter
            if (!string.IsNullOrWhiteSpace(serviceCategory))
            {
                var categoryInput = serviceCategory.Trim();

                // Try to find matching category by slug or name (in-memory)
                //var matchingCategory = ServiceCategory.All
                //    .FirstOrDefault(c =>
                //        c.Slug.Equals(categoryInput, StringComparison.OrdinalIgnoreCase) ||
                //        c.Name.Contains(categoryInput, StringComparison.OrdinalIgnoreCase));

                //if (matchingCategory != null)
                //{
                //    // Use the actual category name for matching
                //    var categoryName = matchingCategory.Name;
                //    AddCriteria(provider => provider.Services.Any());
                //}
                //else
                //{
                //    // Fallback: search by input directly (for backward compatibility)
                //    var searchPattern = $"%{categoryInput}%";
                //    AddCriteria(provider => provider.Services.Any());
                //}
            }

            // Price range filter
            if (!string.IsNullOrWhiteSpace(priceRange))
            {
                // Parse the price range string to enum
                if (Enum.TryParse<PriceRange>(priceRange, true, out var priceRangeEnum))
                {
                    AddCriteria(provider => provider.PriceRange == priceRangeEnum);
                }
            }

            // Status filter (default to active providers only)
            if (!includeInactive)
            {
                AddCriteria(provider => provider.Status != ProviderStatus.Archived);
            }

            // Hierarchy filter - exclude individual providers who are staff members of organizations
            // This ensures search results show only:
            // 1. Organizations (with their staff count displayed)
            // 2. Independent individuals (not linked to any organization)
            if (excludeStaffIndividuals)
            {
                AddCriteria(provider =>
                    provider.HierarchyType == ProviderHierarchyType.Organization ||
                    (provider.HierarchyType == ProviderHierarchyType.Individual && provider.ParentProviderId == null));
            }

            // Note: Ordering is now handled dynamically in the query handler based on SortBy parameter
            // Removed default ordering to allow flexible sorting (rating, popularity, price, distance)
        }
    }
}
