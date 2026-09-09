// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payout/GetPayoutById/GetPayoutByIdQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Queries.Payout.GetProviderPayouts;

namespace Booksy.ServiceCatalog.Application.Queries.Payout.GetPayoutById
{
    /// <summary>
    /// Reads one payout. Returns null when there is no such payout, so the caller decides the
    /// status code.
    /// </summary>
    public sealed record GetPayoutByIdQuery(Guid PayoutId) : IQuery<PayoutDetailsDto?>;
}
