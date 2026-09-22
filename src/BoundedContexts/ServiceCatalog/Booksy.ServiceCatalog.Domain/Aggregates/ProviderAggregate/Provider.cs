// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Provider.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;
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
        public string OwnerFirstName { get; private set; } = string.Empty;
        public string OwnerLastName { get; private set; } = string.Empty;
        public BusinessProfile Profile { get; private set; }

        // Status & Type
        public ProviderStatus Status { get; private set; }
        public ServiceCategory PrimaryCategory { get; private set; }

        // Hierarchy Properties

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
        /// <summary>
        /// Average overall rating across published reviews. Meaningless on its own: an unrated provider stores
        /// 0, so read it together with <see cref="PublishedReviewCount"/> (or <see cref="HasRating"/>).
        /// </summary>
        public decimal AverageRating { get; private set; }

        /// <summary>How many published reviews <see cref="AverageRating"/> is computed over.</summary>
        public int PublishedReviewCount { get; private set; }

        /// <summary>False means "no reviews yet" — never "rated zero".</summary>
        public bool HasRating => PublishedReviewCount > 0;

        /// <summary>
        /// Overwrites the provider's rating with a freshly computed one. Both values are set together, always,
        /// because the count is the only thing that tells an unrated provider apart from a zero.
        /// </summary>
        /// <remarks>
        /// Called by the recompute after every change to the set of published reviews; never incremented.
        /// </remarks>
        public void SetRatingAggregates(decimal averageRating, int publishedReviewCount)
        {
            if (publishedReviewCount < 0)
                throw new DomainValidationException(nameof(PublishedReviewCount), "Published review count cannot be negative");

            if (publishedReviewCount == 0 && averageRating != 0m)
                throw new DomainValidationException(nameof(AverageRating), "A provider with no published reviews has no average");

            if (publishedReviewCount > 0 && (averageRating < 1.0m || averageRating > 5.0m))
                throw new DomainValidationException(nameof(AverageRating), "Average rating must be between 1.0 and 5.0");

            AverageRating = averageRating;
            PublishedReviewCount = publishedReviewCount;
        }

        // Private constructor for EF Core
        private Provider() : base() { }

        /// <summary>
        /// Every provider must carry exactly one category drawn from the ServiceCategory enum.
        /// Callers reach the aggregate through strings (registration payloads, slugs) and
        /// <c>Enum.TryParse</c> happily accepts out-of-range numbers, so the aggregate refuses
        /// undefined values rather than persisting a category nothing can render.
        /// </summary>
        private static void EnsureCategoryIsValid(ServiceCategory primaryCategory)
        {
            if (!primaryCategory.IsDefinedCategory())
                throw new InvalidProviderException(
                    $"'{(int)primaryCategory}' is not a valid service category. A provider must have exactly one category from the ServiceCategory enum.");
        }

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
            int registrationStep = 3,
            string? logoUrl = null)
        {
            EnsureCategoryIsValid(primaryCategory);

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
            string ownerFirstName = "",
            string ownerLastName = "")
        {
            EnsureCategoryIsValid(primaryCategory);

            var profile = BusinessProfile.Create(businessName, description, logoUrl: null, profileImageUrl: null);

            var provider = new Provider
            {
                Id = ProviderId.New(),
                OwnerId = ownerId,
                OwnerFirstName = ownerFirstName ?? string.Empty,
                OwnerLastName = ownerLastName ?? string.Empty,
                Profile = profile,
                Status = ProviderStatus.PendingVerification,
                PrimaryCategory = primaryCategory,
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

            EnsureCategoryIsValid(primaryCategory);

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

        /// <summary>
        /// The provider's default booking policy — including whether a deposit is required and how it is calculated.
        /// Null means "no policy configured", in which case bookings fall back to <see cref="BookingPolicy.Default"/>.
        /// A service may later override this; booking creation resolves service → provider → default.
        /// </summary>
        public BookingPolicy? BookingPolicy { get; private set; }

        /// <summary>
        /// Sets the provider's default booking policy. This is financially material — it determines whether customers
        /// must pay a deposit before a booking can be confirmed — so the change raises a domain event for audit.
        ///
        /// <para><b>Applies to future bookings only.</b> Every booking snapshots the policy that applied when it was
        /// created (<c>Bookings.Policy*</c>), so existing bookings — paid or not — keep the terms the customer agreed
        /// to. Nothing here rewrites history.</para>
        /// </summary>
        public void SetBookingPolicy(BookingPolicy policy, UserId? changedBy = null)
        {
            ArgumentNullException.ThrowIfNull(policy);

            // Capture the prior terms for the audit event before anything changes.
            var previous = BookingPolicy is null
                ? null
                : BookingPolicy.Create(
                    BookingPolicy.MinAdvanceBookingHours, BookingPolicy.MaxAdvanceBookingDays,
                    BookingPolicy.CancellationWindowHours, BookingPolicy.CancellationFeePercentage,
                    BookingPolicy.AllowRescheduling, BookingPolicy.RescheduleWindowHours,
                    BookingPolicy.RequireDeposit, BookingPolicy.DepositPercentage,
                    BookingPolicy.DepositType, BookingPolicy.DepositFixedAmount);

            if (BookingPolicy is null)
            {
                BookingPolicy = policy;
            }
            else
            {
                // Update the tracked instance rather than swapping the reference: EF tracks an owned reference by the
                // parent's key, so a replacement object is ignored and the change would be silently dropped on save.
                BookingPolicy.CopyFrom(policy);
            }

            RaiseDomainEvent(new ProviderBookingPolicyChangedEvent(
                Id,
                changedBy,
                PreviousRequireDeposit: previous?.RequireDeposit,
                PreviousDepositType: previous?.DepositType.ToString(),
                PreviousDepositPercentage: previous?.DepositPercentage,
                PreviousDepositFixedAmount: previous?.DepositFixedAmount,
                RequireDeposit: policy.RequireDeposit,
                DepositType: policy.DepositType.ToString(),
                DepositPercentage: policy.DepositPercentage,
                DepositFixedAmount: policy.DepositFixedAmount,
                ChangedAt: DateTime.UtcNow));
        }

      

        /// <summary>
        /// Whether this salon can take bookings at all. Which PERSON a booking is held
        /// against is a membership question, resolved by BookableResourceResolver; this is
        /// only about the salon being open for business.
        /// </summary>
        /// <remarks>
        /// Replaces a rule that branched on hierarchy: organizations could always accept,
        /// independent individuals could accept, and a "staff" provider could accept if it
        /// had a parent. There is one kind of provider now, so the rule is just its status.
        /// </remarks>
        public bool CanAcceptDirectBookings()
        {
            return AllowOnlineBooking && Status == ProviderStatus.Active;
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

        /// <summary>
        /// Updates a gallery image's caption/alt text and raises a domain event for cache invalidation.
        /// </summary>
        /// <remarks>
        /// Exists so metadata edits go through the aggregate root like every other gallery mutation. The
        /// read path is decorated by <c>CachedProviderReadRepository</c> and invalidation is driven by
        /// these events, so editing <c>Profile</c> directly persists the change but leaves the cache
        /// serving the old caption. Returns false for a no-op edit, in which case no event is raised and
        /// there is nothing to persist.
        /// </remarks>
        public bool UpdateGalleryImageMetadata(Guid imageId, string? caption, string? altText)
        {
            if (!Profile.UpdateGalleryImageMetadata(imageId, caption, altText))
            {
                return false;
            }

            // Reuse GalleryImageUploadedEvent: it carries provider + image id, which is all the cache
            // invalidation needs, and matches how SetPrimaryGalleryImage signals the same thing.
            RaiseDomainEvent(new GalleryImageUploadedEvent(Id, imageId, string.Empty, DateTime.UtcNow));
            return true;
        }
    }
}