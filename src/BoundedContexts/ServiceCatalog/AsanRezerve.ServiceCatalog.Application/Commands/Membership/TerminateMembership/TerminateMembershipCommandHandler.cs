using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Membership.TerminateMembership;

public sealed class TerminateMembershipCommandHandler
    : ICommandHandler<TerminateMembershipCommand, TerminateMembershipResult>
{
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IMembershipAuditRepository _auditRepository;
    private readonly IProviderReadRepository _providerRepository;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TerminateMembershipCommandHandler> _logger;

    public TerminateMembershipCommandHandler(
        IOrganizationMembershipRepository membershipRepository,
        IMembershipAuditRepository auditRepository,
        IProviderReadRepository providerRepository,
        IServiceCatalogUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TerminateMembershipCommandHandler> logger)
    {
        _membershipRepository = membershipRepository;
        _auditRepository = auditRepository;
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<TerminateMembershipResult> Handle(
        TerminateMembershipCommand request,
        CancellationToken cancellationToken)
    {
        var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr))
            throw new UnauthorizedAccessException("User not authenticated");

        var callerId = UserId.From(userIdStr);

        var membership = await _membershipRepository.GetByIdAsync(request.MembershipId, cancellationToken)
            ?? throw new NotFoundException($"Membership {request.MembershipId} not found");

        var organization = await _providerRepository.GetByIdAsync(membership.OrganizationId, cancellationToken)
            ?? throw new NotFoundException("Organization not found");

        // Authorization: an owner of the org may remove staff; a member may remove
        // (leave) their own membership.
        //
        // Ownership is read from the MEMBERSHIP first — that is the source of truth for
        // "who runs this salon". Provider.OwnerId is consulted only as a migration
        // fallback, for organizations registered before owner memberships existed and
        // not yet covered by backfill_memberships.sql STEP 1. Remove the fallback once
        // that backfill has run everywhere (FOLLOW-UPS #40).
        var callerIsSelf = membership.PersonId is not null && callerId.Equals(membership.PersonId);
        var callerMembership = await _membershipRepository.GetActiveByPersonAndOrganizationAsync(
            callerId, membership.OrganizationId, cancellationToken);
        var callerIsOrgOwner = callerMembership?.IsOwner == true || callerId.Equals(organization.OwnerId);

        if (!callerIsOrgOwner && !callerIsSelf)
            throw new ForbiddenException("You cannot terminate this membership.");

        // An organization must always keep at least one active owner.
        if (membership.IsOwner)
        {
            var orgMemberships = await _membershipRepository.GetByOrganizationAsync(
                membership.OrganizationId, cancellationToken);
            var activeOwners = orgMemberships.Count(m =>
                m.IsOwner && m.Status == MembershipStatus.Active);
            if (activeOwners <= 1)
                throw new DomainValidationException(
                    "Cannot remove the last owner of the organization.");
        }

        membership.Terminate(request.Reason);
        await _membershipRepository.UpdateAsync(membership, cancellationToken);
        // Audit: membership changes decide who acts for a business and who gets
        // paid, so every transition is recorded with actor, roles and reason.
        await _auditRepository.AppendAsync(
            MembershipAuditEntry.Record(
                membership.Id,
                membership.OrganizationId,
                MembershipAuditAction.Terminated,
                membership.Status,
                subjectPersonId: membership.PersonId,
                actorPersonId: callerId,
                roles: membership.Roles,
                reason: request.Reason),
            cancellationToken);

        await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

        _logger.LogInformation(
            "Membership {MembershipId} terminated by {CallerId} (self={IsSelf}) in org {OrgId}",
            membership.Id, callerId.Value, callerIsSelf, membership.OrganizationId.Value);

        return new TerminateMembershipResult(
            membership.Id,
            membership.OrganizationId.Value,
            membership.LeftAt!.Value);
    }
}
