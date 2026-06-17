// ========================================
// Booksy.UserManagement.Domain/Entities/CustomerRecentlyVisitedProvider.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.UserManagement.Domain.Entities
{
    /// <summary>
    /// Represents a provider recently visited by a customer
    /// Tracks customer browsing history for personalized recommendations
    /// </summary>
    public sealed class CustomerRecentlyVisitedProvider : Entity<Guid>
    {
        public Guid ProviderId { get; private set; }
        public DateTime VisitedAt { get; private set; }
        public string? ViewSource { get; private set; }  // e.g., "Search", "Map", "Favorites", "Direct"

        private CustomerRecentlyVisitedProvider() : base() { }

        public static CustomerRecentlyVisitedProvider Create(
            Guid providerId,
            DateTime visitedAt,
            string? viewSource = null)
        {
            if (providerId == Guid.Empty)
                throw new ArgumentException("ProviderId cannot be empty", nameof(providerId));

            return new CustomerRecentlyVisitedProvider
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                VisitedAt = visitedAt,
                ViewSource = viewSource
            };
        }

        /// <summary>
        /// Updates the visit timestamp (for when customer revisits the same provider)
        /// </summary>
        public void UpdateVisitTime(DateTime visitedAt)
        {
            VisitedAt = visitedAt;
        }

        /// <summary>
        /// Updates the source of the visit
        /// </summary>
        public void UpdateViewSource(string? viewSource)
        {
            ViewSource = viewSource;
        }
    }
}
