namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Response model for payout operations
/// </summary>
public class PayoutResponse
{
    public Guid PayoutId { get; set; }
    public Guid ProviderId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int PaymentCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ExternalPayoutId { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
