using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SystemDayOfWeek = System.DayOfWeek;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Generates provider availability data for next 90 days with realistic patterns
    /// - Peak hours (10am-12pm, 6pm-8pm): 30% available, 70% booked
    /// - Off-peak hours: 70% available, 30% booked
    /// - Respects Iranian holidays and business hours
    /// - Creates 30-minute time slots
    /// </summary>
    public sealed class AvailabilitySeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<AvailabilitySeeder> _logger;
        private readonly Random _random = new Random(54321); // Deterministic for consistent results

        // Iranian public holidays (Solar Hijri calendar - approximations for 2025-2026)
        private readonly HashSet<DateTime> _iranianHolidays = new()
        {
            // Nowruz (Persian New Year) - March 20-24
            new DateTime(2025, 3, 20),
            new DateTime(2025, 3, 21),
            new DateTime(2025, 3, 22),
            new DateTime(2025, 3, 23),
            new DateTime(2025, 3, 24),

            // Islamic Republic Day - April 1
            new DateTime(2025, 4, 1),

            // Sizdah Be-dar - April 2
            new DateTime(2025, 4, 2),

            // Eid al-Fitr (end of Ramadan) - approximate
            new DateTime(2025, 3, 30),
            new DateTime(2025, 3, 31),

            // Eid al-Adha - approximate
            new DateTime(2025, 6, 6),
            new DateTime(2025, 6, 7),

            // Tasua - approximate
            new DateTime(2025, 7, 5),

            // Ashura - approximate
            new DateTime(2025, 7, 6),

            // Arbaeen - approximate
            new DateTime(2025, 8, 14),

            // Prophet Muhammad's Birthday - approximate
            new DateTime(2025, 9, 4),

            // Imam Reza's Birthday - approximate
            new DateTime(2025, 11, 10),

            // Revolution Day - February 11
            new DateTime(2026, 2, 11)
        };

        // Block reasons for provider blocks
        private readonly string[] _blockReasons = new[]
        {
            "مرخصی",
            "مرخصی استعلاجی",
            "سفر کاری",
            "رویداد خانوادگی",
            "آموزش حرفه‌ای",
            "تعطیلات شخصی",
            "قرار ملاقات پزشکی"
        };

        public AvailabilitySeeder(
            ServiceCatalogDbContext context,
            ILogger<AvailabilitySeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await _context.ProviderAvailability.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Provider availability already seeded. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting provider availability seeding for 90-day rolling window...");

                var providers = await _context.Providers
                    .Include(p => p.BusinessHours)
                    .Where(p => p.Status == ProviderStatus.Active || p.Status == ProviderStatus.PendingVerification)
                    .ToListAsync(cancellationToken);

                if (!providers.Any())
                {
                    _logger.LogWarning("No providers found. Skipping availability seeding.");
                    return;
                }

                // Get existing bookings to mark as booked
                var bookings = await _context.Bookings
                    .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
                    .ToListAsync(cancellationToken);

                var allAvailability = new List<ProviderAvailability>();
                var startDate = DateTime.UtcNow.Date;
                var endDate = startDate.AddDays(90);

                foreach (var provider in providers)
                {
                    _logger.LogDebug("Generating availability for provider {ProviderId}", provider.Id.Value);

                    var providerAvailability = GenerateAvailabilityForProvider(
                        provider,
                        bookings.Where(b => b.ProviderId == provider.Id.Value).ToList(),
                        startDate,
                        endDate);

                    allAvailability.AddRange(providerAvailability);
                }

                await _context.ProviderAvailability.AddRangeAsync(allAvailability, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully seeded {Count} availability slots for {ProviderCount} providers over {Days} days",
                    allAvailability.Count,
                    providers.Count,
                    90);

                LogAvailabilityStatistics(allAvailability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding provider availability");
                throw;
            }
        }

        private List<ProviderAvailability> GenerateAvailabilityForProvider(
            Domain.Aggregates.Provider provider,
            List<Domain.Aggregates.BookingAggregate.Booking> providerBookings,
            DateTime startDate,
            DateTime endDate)
        {
            var availability = new List<ProviderAvailability>();
            var currentDate = startDate;

            while (currentDate < endDate)
            {
                // Skip Fridays (Iranian weekend)
                if (currentDate.DayOfWeek == SystemDayOfWeek.Friday)
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                // Skip Iranian public holidays
                if (_iranianHolidays.Contains(currentDate))
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                // Get business hours for this day - map System.DayOfWeek to Domain DayOfWeek
                var systemDayOfWeek = currentDate.DayOfWeek;
                var domainDayOfWeek = MapToDomainDayOfWeek(systemDayOfWeek);
                var businessHours = provider.BusinessHours
                    .FirstOrDefault(bh => bh.DayOfWeek == domainDayOfWeek);

                if (businessHours == null || !businessHours.IsOpen)
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                // Occasionally block entire days (5% chance)
                if (_random.Next(100) < 5)
                {
                    var blockSlot = ProviderAvailability.CreateBlocked(
                        provider.Id,
                        currentDate,
                        businessHours.OpenTime!.Value,
                        businessHours.CloseTime!.Value,
                        GetRandomBlockReason(),
                        null,
                        "AvailabilitySeeder");

                    availability.Add(blockSlot);
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                // Generate time slots for the day
                var dailySlots = GenerateTimeSlotsForDay(
                    provider,
                    businessHours,
                    currentDate,
                    providerBookings);

                availability.AddRange(dailySlots);
                currentDate = currentDate.AddDays(1);
            }

            return availability;
        }

        private List<ProviderAvailability> GenerateTimeSlotsForDay(
            Domain.Aggregates.Provider provider,
            Domain.Entities.BusinessHours businessHours,
            DateTime date,
            List<Domain.Aggregates.BookingAggregate.Booking> providerBookings)
        {
            var slots = new List<ProviderAvailability>();

            // Get available time ranges (excluding breaks)
            var availableRanges = businessHours.GetAvailableSlots().ToList();

            foreach (var (rangeStart, rangeEnd) in availableRanges)
            {
                var currentTime = rangeStart;
                var slotDuration = 30; // 30-minute slots

                while (currentTime.AddMinutes(slotDuration) <= rangeEnd)
                {
                    var slotEndTime = currentTime.AddMinutes(slotDuration);

                    // Check if there's an existing booking for this slot
                    var existingBooking = FindBookingForSlot(providerBookings, date, currentTime, slotEndTime);

                    ProviderAvailability slot;

                    if (existingBooking != null)
                    {
                        // Mark as booked
                        slot = ProviderAvailability.CreateAvailable(
                            provider.Id,
                            date,
                            currentTime,
                            slotEndTime,
                            existingBooking.StaffId,
                            "AvailabilitySeeder");

                        slot.MarkAsBooked(existingBooking.Id.Value, "AvailabilitySeeder");
                    }
                    else
                    {
                        // Determine status based on time of day and randomness
                        var status = DetermineSlotStatus(currentTime, date);

                        if (status == AvailabilityStatus.Available)
                        {
                            slot = ProviderAvailability.CreateAvailable(
                                provider.Id,
                                date,
                                currentTime,
                                slotEndTime,
                                null, // No specific staff assigned
                                "AvailabilitySeeder");
                        }
                        else if (status == AvailabilityStatus.Blocked)
                        {
                            slot = ProviderAvailability.CreateBlocked(
                                provider.Id,
                                date,
                                currentTime,
                                slotEndTime,
                                GetRandomBlockReason(),
                                null,
                                "AvailabilitySeeder");
                        }
                        else // Break (should be rare as breaks are already excluded)
                        {
                            currentTime = slotEndTime;
                            continue;
                        }
                    }

                    slots.Add(slot);
                    currentTime = slotEndTime;
                }
            }

            return slots;
        }

        private Domain.Aggregates.BookingAggregate.Booking? FindBookingForSlot(
            List<Domain.Aggregates.BookingAggregate.Booking> bookings,
            DateTime date,
            TimeOnly slotStart,
            TimeOnly slotEnd)
        {
            foreach (var booking in bookings)
            {
                // TODO: Fix after Booking schema is updated with TimeSlot value object
                // var bookingDate = booking.TimeSlot.Date;
                // if (bookingDate != date)
                //     continue;
                // var bookingStartTime = booking.TimeSlot.StartTime;
                // var bookingEndTime = booking.TimeSlot.EndTime;
                // Check for overlap
                // if (slotStart < bookingEndTime && slotEnd > bookingStartTime)
                // {
                //     return booking;
                // }
            }

            return null;
        }

        private AvailabilityStatus DetermineSlotStatus(TimeOnly time, DateTime date)
        {
            // Future dates have more availability
            var daysFromNow = (date - DateTime.UtcNow.Date).Days;

            // Peak hours: 10am-12pm, 6pm-8pm (more likely to be booked)
            var isPeakHour = (time >= new TimeOnly(10, 0) && time < new TimeOnly(12, 0)) ||
                            (time >= new TimeOnly(18, 0) && time < new TimeOnly(20, 0));

            // Calculate booking probability
            int bookingProbability;

            if (daysFromNow <= 3)
            {
                // Near future: mostly booked
                bookingProbability = isPeakHour ? 80 : 60;
            }
            else if (daysFromNow <= 14)
            {
                // Medium future
                bookingProbability = isPeakHour ? 60 : 40;
            }
            else if (daysFromNow <= 30)
            {
                // Far future
                bookingProbability = isPeakHour ? 40 : 20;
            }
            else
            {
                // Very far future: mostly available
                bookingProbability = isPeakHour ? 25 : 10;
            }

            var roll = _random.Next(100);

            if (roll < bookingProbability)
            {
                // 90% booked, 10% blocked
                return _random.Next(100) < 90 ? AvailabilityStatus.Booked : AvailabilityStatus.Blocked;
            }
            else
            {
                return AvailabilityStatus.Available;
            }
        }

        private string GetRandomBlockReason()
        {
            return _blockReasons[_random.Next(_blockReasons.Length)];
        }

        private void LogAvailabilityStatistics(List<ProviderAvailability> availability)
        {
            var statistics = new
            {
                Total = availability.Count,
                Available = availability.Count(a => a.Status == AvailabilityStatus.Available),
                Booked = availability.Count(a => a.Status == AvailabilityStatus.Booked),
                Blocked = availability.Count(a => a.Status == AvailabilityStatus.Blocked),
                Break = availability.Count(a => a.Status == AvailabilityStatus.Break),
                TentativeHold = availability.Count(a => a.Status == AvailabilityStatus.TentativeHold)
            };

            var availablePercentage = (double)statistics.Available / statistics.Total * 100;
            var bookedPercentage = (double)statistics.Booked / statistics.Total * 100;

            _logger.LogInformation(
                "Availability Statistics: Total={Total}, Available={Available} ({AvailablePercent:F1}%), " +
                "Booked={Booked} ({BookedPercent:F1}%), Blocked={Blocked}, Break={Break}, TentativeHold={TentativeHold}",
                statistics.Total,
                statistics.Available,
                availablePercentage,
                statistics.Booked,
                bookedPercentage,
                statistics.Blocked,
                statistics.Break,
                statistics.TentativeHold);
        }

        private DayOfWeek MapToDomainDayOfWeek(SystemDayOfWeek systemDayOfWeek)
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
}
