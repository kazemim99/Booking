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
        private readonly List<Service> _services = new();
        private readonly List<BusinessHours> _businessHours = new();
        private readonly List<HolidaySchedule> _holidays = new();
        private readonly List<ExceptionSchedule> _exceptions = new();

        // Core Identity
        public UserId OwnerId { get; private set; }
        public string OwnerFirstName { get; private set; }
        public string OwnerLastName { get; private set; }
        public BusinessProfile Profile { get; private set; }

        // Status & Type
        public ProviderStatus Status { get; private set; }
        public ServiceCategory PrimaryCategory { get; private set; }

        // Hierarchy Properties
        public ProviderHierarchyType HierarchyType { get; private set; }
        public ProviderId? ParentProviderId { get; private set; }
        public bool IsIndependent { get; private set; }

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
        public IReadOnlyList<Service> Services => _services.AsReadOnly();
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
            ServiceCategory primaryCategory,
            ContactInfo contactInfo,
            BusinessAddress address,
            ProviderHierarchyType hierarchyType = ProviderHierarchyType.Organization,
            int registrationStep = 3,
            string? logoUrl = null)
        {
            var profile = BusinessProfile.Create(businessName, description, logoUrl);

            var provider = new Provider
            {
                Id = ProviderId.New(),
                OwnerId = ownerId,
                OwnerFirstName = ownerFirstName,
                OwnerLastName = ownerLastName,
                Profile = profile,
                Status = ProviderStatus.Drafted,
                PrimaryCategory = primaryCategory,
                HierarchyType = hierarchyType,
                ParentProviderId = null,
                IsIndependent = hierarchyType == ProviderHierarchyType.Individual,
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
            ServiceCategory primaryCategory,
            ContactInfo contactInfo,
            BusinessAddress address,
            ProviderHierarchyType hierarchyType = ProviderHierarchyType.Organization)
        {
            var profile = BusinessProfile.Create(businessName, description, logoUrl: null, profileImageUrl: null);

            var provider = new Provider
            {
                Id = ProviderId.New(),
                OwnerId = ownerId,
                Profile = profile,
                Status = ProviderStatus.PendingVerification,
                PrimaryCategory = primaryCategory,
                HierarchyType = hierarchyType,
                ParentProviderId = null,
                IsIndependent = hierarchyType == ProviderHierarchyType.Individual,
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
                provider.PrimaryCategory,
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
            // Preserve existing LogoUrl and ProfileImageUrl when updating profile
            var existingLogoUrl = Profile.LogoUrl;
            var existingProfileImageUrl = Profile.ProfileImageUrl;

            // Only update ProfileImageUrl if a new one is provided, otherwise keep existing
            var updatedProfileImageUrl = profileImageUrl ?? existingProfileImageUrl;

            Profile = BusinessProfile.Create(businessName, description, logoUrl: existingLogoUrl, profileImageUrl: updatedProfileImageUrl);

            RaiseDomainEvent(new BusinessProfileUpdatedEvent(Id, businessName, description, DateTime.UtcNow));
        }

        public void UpdateContactInfo(ContactInfo newContactInfo)
        {
            ContactInfo = newContactInfo;
        }

        public void UpdateAddress(BusinessAddress newAddress)
        {
            var previousAddress = Address;
            var previousCoordinates = previousAddress.Latitude.HasValue && previousAddress.Longitude.HasValue
                ? Coordinates.Create(previousAddress.Latitude.Value, previousAddress.Longitude.Value)
                : null;

            Address = newAddress;

            var newCoordinates = Coordinates.Create(newAddress.Latitude ?? 0, newAddress.Longitude ?? 0);

            RaiseDomainEvent(new ProviderLocationUpdatedEvent(
                providerId: Id,
                providerName: Profile.BusinessName,
                newAddress: newAddress,
                newCoordinates: newCoordinates,
                changeType: LocationChangeType.Relocation,
                updatedByUserId: OwnerId.Value.ToString(),
                effectiveDate: DateTime.UtcNow,
                affectedAppointmentIds: Array.Empty<string>().ToList().AsReadOnly(),
                previousAddress: previousAddress,
                previousCoordinates: previousCoordinates
            ));
        }

        /// <summary>
        /// Updates draft provider information (used when user goes back in registration flow)
        /// </summary>
        public void UpdateDraftInfo(
            string ownerFirstName,
            string ownerLastName,
            string businessName,
            string description,
            ServiceCategory primaryCategory,
            ContactInfo contactInfo,
            BusinessAddress address,
            string? logoUrl = null)
        {
            // Only allow updating draft providers
            if (Status != ProviderStatus.Drafted)
                throw new InvalidOperationException("Can only update draft providers");

            OwnerFirstName = ownerFirstName;
            OwnerLastName = ownerLastName;
            Profile = BusinessProfile.Create(businessName, description, logoUrl);
            PrimaryCategory = primaryCategory;
            ContactInfo = contactInfo;
            Address = address;

            RaiseDomainEvent(new BusinessProfileUpdatedEvent(Id, businessName, description, DateTime.UtcNow));
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

     

        public void SetSatus(ProviderStatus providerStatus)
        {
            Status = providerStatus;
        }

        public void SetAllowOnlineBooking(bool allow)
        {
            AllowOnlineBooking = allow;
        }

      

        // ============================================
        // Provider Hierarchy Methods
        // ============================================

        /// <summary>
        /// Converts an individual provider to an organization
        /// Useful when a solo professional wants to hire staff
        /// </summary>
        public void ConvertToOrganization()
        {
            if (HierarchyType == ProviderHierarchyType.Organization)
                throw new InvalidProviderException("Provider is already an organization");

            if (ParentProviderId != null)
                throw new InvalidProviderException("Cannot convert a staff member to organization. Must leave parent organization first.");

            HierarchyType = ProviderHierarchyType.Organization;
            IsIndependent = false;

            RaiseDomainEvent(new ProviderConvertedToOrganizationEvent(Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Links an individual provider to an organization as a staff member
        /// </summary>
        public void LinkToOrganization(ProviderId organizationId)
        {
            if (HierarchyType != ProviderHierarchyType.Individual)
                throw new InvalidProviderException("Only individual providers can be linked to organizations");

            if (ParentProviderId != null)
                throw new InvalidProviderException("Provider is already linked to an organization");

            // Prevent circular relationships
            if (organizationId == Id)
                throw new InvalidProviderException("Provider cannot be its own parent");

            ParentProviderId = organizationId;
            IsIndependent = false;

            RaiseDomainEvent(new StaffMemberAddedToOrganizationEvent(organizationId, Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Unlinks an individual provider from their parent organization
        /// </summary>
        public void UnlinkFromOrganization(string reason)
        {
            if (ParentProviderId == null)
                throw new InvalidProviderException("Provider is not linked to any organization");

            var parentId = ParentProviderId;
            ParentProviderId = null;
            IsIndependent = true;

            RaiseDomainEvent(new StaffMemberRemovedFromOrganizationEvent(parentId, Id, reason, DateTime.UtcNow));
        }

        /// <summary>
        /// Checks if this provider can accept direct bookings
        /// Organizations can accept bookings if they work solo or have staff
        /// Individuals can accept bookings if they're independent or linked to an org
        /// </summary>
        public bool CanAcceptDirectBookings()
        {
            if (!AllowOnlineBooking || Status != ProviderStatus.Active)
                return false;

            // Organizations can always accept bookings (handled by owner or staff)
            if (HierarchyType == ProviderHierarchyType.Organization)
                return true;

            // Independent individuals can accept bookings
            if (IsIndependent)
                return true;

            // Staff members linked to organizations can accept bookings
            return ParentProviderId != null;
        }

        /// <summary>
        /// Checks if this provider can have staff members
        /// </summary>
        public bool CanHaveStaff()
        {
            return HierarchyType == ProviderHierarchyType.Organization;
        }

        /// <summary>
        /// Validates hierarchy consistency
        /// </summary>
        public void ValidateHierarchy()
        {
            // Individuals shouldn't have ParentProviderId if they're independent
            if (HierarchyType == ProviderHierarchyType.Individual && IsIndependent && ParentProviderId != null)
                throw new InvalidProviderException("Independent individuals cannot have a parent organization");

            // Organizations should not have a parent
            if (HierarchyType == ProviderHierarchyType.Organization && ParentProviderId != null)
                throw new InvalidProviderException("Organizations cannot be linked to another organization");

            // Non-independent individuals must have a parent
            if (HierarchyType == ProviderHierarchyType.Individual && !IsIndependent && ParentProviderId == null)
                throw new InvalidProviderException("Non-independent individuals must be linked to an organization");
        }

        // ============================================
        // Gallery Management Methods
        // ============================================

        /// <summary>
        /// Uploads a gallery image and raises domain event for cache invalidation
        /// </summary>
        public GalleryImage UploadGalleryImage(string imageUrl, string thumbnailUrl, string mediumUrl)
        {
            var galleryImage = Profile.AddGalleryImage(Id, imageUrl, thumbnailUrl, mediumUrl);

            RaiseDomainEvent(new GalleryImageUploadedEvent(Id, galleryImage.Id, imageUrl, DateTime.UtcNow));

            return galleryImage;
        }

        /// <summary>
        /// Deletes a gallery image and raises domain event for cache invalidation
        /// </summary>
        public void DeleteGalleryImage(Guid imageId)
        {
            Profile.RemoveGalleryImage(imageId);

            RaiseDomainEvent(new GalleryImageDeletedEvent(Id, imageId, DateTime.UtcNow));
        }

        /// <summary>
        /// Reorders gallery images and raises domain event for cache invalidation
        /// </summary>
        public void ReorderGalleryImages(Dictionary<Guid, int> imageOrders)
        {
            Profile.ReorderGalleryImages(imageOrders);

            RaiseDomainEvent(new GalleryImagesReorderedEvent(Id, imageOrders, DateTime.UtcNow));
        }

        /// <summary>
        /// Sets a gallery image as primary and raises domain event for cache invalidation
        /// </summary>
        public void SetPrimaryGalleryImage(Guid imageId)
        {
            Profile.SetPrimaryGalleryImage(imageId);

            // Reuse GalleryImageUploadedEvent since it updates the provider
            RaiseDomainEvent(new GalleryImageUploadedEvent(Id, imageId, string.Empty, DateTime.UtcNow));
        }
    }
}