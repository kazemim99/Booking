//===========================================
// Supporting Request/Response Models and Extensions
//===========================================

//===========================================
// Models/Requests/SearchServicesRequest.cs
//===========================================
namespace Booksy.ServiceCatalog.API.Models.Requests
{
    public sealed class SearchServicesRequest
    {
        /// <summary>
        /// Search term to match against service name, description, or tags
        /// </summary>
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Search term must be between 2 and 100 characters")]
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter by service category
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Filter by service type
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Minimum price filter
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Minimum price must be greater than or equal to 0")]
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// Maximum price filter
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Maximum price must be greater than or equal to 0")]
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Maximum duration in minutes
        /// </summary>
        [Range(1, 1440, ErrorMessage = "Maximum duration must be between 1 and 1440 minutes")]
        public int? MaxDurationMinutes { get; set; }

        /// <summary>
        /// Filter for mobile availability
        /// </summary>
        public bool? AvailableAsMobile { get; set; }

        /// <summary>
        /// Filter by provider city
        /// </summary>
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string? City { get; set; }

        /// <summary>
        /// Filter by provider state
        /// </summary>
        [StringLength(50, ErrorMessage = "State cannot exceed 50 characters")]
        public string? State { get; set; }

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

