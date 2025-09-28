
//===========================================
// Models/Requests/SearchProvidersRequest.cs
//===========================================


//===========================================
// Models/Requests/GetProvidersByLocationRequest.cs
//===========================================


//===========================================
// Models/Requests/UpdateBusinessProfileRequest.cs
//===========================================


//===========================================
// Models/Requests/Action Requests
//===========================================
namespace Booksy.ServiceCatalog.API.Models.Requests
{
    public sealed class ActivateProviderRequest
    {
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }
}
