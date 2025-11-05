// ========================================
// Booksy.ServiceCatalog.Application/Services/AvailabilityService.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.DomainServices;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Services
{
    /// <summary>
    /// Application service for checking availability and generating time slots
    /// </summary>
    public sealed class AvailabilityService : IAvailabilityService
    {
        private readonly IBookingReadRepository _bookingRepository;
        private readonly ILogger<AvailabilityService> _logger;

        // Configuration constants
        private const int DefaultSlotIntervalMinutes = 30;
        private const int BufferTimeMinutes = 15; // Buffer between appointments

        public AvailabilityService(
            IBookingReadRepository bookingRepository,
            ILogger<AvailabilityService> logger)
        {
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<AvailableTimeSlot>> GetAvailableTimeSlotsAsync(
            Provider provider,
            Service service,
            DateTime date,
            Staff? staff = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Getting available time slots for Provider {ProviderId}, Service {ServiceId} on {Date}",
                provider.Id, service.Id, date);

            // Validate provider and service
            if (provider.Status != ProviderStatus.Active)
            {
                _logger.LogWarning("Provider {ProviderId} is not active", provider.Id);
                return Array.Empty<AvailableTimeSlot>();
            }

            if (!provider.AllowOnlineBooking)
            {
                _logger.LogWarning("Provider {ProviderId} does not allow online booking", provider.Id);
                return Array.Empty<AvailableTimeSlot>();
            }

            // Check if date is within booking window
            var validationResult = await ValidateBookingConstraintsAsync(provider, service, date, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Booking constraints validation failed: {Errors}",
                    string.Join(", ", validationResult.Errors));
                return Array.Empty<AvailableTimeSlot>();
            }

            // Get business hours for the day
            var dayOfWeek = (DayOfWeek)(int)date.DayOfWeek;
            var businessHours = provider.BusinessHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);

            if (businessHours == null || !businessHours.IsOpen)
            {
                _logger.LogInformation("Provider is closed on {DayOfWeek}", dayOfWeek);
                return Array.Empty<AvailableTimeSlot>();
            }

            // Check for holidays
            if (IsHoliday(provider, date))
            {
                _logger.LogInformation("Provider is closed for holiday on {Date}", date);
                return Array.Empty<AvailableTimeSlot>();
            }

            // Get applicable exceptions (special hours)
            var exception = GetExceptionSchedule(provider, date);
            TimeOnly openTime, closeTime;

            if (exception != null)
            {
                if (exception.IsClosed)
                {
                    _logger.LogInformation("Provider is closed due to exception on {Date}", date);
                    return Array.Empty<AvailableTimeSlot>();
                }
                openTime = exception.OpenTime!.Value;
                closeTime = exception.CloseTime!.Value;
            }
            else
            {
                openTime = businessHours.OpenTime!.Value;
                closeTime = businessHours.CloseTime!.Value;
            }

            // Get qualified staff
            var qualifiedStaff = GetQualifiedStaff(provider, service, staff);
            if (!qualifiedStaff.Any())
            {
                _logger.LogWarning("No qualified staff found for service {ServiceId}", service.Id);
                return Array.Empty<AvailableTimeSlot>();
            }

            // Generate time slots
            var availableSlots = new List<AvailableTimeSlot>();

            foreach (var staffMember in qualifiedStaff)
            {
                var staffSlots = await GenerateTimeSlotsForStaffAsync(
                    date,
                    openTime,
                    closeTime,
                    service.Duration,
                    staffMember,
                    cancellationToken);

                availableSlots.AddRange(staffSlots);
            }

            _logger.LogInformation("Found {Count} available time slots", availableSlots.Count);
            return availableSlots.AsReadOnly();
        }

        public async Task<bool> IsTimeSlotAvailableAsync(
            Provider provider,
            Service service,
            Staff staff,
            DateTime startTime,
            Duration duration,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Checking if time slot is available: Staff {StaffId}, Start {StartTime}, Duration {Duration}",
                staff.Id, startTime, duration);

            // Validate basic constraints
            var validationResult = await ValidateBookingConstraintsAsync(provider, service, startTime, cancellationToken);
            if (!validationResult.IsValid)
            {
                return false;
            }

            // Check if staff is qualified
            if (!service.IsStaffQualified(staff.Id))
            {
                _logger.LogWarning("Staff {StaffId} is not qualified for service {ServiceId}", staff.Id, service.Id);
                return false;
            }

            // Check if staff is active
            if (!staff.IsActive)
            {
                _logger.LogWarning("Staff {StaffId} is not active", staff.Id);
                return false;
            }

            // Check for conflicts with existing bookings
            var endTime = startTime.AddMinutes(duration.Value + BufferTimeMinutes);
            var conflictingBookings = await _bookingRepository.GetConflictingBookingsAsync(
                staff.Id,
                startTime,
                endTime,
                cancellationToken);

            if (conflictingBookings.Any())
            {
                _logger.LogInformation("Time slot conflicts with existing booking(s)");
                return false;
            }

            return true;
        }

        public async Task<IReadOnlyList<Staff>> GetAvailableStaffAsync(
            Provider provider,
            Service service,
            DateTime startTime,
            Duration duration,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Getting available staff for Service {ServiceId} at {StartTime}",
                service.Id, startTime);

            var qualifiedStaff = GetQualifiedStaff(provider, service, null);
            var availableStaff = new List<Staff>();

            foreach (var staff in qualifiedStaff)
            {
                var isAvailable = await IsTimeSlotAvailableAsync(
                    provider,
                    service,
                    staff,
                    startTime,
                    duration,
                    cancellationToken);

                if (isAvailable)
                {
                    availableStaff.Add(staff);
                }
            }

            _logger.LogInformation("Found {Count} available staff members", availableStaff.Count);
            return availableStaff.AsReadOnly();
        }

        public async Task<AvailabilityValidationResult> ValidateBookingConstraintsAsync(
            Provider provider,
            Service service,
            DateTime startTime,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            // Check if provider is active
            if (provider.Status != ProviderStatus.Active)
            {
                errors.Add("Provider is not active");
            }

            // Check if provider allows online booking
            if (!provider.AllowOnlineBooking)
            {
                errors.Add("Provider does not allow online booking");
            }

            // Check if service is active
            if (service.Status != ServiceStatus.Active)
            {
                errors.Add("Service is not active");
            }

            // Check minimum advance booking time
            var hoursUntilBooking = (startTime - DateTime.UtcNow).TotalHours;
            if (service.MinAdvanceBookingHours.HasValue && hoursUntilBooking < service.MinAdvanceBookingHours.Value)
            {
                errors.Add($"Booking must be made at least {service.MinAdvanceBookingHours.Value} hours in advance");
            }

            // Check maximum advance booking time
            var daysUntilBooking = (startTime - DateTime.UtcNow).TotalDays;
            if (service.MaxAdvanceBookingDays.HasValue && daysUntilBooking > service.MaxAdvanceBookingDays.Value)
            {
                errors.Add($"Booking cannot be made more than {service.MaxAdvanceBookingDays.Value} days in advance");
            }

            // Check if booking is in the past
            if (startTime < DateTime.UtcNow)
            {
                errors.Add("Cannot book appointments in the past");
            }

            // Check if provider is open on this day
            var dayOfWeek = (DayOfWeek)(int)startTime.DayOfWeek;
            var businessHours = provider.BusinessHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);

            if (businessHours == null || !businessHours.IsOpen)
            {
                errors.Add($"Provider is closed on {dayOfWeek}");
            }

            // Check for holidays
            if (IsHoliday(provider, startTime.Date))
            {
                errors.Add("Provider is closed on this date (holiday)");
            }

            // Check exceptions
            var exception = GetExceptionSchedule(provider, startTime.Date);
            if (exception != null && exception.IsClosed)
            {
                errors.Add("Provider is closed on this date (exception)");
            }

            // Validate time is within business hours
            if (businessHours != null && businessHours.IsOpen)
            {
                var bookingTime = TimeOnly.FromDateTime(startTime);
                var endTime = bookingTime.AddMinutes(service.Duration.Value);

                TimeOnly openTime, closeTime;
                if (exception != null && !exception.IsClosed)
                {
                    openTime = exception.OpenTime!.Value;
                    closeTime = exception.CloseTime!.Value;
                }
                else
                {
                    openTime = businessHours.OpenTime!.Value;
                    closeTime = businessHours.CloseTime!.Value;
                }

                if (bookingTime < openTime || endTime > closeTime)
                {
                    errors.Add($"Booking time must be between {openTime} and {closeTime}");
                }
            }

            await Task.CompletedTask; // For async pattern consistency

            return errors.Any()
                ? AvailabilityValidationResult.Failure(errors.ToArray())
                : AvailabilityValidationResult.Success();
        }

        // ========================================
        // PRIVATE HELPER METHODS
        // ========================================

        private async Task<List<AvailableTimeSlot>> GenerateTimeSlotsForStaffAsync(
            DateTime date,
            TimeOnly openTime,
            TimeOnly closeTime,
            Duration serviceDuration,
            Staff staff,
            CancellationToken cancellationToken)
        {
            var availableSlots = new List<AvailableTimeSlot>();

            // Start from the open time
            var currentTime = openTime;
            var serviceDurationMinutes = serviceDuration.Value;

            // Get existing bookings for this staff member on this date
            var dayStart = date.Date;
            var dayEnd = date.Date.AddDays(1);
            var existingBookings = await _bookingRepository.GetStaffBookingsInDateRangeAsync(
                staff.Id,
                dayStart,
                dayEnd,
                cancellationToken);

            // Filter only confirmed bookings
            var confirmedBookings = existingBookings
                .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Requested)
                .ToList();

            while (currentTime.AddMinutes(serviceDurationMinutes) <= closeTime)
            {
                var slotStart = date.Date.Add(currentTime.ToTimeSpan());
                var slotEnd = slotStart.AddMinutes(serviceDurationMinutes);

                // Check if this slot conflicts with any existing booking
                var hasConflict = confirmedBookings.Any(booking =>
                {
                    var bookingStart = booking.TimeSlot.StartTime;
                    var bookingEnd = booking.TimeSlot.EndTime.AddMinutes(BufferTimeMinutes);

                    // Check for overlap
                    return slotStart < bookingEnd && slotEnd > bookingStart;
                });

                // Only add slot if it's in the future and has no conflicts
                if (!hasConflict && slotStart > DateTime.UtcNow)
                {
                    availableSlots.Add(new AvailableTimeSlot(
                        slotStart,
                        slotEnd,
                        Duration.FromMinutes(serviceDurationMinutes),
                        staff.Id,
                        staff.FullName));
                }

                // Move to next slot (every 30 minutes by default)
                currentTime = currentTime.AddMinutes(DefaultSlotIntervalMinutes);
            }

            return availableSlots;
        }

        private List<Staff> GetQualifiedStaff(Provider provider, Service service, Staff? specificStaff)
        {
            if (specificStaff != null)
            {
                // Check if the specific staff is qualified and active
                if (service.IsStaffQualified(specificStaff.Id) && specificStaff.IsActive)
                {
                    return new List<Staff> { specificStaff };
                }
                return new List<Staff>();
            }

            // Get all active qualified staff
            return provider.Staff
                .Where(s => s.IsActive && service.IsStaffQualified(s.Id))
                .ToList();
        }

        private bool IsHoliday(Provider provider, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            return provider.Holidays.Any(h =>
                dateOnly == h.Date &&
                h.IsRecurring == false);
        }

        private ExceptionSchedule? GetExceptionSchedule(Provider provider, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            return provider.Exceptions
                .Where(e => e.Date == dateOnly)
                .OrderByDescending(e => e.Date)
                .FirstOrDefault();
        }
    }
}
