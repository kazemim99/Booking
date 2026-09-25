// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Payout/GetPendingPayouts/GetPendingPayoutsQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Payout.GetPendingPayouts
{
    public sealed record GetPendingPayoutsQuery(
        DateTime? BeforeDate = null) : IQuery<List<PayoutSummaryDto>>;
}
