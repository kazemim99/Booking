// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Payout/GetProviderPayouts/GetProviderPayoutsQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Payout.GetProviderPayouts
{
    public sealed record GetProviderPayoutsQuery(
        Guid ProviderId,
        string? Status = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<List<PayoutDetailsDto>>;
}
