using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates
{
    /// <summary>
    /// Service aggregate root - manages service offerings
    /// </summary>
    public sealed class Service : AggregateRoot<ServiceId>, IAuditableEntity
    {
        private readonly List<ServiceOption> _options = new();
        private readonly List<PriceTier> _priceTiers = new();
        private readonly List<Guid> _qualifiedStaff = new();

        // Core Information
        public ProviderId ProviderId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public ServiceCategory Category { get; private set; }
        public ServiceType Type { get; private set; }

        // Pricing & Duration
        public Price BasePrice { get; private set; }
        public Duration Duration { get; private set; }
        public Duration? PreparationTime { get; private set; }
        public Duration? BufferTime { get; private set; }

        // Status & Settings
        public ServiceStatus Status { get; private set; }
        public bool RequiresDeposit { get; private set; }
        public decimal DepositPercentage { get; private set; }
        public bool AllowOnlineBooking { get; private set; }
        public bool AvailableAtLocation { get; private set; }
        public bool AvailableAsMobile { get; private set; }

        // Booking Rules
        public int? MaxAdvanceBookingDays { get; private set; }
        public int? MinAdvanceBookingHours { get; private set; }
        public int? MaxConcurrentBookings { get; private set; }
        public BookingPolicy? BookingPolicy { get; private set; }

        // Metadata
        public string? ImageUrl { get; private set; }


        public Dictionary<string, string> Metadata { get; private set; } = new();

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? ActivatedAt { get; private set; }

        // Collections
        public IReadOnlyList<ServiceOption> Options => _options.AsReadOnly();
        public IReadOnlyList<PriceTier> PriceTiers => _priceTiers.AsReadOnly();
        public IReadOnlyList<Guid> QualifiedStaff => _qualifiedStaff.AsReadOnly();

        public Provider Provider { get; private set; }

        // Private constructor for EF Core
        private Service() : base() { }

        // Factory method
        public static Service Create(
            ProviderId providerId,
            string name,
            string description,
            ServiceCategory category,
            ServiceType type,
            Price basePrice,
            Duration duration)
        {
            var serviceId = ServiceId.New();

            var service = new Service
            {
                Id = serviceId,
                ProviderId = providerId,
                Name = name,
                Description = description,
                Category = category,
                Type = type,
                BasePrice = basePrice,
                Duration = duration,
                Status = ServiceStatus.Draft,
                RequiresDeposit = false,
                DepositPercentage = 0,
                AllowOnlineBooking = true,
                AvailableAtLocation = true,
                AvailableAsMobile = false,
                MaxAdvanceBookingDays = 90,
                MinAdvanceBookingHours = 1,
                MaxConcurrentBookings = 1,
                CreatedAt = DateTime.UtcNow
            };

            service.RaiseDomainEvent(new ServiceCreatedEvent(
                service.Id,
                service.ProviderId,
                service.Name,
                service.Category,
                service.BasePrice,
                service.Duration,
                service.CreatedAt));

            return service;
        }

        // Business Methods
        public void UpdateBasicInfo(string name, string description, ServiceCategory category)
        {
            Name = name;
            Description = description;
            Category = category;

            RaiseDomainEvent(new ServiceUpdatedEvent(Id, name, description, DateTime.UtcNow));
        }

        public void UpdatePricing(Price newBasePrice)
        {
            BasePrice = newBasePrice;
        }

        public void UpdateDuration(Duration newDuration, Duration? preparationTime = null, Duration? bufferTime = null)
        {
            Duration = newDuration;
            PreparationTime = preparationTime;
            BufferTime = bufferTime;
        }

        public void Activate()
        {
            if (Status == ServiceStatus.Active)
                throw new InvalidServiceException("Service is already active");

            // Business rule: Organization services must have at least one qualified staff member
            // Individual hierarchy types don't need qualified staff (they perform their own services)
            if (Provider.HierarchyType != ProviderHierarchyType.Individual && !_qualifiedStaff.Any())
                throw new InvalidServiceException("Organization service must have at least one qualified staff member to be activated");

            Status = ServiceStatus.Active;
            ActivatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new ServiceActivatedEvent(Id, ProviderId, Name, ActivatedAt.Value));
        }

        public void Deactivate(string reason)
        {
            if (Status != ServiceStatus.Active)
                throw new InvalidServiceException("Only active services can be deactivated");

            Status = ServiceStatus.Inactive;

            RaiseDomainEvent(new ServiceDeactivatedEvent(Id, DateTime.UtcNow, reason));
        }

        public void Archive()
        {
            Status = ServiceStatus.Archived;
        }

        // Service Options Management
        public void AddOption(string name, Price additionalPrice, Duration? additionalDuration = null)
        {
            var option = ServiceOption.Create(name, additionalPrice, additionalDuration);
            _options.Add(option);
        }

        public void RemoveOption(Guid optionId)
        {
            var option = _options.FirstOrDefault(o => o.Id == optionId);
            if (option != null)
            {
                _options.Remove(option);
            }
        }

        // Price Tiers Management
        public void AddPriceTier(string name, Price price, string? description = null)
        {
            var tier = PriceTier.Create(name, price, description);
            _priceTiers.Add(tier);
        }

        public void RemovePriceTier(Guid tierId)
        {
            var tier = _priceTiers.FirstOrDefault(t => t.Id == tierId);
            if (tier != null)
            {
                _priceTiers.Remove(tier);
            }
        }

        // Staff Qualification Management
        public void AddQualifiedStaff(Guid staffId)
        {
            if (!_qualifiedStaff.Contains(staffId))
            {
                _qualifiedStaff.Add(staffId);
            }
        }

        public void RemoveQualifiedStaff(Guid staffId)
        {
            _qualifiedStaff.Remove(staffId);
        }

        public bool IsStaffQualified(Guid staffId)
        {
            return _qualifiedStaff.Contains(staffId);
        }

        // Deposit Management
        public void EnableDeposit(decimal percentage)
        {
            if (percentage <= 0 || percentage > 100)
                throw new InvalidServiceException("Deposit percentage must be between 0 and 100");

            RequiresDeposit = true;
            DepositPercentage = percentage;
        }

        public void DisableDeposit()
        {
            RequiresDeposit = false;
            DepositPercentage = 0;
        }

        // Availability Settings
        public void UpdateAvailability(bool atLocation, bool asMobile)
        {
            if (!atLocation && !asMobile)
                throw new InvalidServiceException("Service must be available either at location or as mobile service");

            AvailableAtLocation = atLocation;
            AvailableAsMobile = asMobile;
        }

        // Booking Rules
        public void UpdateBookingRules(int maxAdvanceDays, int minAdvanceHours, int maxConcurrent)
        {
            if (maxAdvanceDays <= 0)
                throw new InvalidServiceException("Max advance booking days must be positive");

            if (minAdvanceHours < 0)
                throw new InvalidServiceException("Min advance booking hours cannot be negative");

            if (maxConcurrent <= 0)
                throw new InvalidServiceException("Max concurrent bookings must be positive");

            MaxAdvanceBookingDays = maxAdvanceDays;
            MinAdvanceBookingHours = minAdvanceHours;
            MaxConcurrentBookings = maxConcurrent;
        }

        /// <summary>
        /// Set a comprehensive booking policy for this service
        /// </summary>
        public void SetBookingPolicy(BookingPolicy policy)
        {
            BookingPolicy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public void SetMetadata(string key, string value)
        {
            Metadata[key] = value;
        }

        public void RemoveMetadata(string key)
        {
            Metadata.Remove(key);
        }

        public void SetImage(string imageUrl)
        {
            ImageUrl = imageUrl;
        }

        // Query Methods
        public bool CanBeBooked()
        {
            // Individual hierarchy services can be booked by the provider themselves
            // Organization hierarchy services require qualified staff
            if (Provider.HierarchyType == ProviderHierarchyType.Individual)
                return Status == ServiceStatus.Active && AllowOnlineBooking;

            return Status == ServiceStatus.Active && AllowOnlineBooking && _qualifiedStaff.Any();
        }

        public Duration GetTotalDuration()
        {
            var total = Duration.Value;
            if (PreparationTime != null) total += PreparationTime.Value;
            if (BufferTime != null) total += BufferTime.Value;
            return Duration.FromMinutes(total);
        }

        public Price CalculateDepositAmount()
        {
            if (!RequiresDeposit) return Price.Zero(BasePrice.Currency);

            var depositAmount = BasePrice.Amount * (DepositPercentage / 100);
            return Price.Create(depositAmount, BasePrice.Currency);
        }
    }
}