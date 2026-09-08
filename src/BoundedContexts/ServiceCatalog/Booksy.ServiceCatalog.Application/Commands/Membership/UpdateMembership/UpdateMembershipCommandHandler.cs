using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.UpdateMembership;

/// <summary>
/// Replaces the legacy <c>UpdateProviderStaffCommand</c>, whose handler was commented out in
/// its entirety — leaving <c>PUT /api/v1/Providers/{id}/staff/{staffId}</c> as a live,
/// authorized route that threw "handler not found" on every call (FOLLOW-UPS #16).
/// </summary>
public sealed class UpdateMembershipCommandHandler
    : ICommandHandler<UpdateMembershipCommand, UpdateMembershipResult>
{
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IMembershipAuditRepository _auditRepository;
    private readonly IProviderReadRepository _providerRepository;
    private readonly IMemberBookabilityService _memberBookability;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UpdateMembershipCommandHandler> _logger;

    public UpdateMembershipCommandHandler(
        IOrganizationMembershipRepository membershipRepository,
        IMembershipAuditRepository auditRepository,
        IProviderReadRepository providerRepository,
        IMemberBookabilityService memberBookability,
        IServiceCatalogUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UpdateMembershipCommandHandler> logger)
    {
        _membershipRepository = membershipRepository;
        _auditRepository = auditRepository;
        _providerRepository = providerRepository;
        _memberBookability = memberBookability;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<UpdateMembershipResult> Handle(
        UpdateMembershipCommand request,
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

        // An owner may edit any member; a member may edit their own membership (their
        // per-salon bio and whether they are currently taking clients). Ownership is read
        // membership-first, with the Provider.OwnerId fallback for un-backfilled orgs
        // (FOLLOW-UPS #40).
        var callerMembership = await _membershipRepository.GetActiveByPersonAndOrganizationAsync(
            callerId, membership.OrganizationId, cancellationToken);
        var callerIsOrgOwner = callerMembership?.IsOwner == true || callerId.Equals(organization.OwnerId);
        var callerIsSelf = membership.PersonId is not null && callerId.Equals(membership.PersonId);

        if (!callerIsOrgOwner && !callerIsSelf)
            throw new ForbiddenException("You cannot update this membership.");

        // Enabling/disabling service provision changes the role set, so it runs through the
        // aggregate's own methods rather than poking at the StaffProfile.
        var providesServicesChanged = false;
        if (request.ProvidesServices is bool wanted && wanted != membership.ProvidesServices)
        {
            if (wanted)
                membership.EnableStaffProfile(request.BioOverride);
            else
                membership.DisableStaffProfile();

            providesServicesChanged = true;
        }

        // DisableStaffProfile drops the profile, so there is nothing left to write details to.
        if (membership.StaffProfile is not null &&
            (request.DisplayName is not null || request.BioOverride is not null || request.PhotoUrl is not null))
        {
            membership.UpdateStaffDetails(request.DisplayName, request.BioOverride, request.PhotoUrl);
        }

        // Schedule and service assignments are only meaningful for a member who
        // provides services; DisableStaffProfile above may have just removed the profile.
        var scheduleChanged = false;
        if (membership.StaffProfile is not null)
        {
            if (request.WorkingDays is not null)
            {
                membership.SetWorkingSchedule(request.WorkingDays.Select(d =>
                    StaffWorkingDay.Create(d.DayOfWeek, d.StartTime, d.EndTime)));
                scheduleChanged = true;
            }

            if (request.ServiceIds is not null)
            {
                membership.SetServiceAssignments(request.ServiceIds);
                scheduleChanged = true;
            }
        }

        await _membershipRepository.UpdateAsync(membership, cancellationToken);

        await _auditRepository.AppendAsync(
            MembershipAuditEntry.Record(
                membership.Id,
                membership.OrganizationId,
                providesServicesChanged
                    ? (membership.ProvidesServices
                        ? MembershipAuditAction.StaffProfileEnabled
                        : MembershipAuditAction.StaffProfileDisabled)
                    : MembershipAuditAction.MemberUpdated,
                membership.Status,
                subjectPersonId: membership.PersonId,
                actorPersonId: callerId,
                roles: membership.Roles),
            cancellationToken);

        // Newly service-providing members must become bookable in the same transaction,
        // exactly as they do when added or when an owner opts in. A schedule or
        // assignment edit re-syncs too, and asks for availability to be REGENERATED —
        // without that the already-generated days would keep serving the old roster.
        if (membership.ProvidesServices && (providesServicesChanged || scheduleChanged))
        {
            await _memberBookability.SyncAsync(
                membership,
                regenerateAvailability: scheduleChanged,
                cancellationToken: cancellationToken);
        }

        await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

        _logger.LogInformation(
            "Membership {MembershipId} updated by {CallerId}; providesServices={ProvidesServices}",
            membership.Id, callerId.Value, membership.ProvidesServices);

        return new UpdateMembershipResult(
            membership.Id,
            membership.OrganizationId.Value,
            membership.PersonId?.Value,
            membership.StaffProfile?.DisplayName,
            membership.StaffProfile?.BioOverride,
            membership.StaffProfile?.PhotoUrl,
            membership.ProvidesServices,
            membership.Roles.Select(r => r.ToString()).ToList(),
            membership.StaffProfile?.WorkingDays
                .Select(d => new MembershipWorkingDayInput(d.DayOfWeek, d.StartTime, d.EndTime))
                .ToList() ?? new List<MembershipWorkingDayInput>(),
            membership.StaffProfile?.ServiceIds.ToList() ?? new List<Guid>());
    }
}
