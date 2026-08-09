using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Identity;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.AcceptInvitationAsMember;

public sealed class AcceptInvitationAsMemberCommandHandler
    : ICommandHandler<AcceptInvitationAsMemberCommand, AcceptInvitationAsMemberResult>
{
    private readonly IProviderInvitationReadRepository _invitationReadRepository;
    private readonly IProviderInvitationWriteRepository _invitationWriteRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IMembershipAuditRepository _auditRepository;
    private readonly IPersonDirectory _personDirectory;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;
    private readonly IMemberBookabilityService _memberBookability;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AcceptInvitationAsMemberCommandHandler> _logger;

    public AcceptInvitationAsMemberCommandHandler(
        IProviderInvitationReadRepository invitationReadRepository,
        IProviderInvitationWriteRepository invitationWriteRepository,
        IOrganizationMembershipRepository membershipRepository,
        IMembershipAuditRepository auditRepository,
        IPersonDirectory personDirectory,
        IServiceCatalogUnitOfWork unitOfWork,
        IMemberBookabilityService memberBookability,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AcceptInvitationAsMemberCommandHandler> logger)
    {
        _invitationReadRepository = invitationReadRepository;
        _invitationWriteRepository = invitationWriteRepository;
        _membershipRepository = membershipRepository;
        _auditRepository = auditRepository;
        _personDirectory = personDirectory;
        _unitOfWork = unitOfWork;
        _memberBookability = memberBookability;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<AcceptInvitationAsMemberResult> Handle(
        AcceptInvitationAsMemberCommand request,
        CancellationToken cancellationToken)
    {
        var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr))
            throw new UnauthorizedAccessException("User not authenticated");

        var personId = UserId.From(userIdStr);

        var invitation = await _invitationReadRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new NotFoundException($"Invitation with ID {request.InvitationId} not found");

        if (invitation.Status != InvitationStatus.Pending)
            throw new DomainValidationException($"Invitation is no longer pending (status: {invitation.Status})");

        // Security: the caller may only accept an invitation sent to their own phone.
        var invitedPerson = await _personDirectory.FindByPhoneAsync(invitation.PhoneNumber.Value, cancellationToken);
        if (invitedPerson is null || invitedPerson.PersonId != personId.Value)
            throw new DomainValidationException("This invitation was sent to a different phone number.");

        // Reuse an existing membership if the person is already a (re)joining member; otherwise
        // create one. Either way the identity is the existing person — never a new account.
        var membership = await _membershipRepository.GetActiveByPersonAndOrganizationAsync(
            personId, invitation.OrganizationId, cancellationToken);

        if (membership is null)
        {
            membership = OrganizationMembership.InviteExisting(personId, invitation.OrganizationId);
            membership.Accept();
            await _membershipRepository.SaveAsync(membership, cancellationToken);
        }

        invitation.AcceptByMember();
        await _invitationWriteRepository.UpdateAsync(invitation, cancellationToken);

        // The accepted member becomes bookable straight away (qualification +
        // availability), so customers can book them the moment they join.
        await _memberBookability.SyncAsync(membership, cancellationToken);

        // Audit: membership changes decide who acts for a business and who gets
        // paid, so every transition is recorded with actor, roles and reason.
        await _auditRepository.AppendAsync(
            MembershipAuditEntry.Record(
                membership.Id,
                membership.OrganizationId,
                MembershipAuditAction.Accepted,
                membership.Status,
                subjectPersonId: membership.PersonId,
                actorPersonId: personId,
                roles: membership.Roles,
                reason: null),
            cancellationToken);

        // Save first, then dispatch domain events (welcome SMS etc.).
        await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

        _logger.LogInformation(
            "Invitation {InvitationId} accepted by person {PersonId}; membership {MembershipId} active in organization {OrgId}",
            invitation.Id, personId.Value, membership.Id, invitation.OrganizationId.Value);

        return new AcceptInvitationAsMemberResult(
            InvitationId: invitation.Id,
            OrganizationId: invitation.OrganizationId.Value,
            MembershipId: membership.Id,
            PersonId: personId.Value,
            Roles: membership.Roles.Select(r => r.ToString()).ToList(),
            AcceptedAt: invitation.RespondedAt!.Value);
    }
}
