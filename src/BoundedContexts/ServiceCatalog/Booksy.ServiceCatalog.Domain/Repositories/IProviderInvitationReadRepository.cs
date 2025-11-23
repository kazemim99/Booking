using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
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
