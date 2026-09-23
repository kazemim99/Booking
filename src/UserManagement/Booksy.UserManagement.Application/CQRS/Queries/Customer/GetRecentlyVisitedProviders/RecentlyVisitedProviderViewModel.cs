// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetRecentlyVisitedProviders/RecentlyVisitedProviderViewModel.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetRecentlyVisitedProviders
{
    /// <summary>
    /// A salon the customer opened recently: one row per salon, placed by its newest visit. Only
    /// Active salons are listed; the salon fields come from ServiceCatalog at read time.
    /// </summary>
    public sealed class RecentlyVisitedProviderViewModel
    {
        public Guid ProviderId { get; init; }

        /// <summary>The newest visit. Same value as <see cref="LastVisitedAt"/>; kept for older clients.</summary>
        public DateTime VisitedAt { get; init; }

        public string? ViewSource { get; init; }

        /// <summary>The salon's business name.</summary>
        public string ProviderName { get; init; } = string.Empty;

        /// <summary>The salon's photo as an absolute URL, or null when it has none.</summary>
        public string? LogoUrl { get; init; }

        public string? City { get; init; }

        /// <summary>0 when <see cref="TotalReviews"/> is 0 — read the two together.</summary>
        public decimal AverageRating { get; init; }

        /// <summary>Published reviews behind <see cref="AverageRating"/>.</summary>
        public int TotalReviews { get; init; }

        /// <summary>The newest visit to this salon.</summary>
        public DateTime LastVisitedAt { get; init; }

        /// <summary>
        /// How many stored visit entries this row stands for. The customer aggregate keeps a single
        /// entry per salon and moves it forward on a revisit — it does not count revisits — so today this is 1.
        /// </summary>
        public int VisitCount { get; init; }
    }
}
