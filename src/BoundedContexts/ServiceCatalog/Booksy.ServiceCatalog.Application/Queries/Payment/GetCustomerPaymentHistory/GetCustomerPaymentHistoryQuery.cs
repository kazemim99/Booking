// ========================================
// GetCustomerPaymentHistoryQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetCustomerPaymentHistory
{
    public sealed record GetCustomerPaymentHistoryQuery(
        Guid CustomerId,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        int Page = 1,
        int PageSize = 20) : IQuery<PaymentHistoryViewModel>;
}
