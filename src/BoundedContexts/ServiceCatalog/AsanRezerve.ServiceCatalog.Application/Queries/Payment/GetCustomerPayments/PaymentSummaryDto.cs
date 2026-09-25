// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Payment/GetCustomerPayments/PaymentSummaryDto.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Queries.Payment.GetCustomerPayments
{
    public sealed record PaymentSummaryDto(
        Guid PaymentId,
        Guid? BookingId,
        Guid ProviderId,
        decimal Amount,
        string Currency,
        string Status,
        string Method,
        string? Description,
        DateTime CreatedAt,
        DateTime? CapturedAt);
}
