using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    /// <summary>A salon's customer book. Every query is scoped to one salon.</summary>
    public interface IProviderCustomerRepository
    {
        Task<ProviderCustomer?> GetAsync(ProviderId providerId, Guid customerId, CancellationToken cancellationToken = default);

        Task<ProviderCustomer?> GetByPhoneAsync(ProviderId providerId, PhoneNumber phoneNumber, CancellationToken cancellationToken = default);

        /// <summary>The salon's customers, optionally filtered by a name or phone fragment, by name.</summary>
        Task<IReadOnlyList<ProviderCustomer>> ListAsync(ProviderId providerId, string? search, CancellationToken cancellationToken = default);

        /// <summary>Which of these phones the salon already has (for import).</summary>
        Task<IReadOnlySet<string>> ExistingPhonesAsync(ProviderId providerId, IReadOnlyCollection<string> phoneValues, CancellationToken cancellationToken = default);

        Task AddAsync(ProviderCustomer customer, CancellationToken cancellationToken = default);

        void Remove(ProviderCustomer customer);

        /// <summary>Per customer: the salon's bookings made for them (cancelled ones not counted).</summary>
        Task<IReadOnlyDictionary<Guid, ProviderCustomerBookingStats>> BookingStatsAsync(
            ProviderId providerId, DateTime nowUtc, CancellationToken cancellationToken = default);
    }

    public sealed record ProviderCustomerBookingStats(int Total, int Upcoming, DateTime? LastBookingAt);
}
