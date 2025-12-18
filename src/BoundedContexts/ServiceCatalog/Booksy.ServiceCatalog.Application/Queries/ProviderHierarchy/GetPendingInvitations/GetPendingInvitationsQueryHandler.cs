using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetPendingInvitations
{
    public sealed class GetPendingInvitationsQueryHandler : IQueryHandler<GetPendingInvitationsQuery, GetPendingInvitationsResult>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IProviderInvitationReadRepository _invitationRepository;
        private readonly ILogger<GetPendingInvitationsQueryHandler> _logger;

        public GetPendingInvitationsQueryHandler(
            IProviderReadRepository providerRepository,
            IProviderInvitationReadRepository invitationRepository,
            ILogger<GetPendingInvitationsQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _invitationRepository = invitationRepository;
            _logger = logger;
        }

        public async Task<GetPendingInvitationsResult> Handle(GetPendingInvitationsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting pending invitations for organization {OrganizationId}", request.OrganizationId);

            var organizationId = ProviderId.From(request.OrganizationId);

            // Validate organization
            var organization = await _providerRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new NotFoundException($"Organization with ID {request.OrganizationId} not found");

            if (organization.HierarchyType != ProviderHierarchyType.Organization)
                throw new DomainValidationException("Provider is not an organization");

            // Get pending invitations
            var invitations = await _invitationRepository.GetByOrganizationIdAndStatusAsync(
                organizationId, InvitationStatus.Pending, cancellationToken);

            var invitationDtos = invitations.Select(i => new InvitationDto(
                InvitationId: i.Id,
                PhoneNumber: i.PhoneNumber.Value,
                InviteeName: i.InviteeName,
                Message: i.Message,
                Status: i.Status.ToString(),
                CreatedAt: i.CreatedAt,
                ExpiresAt: i.ExpiresAt)).ToList();

            return new GetPendingInvitationsResult(
                OrganizationId: organizationId.Value,
                Invitations: invitationDtos);
        }
    }
}
