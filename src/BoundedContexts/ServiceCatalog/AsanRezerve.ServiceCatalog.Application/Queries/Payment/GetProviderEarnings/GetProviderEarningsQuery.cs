// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Payment/GetProviderEarnings/GetProviderEarningsQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Payment.GetProviderEarnings
{
    public sealed record GetProviderEarningsQuery(
        Guid ProviderId,
        DateTime StartDate,
        DateTime EndDate,
        decimal? CommissionPercentage = null) : IQuery<ProviderEarningsViewModel>;
}
