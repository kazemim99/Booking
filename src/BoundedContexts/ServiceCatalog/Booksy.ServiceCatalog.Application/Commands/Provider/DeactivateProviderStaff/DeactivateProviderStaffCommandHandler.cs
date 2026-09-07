// ========================================
// Application/Commands/Provider/DeactivateProviderStaff/DeactivateProviderStaffCommandHandler.cs
//
// Removes a person from an organization's staff, as issued by the legacy Vue admin route
// DELETE /api/v1/Providers/{id}/staff/{staffId}.
//
// This file was previously commented out in its entirety, so no handler was registered for
// DeactivateProviderStaffCommand and every call to that endpoint threw
// InvalidOperationException from MediatR — a 500 on a destructive admin action.
//
// The `staffId` this receives is whatever GetProviderStaffQueryHandler put in StaffDto.Id, and
// that roster is deliberately mixed: a MembershipId for membership rows, and a legacy
// individual sub-provider's ProviderId for rows not yet migrated. So the id is resolved the same
// way BookableResourceResolver resolves a booking's StaffId — membership first, then legacy
// sub-provider — rather than assuming one shape.
//
// Membership removal reuses the same invariants as TerminateMembershipCommandHandler: only an
// owner of the organization (or the member themselves) may do it, and an organization can never
// lose its last active owner.
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.DeactivateProviderStaff
{
    public sealed class DeactivateProviderStaffCommandHandler
        : ICommandHandler<DeactivateProviderStaffCommand, DeactivateProviderStaffResult>
    {
        private readonly IOrganizationMembershipRepository _membershipRepository;
        private readonly IMembershipAuditRepository _auditRepository;
        private readonly IProviderReadRepository _providerReadRepository;
        private readonly IProviderWriteRepository _providerWriteRepository;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DeactivateProviderStaffCommandHandler> _logger;

        public DeactivateProviderStaffCommandHandler(
            IOrganizationMembershipRepository membershipRepository,
            IMembershipAuditRepository auditRepository,
            IProviderReadRepository providerReadRepository,
            IProviderWriteRepository providerWriteRepository,
            IServiceCatalogUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DeactivateProviderStaffCommandHandler> logger)
        {
            _membershipRepository = membershipRepository;
            _auditRepository = auditRepository;
            _providerReadRepository = providerReadRepository;
            _providerWriteRepository = providerWriteRepository;
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<DeactivateProviderStaffResult> Handle(
            DeactivateProviderStaffCommand request,
            CancellationToken cancellationToken)
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr))
                throw new UnauthorizedAccessException("User not authenticated");

            var callerId = UserId.From(userIdStr);
            var organizationId = ProviderId.From(request.ProviderId);

            var organization = await _providerReadRepository.GetByIdAsync(organizationId, cancellationToken)
                ?? throw new NotFoundException($"Provider {request.ProviderId} not found");

            var membership = await _membershipRepository.GetByIdAsync(request.StaffId, cancellationToken);
            if (membership is not null)
            {
                if (!membership.OrganizationId.Equals(organizationId))
                    throw new NotFoundException(
                        $"Membership {request.StaffId} does not belong to provider {request.ProviderId}");

                return await TerminateMembershipAsync(
                    membership, organization.OwnerId, callerId, request, cancellationToken);
            }

            return await DeactivateLegacySubProviderAsync(
                organizationId, callerId, organization.OwnerId, request, cancellationToken);
        }

        private async Task<DeactivateProviderStaffResult> TerminateMembershipAsync(
            Domain.Aggregates.OrganizationMembershipAggregate.OrganizationMembership membership,
            UserId organizationOwnerId,
            UserId callerId,
            DeactivateProviderStaffCommand request,
            CancellationToken cancellationToken)
        {
            var callerIsSelf = membership.PersonId is not null && callerId.Equals(membership.PersonId);
            var callerIsOrgOwner = callerId.Equals(organizationOwnerId);
            if (!callerIsOrgOwner)
            {
                var callerMembership = await _membershipRepository.GetActiveByPersonAndOrganizationAsync(
                    callerId, membership.OrganizationId, cancellationToken);
                callerIsOrgOwner = callerMembership?.IsOwner == true;
            }

            if (!callerIsOrgOwner && !callerIsSelf)
                throw new ForbiddenException("You cannot remove this staff member.");

            // An organization must always retain at least one active owner.
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

            // Terminating an already-terminated membership is a no-op, so a retried DELETE
            // stays a 204 rather than becoming an error.
            if (membership.Status == MembershipStatus.Terminated)
            {
                _logger.LogInformation(
                    "Membership {MembershipId} already terminated; DELETE is a no-op", membership.Id);

                return new DeactivateProviderStaffResult(
                    membership.OrganizationId.Value,
                    membership.Id,
                    membership.StaffProfile?.DisplayName ?? string.Empty,
                    false,
                    membership.LeftAt ?? DateTime.UtcNow,
                    request.Reason);
            }

            membership.Terminate(request.Reason);
            await _membershipRepository.UpdateAsync(membership, cancellationToken);
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
                "Membership {MembershipId} removed from organization {OrgId} by {CallerId} (self={IsSelf})",
                membership.Id, membership.OrganizationId.Value, callerId.Value, callerIsSelf);

            return new DeactivateProviderStaffResult(
                membership.OrganizationId.Value,
                membership.Id,
                membership.StaffProfile?.DisplayName ?? string.Empty,
                false,
                membership.LeftAt!.Value,
                request.Reason);
        }

        private async Task<DeactivateProviderStaffResult> DeactivateLegacySubProviderAsync(
            ProviderId organizationId,
            UserId callerId,
            UserId organizationOwnerId,
            DeactivateProviderStaffCommand request,
            CancellationToken cancellationToken)
        {
            var staffProviderId = ProviderId.From(request.StaffId);
            var staffProvider = await _providerReadRepository.GetByIdAsync(staffProviderId, cancellationToken)
                ?? throw new NotFoundException($"Staff member {request.StaffId} not found");

            if (staffProvider.ParentProviderId is null
                || !staffProvider.ParentProviderId.Equals(organizationId))
            {
                throw new NotFoundException(
                    $"Staff member {request.StaffId} does not belong to provider {organizationId.Value}");
            }

            var callerIsOrgOwner = callerId.Equals(organizationOwnerId);
            if (!callerIsOrgOwner)
            {
                var callerMembership = await _membershipRepository.GetActiveByPersonAndOrganizationAsync(
                    callerId, organizationId, cancellationToken);
                callerIsOrgOwner = callerMembership?.IsOwner == true;
            }

            if (!callerIsOrgOwner)
                throw new ForbiddenException("You cannot remove this staff member.");

            // Provider.Deactivate throws unless the provider is Active, so anything already
            // inactive/suspended/archived is treated as a no-op — a retried DELETE stays a 204.
            if (staffProvider.Status != ProviderStatus.Active)
            {
                return new DeactivateProviderStaffResult(
                    organizationId.Value,
                    request.StaffId,
                    $"{staffProvider.OwnerFirstName} {staffProvider.OwnerLastName}".Trim(),
                    false,
                    DateTime.UtcNow,
                    request.Reason);
            }

            staffProvider.Deactivate(request.Reason);
            await _providerWriteRepository.UpdateAsync(staffProvider, cancellationToken);
            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation(
                "Legacy staff sub-provider {StaffId} deactivated for organization {OrgId} by {CallerId}",
                request.StaffId, organizationId.Value, callerId.Value);

            return new DeactivateProviderStaffResult(
                organizationId.Value,
                request.StaffId,
                $"{staffProvider.OwnerFirstName} {staffProvider.OwnerLastName}".Trim(),
                false,
                DateTime.UtcNow,
                request.Reason);
        }
    }
}
