using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RemoveStaffMember
{
    public sealed class RemoveStaffMemberCommandHandler : ICommandHandler<RemoveStaffMemberCommand, RemoveStaffMemberResult>
    {
        private readonly IProviderReadRepository _providerReadRepository;
        private readonly IProviderWriteRepository _providerWriteRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RemoveStaffMemberCommandHandler> _logger;

        public RemoveStaffMemberCommandHandler(
            IProviderReadRepository providerReadRepository,
            IProviderWriteRepository providerWriteRepository,
            IUnitOfWork unitOfWork,
            ILogger<RemoveStaffMemberCommandHandler> logger)
        {
            _providerReadRepository = providerReadRepository;
            _providerWriteRepository = providerWriteRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<RemoveStaffMemberResult> Handle(RemoveStaffMemberCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Removing staff member {StaffProviderId} from organization {OrganizationId}",
                request.StaffProviderId, request.OrganizationId);

            var organizationId = ProviderId.From(request.OrganizationId);
            var staffProviderId = ProviderId.From(request.StaffProviderId);

            // Validate organization
            var organization = await _providerReadRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new NotFoundException($"Organization with ID {request.OrganizationId} not found");

            if (organization.HierarchyType != ProviderHierarchyType.Organization)
                throw new DomainValidationException("Provider is not an organization");

            // Validate staff member
            var staffProvider = await _providerReadRepository.GetByIdAsync(staffProviderId, cancellationToken);
            if (staffProvider == null)
                throw new NotFoundException($"Staff provider with ID {request.StaffProviderId} not found");

            if (staffProvider.ParentProviderId != organizationId)
                throw new DomainValidationException("Staff member is not linked to this organization");

            // Unlink staff member
            staffProvider.UnlinkFromOrganization(request.Reason);

            await _providerWriteRepository.UpdateAsync(staffProvider, cancellationToken);

            _logger.LogInformation("Staff member {StaffProviderId} removed from organization {OrganizationId}",
                staffProviderId, organizationId);

            return new RemoveStaffMemberResult(
                OrganizationId: organizationId.Value,
                StaffProviderId: staffProviderId.Value,
                RemovedAt: DateTime.UtcNow);
        }
    }
}
