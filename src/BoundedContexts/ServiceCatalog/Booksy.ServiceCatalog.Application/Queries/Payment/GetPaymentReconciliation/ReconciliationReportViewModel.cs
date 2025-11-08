// ========================================
// ReconciliationReportViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetPaymentReconciliation
{
    public sealed record ReconciliationReportViewModel(
        DateTime StartDate,
        DateTime EndDate,
        IReadOnlyList<ReconciliationItemDto> Payments,
        ReconciliationSummaryDto Summary);

    public sealed record ReconciliationItemDto(
        Guid PaymentId,
        Guid? BookingId,
        Guid CustomerId,
        Guid ProviderId,
        decimal Amount,
        decimal RefundedAmount,
        decimal NetAmount,
        string Status,
        string Method,
        string? RefNumber,
        string? Authority,
        DateTime CapturedAt);

    public sealed record ReconciliationSummaryDto(
        int TotalTransactions,
        decimal TotalAmount,
        decimal TotalRefunded,
        decimal NetAmount,
        int PaidCount,
        int RefundedCount,
        int PartiallyRefundedCount,
        string Currency);
}
