// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payment/GetProviderEarnings/GetProviderEarningsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetProviderEarnings
{
    public sealed record GetProviderEarningsQuery(
        Guid ProviderId,
        DateTime StartDate,
        DateTime EndDate,
        decimal? CommissionPercentage = null) : IQuery<ProviderEarningsViewModel>;
}
