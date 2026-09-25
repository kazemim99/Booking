// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Provider/GetProviderProfile/GetProviderProfileQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetProviderProfile
{
    /// <summary>
    /// Query to get comprehensive provider profile for customer-facing pages
    /// Aggregates all data needed for rich profile display in single call
    /// </summary>
    public sealed record GetProviderProfileQuery(
        Guid ProviderId,
        int ReviewsLimit = 5,
        int ServicesLimit = 20,
        int AvailabilityDays = 7) : IQuery<ProviderProfileViewModel>;
}
