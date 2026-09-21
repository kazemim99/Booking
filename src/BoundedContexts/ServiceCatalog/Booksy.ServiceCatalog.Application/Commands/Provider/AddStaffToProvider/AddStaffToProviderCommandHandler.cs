// ========================================
// AddStaffToProviderCommandHandler.cs
// Adds a staff member to the caller's organization as an ORGANIZATION MEMBERSHIP.
//
// This used to mint a synthetic `UserId.CreateNew()` and a shadow Individual
// sub-provider — an account-less duplicate person with no identity. It now creates
// a real membership:
//   • phone given and it belongs to a known person  → membership linked to them
//   • otherwise                                     → an UNCLAIMED membership that
//     carries a display name and is bookable immediately; when that person later
//     accepts an invitation on their phone the membership is claimed and keeps its
//     history and bookings.
// ========================================
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

using Booksy.ServiceCatalog.Application.Services.Notifications;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.AddStaffToProvider;

public sealed class AddStaffToProviderCommandHandler
    : ICommandHandler<AddStaffToProviderCommand, AddStaffToProviderResult>
{
    private readonly IProviderReadRepository _providerRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IMembershipAuditRepository _auditRepository;
    private readonly IPersonDirectory _personDirectory;
    private readonly IMemberBookabilityService _memberBookability;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Services.Notifications.INotificationRaiser _notifications;
    private readonly ILogger<AddStaffToProviderCommandHandler> _logger;

    /// <summary>A membership change is about the salon, for the inbox's tap target.</summary>
    private const string ProviderSubject = "Provider";

    public AddStaffToProviderCommandHandler(
        IProviderReadRepository providerRepository,
        IOrganizationMembershipRepository membershipRepository,
        IMembershipAuditRepository auditRepository,
        IPersonDirectory personDirectory,
        IMemberBookabilityService memberBookability,
        IServiceCatalogUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        Services.Notifications.INotificationRaiser notifications,
        ILogger<AddStaffToProviderCommandHandler> logger)
    {
        _providerRepository = providerRepository;
        _membershipRepository = membershipRepository;
        _auditRepository = auditRepository;
        _personDirectory = personDirectory;
        _memberBookability = memberBookability;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<AddStaffToProviderResult> Handle(
        AddStaffToProviderCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
            throw new ArgumentException("First name is required", nameof(request));

        var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr))
            throw new UnauthorizedAccessException("User not authenticated");
        var callerId = UserId.From(userIdStr);

        // Resolve the organization the CALLER ASKED FOR, then authorize against it.
        //
        // This used to be GetByOwnerIdAsync(callerId) — it ignored request.ProviderId and
        // silently used the caller's own salon instead. A request naming someone else's
        // salon therefore returned 201 Created and added the member to the caller's salon:
        // the route parameter was a lie, and the endpoint's documented 403 could never
        // happen. Silently retargeting a write is worse than refusing it.
        var organizationId = ProviderId.From(request.ProviderId);
        var organization = await _providerRepository.GetByIdAsync(organizationId, cancellationToken)
            ?? throw new NotFoundException($"Provider {request.ProviderId} not found.");

        // Running the salon — adding to its team — is an Owner/Manager capability. Read
        // membership-first, with Provider.OwnerId as the migration-only fallback for
        // organizations registered before ownership moved onto memberships (FOLLOW-UPS #40).
        var callerMembership = await _membershipRepository.GetActiveByPersonAndOrganizationAsync(
            callerId, organizationId, cancellationToken);

        var callerMayManage =
            callerMembership?.Roles.Contains(MembershipRole.Owner) == true ||
            callerMembership?.Roles.Contains(MembershipRole.Manager) == true ||
            callerId.Equals(organization.OwnerId);

        if (!callerMayManage)
            throw new ForbiddenException("You cannot add staff to this organization.");

        var displayName = $"{request.FirstName} {request.LastName}".Trim();

        // Link to a real person when the phone identifies one — one Person per phone.
        UserId? personId = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var person = await _personDirectory.FindByPhoneAsync(request.PhoneNumber, cancellationToken);
            if (person is not null)
            {
                personId = UserId.From(person.PersonId);

                if (await _membershipRepository.HasActiveMembershipAsync(
                        personId, organization.Id, cancellationToken))
                {
                    throw new ConflictException(
                        "This phone number already belongs to a member of your organization.");
                }
            }
        }

        OrganizationMembership membership;
        if (personId is not null)
        {
            // Known person: they are a member straight away (the owner is adding them
            // deliberately, so no invitation round-trip is required to make them bookable).
            membership = OrganizationMembership.InviteExisting(personId, organization.Id);
            membership.Accept();
            membership.EnableStaffProfile();
        }
        else
        {
            // No account behind this person (yet) — a real, bookable member the salon
            // manages on their behalf, claimable later via an invitation.
            membership = OrganizationMembership.CreateUnclaimed(organization.Id, displayName);
        }

        await _membershipRepository.SaveAsync(membership, cancellationToken);

        await _auditRepository.AppendAsync(
            MembershipAuditEntry.Record(
                membership.Id,
                membership.OrganizationId,
                MembershipAuditAction.MemberAdded,
                membership.Status,
                subjectPersonId: membership.PersonId,
                actorPersonId: callerId,
                roles: membership.Roles,
                reason: membership.IsUnclaimed ? "added without an app account" : null),
            cancellationToken);

        // Qualify for the org's services + generate availability so the member is
        // bookable immediately (the behaviour the old synthetic path provided).
        await _memberBookability.SyncAsync(membership, cancellationToken: cancellationToken);

        // Tell the member, not the owner who just added them. Skipped when the person has no account:
        // the outbox is keyed by user id and cannot reach a name, so an unclaimed member is a skip rather
        // than a notification addressed to nobody.
        if (personId is not null)
        {
            await _notifications.RaiseAsync(
                Domain.Enums.NotificationEventCode.StaffAdded,
                recipientId: personId.Value,
                dedupKey: membership.Id,
                parameters: new Dictionary<string, string>
                {
                    [NotificationParameter.BusinessName] = organization.Profile.BusinessName,
                    [NotificationParameter.StaffName] = displayName,
                },
                subjectType: ProviderSubject,
                subjectId: organization.Id.Value,
                cancellationToken: cancellationToken);
        }

        await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

        _logger.LogInformation(
            "Member {MembershipId} ({Name}) added to organization {OrgId}; unclaimed={Unclaimed}",
            membership.Id, displayName, organization.Id.Value, membership.IsUnclaimed);

        return new AddStaffToProviderResult(
            organization.Id.Value,
            membership.Id,               // the bookable resource id
            request.FirstName,
            request.LastName ?? string.Empty,
            request.Role,
            membership.IsActive,
            membership.JoinedAt ?? DateTime.UtcNow);
    }
}
