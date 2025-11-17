// ========================================
// Booksy.UserManagement.Domain/Aggregates/CustomerAggregate/Customer.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Events;
using Booksy.UserManagement.Domain.Exceptions;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Aggregates.CustomerAggregate
{
    /// <summary>
    /// Customer aggregate root - manages customer profile and preferences
    /// </summary>
    public sealed class Customer : AggregateRoot<CustomerId>, IAuditableEntity
    {
        private readonly List<CustomerFavoriteProvider> _favoriteProviders = new();

        // Core Identity
        public UserId UserId { get; private set; }
        public UserProfile Profile { get; private set; }

        // Preferences
        public NotificationPreferences NotificationPreferences { get; private set; }

        // Status
        public bool IsActive { get; private set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }

        // Collections
        public IReadOnlyList<CustomerFavoriteProvider> FavoriteProviders => _favoriteProviders.AsReadOnly();

        // Private constructor for EF Core
        private Customer() : base() { }

        /// <summary>
        /// Factory method for creating a new customer
        /// </summary>
        public static Customer Create(
            UserId userId,
            UserProfile profile)
        {
            if (userId == null)
                throw new ArgumentNullException(nameof(userId));

            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var customer = new Customer
            {
                Id = CustomerId.CreateNew(),
                UserId = userId,
                Profile = profile,
                NotificationPreferences = NotificationPreferences.Default(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            customer.RaiseDomainEvent(new CustomerRegisteredEvent(
                customer.Id,
                customer.UserId,
                customer.Profile.FirstName,
                customer.Profile.LastName,
                customer.CreatedAt));

            return customer;
        }

        /// <summary>
        /// Updates customer profile information
        /// </summary>
        public void UpdateProfile(UserProfile newProfile)
        {
            if (newProfile == null)
                throw new ArgumentNullException(nameof(newProfile));

            if (!IsActive)
                throw new InvalidOperationException("Cannot update profile of inactive customer");

            Profile = newProfile;
            LastModifiedAt = DateTime.UtcNow;

            RaiseDomainEvent(new CustomerProfileUpdatedEvent(
                Id,
                UserId,
                LastModifiedAt.Value));
        }

        /// <summary>
        /// Adds a provider to customer's favorites
        /// </summary>
        public void AddFavoriteProvider(Guid providerId, string? notes = null)
        {
            if (providerId == Guid.Empty)
                throw new ArgumentException("ProviderId cannot be empty", nameof(providerId));

            if (!IsActive)
                throw new InvalidOperationException("Cannot add favorites for inactive customer");

            // Check if already favorited
            if (_favoriteProviders.Any(fp => fp.ProviderId == providerId))
                return; // Already favorited, silently return

            var favorite = CustomerFavoriteProvider.Create(providerId, DateTime.UtcNow, notes);
            _favoriteProviders.Add(favorite);

            RaiseDomainEvent(new FavoriteProviderAddedEvent(
                Id,
                providerId,
                DateTime.UtcNow));
        }

        /// <summary>
        /// Removes a provider from customer's favorites
        /// </summary>
        public void RemoveFavoriteProvider(Guid providerId)
        {
            if (providerId == Guid.Empty)
                throw new ArgumentException("ProviderId cannot be empty", nameof(providerId));

            var favorite = _favoriteProviders.FirstOrDefault(fp => fp.ProviderId == providerId);
            if (favorite != null)
            {
                _favoriteProviders.Remove(favorite);

                RaiseDomainEvent(new FavoriteProviderRemovedEvent(
                    Id,
                    providerId,
                    DateTime.UtcNow));
            }
        }

        /// <summary>
        /// Checks if a provider is favorited by this customer
        /// </summary>
        public bool IsFavorite(Guid providerId)
        {
            return _favoriteProviders.Any(fp => fp.ProviderId == providerId);
        }

        /// <summary>
        /// Updates customer notification preferences
        /// </summary>
        public void UpdateNotificationPreferences(NotificationPreferences preferences)
        {
            if (preferences == null)
                throw new ArgumentNullException(nameof(preferences));

            if (!IsActive)
                throw new InvalidOperationException("Cannot update preferences of inactive customer");

            NotificationPreferences = preferences;
            LastModifiedAt = DateTime.UtcNow;

            RaiseDomainEvent(new NotificationPreferencesUpdatedEvent(
                Id,
                UserId,
                preferences.SmsEnabled,
                preferences.EmailEnabled,
                preferences.ReminderTiming,
                LastModifiedAt.Value));
        }

        /// <summary>
        /// Deactivates the customer account
        /// </summary>
        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            LastModifiedAt = DateTime.UtcNow;

            RaiseDomainEvent(new CustomerDeactivatedEvent(
                Id,
                UserId,
                LastModifiedAt.Value));
        }

        /// <summary>
        /// Reactivates the customer account
        /// </summary>
        public void Reactivate()
        {
            if (IsActive)
                return;

            IsActive = true;
            LastModifiedAt = DateTime.UtcNow;

            RaiseDomainEvent(new CustomerReactivatedEvent(
                Id,
                UserId,
                LastModifiedAt.Value));
        }
    }
}
