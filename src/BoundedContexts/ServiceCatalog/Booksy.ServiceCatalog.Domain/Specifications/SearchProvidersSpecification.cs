
using Booksy.ServiceCatalog.Domain.Enums.Extensions;
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
            ServiceCategory? category = null,
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


            // Provider category filter
            if (category.HasValue)
            {
                AddCriteria(provider => provider.PrimaryCategory == category.Value);
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

            // Service category filter.
            //
            // The whole body of this branch was commented out, so a caller-supplied category was accepted and
            // then silently ignored: `?ServiceCategory=Barbershop` returned the entire catalogue — dentists,
            // gyms and physiotherapists included. Silent because nothing failed; the results merely had nothing
            // to do with what was asked for, which is worse than an error.
            //
            // The enum-typed `category` parameter above already expresses the intended filter, so the string
            // form simply resolves to the same criterion.
            if (!string.IsNullOrWhiteSpace(serviceCategory))
            {
                var categoryInput = serviceCategory.Trim();

                // TryParse alone accepts any numeric string ("99"), so IsDefined guards against a value that
                // parses but names no real category. Slugs are accepted too, because that is the form the
                // category chips and category URLs use ("hair-salon"), and requiring the enum member name
                // here is why the search filter matched nothing for every slug-shaped value.
                var resolved =
                    Enum.TryParse<ServiceCategory>(categoryInput, ignoreCase: true, out var parsedCategory)
                    && Enum.IsDefined(typeof(ServiceCategory), parsedCategory);

                if (!resolved)
                {
                    resolved = ServiceCategoryExtensions.TryParseSlug(categoryInput, out parsedCategory);
                }

                if (resolved)
                {
                    var categoryFilter = parsedCategory;
                    AddCriteria(provider => provider.PrimaryCategory == categoryFilter);
                }
                else
                {
                    // An unrecognised category matches nothing. Returning everything — the previous behaviour —
                    // reads as "here are your results" and quietly hides the fact that the filter never applied.
                    AddCriteria(provider => false);
                }
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
