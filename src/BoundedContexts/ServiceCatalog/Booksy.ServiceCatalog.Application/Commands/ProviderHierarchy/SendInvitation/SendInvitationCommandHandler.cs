using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.SendInvitation
{
    public sealed class SendInvitationCommandHandler : ICommandHandler<SendInvitationCommand, SendInvitationResult>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IProviderInvitationReadRepository _invitationReadRepository;
        private readonly IProviderInvitationWriteRepository _invitationWriteRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SendInvitationCommandHandler> _logger;

        public SendInvitationCommandHandler(
            IProviderReadRepository providerRepository,
            IProviderInvitationReadRepository invitationReadRepository,
            IProviderInvitationWriteRepository invitationWriteRepository,
            IUnitOfWork unitOfWork,
            ILogger<SendInvitationCommandHandler> logger)
        {
            _providerRepository = providerRepository;
            _invitationReadRepository = invitationReadRepository;
            _invitationWriteRepository = invitationWriteRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<SendInvitationResult> Handle(SendInvitationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending invitation from organization {OrganizationId} to {PhoneNumber}",
                request.OrganizationId, request.PhoneNumber);

            // Validate organization exists and is an organization type
            var organizationId = ProviderId.From(request.OrganizationId);
            var organization = await _providerRepository.GetByIdAsync(organizationId, cancellationToken);

            if (organization == null)
                throw new NotFoundException($"Organization with ID {request.OrganizationId} not found");

            if (organization.HierarchyType != ProviderHierarchyType.Organization)
                throw new DomainValidationException("Only organizations can send invitations");

            // Check for existing pending invitation to same phone number
            var existingInvitation = await _invitationReadRepository.GetByPhoneNumberAndOrganizationAsync(
                request.PhoneNumber, organizationId, cancellationToken);

            if (existingInvitation != null)
                throw new DomainValidationException($"A pending invitation already exists for phone number {request.PhoneNumber}");

            // Create invitation
            var phoneNumber = PhoneNumber.From(request.PhoneNumber);
            var invitation = ProviderInvitation.Create(
                organizationId,
                phoneNumber,
                request.InviteeName,
                request.Message);

            await _invitationWriteRepository.SaveAsync(invitation, cancellationToken);
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation("Invitation {InvitationId} sent successfully", invitation.Id);

            return new SendInvitationResult(
                InvitationId: invitation.Id,
                OrganizationId: organization.Id.Value,
                PhoneNumber: request.PhoneNumber,
                ExpiresAt: invitation.ExpiresAt,
                Status: invitation.Status.ToString());
        }
    }
}
