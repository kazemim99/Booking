using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;
using ProviderAggregate = Booksy.ServiceCatalog.Domain.Aggregates.Provider;
using ServiceAggregate = Booksy.ServiceCatalog.Domain.Aggregates.Service;
using DomainDayOfWeek = Booksy.ServiceCatalog.Domain.Enums.DayOfWeek;

namespace Booksy.ServiceCatalog.Application.Services;

/// <inheritdoc cref="IMemberBookabilityService"/>
public sealed class MemberBookabilityService : IMemberBookabilityService
{
    /// <summary>Rolling window of generated availability.</summary>
    private const int AvailabilityDaysAhead = 30;
    private const int SlotMinutes = 30;

    private readonly IProviderReadRepository _providerRepository;
    private readonly IServiceWriteRepository _serviceRepository;
    private readonly IProviderAvailabilityWriteRepository _availabilityRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly ILogger<MemberBookabilityService> _logger;

    public MemberBookabilityService(
        IProviderReadRepository providerRepository,
        IServiceWriteRepository serviceRepository,
        IProviderAvailabilityWriteRepository availabilityRepository,
        IOrganizationMembershipRepository membershipRepository,
        ILogger<MemberBookabilityService> logger)
    {
        _providerRepository = providerRepository;
        _serviceRepository = serviceRepository;
        _availabilityRepository = availabilityRepository;
        _membershipRepository = membershipRepository;
        _logger = logger;
    }

    public async Task<MemberBookabilityResult> SyncAsync(
        OrganizationMembership membership,
        bool regenerateAvailability = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(membership);

        // Only an active, service-providing member is a bookable resource.
        if (!membership.ProvidesServices || membership.Status != MembershipStatus.Active)
            return MemberBookabilityResult.None;

        var organization = await _providerRepository.GetByIdAsync(
            membership.OrganizationId, cancellationToken);

        if (organization is null)
            return MemberBookabilityResult.None;

        var qualified = await QualifyForOrganizationServicesAsync(
            organization, membership, cancellationToken);

        if (regenerateAvailability)
        {
            await _availabilityRepository.RemoveFreeStaffSlotsFromAsync(
                organization.Id, membership.Id, DateTime.UtcNow.Date, cancellationToken);
        }

        var slots = await GenerateAvailabilityAsync(
            organization, membership, cancellationToken);

        _logger.LogInformation(
            "Member {MembershipId} of organization {OrgId} is bookable: qualified for {Qualified} service(s), {Slots} slot(s) generated",
            membership.Id, organization.Id.Value, qualified, slots);

        return new MemberBookabilityResult(qualified, slots);
    }

    public async Task<int> SyncServiceAsync(
        ServiceAggregate service,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);

        var members = await _membershipRepository.GetByOrganizationAsync(
            service.ProviderId, cancellationToken);

        var qualified = 0;
        foreach (var member in members)
        {
            if (member.Status != MembershipStatus.Active || !member.ProvidesServices)
                continue;

            // A member who has narrowed their assignments performs only what they listed.
            if (member.StaffProfile?.PerformsService(service.Id.Value) == false)
                continue;

            if (service.IsStaffQualified(member.Id))
                continue; // idempotent

            service.AddQualifiedStaff(member.Id);
            qualified++;
        }

        // Draft is the state Service.Create leaves behind; it only becomes sellable once
        // somebody can perform it. A salon with no service-providing members yet keeps the
        // service in Draft, and the next member to join activates it via SyncAsync.
        if (qualified > 0 && service.Status != ServiceStatus.Active)
            service.Activate();

        _logger.LogInformation(
            "Service {ServiceId} of organization {OrgId}: {Qualified} member(s) qualified, status {Status}",
            service.Id.Value, service.ProviderId.Value, qualified, service.Status);

        return qualified;
    }

    /// <summary>
    /// Qualifies the member for the organization's services, so the booking engine
    /// will consider them. A Draft service becomes bookable once it has a qualified
    /// member.
    /// </summary>
    /// <remarks>
    /// A member with no service assignments performs everything the salon offers —
    /// the common case, and what every member did before assignments existed. Once a
    /// salon does assign services, this also REMOVES the member from the ones they no
    /// longer perform, so narrowing an assignment actually takes effect.
    /// </remarks>
    private async Task<int> QualifyForOrganizationServicesAsync(
        ProviderAggregate organization,
        OrganizationMembership membership,
        CancellationToken cancellationToken)
    {
        var services = await _serviceRepository.GetServicesByProviderIdAsync(
            organization.Id, cancellationToken);

        var membershipId = membership.Id;
        var profile = membership.StaffProfile;

        var qualified = 0;
        foreach (var service in services)
        {
            var shouldPerform = profile?.PerformsService(service.Id.Value) ?? true;
            var isQualified = service.IsStaffQualified(membershipId);

            if (shouldPerform == isQualified)
                continue; // already in the right state — idempotent

            if (shouldPerform)
            {
                service.AddQualifiedStaff(membershipId);
                if (service.Status != ServiceStatus.Active)
                    service.Activate();

                qualified++;
            }
            else
            {
                service.RemoveQualifiedStaff(membershipId);
            }

            await _serviceRepository.SaveServiceAsync(service, cancellationToken);
        }

        return qualified;
    }

    /// <summary>
    /// Generates the member's availability. Slots belong to the organization and
    /// identify the member via StaffId, so no per-member provider record is needed.
    /// Days that already have slots for this member are skipped, making repeat calls
    /// safe.
    /// </summary>
    /// <remarks>
    /// The window for a day is the member's own working hours narrowed to the salon's
    /// opening hours — a member cannot be bookable while the shop is shut, and a shop
    /// cannot book a member who is not rostered. A member with no schedule of their own
    /// simply works the salon's hours, which is the default and the common case.
    /// </remarks>
    private async Task<int> GenerateAvailabilityAsync(
        ProviderAggregate organization,
        OrganizationMembership membership,
        CancellationToken cancellationToken)
    {
        var hoursByDay = organization.BusinessHours
            .Where(h => h.IsOpen && h.OpenTime is not null && h.CloseTime is not null)
            .GroupBy(h => h.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.First());

        if (hoursByDay.Count == 0)
            return 0;

        var profile = membership.StaffProfile;
        var memberDays = profile is { HasOwnSchedule: true }
            ? profile.WorkingDays.ToDictionary(d => d.DayOfWeek)
            : null;

        var membershipId = membership.Id;
        var today = SalonTime.Now.Date;
        var generated = 0;

        for (var offset = 0; offset < AvailabilityDaysAhead; offset++)
        {
            var date = today.AddDays(offset);
            var domainDay = (DomainDayOfWeek)(int)date.DayOfWeek;

            if (!hoursByDay.TryGetValue(domainDay, out var hours))
                continue;

            var open = hours.OpenTime!.Value;
            var close = hours.CloseTime!.Value;

            if (memberDays is not null)
            {
                // The member keeps hours of their own: they are off on any day they
                // did not roster, and otherwise work the overlap with the salon's day.
                if (!memberDays.TryGetValue(domainDay, out var workingDay))
                    continue;

                var window = workingDay.IntersectWith(open, close);
                if (window is null)
                    continue;

                (open, close) = window.Value;
            }

            // Skip a day this member already has slots for (idempotency).
            if (await _availabilityRepository.HasSlotsForStaffOnDateAsync(
                    organization.Id, date, membershipId, cancellationToken))
                continue;

            var slotStart = open;
            while (true)
            {
                var slotEnd = slotStart.AddMinutes(SlotMinutes);
                // Stop at/after closing; the <= guard also catches a midnight wrap.
                if (slotEnd <= slotStart || slotEnd > close)
                    break;

                var slot = ProviderAvailability.CreateAvailable(
                    organization.Id, date, slotStart, slotEnd, membershipId);

                await _availabilityRepository.SaveAsync(slot, cancellationToken);
                generated++;
                slotStart = slotEnd;
            }
        }

        return generated;
    }
}
