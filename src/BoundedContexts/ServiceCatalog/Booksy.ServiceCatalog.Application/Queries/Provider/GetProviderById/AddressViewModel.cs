// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderById/AddressViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById
{
    public sealed class AddressViewModel
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}