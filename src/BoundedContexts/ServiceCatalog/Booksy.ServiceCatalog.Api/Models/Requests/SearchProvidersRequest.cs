
//===========================================
// Models/Requests/SearchProvidersRequest.cs
//===========================================

namespace Booksy.ServiceCatalog.API.Models.Requests
{
    public sealed class SearchProvidersRequest
    {
        /// <summary>
        /// Search term to match against business name or description
        /// </summary>
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Search term must be between 2 and 100 characters")]
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter by provider type
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Filter by city
        /// </summary>
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string? City { get; set; }

        /// <summary>
        /// Filter by state
        /// </summary>
        [StringLength(50, ErrorMessage = "State cannot exceed 50 characters")]
        public string? State { get; set; }

        /// <summary>
        /// Filter by country
        /// </summary>
        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        public string? Country { get; set; }

        /// <summary>
        /// Filter for online booking capability
        /// </summary>
        public bool? AllowsOnlineBooking { get; set; }

        /// <summary>
        /// Filter for mobile services
        /// </summary>
        public bool? OffersMobileServices { get; set; }

        /// <summary>
        /// Filter for verified providers only
        /// </summary>
        public bool? VerifiedOnly { get; set; }

        /// <summary>
        /// Minimum average rating
        /// </summary>
        [Range(0.0, 5.0, ErrorMessage = "Rating must be between 0.0 and 5.0")]
        public decimal? MinRating { get; set; }

        /// <summary>
        /// Include inactive providers
        /// </summary>
        public bool IncludeInactive { get; set; } = false;

        /// <summary>
        /// Page number for pagination
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size for pagination
        /// </summary>
        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 20;
    }
}
