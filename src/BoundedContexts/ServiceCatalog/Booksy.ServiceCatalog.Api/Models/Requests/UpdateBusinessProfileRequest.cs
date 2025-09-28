
//===========================================
// Models/Requests/SearchProvidersRequest.cs
//===========================================


//===========================================
// Models/Requests/GetProvidersByLocationRequest.cs
//===========================================


//===========================================
// Models/Requests/UpdateBusinessProfileRequest.cs
//===========================================

namespace Booksy.ServiceCatalog.API.Models.Requests
{
    public sealed class UpdateBusinessProfileRequest
    {
        /// <summary>
        /// Business name
        /// </summary>
        [Required(ErrorMessage = "Business name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Business name must be between 2 and 200 characters")]
        public string BusinessName { get; set; } = string.Empty;

        /// <summary>
        /// Business description
        /// </summary>
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Contact information
        /// </summary>
        [Required(ErrorMessage = "Contact information is required")]
        public ContactInfoRequest ContactInfo { get; set; } = new();

        /// <summary>
        /// Business address
        /// </summary>
        [Required(ErrorMessage = "Address is required")]
        public AddressRequest Address { get; set; } = new();

        /// <summary>
        /// Business operating hours
        /// </summary>
        public Dictionary<DayOfWeek, BusinessHourRequest?> BusinessHours { get; set; } = new();

        /// <summary>
        /// Logo URL
        /// </summary>
        [Url(ErrorMessage = "Invalid logo URL format")]
        public string? LogoUrl { get; set; }

        /// <summary>
        /// Website URL
        /// </summary>
        [Url(ErrorMessage = "Invalid website URL format")]
        public string? WebsiteUrl { get; set; }

        /// <summary>
        /// Business tags/categories
        /// </summary>
        public List<string> Tags { get; set; } = new();
    }
}
