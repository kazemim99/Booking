// ========================================
// Application/Queries/Provider/GetProviderStaff/GetProviderStaffQueryHandler.cs
// Returns an organization's staff. Staff are modelled as Active Individual sub-providers
// (ParentProviderId == organization), so we read them via GetStaffByOrganizationIdAsync.
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderStaff
{
    public sealed class GetProviderStaffQueryHandler
        : IQueryHandler<GetProviderStaffQuery, GetProviderStaffResult>
    {
        private readonly IProviderReadRepository _providerRepository;

        public GetProviderStaffQueryHandler(IProviderReadRepository providerRepository)
        {
            _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        }

        public async Task<GetProviderStaffResult> Handle(
            GetProviderStaffQuery request,
            CancellationToken cancellationToken)
        {
            var organizationId = ProviderId.From(request.ProviderId);

            var organization = await _providerRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization is null)
                throw new KeyNotFoundException($"Provider {request.ProviderId} not found");

            var staff = await _providerRepository.GetStaffByOrganizationIdAsync(organizationId, cancellationToken);

            var includeInactive = request.IncludeInactive ?? false;
            var staffDtos = staff
                .Where(s => includeInactive || s.Status == ProviderStatus.Active)
                .Select(s => new StaffDto(
                    s.Id.Value,
                    s.OwnerFirstName,
                    s.OwnerLastName,
                    $"{s.OwnerFirstName} {s.OwnerLastName}".Trim(),
                    s.ContactInfo?.PrimaryPhone?.Value,
                    "Staff",
                    s.Status == ProviderStatus.Active,
                    s.RegisteredAt,
                    null,
                    null)
                {
                    Biography = string.Empty,
                    ProfilePhotoUrl = s.Profile?.ProfileImageUrl ?? string.Empty
                })
                .ToList();

            return new GetProviderStaffResult(
                organization.Id.Value,
                organization.Profile.BusinessName,
                staffDtos);
        }
    }
}
