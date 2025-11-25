using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    public interface IProviderJoinRequestReadRepository : IReadRepository<ProviderJoinRequest, Guid>
    {
        Task<IReadOnlyList<ProviderJoinRequest>> GetByOrganizationIdAsync(
            ProviderId organizationId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProviderJoinRequest>> GetByOrganizationIdAndStatusAsync(
            ProviderId organizationId,
            JoinRequestStatus status,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProviderJoinRequest>> GetByRequesterIdAsync(
            ProviderId requesterId,
            CancellationToken cancellationToken = default);

        Task<ProviderJoinRequest?> GetPendingByRequesterAndOrganizationAsync(
            ProviderId requesterId,
            ProviderId organizationId,
            CancellationToken cancellationToken = default);

        Task<int> CountPendingByOrganizationAsync(
            ProviderId organizationId,
            CancellationToken cancellationToken = default);

        Task<bool> HasPendingRequestAsync(
            ProviderId requesterId,
            ProviderId organizationId,
            CancellationToken cancellationToken = default);
    }
}
