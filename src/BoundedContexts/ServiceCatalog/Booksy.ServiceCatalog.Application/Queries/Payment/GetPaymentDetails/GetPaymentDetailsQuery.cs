// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payment/GetPaymentDetails/GetPaymentDetailsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetPaymentDetails
{
    public sealed record GetPaymentDetailsQuery(Guid PaymentId) : IQuery<PaymentDetailsViewModel?>;
}
