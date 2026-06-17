// ========================================
// AddStaffToProviderCommandHandler.cs
// Adds a staff member to the caller's organization as an Active Individual
// sub-provider (the model the booking flow resolves staff through), and
// qualifies the staff for the organization's services so they become bookable.
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.AddStaffToProvider;

public sealed class AddStaffToProviderCommandHandler
    : ICommandHandler<AddStaffToProviderCommand, AddStaffToProviderResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IServiceWriteRepository _serviceRepository;
    private readonly IProviderAvailabilityWriteRepository _availabilityRepository;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AddStaffToProviderCommandHandler> _logger;

    // Availability auto-generation window for a new staff member.
    private const int AvailabilityDaysAhead = 30;
    private const int SlotMinutes = 30;

    public AddStaffToProviderCommandHandler(
        IProviderWriteRepository providerRepository,
        IServiceWriteRepository serviceRepository,
        IProviderAvailabilityWriteRepository availabilityRepository,
        IServiceCatalogUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AddStaffToProviderCommandHandler> logger)
    {
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _serviceRepository = serviceRepository ?? throw new ArgumentNullException(nameof(serviceRepository));
        _availabilityRepository = availabilityRepository ?? throw new ArgumentNullException(nameof(availabilityRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AddStaffToProviderResult> Handle(
        AddStaffToProviderCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
            throw new ArgumentException("First name is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.LastName))
            throw new ArgumentException("Last name is required", nameof(request));

        // A provider may only add staff to their OWN organization.
        var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("User not authenticated");

        var organization = await _providerRepository.GetByOwnerIdAsync(UserId.From(userId), cancellationToken);
        if (organization is null)
            throw new KeyNotFoundException("No provider found for the authenticated user.");

        // Create the staff member as an Active Individual sub-provider under the organization.
        // (Synthetic owner id — staff are an internal resource and don't get a login.)
        var staff = Booksy.ServiceCatalog.Domain.Aggregates.Provider.RegisterStaffMember(
            organization,
            UserId.CreateNew(),
            request.FirstName,
            request.LastName);

        await _providerRepository.SaveProviderAsync(staff, cancellationToken);

        // Generate bookable availability slots for the staff from the org's business hours, so the
        // customer-facing slot picker has times to show. Tracked here, committed below in one batch.
        var slots = await GenerateStaffAvailabilityAsync(organization, staff.Id, cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);

        // Qualify the new staff member for every one of the organization's services so a
        // customer can book them immediately. (Booking checks Service.IsStaffQualified(staffId).)
        var services = await _serviceRepository.GetServicesByProviderIdAsync(organization.Id, cancellationToken);
        var qualified = 0;
        foreach (var service in services)
        {
            service.AddQualifiedStaff(staff.Id.Value);
            // A Draft service becomes bookable once it has a qualified staff member.
            if (service.Status != Booksy.ServiceCatalog.Domain.Enums.ServiceStatus.Active)
                service.Activate();
            await _serviceRepository.SaveServiceAsync(service, cancellationToken);
            qualified++;
        }

        _logger.LogInformation(
            "Staff {StaffId} ({First} {Last}) added to organization {OrgId}; qualified for {Count} service(s)",
            staff.Id.Value, request.FirstName, request.LastName, organization.Id.Value, qualified);

        return new AddStaffToProviderResult(
            organization.Id.Value,
            staff.Id.Value,
            request.FirstName,
            request.LastName,
            request.Role,
            true,
            staff.RegisteredAt);
    }

    /// <summary>
    /// Generates Available slots for the staff member from the organization's business hours,
    /// for the next <see cref="AvailabilityDaysAhead"/> days in <see cref="SlotMinutes"/>-minute
    /// increments. Slots are tracked on the repository; the caller commits them in one batch.
    /// </summary>
    private async Task<int> GenerateStaffAvailabilityAsync(
        Booksy.ServiceCatalog.Domain.Aggregates.Provider organization,
        Booksy.ServiceCatalog.Domain.ValueObjects.ProviderId staffId,
        CancellationToken cancellationToken)
    {
        var hoursByDay = organization.BusinessHours
            .Where(h => h.IsOpen)
            .GroupBy(h => h.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.First());
        if (hoursByDay.Count == 0)
            return 0;

        var today = DateTime.UtcNow.Date;
        var count = 0;
        for (var d = 0; d < AvailabilityDaysAhead; d++)
        {
            var date = today.AddDays(d);
            // BusinessHours uses the domain DayOfWeek enum (same int values as System.DayOfWeek).
            var domainDay = (Booksy.ServiceCatalog.Domain.Enums.DayOfWeek)(int)date.DayOfWeek;
            if (!hoursByDay.TryGetValue(domainDay, out var bh) || bh.OpenTime is null || bh.CloseTime is null)
                continue;

            var slotStart = bh.OpenTime.Value;
            var close = bh.CloseTime.Value;
            while (true)
            {
                var slotEnd = slotStart.AddMinutes(SlotMinutes);
                // Stop at/after closing time; the `<= slotStart` guard handles a midnight wrap.
                if (slotEnd <= slotStart || slotEnd > close)
                    break;

                var slot = Booksy.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate.ProviderAvailability
                    .CreateAvailable(staffId, date, slotStart, slotEnd);
                await _availabilityRepository.SaveAsync(slot, cancellationToken);
                count++;
                slotStart = slotEnd;
            }
        }

        _logger.LogInformation("Generated {Count} availability slots for staff {StaffId}", count, staffId.Value);
        return count;
    }
}
