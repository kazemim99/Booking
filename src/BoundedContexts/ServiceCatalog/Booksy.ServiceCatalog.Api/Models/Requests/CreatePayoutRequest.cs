namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for creating a payout for a provider
/// </summary>
public class CreatePayoutRequest
{
    /// <summary>
    /// Provider ID to create payout for
    /// </summary>
    [Required]
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Start of the payout period
    /// </summary>
    [Required]
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// End of the payout period
    /// </summary>
    [Required]
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Commission percentage (defaults to 15%)
    /// </summary>
    [Range(0, 100)]
    public decimal? CommissionPercentage { get; set; }

    /// <summary>
    /// Optional notes about the payout
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}
