// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payout/GetProviderPayouts/GetProviderPayoutsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Payout.GetProviderPayouts
{
    public sealed record GetProviderPayoutsQuery(
        Guid ProviderId,
        string? Status = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<List<PayoutDetailsDto>>;
}
