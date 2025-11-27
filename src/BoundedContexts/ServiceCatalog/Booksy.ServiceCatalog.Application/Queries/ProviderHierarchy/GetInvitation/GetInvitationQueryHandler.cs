using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetInvitation
{
    public sealed class GetInvitationQueryHandler : IQueryHandler<GetInvitationQuery, GetInvitationResult>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IProviderInvitationReadRepository _invitationRepository;
        private readonly ILogger<GetInvitationQueryHandler> _logger;

        public GetInvitationQueryHandler(
            IProviderReadRepository providerRepository,
            IProviderInvitationReadRepository invitationRepository,
            ILogger<GetInvitationQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _invitationRepository = invitationRepository;
            _logger = logger;
        }

        public async Task<GetInvitationResult> Handle(GetInvitationQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting invitation {InvitationId} for organization {OrganizationId}",
                request.InvitationId, request.OrganizationId);

            var organizationId = ProviderId.From(request.OrganizationId);

            // Validate organization exists
            var organization = await _providerRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new NotFoundException($"Organization with ID {request.OrganizationId} not found");

            // Get invitation by ID
            var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken);
            if (invitation == null)
                throw new NotFoundException($"Invitation with ID {request.InvitationId} not found");

            // Verify invitation belongs to the organization
            if (invitation.OrganizationId.Value != organizationId.Value)
                throw new NotFoundException($"Invitation {request.InvitationId} does not belong to organization {request.OrganizationId}");

            // Get organization details for response
            var organizationName = organization.Profile?.BusinessName ?? "سازمان";
            var organizationLogo = organization.Profile?.LogoUrl;

            return new GetInvitationResult(
                InvitationId: invitation.Id,
                OrganizationId: organizationId.Value,
                OrganizationName: organizationName,
                OrganizationLogo: organizationLogo,
                PhoneNumber: invitation.PhoneNumber.Value,
                InviteeName: invitation.InviteeName,
                Message: invitation.Message,
                Status: invitation.Status.ToString(),
                CreatedAt: invitation.CreatedAt,
                ExpiresAt: invitation.ExpiresAt,
                RespondedAt: invitation.RespondedAt);
        }
    }
}
