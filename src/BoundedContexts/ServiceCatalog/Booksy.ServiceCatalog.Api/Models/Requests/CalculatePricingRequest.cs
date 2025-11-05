namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for calculating pricing with taxes, discounts, and fees
/// </summary>
public class CalculatePricingRequest
{
    /// <summary>
    /// Base amount before any calculations
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Base amount must be greater than 0")]
    public decimal BaseAmount { get; set; }

    /// <summary>
    /// Currency code (e.g., USD, EUR, GBP)
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Tax percentage (optional)
    /// </summary>
    [Range(0, 100)]
    public decimal? TaxPercentage { get; set; }

    /// <summary>
    /// Whether tax is included in the base amount
    /// </summary>
    public bool TaxInclusive { get; set; } = false;

    /// <summary>
    /// Discount percentage (optional)
    /// </summary>
    [Range(0, 100)]
    public decimal? DiscountPercentage { get; set; }

    /// <summary>
    /// Discount amount (optional, takes precedence over percentage)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? DiscountAmount { get; set; }

    /// <summary>
    /// Platform fee percentage (optional)
    /// </summary>
    [Range(0, 100)]
    public decimal? PlatformFeePercentage { get; set; }

    /// <summary>
    /// Required deposit percentage (optional)
    /// </summary>
    [Range(0, 100)]
    public decimal? DepositPercentage { get; set; }
}
