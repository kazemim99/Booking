// ========================================
// Booksy.ServiceCatalog.Application/Services/AvailabilityService.cs
// ========================================
using Booksy.ServiceCatalog.Application.Abstractions.Identity;
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
        private readonly IOrganizationMembershipRepository _membershipRepository;
        private readonly IPersonDirectory _personDirectory;
        private readonly ILogger<AvailabilityService> _logger;

        // Configuration constants
        private const int DefaultSlotIntervalMinutes = 30;
        private const int BufferTimeMinutes = 15; // Buffer between appointments

        public AvailabilityService(
            IBookingReadRepository bookingRepository,
            IProviderReadRepository providerRepository,
            IOrganizationMembershipRepository membershipRepository,
            IPersonDirectory personDirectory,
            ILogger<AvailabilityService> logger)
        {
            _bookingRepository = bookingRepository;
            _providerRepository = providerRepository;
            _membershipRepository = membershipRepository;
            _personDirectory = personDirectory;
            _logger = logger;
        }

        public async Task<IReadOnlyList<AvailableTimeSlot>> GetAvailableTimeSlotsAsync(
            Provider provider,
            Service service,
            DateTime date,
            Guid? staffId = null,
            Duration? durationOverride = null,
            CancellationToken cancellationToken = default)
        {
            // Multi-service visits occupy their combined duration.
            var effectiveDuration = durationOverride ?? service.Duration;

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

            // Resolve the bookable resources: the organization's members, or the
            // business itself when it is a solo operation.
            var resources = await ResolveBookableResourcesAsync(provider, service, staffId, cancellationToken);
            if (resources.Count == 0)
            {
                _logger.LogWarning("No bookable resource found for service {ServiceId}", service.Id);
                return Array.Empty<AvailableTimeSlot>();
            }

            // Generate time slots
            var availableSlots = new List<AvailableTimeSlot>();

            foreach (var resource in resources)
            {
                var staffSlots = await GenerateTimeSlotsForResourceAsync(
                    date,
                    openTime,
                    closeTime,
                    effectiveDuration,
                    resource,
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

            // Legacy provider-typed API (still consumed by older queries): only the
            // sub-provider staff are Provider records. Member-based availability is
            // served by GetAvailableTimeSlotsAsync, which is resource-based.
            var qualifiedIndividuals = (await _providerRepository
                    .GetStaffByOrganizationIdAsync(provider.Id, cancellationToken))
                .Where(s => s.Status == ProviderStatus.Active)
                .ToList();
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
                errors.Add("این کسب‌وکار فعال نیست.");
            }

            // Check if provider allows online booking
            if (!provider.AllowOnlineBooking)
            {
                errors.Add("رزرو آنلاین برای این کسب‌وکار غیرفعال است.");
            }

            // Check if service is active
            if (service.Status != ServiceStatus.Active)
            {
                errors.Add("این خدمت فعال نیست؛ آن را از بخش خدمات فعال کنید.");
            }

            // Check minimum advance booking time
            var hoursUntilBooking = (startTime - DateTime.UtcNow).TotalHours;
            if (service.MinAdvanceBookingHours.HasValue && hoursUntilBooking < service.MinAdvanceBookingHours.Value)
            {
                errors.Add($"رزرو باید حداقل {service.MinAdvanceBookingHours.Value} ساعت زودتر انجام شود.");
            }

            // Check maximum advance booking time
            var daysUntilBooking = (startTime - DateTime.UtcNow).TotalDays;
            if (service.MaxAdvanceBookingDays.HasValue && daysUntilBooking > service.MaxAdvanceBookingDays.Value)
            {
                errors.Add($"رزرو بیش از {service.MaxAdvanceBookingDays.Value} روز آینده امکان‌پذیر نیست.");
            }

            // Check if booking is in the past
            if (startTime < DateTime.UtcNow)
            {
                errors.Add("امکان رزرو در گذشته وجود ندارد.");
            }

            // Check if provider is open on this day
            var dayOfWeek = (DayOfWeek)(int)startTime.DayOfWeek;
            var businessHours = provider.BusinessHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);

            if (businessHours == null || !businessHours.IsOpen)
            {
                errors.Add("در این روز هفته تعطیل هستید؛ ساعات کاری را بررسی کنید.");
            }

            // Check for holidays
            if (IsHoliday(provider, startTime.Date))
            {
                errors.Add("این تاریخ به‌عنوان تعطیلی ثبت شده است.");
            }

            // Check exceptions
            var exception = GetExceptionSchedule(provider, startTime.Date);
            if (exception != null && exception.IsClosed)
            {
                errors.Add("برای این تاریخ استثنای تعطیلی ثبت کرده‌اید.");
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
                    errors.Add($"زمان رزرو باید بین {openTime} و {closeTime} باشد.");
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
                errors.Add("این کسب‌وکار فعال نیست.");
            }

            // Check if provider allows online booking
            if (!provider.AllowOnlineBooking)
            {
                errors.Add("رزرو آنلاین برای این کسب‌وکار غیرفعال است.");
            }

            // Check if service is active
            if (service.Status != ServiceStatus.Active)
            {
                errors.Add("این خدمت فعال نیست؛ آن را از بخش خدمات فعال کنید.");
            }

            // Check maximum advance booking time (DATE-LEVEL only, not time-level)
            var daysUntilBooking = (date.Date - DateTime.UtcNow.Date).TotalDays;
            if (service.MaxAdvanceBookingDays.HasValue && daysUntilBooking > service.MaxAdvanceBookingDays.Value)
            {
                errors.Add($"رزرو بیش از {service.MaxAdvanceBookingDays.Value} روز آینده امکان‌پذیر نیست.");
            }

            // Check if date is in the past
            if (date.Date < DateTime.UtcNow.Date)
            {
                errors.Add("امکان رزرو در گذشته وجود ندارد.");
            }

            // Check if provider is open on this day of week
            var dayOfWeek = (DayOfWeek)(int)date.DayOfWeek;
            var businessHours = provider.BusinessHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);

            if (businessHours == null || !businessHours.IsOpen)
            {
                errors.Add("در این روز هفته تعطیل هستید؛ ساعات کاری را بررسی کنید.");
            }

            // Check for holidays
            if (IsHoliday(provider, date.Date))
            {
                errors.Add("این تاریخ به‌عنوان تعطیلی ثبت شده است.");
            }

            // Check exceptions
            var exception = GetExceptionSchedule(provider, date.Date);
            if (exception != null && exception.IsClosed)
            {
                errors.Add("برای این تاریخ استثنای تعطیلی ثبت کرده‌اید.");
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
        private async Task<List<AvailableTimeSlot>> GenerateTimeSlotsForResourceAsync(
            DateTime date,
            TimeOnly openTime,
            TimeOnly closeTime,
            Duration serviceDuration,
            BookableResource resource,
            CancellationToken cancellationToken)
        {
            var availableSlots = new List<AvailableTimeSlot>();

            var serviceDurationMinutes = serviceDuration.Value;

            // Get existing bookings for this resource on this date
            var dayStart = date.Date;
            var dayEnd = date.Date.AddDays(1);
            var existingBookings = await _bookingRepository.GetStaffBookingsInDateRangeAsync(
                resource.Id,
                dayStart,
                dayEnd,
                cancellationToken);

            // Filter only confirmed bookings
            var confirmedBookings = existingBookings
                .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Requested)
                .ToList();

            foreach (var minuteOfDay in EnumerateSlotStartMinutes(
                openTime, closeTime, (int)serviceDurationMinutes, DefaultSlotIntervalMinutes))
            {
                var slotStart = date.Date.AddMinutes(minuteOfDay);
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
                if (!hasConflict && slotStartUtc > DateTime.Now)
                {
                    availableSlots.Add(new AvailableTimeSlot(
                        slotStart,
                        slotEnd,
                        Duration.FromMinutes(serviceDurationMinutes),
                        resource.Id,
                        resource.Name));
                }
            }

            return availableSlots;
        }

        /// <summary>
        /// Slot start times as minutes-of-day within [openTime, closeTime].
        /// Iterates plain integers rather than marching a <see cref="TimeOnly"/>:
        /// TimeOnly.AddMinutes wraps past midnight, so with a closing time near
        /// 24:00 (e.g. a 00:00–23:59 schedule) the old while-loop condition
        /// stayed true forever and hung the request thread.
        /// </summary>
        public static IEnumerable<int> EnumerateSlotStartMinutes(
            TimeOnly openTime,
            TimeOnly closeTime,
            int serviceDurationMinutes,
            int intervalMinutes)
        {
            if (serviceDurationMinutes <= 0 || intervalMinutes <= 0)
                yield break;

            var closeMinutes = (int)closeTime.ToTimeSpan().TotalMinutes;
            for (var m = (int)openTime.ToTimeSpan().TotalMinutes;
                 m + serviceDurationMinutes <= closeMinutes;
                 m += intervalMinutes)
            {
                yield return m;
            }
        }

        /// <summary>
        /// A bookable resource: the thing a customer's booking is assigned to.
        /// Either a MEMBER of the organization (id = MembershipId) or, for a solo
        /// business with no members, the ORGANIZATION itself (id = ProviderId).
        /// Deliberately not a provider record — members are people, not businesses.
        /// </summary>
        private sealed record BookableResource(Guid Id, string Name);

        /// <summary>
        /// Resolves who can perform this service. Members of the organization are the
        /// primary source; a solo business falls back to being bookable as itself.
        /// Legacy individual sub-providers are still honoured until they are migrated.
        /// </summary>
        private async Task<List<BookableResource>> ResolveBookableResourcesAsync(
            Provider provider,
            Service service,
            Guid? staffId,
            CancellationToken cancellationToken)
        {
            var resources = new List<BookableResource>();

            // 1. Members of the organization who provide services.
            var members = await _membershipRepository.GetByOrganizationAsync(provider.Id, cancellationToken);
            var serviceProviders = members
                .Where(m => m.Status == MembershipStatus.Active && m.ProvidesServices)
                .ToList();

            if (serviceProviders.Count > 0)
            {
                var personIds = serviceProviders
                    .Where(m => m.PersonId is not null)
                    .Select(m => m.PersonId!.Value)
                    .Distinct()
                    .ToList();

                var people = personIds.Count > 0
                    ? await _personDirectory.FindByIdsAsync(personIds, cancellationToken)
                    : new Dictionary<Guid, PersonInfo>();

                foreach (var member in serviceProviders)
                {
                    // Prefer the person's real name; fall back to the salon-provided
                    // display name for an unclaimed member; then the business name.
                    var name = member.StaffProfile?.DisplayName ?? provider.Profile.BusinessName;
                    if (member.PersonId is not null && people.TryGetValue(member.PersonId.Value, out var person))
                    {
                        var full = $"{person.FirstName} {person.LastName}".Trim();
                        if (!string.IsNullOrEmpty(full))
                            name = full;
                    }

                    resources.Add(new BookableResource(member.Id, name));
                }
            }

            // 2. Legacy individual sub-providers (pre-membership staff), until migrated.
            var legacyStaff = await _providerRepository.GetStaffByOrganizationIdAsync(provider.Id, cancellationToken);
            foreach (var staff in legacyStaff.Where(s => s.Status == ProviderStatus.Active))
            {
                if (resources.Any(r => r.Id == staff.Id.Value))
                    continue;

                var name = $"{staff.OwnerFirstName} {staff.OwnerLastName}".Trim();
                if (string.IsNullOrEmpty(name))
                    name = staff.Profile.BusinessName;

                resources.Add(new BookableResource(staff.Id.Value, name));
            }

            // 3. Solo business: bookable as itself when nobody else can serve.
            if (resources.Count == 0 &&
                provider.HierarchyType == ProviderHierarchyType.Organization &&
                provider.CanAcceptDirectBookings())
            {
                _logger.LogInformation(
                    "No members for organization {ProviderId}; offering direct booking with the business",
                    provider.Id);
                resources.Add(new BookableResource(provider.Id.Value, provider.Profile.BusinessName));
            }

            // Restrict to a specific resource when the customer chose one.
            if (staffId.HasValue)
            {
                resources = resources.Where(r => r.Id == staffId.Value).ToList();
                if (resources.Count == 0)
                {
                    _logger.LogWarning(
                        "Requested resource {StaffId} is not bookable for organization {ProviderId}",
                        staffId.Value, provider.Id);
                }
            }

            _logger.LogInformation(
                "Resolved {Count} bookable resource(s) for service {ServiceId}",
                resources.Count, service.Id);

            return resources;
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
