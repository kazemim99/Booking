// ========================================
// Booksy.UserManagement.Domain/Entities/CustomerFavoriteProvider.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.UserManagement.Domain.Entities
{
    /// <summary>
    /// Represents a provider favorited by a customer
    /// </summary>
    public sealed class CustomerFavoriteProvider : Entity<Guid>
    {
        public Guid ProviderId { get; private set; }
        public DateTime AddedAt { get; private set; }
        public string? Notes { get; private set; }

        private CustomerFavoriteProvider() : base() { }

        public static CustomerFavoriteProvider Create(Guid providerId, DateTime addedAt, string? notes = null)
        {
            if (providerId == Guid.Empty)
                throw new ArgumentException("ProviderId cannot be empty", nameof(providerId));

            return new CustomerFavoriteProvider
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                AddedAt = addedAt,
                Notes = notes
            };
        }

        public void UpdateNotes(string? notes)
        {
            Notes = notes;
        }
    }
}
