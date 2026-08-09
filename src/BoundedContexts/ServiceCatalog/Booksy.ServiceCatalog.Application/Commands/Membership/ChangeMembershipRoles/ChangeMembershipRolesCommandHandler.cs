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

namespace Booksy.ServiceCatalog.Application.Commands.Membership.ChangeMembershipRoles;

public sealed class ChangeMembershipRolesCommandHandler
    : ICommandHandler<ChangeMembershipRolesCommand, ChangeMembershipRolesResult>
{
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IMembershipAuditRepository _auditRepository;
    private readonly IProviderReadRepository _providerRepository;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ChangeMembershipRolesCommandHandler> _logger;

    public ChangeMembershipRolesCommandHandler(
        IOrganizationMembershipRepository membershipRepository,
        IMembershipAuditRepository auditRepository,
        IProviderReadRepository providerRepository,
        IServiceCatalogUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ChangeMembershipRolesCommandHandler> logger)
    {
        _membershipRepository = membershipRepository;
        _auditRepository = auditRepository;
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ChangeMembershipRolesResult> Handle(
        ChangeMembershipRolesCommand request,
        CancellationToken cancellationToken)
    {
        var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr))
            throw new UnauthorizedAccessException("User not authenticated");
        var callerId = UserId.From(userIdStr);

        var roles = ParseRoles(request.Roles);

        var membership = await _membershipRepository.GetByIdAsync(request.MembershipId, cancellationToken)
            ?? throw new NotFoundException($"Membership {request.MembershipId} not found");

        var organization = await _providerRepository.GetByIdAsync(membership.OrganizationId, cancellationToken)
            ?? throw new NotFoundException("Organization not found");

        // Only an owner of the organization may change roles.
        var callerIsOrgOwner = callerId.Equals(organization.OwnerId);
        if (!callerIsOrgOwner)
        {
            var callerMembership = await _membershipRepository.GetActiveByPersonAndOrganizationAsync(
                callerId, membership.OrganizationId, cancellationToken);
            callerIsOrgOwner = callerMembership?.IsOwner == true;
        }
        if (!callerIsOrgOwner)
            throw new UnauthorizedAccessException("Only an organization owner can change member roles.");

        // Keep at least one active owner: block demoting the last owner.
        var demotingAnOwner = membership.IsOwner && !roles.Contains(MembershipRole.Owner);
        if (demotingAnOwner)
        {
            var orgMemberships = await _membershipRepository.GetByOrganizationAsync(
                membership.OrganizationId, cancellationToken);
            var activeOwners = orgMemberships.Count(m => m.IsOwner && m.Status == MembershipStatus.Active);
            if (activeOwners <= 1)
                throw new DomainValidationException("Cannot remove the last owner of the organization.");
        }

        membership.ChangeRoles(roles);
        await _membershipRepository.UpdateAsync(membership, cancellationToken);
        // Audit: membership changes decide who acts for a business and who gets
        // paid, so every transition is recorded with actor, roles and reason.
        await _auditRepository.AppendAsync(
            MembershipAuditEntry.Record(
                membership.Id,
                membership.OrganizationId,
                MembershipAuditAction.RolesChanged,
                membership.Status,
                subjectPersonId: membership.PersonId,
                actorPersonId: callerId,
                roles: membership.Roles,
                reason: null),
            cancellationToken);

        await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

        _logger.LogInformation(
            "Membership {MembershipId} roles changed to [{Roles}] by {CallerId}",
            membership.Id, string.Join(",", membership.Roles), callerId.Value);

        return new ChangeMembershipRolesResult(
            membership.Id,
            membership.OrganizationId.Value,
            membership.Roles.Select(r => r.ToString()).ToList(),
            membership.ProvidesServices);
    }

    private static IReadOnlyList<MembershipRole> ParseRoles(IReadOnlyList<string> roles)
    {
        if (roles is null || roles.Count == 0)
            throw new DomainValidationException("At least one role is required.");

        var parsed = new List<MembershipRole>();
        foreach (var role in roles)
        {
            if (!Enum.TryParse<MembershipRole>(role, ignoreCase: true, out var value))
                throw new DomainValidationException($"Unknown role '{role}'.");
            parsed.Add(value);
        }
        return parsed;
    }
}
