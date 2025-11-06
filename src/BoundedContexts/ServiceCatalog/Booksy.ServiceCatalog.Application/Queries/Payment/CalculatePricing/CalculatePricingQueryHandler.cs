// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payment/CalculatePricing/CalculatePricingQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.CalculatePricing
{
    public sealed class CalculatePricingQueryHandler : IQueryHandler<CalculatePricingQuery, PricingCalculationViewModel>
    {
        public Task<PricingCalculationViewModel> Handle(CalculatePricingQuery request, CancellationToken cancellationToken)
        {
            // Start with base amount
            var baseAmount = Money.Create(request.BaseAmount, request.Currency);

            // Apply discount
            var discountAmount = Money.Zero(request.Currency);
            if (request.DiscountAmount.HasValue && request.DiscountAmount.Value > 0)
            {
                discountAmount = Money.Create(request.DiscountAmount.Value, request.Currency);
            }
            else if (request.DiscountPercentage.HasValue && request.DiscountPercentage.Value > 0)
            {
                discountAmount = baseAmount.Multiply(request.DiscountPercentage.Value / 100m);
            }

            var subtotalAfterDiscount = baseAmount.Subtract(discountAmount);

            // Calculate tax
            var taxAmount = Money.Zero(request.Currency);
            if (request.TaxPercentage.HasValue && request.TaxPercentage.Value > 0)
            {
                var taxRate = TaxRate.Create(
                    request.TaxPercentage.Value,
                    "Tax",
                    "TAX",
                    request.TaxInclusive);

                taxAmount = taxRate.CalculateTaxAmount(subtotalAfterDiscount);

                if (request.TaxInclusive)
                {
                    // Tax is already included, extract it from subtotal
                    subtotalAfterDiscount = taxRate.CalculateBaseAmount(subtotalAfterDiscount);
                }
            }

            // Calculate total (subtotal + tax if not inclusive)
            var totalAmount = request.TaxInclusive
                ? subtotalAfterDiscount.Add(taxAmount)  // Add back the extracted tax
                : subtotalAfterDiscount.Add(taxAmount);  // Add tax on top

            // Calculate platform fee (if applicable)
            var platformFee = Money.Zero(request.Currency);
            if (request.PlatformFeePercentage.HasValue && request.PlatformFeePercentage.Value > 0)
            {
                platformFee = totalAmount.Multiply(request.PlatformFeePercentage.Value / 100m);
                totalAmount = totalAmount.Add(platformFee);
            }

            // Calculate deposit amount
            var depositAmount = Money.Zero(request.Currency);
            if (request.DepositPercentage.HasValue && request.DepositPercentage.Value > 0)
            {
                depositAmount = totalAmount.Multiply(request.DepositPercentage.Value / 100m);
            }

            var breakdown = new PricingBreakdown(
                baseAmount.Amount,
                "Base service price",
                request.DiscountPercentage,
                discountAmount.Amount,
                subtotalAfterDiscount.Amount,
                request.TaxPercentage,
                request.TaxInclusive,
                taxAmount.Amount,
                request.PlatformFeePercentage,
                platformFee.Amount,
                totalAmount.Amount,
                request.DepositPercentage,
                depositAmount.Amount,
                request.Currency);

            var result = new PricingCalculationViewModel(
                baseAmount.Amount,
                taxAmount.Amount,
                discountAmount.Amount,
                subtotalAfterDiscount.Amount,
                platformFee.Amount,
                depositAmount.Amount,
                totalAmount.Amount,
                request.Currency,
                breakdown);

            return Task.FromResult(result);
        }
    }
}
