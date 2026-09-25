// ========================================
// GetPaymentByAuthorityQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Application.Queries.Payment.GetPaymentDetails;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Payment.GetPaymentByAuthority
{
    public sealed record GetPaymentByAuthorityQuery(string Authority) : IQuery<PaymentDetailsViewModel?>;
}
