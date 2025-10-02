// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Provider.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

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

        // Core Identity
        public UserId OwnerId { get; private set; }
        public BusinessProfile Profile { get; private set; }

        // Status & Type
        public ProviderStatus Status { get; private set; }
        public ProviderType ProviderType { get; private set; }

        // Business Information
        public ContactInfo ContactInfo { get; private set; }
        public BusinessAddress Address { get; private set; }

        // Settings
        public bool RequiresApproval { get; private set; }
        public bool AllowOnlineBooking { get; private set; }
        public bool OffersMobileServices { get; private set; }

        // Timestamps
        public DateTime RegisteredAt { get; private set; }
        public DateTime? ActivatedAt { get; private set; }
        public DateTime? VerifiedAt { get; private set; }
        public DateTime? LastActiveAt { get; private set; }

        // Collections
        public IReadOnlyList<Staff> Staff => _staff.AsReadOnly();
        public IReadOnlyList<Service> Services => _service.AsReadOnly();
        public IReadOnlyList<BusinessHours> BusinessHours => _businessHours.AsReadOnly();

        // Audit Properties
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }
        public decimal AverageRating { get; internal set; }

        // Private constructor for EF Core
        private Provider() : base() { }

        // Factory method for new provider registration
        public static Provider RegisterProvider(
            UserId ownerId,
            string businessName,
            string description,
            ProviderType type,
            ContactInfo contactInfo,
            BusinessAddress address)
        {
            var providerId = ProviderId.New();
            var profile = BusinessProfile.Create(businessName, description);

            var provider = new Provider
            {
                Id = providerId,
                OwnerId = ownerId,
                Profile = profile,
                Status = ProviderStatus.PendingVerification,
                ProviderType = type,
                ContactInfo = contactInfo,
                Address = address,
                RequiresApproval = false,
                AllowOnlineBooking = true,
                OffersMobileServices = false,
                RegisteredAt = DateTime.UtcNow
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

        public void UpdateBusinessProfile(string businessName, string description, string? website = null)
        {
            Profile = BusinessProfile.Create(businessName, description, website);

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

        public void AddStaff(string firstName, string lastName, Email email, StaffRole role, PhoneNumber? phone = null)
        {
            // Business rule: Cannot add staff if provider is not active
            if (Status != ProviderStatus.Active)
                throw new InvalidProviderException("Cannot add staff to inactive provider");

            // Business rule: Email must be unique among staff
            if (_staff.Any(s => s.Email.Value.Equals(email.Value, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidProviderException("Staff member with this email already exists");

            var staff = Entities.Staff.Create(firstName, lastName, email, role, phone);
            _staff.Add(staff);

            RaiseDomainEvent(new StaffAddedEvent(Id, staff.Id, staff.FullName, role, DateTime.UtcNow));
        }

        public void RemoveStaff(Guid staffId, string reason)
        {
            var staff = _staff.FirstOrDefault(s => s.Id == staffId);
            if (staff == null)
                throw new InvalidProviderException("Staff member not found");

            staff.Deactivate(reason);
        }

        public void SetBusinessHours(DayOfWeek dayOfWeek, TimeOnly openTime, TimeOnly closeTime)
        {
            var existingHours = _businessHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);
            if (existingHours != null)
            {
                existingHours.UpdateHours(openTime, closeTime);
            }
            else
            {
                var businessHours = Entities.BusinessHours.Create(dayOfWeek, openTime, closeTime);
                _businessHours.Add(businessHours);
            }

            RaiseDomainEvent(new BusinessHoursUpdatedEvent(Id, dayOfWeek, openTime, closeTime, DateTime.UtcNow));
        }

        public void SetClosed(DayOfWeek dayOfWeek)
        {
            var existingHours = _businessHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);
            if (existingHours != null)
            {
                existingHours.SetClosed();
            }
            else
            {
                var businessHours = Entities.BusinessHours.CreateClosed(dayOfWeek);
                _businessHours.Add(businessHours);
            }
        }

        public bool IsOpenOn(DayOfWeek dayOfWeek)
        {
            var hours = _businessHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);
            return hours?.IsOpen ?? false;
        }

        public OperatingHours? GetOperatingHours(DayOfWeek dayOfWeek)
        {
            var hours = _businessHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);
            return hours?.OperatingHours;
        }

        public void UpdateSettings(bool requiresApproval, bool allowOnlineBooking, bool offersMobileServices)
        {
            RequiresApproval = requiresApproval;
            AllowOnlineBooking = allowOnlineBooking;
            OffersMobileServices = offersMobileServices;
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
    }
}