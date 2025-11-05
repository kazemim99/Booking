namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Payment information response model
/// </summary>
public class PaymentInfoResponse
{
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal DepositAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? DepositTransactionId { get; set; }
    public string? FullPaymentTransactionId { get; set; }
    public List<string> RefundTransactionIds { get; set; } = new();
}
