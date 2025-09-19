// ========================================
// Booksy.ServiceCatalog.Application/Specifications/Service/BookableServiceSpecification.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Specifications.Service
{
    /// <summary>
    /// Specification for services within a price range - Following UserManagement pattern
    /// </summary>
    public sealed class ServiceInPriceRangeSpecification : BaseSpecification<Domain.Aggregates.Service>
    {
        public ServiceInPriceRangeSpecification(
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string currency = "USD",
            bool activeOnly = true,
            bool includePriceTiers = false)
        {
            // Base criteria
            if (activeOnly)
            {
                AddCriteria(service => service.Status == ServiceStatus.Active);
            }

            // Currency filter
            AddCriteria(service => service.BasePrice.Currency == currency);

            // Price range filters
            if (minPrice.HasValue)
            {
                AddCriteria(service => service.BasePrice.Amount >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                AddCriteria(service => service.BasePrice.Amount <= maxPrice.Value);
            }

            // Include price tiers if requested
            if (includePriceTiers)
            {
                AddInclude(s => s.PriceTiers);
            }

        }

        public static ServiceInPriceRangeSpecification CreateAffordableServices(decimal budget, string currency = "USD")
        {
            var spec = new ServiceInPriceRangeSpecification(maxPrice: budget, currency: currency);

            // Additional criteria for affordable services (including deposit consideration)
            spec.AddCriteria(service =>
                !service.RequiresDeposit ||
                (service.DepositPercentage / 100 * service.BasePrice.Amount) <= (budget * 0.5m));

            return spec;
        }

        public static ServiceInPriceRangeSpecification CreatePremiumServices(decimal threshold, string currency = "USD")
        {
            var spec = new ServiceInPriceRangeSpecification(minPrice: threshold, currency: currency);


            return spec;
        }
    }
}

