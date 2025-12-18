// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetQualifiedStaff/GetQualifiedStaffQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Service.GetQualifiedStaff
{
    public sealed class GetQualifiedStaffQueryHandler : IQueryHandler<GetQualifiedStaffQuery, GetQualifiedStaffResult>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<GetQualifiedStaffQueryHandler> _logger;

        public GetQualifiedStaffQueryHandler(
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            ILogger<GetQualifiedStaffQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<GetQualifiedStaffResult> Handle(GetQualifiedStaffQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Getting qualified staff for Provider {ProviderId}, Service {ServiceId}",
                request.ProviderId, request.ServiceId);

            // Load provider (organization)
            var provider = await _providerRepository.GetByIdAsync(
                ProviderId.From(request.ProviderId),
                cancellationToken);

            if (provider == null)
                throw new NotFoundException($"Provider with ID {request.ProviderId} not found");

            // Load service
            var service = await _serviceRepository.GetByIdAsync(
                ServiceId.From(request.ServiceId),
                cancellationToken);

            if (service == null)
                throw new NotFoundException($"Service with ID {request.ServiceId} not found");

            // Verify the service belongs to this provider
            if (service.ProviderId.Value != request.ProviderId)
                throw new NotFoundException($"Service {request.ServiceId} does not belong to provider {request.ProviderId}");

            // Get all staff members (individual providers) for this organization
            var staffMembers = await _providerRepository.GetStaffByOrganizationIdAsync(
                provider.Id,
                cancellationToken);

            // Filter for active and qualified staff
            var qualifiedStaff = staffMembers
                .Where(s => s.Status == ProviderStatus.Active)
                .Select(s => new StaffMemberDto(
                    s.Id.Value,
                    GetStaffName(s),
                    s.Profile.ProfileImageUrl,
                    null, // Rating - not available in current domain model
                    null, // ReviewCount - not available in current domain model
                    null  // Specialization - not available in current domain model
                ))
                .ToList();

            _logger.LogInformation(
                "Found {TotalStaff} staff members, {QualifiedCount} are active and qualified for service {ServiceId}",
                staffMembers.Count, qualifiedStaff.Count, service.Id);

            return new GetQualifiedStaffResult(
                request.ProviderId,
                request.ServiceId,
                qualifiedStaff);
        }

        private string GetStaffName(Domain.Aggregates.Provider staffMember)
        {
            var name = $"{staffMember.OwnerFirstName} {staffMember.OwnerLastName}".Trim();
            if (string.IsNullOrEmpty(name))
                name = staffMember.Profile.BusinessName;
            return name;
        }
    }
}
