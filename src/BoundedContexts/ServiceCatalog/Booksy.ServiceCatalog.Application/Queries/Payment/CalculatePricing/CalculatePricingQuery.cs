// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payment/CalculatePricing/CalculatePricingQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.CalculatePricing
{
    public sealed record CalculatePricingQuery(
        decimal BaseAmount,
        string Currency,
        decimal? TaxPercentage = null,
        bool TaxInclusive = false,
        decimal? DiscountPercentage = null,
        decimal? DiscountAmount = null,
        decimal? PlatformFeePercentage = null,
        decimal? DepositPercentage = null) : IQuery<PricingCalculationViewModel>;
}
