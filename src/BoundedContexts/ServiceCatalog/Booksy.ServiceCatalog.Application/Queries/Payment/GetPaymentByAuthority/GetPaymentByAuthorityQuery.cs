// ========================================
// GetPaymentByAuthorityQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Queries.Payment.GetPaymentDetails;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetPaymentByAuthority
{
    public sealed record GetPaymentByAuthorityQuery(string Authority) : IQuery<PaymentDetailsViewModel?>;
}
