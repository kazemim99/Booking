//===========================================
// Supporting Request/Response Models and Extensions
//===========================================

//===========================================
// Models/Requests/SearchServicesRequest.cs
//===========================================
namespace Booksy.ServiceCatalog.API.Models.Requests
{
    public sealed class GetServicesByProviderRequest
    {
        /// <summary>
        /// Filter by service status
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter by service category
        /// </summary>
        public string? Category { get; set; }

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
