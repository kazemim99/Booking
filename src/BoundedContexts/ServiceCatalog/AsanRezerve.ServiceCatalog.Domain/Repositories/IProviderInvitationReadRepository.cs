using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Repositories
{
    public interface IProviderInvitationReadRepository : IReadRepository<ProviderInvitation, Guid>
    {
        Task<IReadOnlyList<ProviderInvitation>> GetByOrganizationIdAsync(
            ProviderId organizationId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProviderInvitation>> GetByOrganizationIdAndStatusAsync(
            ProviderId organizationId,
            InvitationStatus status,
            CancellationToken cancellationToken = default);

        Task<ProviderInvitation?> GetByPhoneNumberAndOrganizationAsync(
            string phoneNumber,
            ProviderId organizationId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProviderInvitation>> GetPendingByPhoneNumberAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProviderInvitation>> GetExpiredInvitationsAsync(
            CancellationToken cancellationToken = default);

        Task<int> CountPendingByOrganizationAsync(
            ProviderId organizationId,
            CancellationToken cancellationToken = default);
    }
}
