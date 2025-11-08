// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payment/GetPaymentDetails/PaymentDetailsViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetPaymentDetails
{
    public sealed record PaymentDetailsViewModel(
        Guid PaymentId,
        Guid? BookingId,
        Guid CustomerId,
        Guid ProviderId,
        decimal Amount,
        string Currency,
        decimal PaidAmount,
        decimal RefundedAmount,
        string Status,
        string Method,
        string? PaymentIntentId,
        string? PaymentMethodId,
        string? Description,
        string? FailureReason,
        Dictionary<string, string> Metadata,
        List<TransactionDto> Transactions,
        DateTime CreatedAt,
        DateTime? AuthorizedAt,
        DateTime? CapturedAt,
        DateTime? RefundedAt,
        DateTime? FailedAt,
        string? Authority = null,
        string? RefNumber = null,
        string? CardPan = null,
        decimal? Fee = null,
        string? PaymentUrl = null);

    public sealed record TransactionDto(
        Guid Id,
        string Type,
        decimal Amount,
        string Currency,
        string? ExternalTransactionId,
        string? Reference,
        string Status,
        string? StatusReason,
        DateTime ProcessedAt,
        DateTime? CompletedAt);
}
