using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CancelInvitation
{
    public sealed class CancelInvitationCommandHandler : ICommandHandler<CancelInvitationCommand, CancelInvitationResult>
    {
        private readonly IProviderInvitationReadRepository _invitationReadRepository;
        private readonly IProviderInvitationWriteRepository _invitationWriteRepository;
        private readonly ILogger<CancelInvitationCommandHandler> _logger;

        public CancelInvitationCommandHandler(
            IProviderInvitationReadRepository invitationReadRepository,
            IProviderInvitationWriteRepository invitationWriteRepository,
            ILogger<CancelInvitationCommandHandler> logger)
        {
            _invitationReadRepository = invitationReadRepository;
            _invitationWriteRepository = invitationWriteRepository;
            _logger = logger;
        }

        public async Task<CancelInvitationResult> Handle(CancelInvitationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cancelling invitation {InvitationId} for organization {OrganizationId}",
                request.InvitationId, request.OrganizationId);

            // Get the invitation
            var invitation = await _invitationReadRepository.GetByIdAsync(request.InvitationId, cancellationToken);

            if (invitation == null)
                throw new NotFoundException($"Invitation with ID {request.InvitationId} not found");

            // Verify the invitation belongs to the organization
            var organizationId = ProviderId.From(request.OrganizationId);
            if (invitation.OrganizationId != organizationId)
                throw new DomainValidationException("Invitation does not belong to this organization");

            // Cancel the invitation (this will throw if status is not Pending)
            invitation.Cancel();

            _logger.LogInformation("Invitation {InvitationId} cancelled successfully", invitation.Id);

            await _invitationWriteRepository.UpdateAsync(invitation, cancellationToken);

            return new CancelInvitationResult(
                InvitationId: invitation.Id,
                Success: true,
                Message: "Invitation cancelled successfully");
        }
    }
}
