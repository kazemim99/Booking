using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.SetOwnerProvidesServices;

public sealed class SetOwnerProvidesServicesCommandHandler
    : ICommandHandler<SetOwnerProvidesServicesCommand, SetOwnerProvidesServicesResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IMembershipAuditRepository _auditRepository;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;
    private readonly IMemberBookabilityService _memberBookability;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SetOwnerProvidesServicesCommandHandler> _logger;

    public SetOwnerProvidesServicesCommandHandler(
        IProviderWriteRepository providerRepository,
        IOrganizationMembershipRepository membershipRepository,
        IMembershipAuditRepository auditRepository,
        IServiceCatalogUnitOfWork unitOfWork,
        IMemberBookabilityService memberBookability,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SetOwnerProvidesServicesCommandHandler> logger)
    {
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _membershipRepository = membershipRepository ?? throw new ArgumentNullException(nameof(membershipRepository));
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _memberBookability = memberBookability;
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SetOwnerProvidesServicesResult> Handle(
        SetOwnerProvidesServicesCommand request,
        CancellationToken cancellationToken)
    {
        var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr))
            throw new UnauthorizedAccessException("User not authenticated");

        var ownerId = UserId.From(userIdStr);

        var organization = await _providerRepository.GetByOwnerIdAsync(ownerId, cancellationToken)
            ?? throw new KeyNotFoundException("No provider found for the authenticated user.");

        // The owner's membership is the one identity record for "I run/work at this salon".
        var membership = await _membershipRepository.GetActiveByPersonAndOrganizationAsync(
            ownerId, organization.Id, cancellationToken);

        MembershipAuditAction auditAction;
        if (membership is null)
        {
            membership = OrganizationMembership.CreateOwner(ownerId, organization.Id, request.ProvidesServices);
            await _membershipRepository.SaveAsync(membership, cancellationToken);
            auditAction = MembershipAuditAction.OwnerCreated;
        }
        else
        {
            if (request.ProvidesServices)
                membership.EnableStaffProfile();
            else
                membership.DisableStaffProfile();

            auditAction = request.ProvidesServices
                ? MembershipAuditAction.StaffProfileEnabled
                : MembershipAuditAction.StaffProfileDisabled;

            await _membershipRepository.UpdateAsync(membership, cancellationToken);
        }

        // Save first, then dispatch domain events (MembershipActivated / StaffProfileEnabled),
        // consistent with the other membership handlers.
        await _auditRepository.AppendAsync(
            MembershipAuditEntry.Record(
                membership.Id,
                membership.OrganizationId,
                auditAction,
                membership.Status,
                subjectPersonId: membership.PersonId,
                actorPersonId: ownerId,
                roles: membership.Roles),
            cancellationToken);

        // A member who provides services must be immediately bookable — qualified for
        // the org's services with availability generated. Tracked here, committed below.
        await _memberBookability.SyncAsync(membership, cancellationToken);

        await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

        _logger.LogInformation(
            "Owner {OwnerId} providesServices={ProvidesServices} for organization {OrgId}; membership {MembershipId} roles=[{Roles}]",
            ownerId.Value, request.ProvidesServices, organization.Id.Value, membership.Id,
            string.Join(",", membership.Roles));

        return new SetOwnerProvidesServicesResult(
            organization.Id.Value,
            membership.Id,
            membership.ProvidesServices,
            membership.Roles.Select(r => r.ToString()).ToList());
    }
}
