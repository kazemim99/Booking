using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.Exceptions;

/// <summary>
/// Exception thrown when a service price is invalid or violates business rules
/// </summary>
public sealed class InvalidServicePriceException : DomainException
{
    public ServiceId? ServiceId { get; }
    public ProviderId? ProviderId { get; }
    public Money? InvalidPrice { get; }
    public Money? MinimumPrice { get; }
    public Money? MaximumPrice { get; }
    public CurrencyCode? ExpectedCurrency { get; }
    public CurrencyCode? ActualCurrency { get; }
    public PriceValidationError ValidationError { get; }
    public string? ValidationRule { get; }
    public decimal? PercentageViolation { get; }

    /// <summary>
    /// Initializes a new instance for price range violations
    /// </summary>
    public InvalidServicePriceException(
        ServiceId serviceId,
        Money invalidPrice,
        Money minimumPrice,
        Money maximumPrice)
        : base($"Service price {invalidPrice} for service '{serviceId}' is outside allowed range ({minimumPrice} - {maximumPrice}).")
    {
        ServiceId = serviceId;
        InvalidPrice = invalidPrice;
        MinimumPrice = minimumPrice;
        MaximumPrice = maximumPrice;
        ValidationError = PriceValidationError.OutsideAllowedRange;

        PercentageViolation = CalculatePercentageViolation(invalidPrice, minimumPrice, maximumPrice);

        Data.Add("ServiceId", serviceId.ToString());
        Data.Add("InvalidPrice", invalidPrice.ToString());
        Data.Add("MinimumPrice", minimumPrice.ToString());
        Data.Add("MaximumPrice", maximumPrice.ToString());
        Data.Add("ValidationError", ValidationError.ToString());
        if (PercentageViolation.HasValue)
            Data.Add("PercentageViolation", PercentageViolation.Value.ToString("F2"));
    }

    /// <summary>
    /// Initializes a new instance for currency mismatches
    /// </summary>
    public InvalidServicePriceException(
        ServiceId serviceId,
        CurrencyCode expectedCurrency,
        CurrencyCode actualCurrency)
        : base($"Service '{serviceId}' price currency mismatch. Expected: {expectedCurrency}, Actual: {actualCurrency}")
    {
        ServiceId = serviceId;
        ExpectedCurrency = expectedCurrency;
        ActualCurrency = actualCurrency;
        ValidationError = PriceValidationError.CurrencyMismatch;

        Data.Add("ServiceId", serviceId.ToString());
        Data.Add("ExpectedCurrency", expectedCurrency.ToString());
        Data.Add("ActualCurrency", actualCurrency.ToString());
        Data.Add("ValidationError", ValidationError.ToString());
    }

    /// <summary>
    /// Initializes a new instance for negative prices
    /// </summary>
    public InvalidServicePriceException(ServiceId serviceId, Money invalidPrice)
        : base($"Service price cannot be negative. Service: '{serviceId}', Price: {invalidPrice}")
    {
        ServiceId = serviceId;
        InvalidPrice = invalidPrice;
        ValidationError = PriceValidationError.NegativePrice;

        Data.Add("ServiceId", serviceId.ToString());
        Data.Add("InvalidPrice", invalidPrice.ToString());
        Data.Add("ValidationError", ValidationError.ToString());
    }

    /// <summary>
    /// Initializes a new instance for validation rule violations
    /// </summary>
    public InvalidServicePriceException(
        ServiceId serviceId,
        Money invalidPrice,
        string validationRule,
        PriceValidationError validationError)
        : base($"Service price {invalidPrice} for service '{serviceId}' violates validation rule: {validationRule}")
    {
        ServiceId = serviceId;
        InvalidPrice = invalidPrice;
        ValidationRule = validationRule;
        ValidationError = validationError;

        Data.Add("ServiceId", serviceId.ToString());
        Data.Add("InvalidPrice", invalidPrice.ToString());
        Data.Add("ValidationRule", validationRule);
        Data.Add("ValidationError", ValidationError.ToString());
    }

    /// <summary>
    /// Initializes a new instance with custom message
    /// </summary>
    public InvalidServicePriceException(string message, PriceValidationError validationError = PriceValidationError.General)
        : base(message)
    {
        ValidationError = validationError;
        Data.Add("ValidationError", ValidationError.ToString());
    }

    /// <summary>
    /// Initializes a new instance with custom message and inner exception
    /// </summary>
    public InvalidServicePriceException(string message, Exception innerException, PriceValidationError validationError = PriceValidationError.General)
        : base(message, innerException)
    {
        ValidationError = validationError;
        Data.Add("ValidationError", ValidationError.ToString());
    }

    /// <summary>
    /// Creates exception for zero price violations
    /// </summary>
    public static InvalidServicePriceException ZeroPriceNotAllowed(ServiceId serviceId, ProviderId providerId)
    {
        var exception = new InvalidServicePriceException(
            $"Zero price is not allowed for service '{serviceId}' of provider '{providerId}'.",
            PriceValidationError.ZeroPriceNotAllowed);

        exception.Data.Add("ServiceId", serviceId.ToString());
        exception.Data.Add("ProviderId", providerId.ToString());

        return exception;
    }

    /// <summary>
    /// Creates exception for precision violations
    /// </summary>
    public static InvalidServicePriceException InvalidPrecision(
        ServiceId serviceId,
        Money invalidPrice,
        int maxDecimalPlaces)
    {
        var exception = new InvalidServicePriceException(
            $"Service price {invalidPrice} for service '{serviceId}' has too many decimal places. Maximum allowed: {maxDecimalPlaces}",
            PriceValidationError.InvalidPrecision);

        exception.Data.Add("ServiceId", serviceId.ToString());
        exception.Data.Add("InvalidPrice", invalidPrice.ToString());
        exception.Data.Add("MaxDecimalPlaces", maxDecimalPlaces.ToString());

        return exception;
    }

    /// <summary>
    /// Creates exception for competitive pricing violations
    /// </summary>
    public static InvalidServicePriceException CompetitivePricingViolation(
        ServiceId serviceId,
        Money proposedPrice,
        Money marketAverage,
        decimal maxDeviationPercentage)
    {
        var deviation = Math.Abs(((proposedPrice.Amount - marketAverage.Amount) / marketAverage.Amount) * 100);
        var exception = new InvalidServicePriceException(
            $"Service price {proposedPrice} for service '{serviceId}' deviates {deviation:F1}% from market average {marketAverage}, exceeding maximum allowed deviation of {maxDeviationPercentage:F1}%",
            PriceValidationError.CompetitivePricingViolation);

        exception.Data.Add("ServiceId", serviceId.ToString());
        exception.Data.Add("ProposedPrice", proposedPrice.ToString());
        exception.Data.Add("MarketAverage", marketAverage.ToString());
        exception.Data.Add("MaxDeviationPercentage", maxDeviationPercentage.ToString("F1"));
        exception.Data.Add("ActualDeviation", deviation.ToString("F1"));

        return exception;
    }

    /// <summary>
    /// Creates exception for provider tier pricing violations
    /// </summary>
    public static InvalidServicePriceException ProviderTierViolation(
        ServiceId serviceId,
        ProviderId providerId,
        Money proposedPrice,
        string providerTier,
        Money maxAllowedForTier)
    {
        var exception = new InvalidServicePriceException(
            $"Service price {proposedPrice} for service '{serviceId}' exceeds maximum allowed for provider tier '{providerTier}'. Maximum allowed: {maxAllowedForTier}",
            PriceValidationError.ProviderTierViolation);

        exception.Data.Add("ServiceId", serviceId.ToString());
        exception.Data.Add("ProviderId", providerId.ToString());
        exception.Data.Add("ProposedPrice", proposedPrice.ToString());
        exception.Data.Add("ProviderTier", providerTier);
        exception.Data.Add("MaxAllowedForTier", maxAllowedForTier.ToString());

        return exception;
    }

    /// <summary>
    /// Creates exception for duration-based pricing violations
    /// </summary>
    public static InvalidServicePriceException DurationBasedPricingViolation(
        ServiceId serviceId,
        Money proposedPrice,
        TimeSpan serviceDuration,
        Money minimumPerHour)
    {
        var hourlyRate = proposedPrice.Amount / (decimal)serviceDuration.TotalHours;
        var exception = new InvalidServicePriceException(
            $"Service price {proposedPrice} for service '{serviceId}' results in hourly rate of {proposedPrice.Currency}, which is below minimum of {minimumPerHour}",
            PriceValidationError.DurationBasedPricingViolation);

        exception.Data.Add("ServiceId", serviceId.ToString());
        exception.Data.Add("ProposedPrice", proposedPrice.ToString());
        exception.Data.Add("ServiceDuration", serviceDuration.ToString());
        exception.Data.Add("MinimumPerHour", minimumPerHour.ToString());
        exception.Data.Add("CalculatedHourlyRate", hourlyRate.ToString("F2"));

        return exception;
    }

    /// <summary>
    /// Creates exception for promotional pricing violations
    /// </summary>
    public static InvalidServicePriceException PromotionalPricingViolation(
        ServiceId serviceId,
        Money promotionalPrice,
        Money originalPrice,
        decimal maxDiscountPercentage)
    {
        var discountPercentage = ((originalPrice.Amount - promotionalPrice.Amount) / originalPrice.Amount) * 100;
        var exception = new InvalidServicePriceException(
            $"Promotional price {promotionalPrice} for service '{serviceId}' represents {discountPercentage:F1}% discount, exceeding maximum allowed discount of {maxDiscountPercentage:F1}%",
            PriceValidationError.PromotionalPricingViolation);

        exception.Data.Add("ServiceId", serviceId.ToString());
        exception.Data.Add("PromotionalPrice", promotionalPrice.ToString());
        exception.Data.Add("OriginalPrice", originalPrice.ToString());
        exception.Data.Add("MaxDiscountPercentage", maxDiscountPercentage.ToString("F1"));
        exception.Data.Add("ActualDiscountPercentage", discountPercentage.ToString("F1"));

        return exception;
    }

    /// <summary>
    /// Creates exception for package pricing violations
    /// </summary>
    public static InvalidServicePriceException PackagePricingViolation(
        IEnumerable<ServiceId> packageServices,
        Money packagePrice,
        Money totalIndividualPrice,
        decimal minDiscountPercentage)
    {
        var actualDiscount = ((totalIndividualPrice.Amount - packagePrice.Amount) / totalIndividualPrice.Amount) * 100;
        var exception = new InvalidServicePriceException(
            $"Package price {packagePrice} provides only {actualDiscount:F1}% discount, below minimum required {minDiscountPercentage:F1}% for service package",
            PriceValidationError.PackagePricingViolation);

        exception.Data.Add("PackageServices", string.Join(",", packageServices));
        exception.Data.Add("PackagePrice", packagePrice.ToString());
        exception.Data.Add("TotalIndividualPrice", totalIndividualPrice.ToString());
        exception.Data.Add("MinDiscountPercentage", minDiscountPercentage.ToString("F1"));
        exception.Data.Add("ActualDiscountPercentage", actualDiscount.ToString("F1"));

        return exception;
    }

    /// <summary>
    /// Calculate percentage violation from allowed range
    /// </summary>
    private static decimal? CalculatePercentageViolation(Money invalidPrice, Money minPrice, Money maxPrice)
    {
        if (invalidPrice.Amount < minPrice.Amount)
        {
            return ((minPrice.Amount - invalidPrice.Amount) / minPrice.Amount) * 100;
        }

        if (invalidPrice.Amount > maxPrice.Amount)
        {
            return ((invalidPrice.Amount - maxPrice.Amount) / maxPrice.Amount) * 100;
        }

        return null;
    }

    /// <summary>
    /// Get error code for this exception
    /// </summary>
    public override string ErrorCode => $"INVALID_SERVICE_PRICE_{ValidationError.ToString().ToUpperInvariant()}";

    /// <summary>
    /// Get severity level for this exception
    /// </summary>
    public ExceptionSeverity Severity => ValidationError switch
    {
        PriceValidationError.NegativePrice => ExceptionSeverity.Critical,
        PriceValidationError.CurrencyMismatch => ExceptionSeverity.Critical,
        PriceValidationError.OutsideAllowedRange => ExceptionSeverity.High,
        PriceValidationError.ProviderTierViolation => ExceptionSeverity.High,
        PriceValidationError.CompetitivePricingViolation => ExceptionSeverity.Medium,
        PriceValidationError.DurationBasedPricingViolation => ExceptionSeverity.Medium,
        PriceValidationError.ZeroPriceNotAllowed => ExceptionSeverity.Medium,
        PriceValidationError.InvalidPrecision => ExceptionSeverity.Low,
        PriceValidationError.PromotionalPricingViolation => ExceptionSeverity.Low,
        PriceValidationError.PackagePricingViolation => ExceptionSeverity.Low,
        _ => ExceptionSeverity.Medium
    };

    /// <summary>
    /// Get suggested actions for resolving this exception
    /// </summary>
    public IEnumerable<string> GetSuggestedActions()
    {
        return ValidationError switch
        {
            PriceValidationError.NegativePrice => new[]
            {
                "Set a positive price value",
                "Review pricing calculation logic",
                "Check for data entry errors"
            },
            PriceValidationError.ZeroPriceNotAllowed => new[]
            {
                "Set a minimum price above zero",
                "Use promotional pricing if offering free services",
                "Review service pricing strategy"
            },
            PriceValidationError.OutsideAllowedRange => new[]
            {
                "Adjust price to fall within allowed range",
                "Review pricing policy limits",
                "Request approval for exception pricing"
            },
            PriceValidationError.CurrencyMismatch => new[]
            {
                "Convert price to expected currency",
                "Update provider currency settings",
                "Verify multi-currency configuration"
            },
            PriceValidationError.InvalidPrecision => new[]
            {
                "Round price to allowed decimal places",
                "Update pricing rules for precision",
                "Review currency precision requirements"
            },
            PriceValidationError.CompetitivePricingViolation => new[]
            {
                "Adjust price closer to market average",
                "Justify pricing with value proposition",
                "Review competitive pricing strategy"
            },
            PriceValidationError.ProviderTierViolation => new[]
            {
                "Reduce price to match provider tier limits",
                "Upgrade provider tier if justified",
                "Review tier-based pricing policies"
            },
            PriceValidationError.DurationBasedPricingViolation => new[]
            {
                "Increase price to meet minimum hourly rate",
                "Adjust service duration",
                "Review time-based pricing rules"
            },
            PriceValidationError.PromotionalPricingViolation => new[]
            {
                "Reduce promotional discount percentage",
                "Extend promotional period instead of deeper discount",
                "Review promotional pricing policies"
            },
            PriceValidationError.PackagePricingViolation => new[]
            {
                "Increase package discount to meet minimum",
                "Adjust individual service prices",
                "Review package pricing strategy"
            },
            _ => new[]
            {
                "Review price validation rules",
                "Check pricing configuration",
                "Contact support for pricing guidance"
            }
        };
    }

    /// <summary>
    /// Check if this exception is recoverable
    /// </summary>
    public bool IsRecoverable => ValidationError switch
    {
        PriceValidationError.NegativePrice => false,
        PriceValidationError.CurrencyMismatch => false,
        _ => true
    };

    /// <summary>
    /// Get price adjustment recommendation
    /// </summary>
    public PriceAdjustmentRecommendation? GetPriceAdjustmentRecommendation()
    {
        if (InvalidPrice == null) return null;

        return ValidationError switch
        {
            PriceValidationError.OutsideAllowedRange when MinimumPrice != null && MaximumPrice != null =>
                new PriceAdjustmentRecommendation
                {
                    RecommendedPrice = InvalidPrice.Amount < MinimumPrice.Amount ? MinimumPrice : MaximumPrice,
                    Reason = InvalidPrice.Amount < MinimumPrice.Amount ? "Adjust to minimum allowed price" : "Adjust to maximum allowed price",
                    AdjustmentType = PriceAdjustmentType.ToAllowedRange
                },

            PriceValidationError.NegativePrice =>
                new PriceAdjustmentRecommendation
                {
                    RecommendedPrice = Money.Create(Math.Abs(InvalidPrice.Amount), InvalidPrice.Currency),
                    Reason = "Remove negative sign from price",
                    AdjustmentType = PriceAdjustmentType.ToPositive
                },

            PriceValidationError.InvalidPrecision =>
                new PriceAdjustmentRecommendation
                {
                    Reason = "Round to allowed decimal places",
                    AdjustmentType = PriceAdjustmentType.RoundToPrecision
                },

            _ => null
        };
    }
}

/// <summary>
/// Types of price validation errors
/// </summary>
public enum PriceValidationError
{
    General,
    NegativePrice,
    ZeroPriceNotAllowed,
    OutsideAllowedRange,
    CurrencyMismatch,
    InvalidPrecision,
    CompetitivePricingViolation,
    ProviderTierViolation,
    DurationBasedPricingViolation,
    PromotionalPricingViolation,
    PackagePricingViolation
}

/// <summary>
/// Price adjustment recommendation
/// </summary>
public class PriceAdjustmentRecommendation
{
    public Money RecommendedPrice { get; init; } = null!;
    public string Reason { get; init; } = string.Empty;
    public PriceAdjustmentType AdjustmentType { get; init; }
}

/// <summary>
/// Types of price adjustments
/// </summary>
public enum PriceAdjustmentType
{
    ToAllowedRange,
    ToPositive,
    RoundToPrecision,
    ToMarketRate,
    ToTierLimit
}