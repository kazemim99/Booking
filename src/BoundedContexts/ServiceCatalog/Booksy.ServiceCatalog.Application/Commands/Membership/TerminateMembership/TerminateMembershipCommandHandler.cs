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

namespace Booksy.ServiceCatalog.Application.Commands.Membership.TerminateMembership;

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
        var callerIsSelf = membership.PersonId is not null && callerId.Equals(membership.PersonId);
        var callerIsOrgOwner = callerId.Equals(organization.OwnerId);
        if (!callerIsOrgOwner)
        {
            var callerMembership = await _membershipRepository.GetActiveByPersonAndOrganizationAsync(
                callerId, membership.OrganizationId, cancellationToken);
            callerIsOrgOwner = callerMembership?.IsOwner == true;
        }

        if (!callerIsOrgOwner && !callerIsSelf)
            throw new UnauthorizedAccessException("You cannot terminate this membership.");

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
