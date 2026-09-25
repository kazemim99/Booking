// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Payment/GetCustomerPayments/GetCustomerPaymentsQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Payment.GetCustomerPayments
{
    public sealed record GetCustomerPaymentsQuery(
        Guid CustomerId,
        string? Status = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<List<PaymentSummaryDto>>;
}
