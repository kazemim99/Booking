
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.Specifications.Provider;
using Microsoft.Extensions.Logging;


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
            bool includeInactive = false)
        {
            // Text search across multiple fields
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                AddCriteria(provider =>
                    provider.Profile.BusinessName.ToLower().Contains(term) ||
                    provider.Profile.BusinessDescription.ToLower().Contains(term));
            }

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
                var categoryLower = serviceCategory.Trim().ToLower();
                AddCriteria(provider => provider.Services.Any(s =>
                    s.Category.Name.ToLower().Contains(categoryLower) &&
                    s.Status == ServiceStatus.Active));
            }

            // Status filter (default to active providers only)
            if (includeInactive)
            {
                AddCriteria(provider => provider.Status != ProviderStatus.Archived);
            }
            //else
            //{
            //    AddCriteria(provider => provider.Status == );
            //}

            // Note: Ordering is now handled dynamically in the query handler based on SortBy parameter
            // Removed default ordering to allow flexible sorting (rating, popularity, price, distance)
        }
    }
}
