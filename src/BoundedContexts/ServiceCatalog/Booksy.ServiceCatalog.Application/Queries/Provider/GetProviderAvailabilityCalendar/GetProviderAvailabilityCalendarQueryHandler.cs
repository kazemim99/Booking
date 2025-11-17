using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using SystemDayOfWeek = System.DayOfWeek;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderAvailabilityCalendar;

public sealed class GetProviderAvailabilityCalendarQueryHandler
    : IQueryHandler<GetProviderAvailabilityCalendarQuery, ProviderAvailabilityCalendarViewModel>
{
    private readonly IProviderAvailabilityReadRepository _availabilityRepository;
    private readonly IProviderWriteRepository _providerRepository;
    private readonly ILogger<GetProviderAvailabilityCalendarQueryHandler> _logger;

    public GetProviderAvailabilityCalendarQueryHandler(
        IProviderAvailabilityReadRepository availabilityRepository,
        IProviderWriteRepository providerRepository,
        ILogger<GetProviderAvailabilityCalendarQueryHandler> logger)
    {
        _availabilityRepository = availabilityRepository ?? throw new ArgumentNullException(nameof(availabilityRepository));
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProviderAvailabilityCalendarViewModel> Handle(
        GetProviderAvailabilityCalendarQuery query,
        CancellationToken cancellationToken)
    {
        // Validate provider exists
        var provider = await _providerRepository.GetByIdAsync(
            ProviderId.From(query.ProviderId),
            cancellationToken);

        if (provider == null)
        {
            _logger.LogWarning("Provider {ProviderId} not found", query.ProviderId);
            throw new DomainValidationException($"Provider with ID {query.ProviderId} not found");
        }

        // Validate days parameter
        if (query.Days != 7 && query.Days != 14 && query.Days != 30)
        {
            throw new DomainValidationException("Days parameter must be 7, 14, or 30");
        }

        var endDate = query.StartDate.AddDays(query.Days - 1);

        _logger.LogInformation(
            "Fetching availability calendar for provider {ProviderId} from {StartDate} to {EndDate} ({Days} days)",
            query.ProviderId,
            query.StartDate,
            endDate,
            query.Days);

        // Query availability slots from repository
        var availabilitySlots = await _availabilityRepository.GetAvailabilityByDateRangeAsync(
            provider.Id,
            query.StartDate.ToDateTime(TimeOnly.MinValue),
            endDate.ToDateTime(TimeOnly.MinValue),
            cancellationToken);

        _logger.LogDebug("Found {Count} availability slots", availabilitySlots.Count);

        // Get business hours to determine which days provider is open
        var businessHours = provider.BusinessHours?.ToList() ?? new List<Domain.Entities.BusinessHours>();

        // Build daily availability view models
        var days = new List<DayAvailabilityViewModel>();
        var currentDate = query.StartDate;

        while (currentDate <= endDate)
        {
            var dayOfWeek = MapSystemDayOfWeek(currentDate.DayOfWeek);
            var businessHoursForDay = businessHours.FirstOrDefault(bh => bh.DayOfWeek == dayOfWeek);
            var isOpen = businessHoursForDay?.IsOpen ?? false;

            var daySlotsDb = availabilitySlots
                .Where(a => DateOnly.FromDateTime(a.Date) == currentDate)
                .ToList();

            List<TimeSlotViewModel> availableSlots;
            List<TimeSlotViewModel> bookedSlots;
            string? closedReason = null;

            if (!isOpen)
            {
                availableSlots = new List<TimeSlotViewModel>();
                bookedSlots = new List<TimeSlotViewModel>();
                closedReason = "Provider closed";
            }
            else if (!daySlotsDb.Any())
            {
                // No availability data for this day (might be holiday or blocked)
                availableSlots = new List<TimeSlotViewModel>();
                bookedSlots = new List<TimeSlotViewModel>();
                closedReason = "No availability data";
            }
            else
            {
                availableSlots = daySlotsDb
                    .Where(s => s.Status == AvailabilityStatus.Available)
                    .Select(s => MapToTimeSlotViewModel(s, canBook: true))
                    .ToList();

                bookedSlots = daySlotsDb
                    .Where(s => s.Status == AvailabilityStatus.Booked)
                    .Select(s => MapToTimeSlotViewModel(s, canBook: false))
                    .ToList();
            }

            var dayStatus = DetermineDayStatus(availableSlots.Count, bookedSlots.Count, isOpen);

            days.Add(new DayAvailabilityViewModel(
                Date: currentDate,
                DayOfWeek: dayOfWeek,
                IsOpen: isOpen,
                ClosedReason: closedReason,
                AvailableSlots: availableSlots,
                BookedSlots: bookedSlots,
                Status: dayStatus));

            currentDate = currentDate.AddDays(1);
        }

        // Calculate heatmap data
        var heatmap = CalculateHeatmap(availabilitySlots.ToList(), days);

        return new ProviderAvailabilityCalendarViewModel(
            ProviderId: query.ProviderId,
            StartDate: query.StartDate,
            EndDate: endDate,
            TotalDays: query.Days,
            Days: days,
            Heatmap: heatmap);
    }

    private TimeSlotViewModel MapToTimeSlotViewModel(
        Domain.Aggregates.ProviderAvailabilityAggregate.ProviderAvailability slot,
        bool canBook)
    {
        var durationMinutes = (int)(slot.EndTime - slot.StartTime).TotalMinutes;

        return new TimeSlotViewModel(
            StartTime: slot.StartTime.ToString("HH:mm"),
            EndTime: slot.EndTime.ToString("HH:mm"),
            DurationMinutes: durationMinutes,
            Status: slot.Status.ToString(),
            StaffId: slot.StaffId,
            BookingId: slot.BookingId,
            CanBook: canBook);
    }

    private AvailabilityDayStatus DetermineDayStatus(int availableCount, int bookedCount, bool isOpen)
    {
        if (!isOpen)
            return AvailabilityDayStatus.Closed;

        var totalSlots = availableCount + bookedCount;
        if (totalSlots == 0)
            return AvailabilityDayStatus.Closed;

        var availablePercentage = (decimal)availableCount / totalSlots * 100;

        return availablePercentage switch
        {
            0 => AvailabilityDayStatus.FullyBooked,
            < 30 => AvailabilityDayStatus.HighDemand,
            < 70 => AvailabilityDayStatus.Moderate,
            _ => AvailabilityDayStatus.MostlyAvailable
        };
    }

    private AvailabilityHeatmapViewModel CalculateHeatmap(
        List<Domain.Aggregates.ProviderAvailabilityAggregate.ProviderAvailability> slots,
        List<DayAvailabilityViewModel> days)
    {
        var totalSlots = slots.Count;

        if (totalSlots == 0)
        {
            return new AvailabilityHeatmapViewModel(
                AvailablePercentage: 0,
                BookedPercentage: 0,
                BlockedPercentage: 0,
                TotalSlots: 0,
                AvailableSlots: 0,
                BookedSlots: 0,
                BlockedSlots: 0,
                DailyHeatmap: new List<DayHeatmapViewModel>());
        }

        var availableCount = slots.Count(s => s.Status == AvailabilityStatus.Available);
        var bookedCount = slots.Count(s => s.Status == AvailabilityStatus.Booked);
        var blockedCount = slots.Count(s => s.Status == AvailabilityStatus.Blocked ||
                                           s.Status == AvailabilityStatus.Break);

        var availablePercentage = Math.Round((decimal)availableCount / totalSlots * 100, 2);
        var bookedPercentage = Math.Round((decimal)bookedCount / totalSlots * 100, 2);
        var blockedPercentage = Math.Round((decimal)blockedCount / totalSlots * 100, 2);

        // Daily heatmap for calendar color coding
        var dailyHeatmap = days
            .Where(d => d.IsOpen)
            .Select(d =>
            {
                var dayTotal = d.AvailableSlots.Count + d.BookedSlots.Count;
                var dayAvailablePercentage = dayTotal > 0
                    ? Math.Round((decimal)d.AvailableSlots.Count / dayTotal * 100, 2)
                    : 0;

                var color = GetHeatmapColor(dayAvailablePercentage);

                return new DayHeatmapViewModel(
                    Date: d.Date,
                    AvailablePercentage: dayAvailablePercentage,
                    HeatmapColor: color);
            })
            .ToList();

        return new AvailabilityHeatmapViewModel(
            AvailablePercentage: availablePercentage,
            BookedPercentage: bookedPercentage,
            BlockedPercentage: blockedPercentage,
            TotalSlots: totalSlots,
            AvailableSlots: availableCount,
            BookedSlots: bookedCount,
            BlockedSlots: blockedCount,
            DailyHeatmap: dailyHeatmap);
    }

    private string GetHeatmapColor(decimal availablePercentage)
    {
        // Color coding for UI calendar heatmap
        return availablePercentage switch
        {
            0 => "gray",        // Fully booked
            < 30 => "red",      // High demand (< 30% available)
            < 70 => "yellow",   // Moderate (30-70% available)
            _ => "green"        // Mostly available (> 70%)
        };
    }

    private DayOfWeek MapSystemDayOfWeek(SystemDayOfWeek systemDayOfWeek)
    {
        return systemDayOfWeek switch
        {
            SystemDayOfWeek.Sunday => DayOfWeek.Sunday,
            SystemDayOfWeek.Monday => DayOfWeek.Monday,
            SystemDayOfWeek.Tuesday => DayOfWeek.Tuesday,
            SystemDayOfWeek.Wednesday => DayOfWeek.Wednesday,
            SystemDayOfWeek.Thursday => DayOfWeek.Thursday,
            SystemDayOfWeek.Friday => DayOfWeek.Friday,
            SystemDayOfWeek.Saturday => DayOfWeek.Saturday,
            _ => throw new ArgumentOutOfRangeException(nameof(systemDayOfWeek))
        };
    }
}
