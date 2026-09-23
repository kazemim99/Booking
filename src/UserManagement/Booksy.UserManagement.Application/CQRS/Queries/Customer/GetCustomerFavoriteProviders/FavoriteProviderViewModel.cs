// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetCustomerFavoriteProviders/FavoriteProviderViewModel.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerFavoriteProviders
{
    /// <summary>
    /// One of the customer's favourite salons. Only Active salons are listed; the salon fields come
    /// from ServiceCatalog at read time.
    /// </summary>
    public sealed class FavoriteProviderViewModel
    {
        public Guid ProviderId { get; init; }
        public string? Notes { get; init; }
        public DateTime AddedAt { get; init; }

        /// <summary>The salon's business name.</summary>
        public string ProviderName { get; init; } = string.Empty;

        /// <summary>The salon's photo as an absolute URL, or null when it has none.</summary>
        public string? LogoUrl { get; init; }

        public string? City { get; init; }

        /// <summary>0 when <see cref="TotalReviews"/> is 0 — read the two together.</summary>
        public decimal AverageRating { get; init; }

        /// <summary>Published reviews behind <see cref="AverageRating"/>.</summary>
        public int TotalReviews { get; init; }
    }
}
