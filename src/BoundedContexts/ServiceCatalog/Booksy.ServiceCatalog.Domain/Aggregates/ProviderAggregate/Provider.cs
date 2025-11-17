// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Provider.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using System.Collections.Immutable;

namespace Booksy.ServiceCatalog.Domain.Aggregates
{
    /// <summary>
    /// Provider aggregate root - manages service provider business operations
    /// </summary>
    public sealed class Provider : AggregateRoot<ProviderId>, IAuditableEntity
    {
        private readonly List<Staff> _staff = new();
        private readonly List<Service> _service = new();
        private readonly List<BusinessHours> _businessHours = new();
        private readonly List<HolidaySchedule> _holidays = new();
        private readonly List<ExceptionSchedule> _exceptions = new();

        // Core Identity
        public UserId OwnerId { get; private set; }
        public BusinessProfile Profile { get; private set; }

        // Status & Type
        public ProviderStatus Status { get; private set; }
        public ProviderType ProviderType { get; private set; }

        // Registration Progress Tracking
        public int RegistrationStep { get; private set; }
        public bool IsRegistrationComplete { get; private set; }

        // Business Information
        public ContactInfo ContactInfo { get; private set; }
        public BusinessAddress Address { get; private set; }

        // Settings
        public bool RequiresApproval { get; private set; }
        public bool AllowOnlineBooking { get; private set; }
        public bool OffersMobileServices { get; private set; }
        public PriceRange PriceRange { get; private set; }

        // Timestamps
        public DateTime RegisteredAt { get; private set; }
        public DateTime? ActivatedAt { get; private set; }
        public DateTime? VerifiedAt { get; private set; }
        public DateTime? LastActiveAt { get; private set; }

        // Collections
        public IReadOnlyList<Staff> Staff => _staff.AsReadOnly();
        public IReadOnlyList<Service> Services => _service.AsReadOnly();
        public IReadOnlyList<BusinessHours> BusinessHours => _businessHours.AsReadOnly();
        public IReadOnlyList<HolidaySchedule> Holidays => _holidays.AsReadOnly();
        public IReadOnlyList<ExceptionSchedule> Exceptions => _exceptions.AsReadOnly();

        // Audit Properties
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }
        public decimal AverageRating { get; internal set; }

        // Private constructor for EF Core
        private Provider() : base() { }

        // Factory method for creating draft provider (progressive registration)
        public static Provider CreateDraft(
            UserId ownerId,
            string ownerFirstName,
            string ownerLastName,
            string businessName,
            string description,
            ProviderType type,
            ContactInfo contactInfo,
            BusinessAddress address,
            int registrationStep = 3)
        {
            var profile = BusinessProfile.Create(businessName, description,"profileImageUrl");

            var provider = new Provider
            {
                Id = ProviderId.New(),
                OwnerId = ownerId,
                Profile = profile,
                Status = ProviderStatus.Drafted,
                ProviderType = type,
                ContactInfo = contactInfo,
                Address = address,
                RequiresApproval = false,
                AllowOnlineBooking = true,
                OffersMobileServices = false,
                PriceRange = PriceRange.Moderate,
                RegisteredAt = DateTime.UtcNow,
                RegistrationStep = registrationStep,
                IsRegistrationComplete = false
            };

            // Raise event for draft creation - UserManagement will update User.Profile with owner's name
            provider.RaiseDomainEvent(new ProviderDraftCreatedEvent(
                provider.Id,
                provider.OwnerId,
                ownerFirstName,
                ownerLastName,
                provider.Profile.BusinessName,
                DateTime.UtcNow));

            return provider;
        }

        // Factory method for new provider registration (legacy - full registration)
        public static Provider RegisterProvider(
            UserId ownerId,
            string businessName,
            string description,
            ProviderType type,
            ContactInfo contactInfo,
            BusinessAddress address)
        {
            var profile = BusinessProfile.Create(businessName, description, "profileImageUrl");

            var provider = new Provider
            {
                Id = ProviderId.New(),
                OwnerId = ownerId,
                Profile = profile,
                Status = ProviderStatus.PendingVerification,
                ProviderType = type,
                ContactInfo = contactInfo,
                Address = address,
                RequiresApproval = false,
                AllowOnlineBooking = true,
                OffersMobileServices = false,
                PriceRange = PriceRange.Moderate,
                RegisteredAt = DateTime.UtcNow,
                RegistrationStep = 9,
                IsRegistrationComplete = true
            };


            provider.RaiseDomainEvent(new ProviderRegisteredEvent(
                provider.Id,
                provider.OwnerId,
                provider.Profile.BusinessName,
                provider.ProviderType,
                provider.RegisteredAt));

            return provider;
        }

        // Business Methods
        public void Activate()
        {
            if (Status == ProviderStatus.Active)
                throw new InvalidProviderException("Provider is already active");

            if (Status == ProviderStatus.Suspended)
                throw new InvalidProviderException("Cannot activate suspended provider");

            Status = ProviderStatus.Active;
            ActivatedAt = DateTime.UtcNow;
            LastActiveAt = DateTime.UtcNow;

            RaiseDomainEvent(new ProviderActivatedEvent(Id, ActivatedAt.Value));
        }

        public void Deactivate(string reason)
        {
            if (Status != ProviderStatus.Active)
                throw new InvalidProviderException("Only active providers can be deactivated");

            Status = ProviderStatus.Inactive;

            RaiseDomainEvent(new ProviderDeactivatedEvent(Id, DateTime.UtcNow, reason));
        }

        public void UpdateBusinessProfile(string businessName, string description, string? profileImageUrl)
        {
            Profile = BusinessProfile.Create(businessName, description,profileImageUrl);

            RaiseDomainEvent(new BusinessProfileUpdatedEvent(Id, businessName, description, DateTime.UtcNow));
        }

        public void UpdateContactInfo(ContactInfo newContactInfo)
        {
            ContactInfo = newContactInfo;
        }

        public void UpdateAddress(BusinessAddress newAddress)
        {
            Address = newAddress;
        }

        // Registration Progress Methods
        public void UpdateRegistrationStep(int step)
        {
            if (step < 1 || step > 9)
                throw new ArgumentException("Registration step must be between 1 and 9", nameof(step));

            RegistrationStep = step;
        }

        public void CompleteRegistration()
        {
            if (IsRegistrationComplete)
                throw new InvalidProviderException("Registration is already complete");

            IsRegistrationComplete = true;
            RegistrationStep = 9;
            Status = ProviderStatus.PendingVerification;
        }

        public Staff AddStaff(string firstName, string lastName, StaffRole role, PhoneNumber? phone = null)
        {
            // Business rule: Cannot add staff if provider is not active
            if (Status == ProviderStatus.Inactive)
                throw new InvalidProviderException("Cannot add staff to inactive provider");

            // Business rule: Email must be unique among staff

            var staff = Entities.Staff.Create(firstName, lastName, role, Id, phone);
            _staff.Add(staff);

            RaiseDomainEvent(new StaffAddedEvent(Id, staff.Id, staff.FullName, role, DateTime.UtcNow));

            return staff;
        }

        public void RemoveStaff(Guid staffId, string reason)
        {
            var staff = _staff.FirstOrDefault(s => s.Id == staffId);
            if (staff == null)
                throw new InvalidProviderException("Staff member not found");

            staff.Deactivate(reason);
        }

        /// <summary>
        /// Sets business hours for all days of the week - replaces entire collection (Value Object pattern)
        /// </summary>
        public void SetBusinessHours(Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close)> hours)
        {
            // For owned collections in EF Core, we need to remove items individually
            // to avoid issues with shadow properties
            var existingHours = _businessHours.ToList();
            foreach (var hour in existingHours)
            {
                _businessHours.Remove(hour);
            }

            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                if (hours.TryGetValue(day, out var times) && times.Open.HasValue && times.Close.HasValue)
                {
                    _businessHours.Add(Entities.BusinessHours.CreateOpen(Id, day, times.Open.Value, times.Close.Value));
                }
                else
                {
                    _businessHours.Add(Entities.BusinessHours.CreateClosed(Id, day));
                }
            }

            RaiseDomainEvent(new BusinessHoursUpdatedEvent(Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Sets business hours with breaks for all days of the week
        /// </summary>
        public void SetBusinessHoursWithBreaks(
            Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close, IEnumerable<BreakPeriod>? Breaks)> hoursWithBreaks)
        {
            // Remove existing hours
            var existingHours = _businessHours.ToList();
            foreach (var hour in existingHours)
            {
                _businessHours.Remove(hour);
            }

            // Add new hours with breaks
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                if (hoursWithBreaks.TryGetValue(day, out var times) &&
                    times.Open.HasValue &&
                    times.Close.HasValue)
                {
                    if (times.Breaks?.Any() == true)
                    {
                        _businessHours.Add(Entities.BusinessHours.CreateWithBreaks(
                            Id,
                            day,
                            times.Open.Value,
                            times.Close.Value,
                            times.Breaks));
                    }
                    else
                    {
                        _businessHours.Add(Entities.BusinessHours.CreateOpen(
                            Id,
                            day,
                            times.Open.Value,
                            times.Close.Value));
                    }
                }
                else
                {
                    _businessHours.Add(Entities.BusinessHours.CreateClosed(Id, day));
                }
            }

            RaiseDomainEvent(new BusinessHoursUpdatedEvent(Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Gets business hours for a specific day
        /// </summary>
        public Entities.BusinessHours? GetBusinessHoursFor(DayOfWeek day)
        {
            return _businessHours.FirstOrDefault(h => h.DayOfWeek == day);
        }

        public bool IsOpenOn(DayOfWeek dayOfWeek)
        {
            return GetBusinessHoursFor(dayOfWeek)?.IsOpen ?? false;
        }

        // ============================================
        // Holiday Management
        // ============================================

        /// <summary>
        /// Adds a single-date holiday
        /// </summary>
        public Entities.HolidaySchedule AddHoliday(DateOnly date, string reason)
        {
            var holiday = HolidaySchedule.CreateSingle(Id, date, reason);
            _holidays.Add(holiday);

            RaiseDomainEvent(new HolidayAddedEvent(Id, holiday.Id, date, reason, DateTime.UtcNow));

            return holiday;
        }

        /// <summary>
        /// Adds a recurring holiday
        /// </summary>
        public Entities.HolidaySchedule AddRecurringHoliday(DateOnly date, string reason, RecurrencePattern pattern)
        {
            var holiday = Entities.HolidaySchedule.CreateRecurring(Id, date, reason, pattern);
            _holidays.Add(holiday);

            RaiseDomainEvent(new HolidayAddedEvent(Id, holiday.Id, date, reason, DateTime.UtcNow));

            return holiday;
        }

        /// <summary>
        /// Removes a holiday
        /// </summary>
        public void RemoveHoliday(Guid holidayId)
        {
            var holiday = _holidays.FirstOrDefault(h => h.Id == holidayId);
            if (holiday == null)
                throw new InvalidProviderException("Holiday not found");

            _holidays.Remove(holiday);

            RaiseDomainEvent(new HolidayRemovedEvent(Id, holidayId, DateTime.UtcNow));
        }

        /// <summary>
        /// Checks if a specific date is a holiday
        /// </summary>
        public bool IsHoliday(DateOnly date)
        {
            return _holidays.Any(h => h.OccursOn(date));
        }

        // ============================================
        // Exception Schedule Management
        // ============================================

        /// <summary>
        /// Adds an exception with modified hours for a specific date
        /// </summary>
        public ExceptionSchedule AddException(DateOnly date, TimeOnly openTime, TimeOnly closeTime, string reason)
        {
            // Check for conflicts with holidays
            if (IsHoliday(date))
                throw new InvalidProviderException($"Cannot add exception on holiday date {date:yyyy-MM-dd}");

            var exception = Entities.ExceptionSchedule.CreateWithModifiedHours(Id, date, openTime, closeTime, reason);
            _exceptions.Add(exception);

            RaiseDomainEvent(new ExceptionAddedEvent(Id, exception.Id, date, openTime, closeTime, reason, DateTime.UtcNow));

            return exception;
        }

        /// <summary>
        /// Adds an exception marking a date as closed
        /// </summary>
        public Entities.ExceptionSchedule AddClosedException(DateOnly date, string reason)
        {
            // Check for conflicts with holidays
            if (IsHoliday(date))
                throw new InvalidProviderException($"Cannot add exception on holiday date {date:yyyy-MM-dd}");

            var exception = Entities.ExceptionSchedule.CreateClosed(Id, date, reason);
            _exceptions.Add(exception);

            RaiseDomainEvent(new ExceptionAddedEvent(Id, exception.Id, date, null, null, reason, DateTime.UtcNow));

            return exception;
        }

        /// <summary>
        /// Removes an exception
        /// </summary>
        public void RemoveException(Guid exceptionId)
        {
            var exception = _exceptions.FirstOrDefault(e => e.Id == exceptionId);
            if (exception == null)
                throw new InvalidProviderException("Exception not found");

            _exceptions.Remove(exception);

            RaiseDomainEvent(new ExceptionRemovedEvent(Id, exceptionId, DateTime.UtcNow));
        }

        /// <summary>
        /// Gets exception for a specific date if exists
        /// </summary>
        public Entities.ExceptionSchedule? GetExceptionFor(DateOnly date)
        {
            return _exceptions.FirstOrDefault(e => e.Date == date);
        }

        // ============================================
        // Availability Calculation
        // ============================================

        /// <summary>
        /// Gets availability for a specific date considering all schedule layers
        /// Priority: Holiday > Exception > Break > Base Hours
        /// </summary>
        public (bool IsAvailable, string? Reason, IEnumerable<(TimeOnly Start, TimeOnly End)> Slots) GetAvailabilityForDate(DateOnly date)
        {
            // Check if date is a holiday (highest priority)
            var holiday = _holidays.FirstOrDefault(h => h.OccursOn(date));
            if (holiday != null)
            {
                return (false, $"Holiday: {holiday.Reason}", Enumerable.Empty<(TimeOnly, TimeOnly)>());
            }

            // Check for exception schedule
            var exception = GetExceptionFor(date);
            if (exception != null)
            {
                if (exception.IsClosed)
                {
                    return (false, $"Closed: {exception.Reason}", Enumerable.Empty<(TimeOnly, TimeOnly)>());
                }

                // Use exception hours (no breaks applied to exceptions)
                var exceptionSlot = new[] { (exception.OpenTime!.Value, exception.CloseTime!.Value) };
                return (true, null, exceptionSlot);
            }

            // Use base hours for this day of week
            // Convert System.DayOfWeek to Domain.Enums.DayOfWeek
            var dayOfWeek = (Enums.DayOfWeek)(int)date.DayOfWeek;
            var businessHours = GetBusinessHoursFor(dayOfWeek);
            if (businessHours == null || !businessHours.IsOpen)
            {
                return (false, "Closed", Enumerable.Empty<(TimeOnly, TimeOnly)>());
            }

            // Get available slots considering breaks
            var slots = businessHours.GetAvailableSlots();
            return (slots.Any(), null, slots);
        }

        public void UpdateSettings(bool requiresApproval, bool allowOnlineBooking, bool offersMobileServices)
        {
            RequiresApproval = requiresApproval;
            AllowOnlineBooking = allowOnlineBooking;
            OffersMobileServices = offersMobileServices;
        }

        public void UpdatePriceRange(PriceRange priceRange)
        {
            PriceRange = priceRange;
        }

        public void RecordActivity()
        {
            LastActiveAt = DateTime.UtcNow;
        }

        // Verification methods
        public void Verify(string verifiedBy)
        {
            if (Status != ProviderStatus.PendingVerification)
                throw new InvalidProviderException("Provider is not pending verification");

            VerifiedAt = DateTime.UtcNow;
            Status = ProviderStatus.Verified;
        }

        // Query methods
        public bool CanAcceptBookings()
        {
            return Status == ProviderStatus.Active && AllowOnlineBooking;
        }

        public bool HasActiveStaff()
        {
            return _staff.Any(s => s.IsActive);
        }

        public void SetSatus(ProviderStatus providerStatus)
        {
            Status = providerStatus;
        }

        public void SetAllowOnlineBooking(bool allow)
        {
            AllowOnlineBooking = allow;
        }

        public void UpdateStaffNotes(Guid id, string? notes)
        {
            var staff = GetStaffById(id);
            staff.UpdateNotes(notes);
        }

        public void UpdateStaffBiography(Guid id, string? biography)
        {
            var staff = GetStaffById(id);
            staff.UpdateBiography(biography);
        }

        public void UpdateStaffProfilePhoto(Guid id, string? photoUrl)
        {
            var staff = GetStaffById(id);
            staff.UpdateProfilePhoto(photoUrl);
        }

        public void UpdateStaff(Guid staffId, string firstName, string lastName, Email email, PhoneNumber? phone, StaffRole role)
        {
            var staff = GetStaffById(staffId);
            staff.UpdateContactInfo(firstName,lastName, email, phone);
            staff.UpdateRole(role);



        }

        public Staff GetStaffById(Guid staffId)
        {
            return _staff.First(s => s.Id == staffId);
        }

        public void ActivateStaff(Guid staffId)
        {
            _staff.First(s => s.Id == staffId).Reactivate();
        }

        public void DeactivateStaff(Guid staffId, string reason)
        {

            var staff = _staff.FirstOrDefault(s => s.Id == staffId) ?? 
                throw new DomainValidationException("Staff member not found");
            staff.Deactivate(reason);
        }

        public IReadOnlyList<Staff> GetActiveStaff()
        {
            return _staff.Where(s => s.IsActive).ToImmutableList();
        }
    }
}