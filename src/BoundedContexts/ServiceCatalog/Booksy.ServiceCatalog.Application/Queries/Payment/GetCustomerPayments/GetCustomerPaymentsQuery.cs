// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payment/GetCustomerPayments/GetCustomerPaymentsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetCustomerPayments
{
    public sealed record GetCustomerPaymentsQuery(
        Guid CustomerId,
        string? Status = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<List<PaymentSummaryDto>>;
}
