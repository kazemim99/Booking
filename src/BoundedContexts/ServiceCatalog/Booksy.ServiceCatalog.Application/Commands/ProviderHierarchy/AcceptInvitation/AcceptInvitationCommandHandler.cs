using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.AcceptInvitation
{
    public sealed class AcceptInvitationCommandHandler : ICommandHandler<AcceptInvitationCommand, AcceptInvitationResult>
    {
        private readonly IProviderReadRepository _providerReadRepository;
        private readonly IProviderWriteRepository _providerWriteRepository;
        private readonly IProviderInvitationReadRepository _invitationReadRepository;
        private readonly IProviderInvitationWriteRepository _invitationWriteRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AcceptInvitationCommandHandler> _logger;

        public AcceptInvitationCommandHandler(
            IProviderReadRepository providerReadRepository,
            IProviderWriteRepository providerWriteRepository,
            IProviderInvitationReadRepository invitationReadRepository,
            IProviderInvitationWriteRepository invitationWriteRepository,
            IUnitOfWork unitOfWork,
            ILogger<AcceptInvitationCommandHandler> logger)
        {
            _providerReadRepository = providerReadRepository;
            _providerWriteRepository = providerWriteRepository;
            _invitationReadRepository = invitationReadRepository;
            _invitationWriteRepository = invitationWriteRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<AcceptInvitationResult> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Accepting invitation {InvitationId} by provider {ProviderId}",
                request.InvitationId, request.IndividualProviderId);

            // Get invitation
            var invitation = await _invitationReadRepository.GetByIdAsync(request.InvitationId, cancellationToken);
            if (invitation == null)
                throw new NotFoundException($"Invitation with ID {request.InvitationId} not found");

            if (invitation.Status != InvitationStatus.Pending)
                throw new DomainValidationException($"Invitation is no longer pending (status: {invitation.Status})");

            // Get individual provider
            var individualProviderId = ProviderId.From(request.IndividualProviderId);
            var individualProvider = await _providerReadRepository.GetByIdAsync(individualProviderId, cancellationToken);

            if (individualProvider == null)
                throw new NotFoundException($"Provider with ID {request.IndividualProviderId} not found");

            if (individualProvider.HierarchyType != ProviderHierarchyType.Individual)
                throw new DomainValidationException("Only individual providers can accept invitations");

            if (individualProvider.ParentProviderId != null)
                throw new DomainValidationException("Provider is already linked to an organization");

            // Accept invitation and link provider
            invitation.Accept(individualProviderId);
            individualProvider.LinkToOrganization(invitation.OrganizationId);

            await _invitationWriteRepository.UpdateAsync(invitation, cancellationToken);
            await _providerWriteRepository.UpdateAsync(individualProvider, cancellationToken);

            _logger.LogInformation("Invitation {InvitationId} accepted. Provider {ProviderId} linked to organization {OrganizationId}",
                invitation.Id, individualProviderId, invitation.OrganizationId);

            return new AcceptInvitationResult(
                InvitationId: invitation.Id,
                OrganizationId: invitation.OrganizationId.Value,
                IndividualProviderId: individualProvider.Id.Value,
                AcceptedAt: invitation.RespondedAt!.Value);
        }
    }
}
