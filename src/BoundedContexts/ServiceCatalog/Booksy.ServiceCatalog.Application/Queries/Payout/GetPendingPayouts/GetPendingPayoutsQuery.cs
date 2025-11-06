// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payout/GetPendingPayouts/GetPendingPayoutsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Payout.GetPendingPayouts
{
    public sealed record GetPendingPayoutsQuery(
        DateTime? BeforeDate = null) : IQuery<List<PayoutSummaryDto>>;
}
