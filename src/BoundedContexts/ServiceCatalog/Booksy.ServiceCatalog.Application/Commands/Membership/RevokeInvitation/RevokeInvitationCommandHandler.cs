using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.RevokeInvitation;

public sealed class RevokeInvitationCommandHandler
    : ICommandHandler<RevokeInvitationCommand, RevokeInvitationResult>
{
    private readonly IProviderInvitationReadRepository _invitationReadRepository;
    private readonly IProviderInvitationWriteRepository _invitationWriteRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IProviderReadRepository _providerRepository;
    private readonly IMembershipAuditRepository _auditRepository;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RevokeInvitationCommandHandler> _logger;

    public RevokeInvitationCommandHandler(
        IProviderInvitationReadRepository invitationReadRepository,
        IProviderInvitationWriteRepository invitationWriteRepository,
        IOrganizationMembershipRepository membershipRepository,
        IProviderReadRepository providerRepository,
        IMembershipAuditRepository auditRepository,
        IServiceCatalogUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RevokeInvitationCommandHandler> logger)
    {
        _invitationReadRepository = invitationReadRepository;
        _invitationWriteRepository = invitationWriteRepository;
        _membershipRepository = membershipRepository;
        _providerRepository = providerRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<RevokeInvitationResult> Handle(
        RevokeInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr))
            throw new UnauthorizedAccessException("User not authenticated");
        var callerId = UserId.From(userIdStr);

        var invitation = await _invitationReadRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new NotFoundException($"Invitation with ID {request.InvitationId} not found");

        var organization = await _providerRepository.GetByIdAsync(invitation.OrganizationId, cancellationToken)
            ?? throw new NotFoundException("Organization not found");

        // Only an owner of the inviting organization may withdraw its invitations.
        var callerIsOwner = callerId.Equals(organization.OwnerId);
        if (!callerIsOwner)
        {
            var callerMembership = await _membershipRepository.GetActiveByPersonAndOrganizationAsync(
                callerId, invitation.OrganizationId, cancellationToken);
            callerIsOwner = callerMembership?.IsOwner == true;
        }
        if (!callerIsOwner)
            throw new UnauthorizedAccessException("Only an organization owner can revoke an invitation.");

        if (invitation.Status != InvitationStatus.Pending)
            throw new DomainValidationException(
                $"Only a pending invitation can be revoked (status: {invitation.Status}).");

        invitation.Cancel();
        await _invitationWriteRepository.UpdateAsync(invitation, cancellationToken);

        // The attempt is history: recorded, not erased.
        await _auditRepository.AppendAsync(
            MembershipAuditEntry.RecordInvitation(
                invitation.Id,
                invitation.OrganizationId,
                MembershipAuditAction.InvitationRevoked,
                actorPersonId: callerId,
                reason: request.Reason),
            cancellationToken);

        await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

        _logger.LogInformation(
            "Invitation {InvitationId} revoked by {CallerId} for organization {OrgId}",
            invitation.Id, callerId.Value, invitation.OrganizationId.Value);

        return new RevokeInvitationResult(
            invitation.Id,
            invitation.OrganizationId.Value,
            invitation.Status.ToString(),
            invitation.RespondedAt ?? DateTime.UtcNow);
    }
}
