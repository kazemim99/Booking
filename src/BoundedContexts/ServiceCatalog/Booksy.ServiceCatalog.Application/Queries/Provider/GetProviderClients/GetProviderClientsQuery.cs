// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderClients/GetProviderClientsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderClients
{
    /// <summary>
    /// The provider's client book, derived from their bookings (one row per
    /// distinct customer, most-recent activity first).
    /// </summary>
    public sealed record GetProviderClientsQuery(Guid ProviderId)
        : IQuery<GetProviderClientsResult>;

    public sealed record GetProviderClientsResult(
        IReadOnlyList<ProviderClientDto> Clients);

    public sealed record ProviderClientDto(
        Guid CustomerId,
        string Name,
        string Phone,
        int TotalBookings,
        int CompletedBookings,
        int UpcomingBookings,
        DateTime? LastVisitAt);
}
