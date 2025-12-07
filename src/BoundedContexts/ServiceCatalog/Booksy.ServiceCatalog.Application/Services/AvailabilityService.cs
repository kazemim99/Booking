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
        private readonly IProviderReadRepository _providerRepository;
        private readonly ILogger<AvailabilityService> _logger;

        // Configuration constants
        private const int DefaultSlotIntervalMinutes = 30;
        private const int BufferTimeMinutes = 15; // Buffer between appointments

        public AvailabilityService(
            IBookingReadRepository bookingRepository,
            IProviderReadRepository providerRepository,
            ILogger<AvailabilityService> logger)
        {
            _bookingRepository = bookingRepository;
            _providerRepository = providerRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<AvailableTimeSlot>> GetAvailableTimeSlotsAsync(
            Provider provider,
            Service service,
            DateTime date,
            Provider? individualProvider = null,
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

            // Check if date is within booking window (date-level validation only)
            var validationResult = await ValidateDateConstraintsAsync(provider, service, date, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Date constraints validation failed: {Errors}",
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

            // Get qualified individual providers (staff) - NOW USING HIERARCHY
            var qualifiedIndividuals = await GetQualifiedIndividualProvidersAsync(provider, service, individualProvider, cancellationToken);
            if (!qualifiedIndividuals.Any())
            {
                _logger.LogWarning("No qualified individual providers found for service {ServiceId}", service.Id);
                return Array.Empty<AvailableTimeSlot>();
            }

            // Generate time slots
            var availableSlots = new List<AvailableTimeSlot>();

            foreach (var individual in qualifiedIndividuals)
            {
                var staffSlots = await GenerateTimeSlotsForIndividualAsync(
                    date,
                    openTime,
                    closeTime,
                    service.Duration,
                    individual,
                    cancellationToken);

                availableSlots.AddRange(staffSlots);
            }

            _logger.LogInformation("Found {Count} available time slots", availableSlots.Count);
            return availableSlots.AsReadOnly();
        }

        public async Task<bool> IsTimeSlotAvailableAsync(
            Provider provider,
            Service service,
            Provider individualProvider,
            DateTime startTime,
            Duration duration,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Checking if time slot is available: Individual Provider {ProviderId}, Start {StartTime}, Duration {Duration}",
                individualProvider.Id, startTime, duration);

            // Validate basic constraints
            var validationResult = await ValidateBookingConstraintsAsync(provider, service, startTime, cancellationToken);
            if (!validationResult.IsValid)
            {
                return false;
            }

            // Check if individual provider is active
            if (individualProvider.Status != ProviderStatus.Active)
            {
                _logger.LogWarning("Individual Provider {ProviderId} is not active", individualProvider.Id);
                return false;
            }

            // Check if individual provider is qualified for the service
            if (!service.IsStaffQualified(individualProvider.Id.Value))
            {
                _logger.LogWarning("Individual Provider {ProviderId} is not qualified for service {ServiceId}",
                    individualProvider.Id, service.Id);
                return false;
            }

            // Check for conflicts with existing bookings
            var endTime = startTime.AddMinutes(duration.Value + BufferTimeMinutes);
            var conflictingBookings = await _bookingRepository.GetConflictingBookingsAsync(
                individualProvider.Id.Value,
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

        public async Task<IReadOnlyList<Provider>> GetAvailableStaffAsync(
            Provider provider,
            Service service,
            DateTime startTime,
            Duration duration,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Getting available individual providers for Service {ServiceId} at {StartTime}",
                service.Id, startTime);

            var qualifiedIndividuals = await GetQualifiedIndividualProvidersAsync(provider, service, null, cancellationToken);
            var availableIndividuals = new List<Provider>();

            foreach (var individual in qualifiedIndividuals)
            {
                var isAvailable = await IsTimeSlotAvailableAsync(
                    provider,
                    service,
                    individual,
                    startTime,
                    duration,
                    cancellationToken);

                if (isAvailable)
                {
                    availableIndividuals.Add(individual);
                }
            }

            _logger.LogInformation("Found {Count} available individual providers", availableIndividuals.Count);
            return availableIndividuals.AsReadOnly();
        }

        public async Task<AvailabilityValidationResult> ValidateBookingConstraintsAsync(
            Provider provider,
            Service service,
            DateTime startTime,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            // Ensure startTime is in UTC for proper comparison
            // If DateTimeKind is Unspecified, assume it's already meant to be UTC
            if (startTime.Kind == DateTimeKind.Unspecified)
            {
                startTime = DateTime.SpecifyKind(startTime, DateTimeKind.Utc);
            }
            else if (startTime.Kind == DateTimeKind.Local)
            {
                startTime = startTime.ToUniversalTime();
            }

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

        public Task<AvailabilityValidationResult> ValidateDateConstraintsAsync(
            Provider provider,
            Service service,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            // Ensure date is in UTC for proper comparison
            if (date.Kind == DateTimeKind.Unspecified)
            {
                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            }
            else if (date.Kind == DateTimeKind.Local)
            {
                date = date.ToUniversalTime();
            }

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

            // Check maximum advance booking time (DATE-LEVEL only, not time-level)
            var daysUntilBooking = (date.Date - DateTime.UtcNow.Date).TotalDays;
            if (service.MaxAdvanceBookingDays.HasValue && daysUntilBooking > service.MaxAdvanceBookingDays.Value)
            {
                errors.Add($"Booking cannot be made more than {service.MaxAdvanceBookingDays.Value} days in advance");
            }

            // Check if date is in the past
            if (date.Date < DateTime.UtcNow.Date)
            {
                errors.Add("Cannot book appointments in the past");
            }

            // Check if provider is open on this day of week
            var dayOfWeek = (DayOfWeek)(int)date.DayOfWeek;
            var businessHours = provider.BusinessHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);

            if (businessHours == null || !businessHours.IsOpen)
            {
                errors.Add($"Provider is closed on {dayOfWeek}");
            }

            // Check for holidays
            if (IsHoliday(provider, date.Date))
            {
                errors.Add("Provider is closed on this date (holiday)");
            }

            // Check exceptions
            var exception = GetExceptionSchedule(provider, date.Date);
            if (exception != null && exception.IsClosed)
            {
                errors.Add("Provider is closed on this date (exception)");
            }

            return Task.FromResult(errors.Any()
                ? AvailabilityValidationResult.Failure(errors.ToArray())
                : AvailabilityValidationResult.Success());
        }

        // ========================================
        // PRIVATE HELPER METHODS - USING HIERARCHY MODEL
        // ========================================

        /// <summary>
        /// Generate time slots for a specific individual provider (staff member)
        /// </summary>
        private async Task<List<AvailableTimeSlot>> GenerateTimeSlotsForIndividualAsync(
            DateTime date,
            TimeOnly openTime,
            TimeOnly closeTime,
            Duration serviceDuration,
            Provider individualProvider,
            CancellationToken cancellationToken)
        {
            var availableSlots = new List<AvailableTimeSlot>();

            // Start from the open time
            var currentTime = openTime;
            var serviceDurationMinutes = serviceDuration.Value;

            // Get existing bookings for this individual provider on this date
            var dayStart = date.Date;
            var dayEnd = date.Date.AddDays(1);
            var existingBookings = await _bookingRepository.GetStaffBookingsInDateRangeAsync(
                individualProvider.Id.Value,
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

                // Ensure slotStart is in UTC for comparison
                var slotStartUtc = slotStart.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(slotStart, DateTimeKind.Utc)
                    : slotStart.ToUniversalTime();

                // Only add slot if it's in the future and has no conflicts
                if (!hasConflict && slotStartUtc > DateTime.UtcNow)
                {
                    var staffName = $"{individualProvider.OwnerFirstName} {individualProvider.OwnerLastName}".Trim();
                    if (string.IsNullOrEmpty(staffName))
                        staffName = individualProvider.Profile.BusinessName;

                    availableSlots.Add(new AvailableTimeSlot(
                        slotStart,
                        slotEnd,
                        Duration.FromMinutes(serviceDurationMinutes),
                        individualProvider.Id.Value,
                        staffName));
                }

                // Move to next slot (every 30 minutes by default)
                currentTime = currentTime.AddMinutes(DefaultSlotIntervalMinutes);
            }

            return availableSlots;
        }

        /// <summary>
        /// Get qualified individual providers (staff) for a service using hierarchy model
        /// </summary>
        private async Task<List<Provider>> GetQualifiedIndividualProvidersAsync(
            Provider provider,
            Service service,
            Provider? specificIndividual,
            CancellationToken cancellationToken)
        {
            if (specificIndividual != null)
            {
                // Check if the specific individual is qualified and active
                if (service.IsStaffQualified(specificIndividual.Id.Value) &&
                    specificIndividual.Status == ProviderStatus.Active)
                {
                    return new List<Provider> { specificIndividual };
                }
                return new List<Provider>();
            }

            // Get all staff members (individual providers) for this organization
            var staffMembers = await _providerRepository.GetStaffByOrganizationIdAsync(
                provider.Id,
                cancellationToken);

            // Filter for active and qualified staff
            var qualifiedStaff = staffMembers
                .Where(s => s.Status == ProviderStatus.Active &&
                           service.IsStaffQualified(s.Id.Value))
                .ToList();

            _logger.LogInformation(
                "Found {TotalStaff} staff members, {QualifiedCount} are qualified for service {ServiceId}",
                staffMembers.Count, qualifiedStaff.Count, service.Id);

            return qualifiedStaff;
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
