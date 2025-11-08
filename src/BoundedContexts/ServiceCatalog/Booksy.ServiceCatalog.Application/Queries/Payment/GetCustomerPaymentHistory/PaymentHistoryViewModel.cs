// ========================================
// PaymentHistoryViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetCustomerPaymentHistory
{
    public sealed record PaymentHistoryViewModel(
        IReadOnlyList<PaymentHistoryItemDto> Payments,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages);

    public sealed record PaymentHistoryItemDto(
        Guid PaymentId,
        Guid? BookingId,
        decimal Amount,
        string Currency,
        string Status,
        string Method,
        string? Description,
        string? RefNumber,
        string? CardPan,
        DateTime CreatedAt,
        DateTime? CapturedAt);
}
