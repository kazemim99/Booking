
//===========================================
// Models/Requests/SearchProvidersRequest.cs
//===========================================


//===========================================
// Models/Requests/GetProvidersByLocationRequest.cs
//===========================================

namespace Booksy.ServiceCatalog.API.Models.Requests
{
    public sealed class GetProvidersByLocationRequest
    {
        /// <summary>
        /// Latitude coordinate
        /// </summary>
        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90.0 and 90.0")]
        public double Latitude { get; set; }

        /// <summary>
        /// Longitude coordinate
        /// </summary>
        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180.0 and 180.0")]
        public double Longitude { get; set; }

        /// <summary>
        /// Search radius in kilometers
        /// </summary>
        [Range(0.1, 1000.0, ErrorMessage = "Radius must be between 0.1 and 1000.0 kilometers")]
        public double RadiusKm { get; set; } = 10.0;

        /// <summary>
        /// Filter by provider type
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Filter for mobile services
        /// </summary>
        public bool? OffersMobileServices { get; set; }

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
