// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Payment/GetPaymentDetails/GetPaymentDetailsQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Payment.GetPaymentDetails
{
    public sealed record GetPaymentDetailsQuery(Guid PaymentId) : IQuery<PaymentDetailsViewModel?>;
}
