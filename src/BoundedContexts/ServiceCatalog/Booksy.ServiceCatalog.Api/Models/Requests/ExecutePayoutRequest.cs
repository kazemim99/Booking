namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for executing a payout via payment gateway
/// </summary>
public class ExecutePayoutRequest
{
    /// <summary>
    /// Stripe Connected Account ID for the provider
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ConnectedAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Optional description for the payout
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
}
