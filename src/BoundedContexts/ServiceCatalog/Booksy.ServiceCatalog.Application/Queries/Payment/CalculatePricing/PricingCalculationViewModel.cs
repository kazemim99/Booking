// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payment/CalculatePricing/PricingCalculationViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Payment.CalculatePricing
{
    public sealed record PricingCalculationViewModel(
        decimal BaseAmount,
        decimal TaxAmount,
        decimal DiscountAmount,
        decimal SubtotalAfterDiscount,
        decimal PlatformFee,
        decimal DepositAmount,
        decimal TotalAmount,
        string Currency,
        PricingBreakdown Breakdown);

    public sealed record PricingBreakdown(
        decimal BaseAmount,
        string BaseDescription,
        decimal? DiscountPercentage,
        decimal? DiscountAmount,
        decimal SubtotalAfterDiscount,
        decimal? TaxPercentage,
        bool TaxInclusive,
        decimal TaxAmount,
        decimal? PlatformFeePercentage,
        decimal PlatformFee,
        decimal TotalAmount,
        decimal? DepositPercentage,
        decimal DepositAmount,
        string Currency);
}
