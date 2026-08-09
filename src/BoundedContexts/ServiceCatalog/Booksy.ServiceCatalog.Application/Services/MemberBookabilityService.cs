using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;
using ProviderAggregate = Booksy.ServiceCatalog.Domain.Aggregates.Provider;
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
    private readonly ILogger<MemberBookabilityService> _logger;

    public MemberBookabilityService(
        IProviderReadRepository providerRepository,
        IServiceWriteRepository serviceRepository,
        IProviderAvailabilityWriteRepository availabilityRepository,
        ILogger<MemberBookabilityService> logger)
    {
        _providerRepository = providerRepository;
        _serviceRepository = serviceRepository;
        _availabilityRepository = availabilityRepository;
        _logger = logger;
    }

    public async Task<MemberBookabilityResult> SyncAsync(
        OrganizationMembership membership,
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
            organization, membership.Id, cancellationToken);

        var slots = await GenerateAvailabilityAsync(
            organization, membership.Id, cancellationToken);

        _logger.LogInformation(
            "Member {MembershipId} of organization {OrgId} is bookable: qualified for {Qualified} service(s), {Slots} slot(s) generated",
            membership.Id, organization.Id.Value, qualified, slots);

        return new MemberBookabilityResult(qualified, slots);
    }

    /// <summary>
    /// Qualifies the member for every service the organization offers, so the
    /// booking engine will consider them. A Draft service becomes bookable once it
    /// has a qualified member.
    /// </summary>
    private async Task<int> QualifyForOrganizationServicesAsync(
        ProviderAggregate organization,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        var services = await _serviceRepository.GetServicesByProviderIdAsync(
            organization.Id, cancellationToken);

        var qualified = 0;
        foreach (var service in services)
        {
            if (service.IsStaffQualified(membershipId))
                continue; // idempotent

            service.AddQualifiedStaff(membershipId);
            if (service.Status != ServiceStatus.Active)
                service.Activate();

            await _serviceRepository.SaveServiceAsync(service, cancellationToken);
            qualified++;
        }

        return qualified;
    }

    /// <summary>
    /// Generates the member's availability from the organization's business hours.
    /// Slots belong to the organization and identify the member via StaffId, so no
    /// per-member provider record is needed. Days that already have slots for this
    /// member are skipped, making repeat calls safe.
    /// </summary>
    private async Task<int> GenerateAvailabilityAsync(
        ProviderAggregate organization,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        var hoursByDay = organization.BusinessHours
            .Where(h => h.IsOpen && h.OpenTime is not null && h.CloseTime is not null)
            .GroupBy(h => h.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.First());

        if (hoursByDay.Count == 0)
            return 0;

        var today = DateTime.UtcNow.Date;
        var generated = 0;

        for (var offset = 0; offset < AvailabilityDaysAhead; offset++)
        {
            var date = today.AddDays(offset);
            var domainDay = (DomainDayOfWeek)(int)date.DayOfWeek;

            if (!hoursByDay.TryGetValue(domainDay, out var hours))
                continue;

            var open = hours.OpenTime!.Value;
            var close = hours.CloseTime!.Value;

            // Skip a day this member already has slots for (idempotency).
            var existing = await _availabilityRepository.FindOverlappingSlotsAsync(
                organization.Id, date, open, close, membershipId, cancellationToken);
            if (existing.Count > 0)
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
