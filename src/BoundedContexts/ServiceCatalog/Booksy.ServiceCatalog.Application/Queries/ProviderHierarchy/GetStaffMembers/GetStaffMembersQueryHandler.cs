using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetStaffMembers
{
    public sealed class GetStaffMembersQueryHandler : IQueryHandler<GetStaffMembersQuery, GetStaffMembersResult>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly ILogger<GetStaffMembersQueryHandler> _logger;

        public GetStaffMembersQueryHandler(
            IProviderReadRepository providerRepository,
            ILogger<GetStaffMembersQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _logger = logger;
        }

        public async Task<GetStaffMembersResult> Handle(GetStaffMembersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting staff members for organization {OrganizationId}", request.OrganizationId);

            var organizationId = ProviderId.From(request.OrganizationId);

            // Validate organization
            var organization = await _providerRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new NotFoundException($"Organization with ID {request.OrganizationId} not found");

            if (organization.HierarchyType != ProviderHierarchyType.Organization)
                throw new DomainValidationException("Provider is not an organization");

            // Get staff members
            var staffMembers = await _providerRepository.GetStaffByOrganizationIdAsync(organizationId, cancellationToken);

            var staffDtos = staffMembers.Select(s => new StaffMemberDto(
                ProviderId: s.Id.Value,
                BusinessName: s.Profile.BusinessName,
                ProfileImageUrl: s.Profile.ProfileImageUrl,
                Status: s.Status.ToString(),
                JoinedAt: s.CreatedAt)).ToList();

            return new GetStaffMembersResult(
                OrganizationId: organizationId.Value,
                StaffMembers: staffDtos);
        }
    }
}
